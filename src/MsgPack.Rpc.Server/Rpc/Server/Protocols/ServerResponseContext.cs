#region -- License Terms --
//
// MessagePack for CLI
//
// Copyright (C) 2010 FUJIWARA, Yusuke
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//
#endregion -- License Terms --

using System;
using System.Diagnostics.Contracts;
using System.IO;
using MsgPack.Rpc.Protocols;
using MsgPack.Serialization;

namespace MsgPack.Rpc.Server.Protocols
{
	public sealed class ServerResponseContext : MessageContext
	{
		/// <summary>
		///		Constant part of the response header.
		/// </summary>
		private static readonly ArraySegment<byte> _responseHeader =
			new ArraySegment<byte>( new byte[] { 0x94, 0x01 } ); // [FixArray4], [Response:1]

		private Packer _returnDataPacker;

		public Packer ReturnDataPacker
		{
			get { return this._returnDataPacker; }
		}

		private Packer _errorDataPacker;

		public Packer ErrorDataPacker
		{
			get { return this._errorDataPacker; }
		}

		/// <summary>
		///		The reusable buffer to pack <see cref="Id"/>.
		///		This value will not be <c>null</c>.
		/// </summary>
		private readonly MemoryStream _idBuffer;

		/// <summary>
		///		The reusable buffer to pack error ID.
		///		This value will not be <c>null</c>.
		/// </summary>
		private readonly MemoryStream _errorDataBuffer;

		/// <summary>
		///		The reusable buffer to pack return value or error detail.
		///		This value will not be <c>null</c>.
		/// </summary>
		private readonly MemoryStream _returnDataBuffer;

		/// <summary>
		///		The resusable buffer to hold sending response data.
		/// </summary>
		/// <remarks>
		///		Each segment corresponds to the message segment.
		///		<list type="table">
		///			<listheader>
		///				<term>Index</term>
		///				<description>Content</description>
		///			</listheader>
		///			<item>
		///				<term>0</term>
		///				<description>
		///					Common response header, namely array header and message type.
		///					Do not change this element.
		///				</description>
		///			</item>
		///			<item>
		///				<term>1</term>
		///				<description>
		///					Message ID to correpond the response to the request.
		///				</description>
		///			</item>
		///			<item>
		///				<term>2</term>
		///				<description>
		///					Error identifier.
		///				</description>
		///			</item>
		///			<item>
		///				<term>3</term>
		///				<description>
		///					Return value.
		///				</description>
		///			</item>
		///		</list>
		/// </remarks>
		internal readonly ArraySegment<byte>[] SendingBuffer;

		public ServerResponseContext()
		{
			this._idBuffer = new MemoryStream( 5 );
			// TODO: Configurable
			this._errorDataBuffer = new MemoryStream( 128 );
			// TODO: Configurable
			this._returnDataBuffer = new MemoryStream( 65536 );
			this.SendingBuffer = new ArraySegment<byte>[ 4 ];
			this.SendingBuffer[ 0 ] = _responseHeader;
			this._returnDataPacker = Packer.Create( this._returnDataBuffer, false );
			this._errorDataPacker = Packer.Create( this._errorDataBuffer, false );
		}

		internal void Serialize<T>( T returnValue, RpcErrorMessage error, MessagePackSerializer<T> returnValueSerializer )
		{
			// FIXME: Overwrite for error/timeout
			if ( MsgPackRpcServerProtocolsTrace.ShouldTrace( MsgPackRpcServerProtocolsTrace.SerializeResponse ) )
			{
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.SerializeResponse,
					"Serialize response. {{ \"Error\" : {0}, \"ReturnValue\" : \"{1}\" }}",
					error,
					returnValue
				);
			}

			if ( error.IsSuccess )
			{
				this.ErrorDataPacker.PackNull();

				if ( returnValueSerializer == null )
				{
					// void
					this.ReturnDataPacker.PackNull();
				}
				else
				{
					returnValueSerializer.PackTo( this.ReturnDataPacker, returnValue );
				}
			}
			else
			{
				this.ErrorDataPacker.Pack( error.Error.Identifier );
				this.ReturnDataPacker.Pack( error.Detail );
			}
		}

		internal void Prepare()
		{
			Contract.Assert( this.SendingBuffer[ 0 ].Array != null );
			// FIXME: Should be copied from request context.
			using ( var packer = Packer.Create( this._idBuffer, false ) )
			{
				packer.Pack( this.MessageId );
			}

			this.SendingBuffer[ 1 ] = new ArraySegment<byte>( this._idBuffer.GetBuffer(), 0, unchecked( ( int )this._idBuffer.Length ) );
			this.SendingBuffer[ 2 ] = new ArraySegment<byte>( this._errorDataBuffer.GetBuffer(), 0, unchecked( ( int )this._errorDataBuffer.Length ) );
			this.SendingBuffer[ 3 ] = new ArraySegment<byte>( this._returnDataBuffer.GetBuffer(), 0, unchecked( ( int )this._returnDataBuffer.Length ) );
			this.SetBuffer( null, 0, 0 );
			this.BufferList = this.SendingBuffer;
		}

		internal sealed override void Clear()
		{
			this._idBuffer.SetLength( 0 );
			this._errorDataBuffer.SetLength( 0 );
			this._returnDataBuffer.SetLength( 0 );
			this.BufferList = null;
			this.SendingBuffer[ 1 ] = default( ArraySegment<byte> );
			this.SendingBuffer[ 2 ] = default( ArraySegment<byte> );
			this.SendingBuffer[ 3 ] = default( ArraySegment<byte> );
 			this._returnDataPacker.Dispose();
			this._returnDataPacker = Packer.Create( this._returnDataBuffer, false );
			this._errorDataPacker.Dispose();
			this._errorDataPacker = Packer.Create( this._errorDataBuffer, false );
			base.Clear();
		}

		/// <summary>
		///		Gets the copy of the current error data.
		/// </summary>
		/// <returns>
		///		The copy of the current error data.
		/// </returns>
		public byte[] GetErrorData()
		{
			Contract.Ensures( Contract.Result<byte[]>() != null );

			return this._errorDataBuffer.ToArray();
		}

		/// <summary>
		///		Gets the copy of the current return value data.
		/// </summary>
		/// <returns>
		///		The copy of the current return value data.
		/// </returns>
		public byte[] GetReturnValueData()
		{
			Contract.Ensures( Contract.Result<byte[]>() != null );

			return this._returnDataBuffer.ToArray();
		}
	}
}
