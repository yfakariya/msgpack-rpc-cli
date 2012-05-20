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
	/// <summary>
	///		Represents context information for the reponse message.
	/// </summary>
	public sealed class ServerResponseContext : OutboundMessageContext
	{
		/// <summary>
		///		Constant part of the response header.
		/// </summary>
		private static readonly ArraySegment<byte> _responseHeader =
			new ArraySegment<byte>( new byte[] { 0x94, 0x01 } ); // [FixArray4], [Response:1]

		private Packer _returnDataPacker;

		/// <summary>
		///		Gets the <see cref="Packer"/> to pack return value.
		/// </summary>
		/// <value>
		///		The <see cref="Packer"/> to pack return value.
		///		This value will not be <c>null</c>.
		/// </value>
		public Packer ReturnDataPacker
		{
			get
			{
				Contract.Ensures( Contract.Result<Packer>() != null );

				return this._returnDataPacker;
			}
		}

		private Packer _errorDataPacker;


		/// <summary>
		///		Gets the <see cref="Packer"/> to pack <see cref="RpcError"/>.
		/// </summary>
		/// <value>
		///		The <see cref="Packer"/> to pack <see cref="RpcError"/>.
		///		This value will not be <c>null</c>.
		/// </value>
		public Packer ErrorDataPacker
		{
			get
			{
				Contract.Ensures( Contract.Result<Packer>() != null );

				return this._errorDataPacker;
			}
		}

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

#if MONO
		private readonly MemoryStream _unifiedSendingBuffer;
#endif

		/// <summary>
		///		Initializes a new instance of the <see cref="ServerResponseContext"/> class with default settings.
		/// </summary>
		public ServerResponseContext()
			: this( null )
		{
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="ServerResponseContext"/> class with specified configuration.
		/// </summary>
		/// <param name="configuration">
		///		An <see cref="RpcServerConfiguration"/> to tweak this instance initial state.
		/// </param>
		public ServerResponseContext( RpcServerConfiguration configuration )
		{
			this._errorDataBuffer = 
				new MemoryStream( ( configuration ?? RpcServerConfiguration.Default).InitialErrorBufferLength );
			this._returnDataBuffer =
				new MemoryStream( ( configuration ?? RpcServerConfiguration.Default).InitialReturnValueBufferLength);
			this.SendingBuffer = new ArraySegment<byte>[ 4 ];
			this.SendingBuffer[ 0 ] = _responseHeader;
#if MONO
			this._unifiedSendingBuffer = new MemoryStream( ( configuration ?? RpcServerConfiguration.Default ).InitialReceiveBufferLength );
			this._unifiedSendingBuffer.Write( this.SendingBuffer[ 0 ].Array, this.SendingBuffer[ 0 ].Offset, this.SendingBuffer[ 0 ].Count );
#endif
			this._returnDataPacker = Packer.Create( this._returnDataBuffer, false );
			this._errorDataPacker = Packer.Create( this._errorDataBuffer, false );
		}

		/// <summary>
		///		Serializes the specified response data.
		/// </summary>
		/// <typeparam name="T">The type of return value.</typeparam>
		/// <param name="returnValue">The return value.</param>
		/// <param name="error">The error.</param>
		/// <param name="returnValueSerializer">The serializer for the return value.</param>
		internal void Serialize<T>( T returnValue, RpcErrorMessage error, MessagePackSerializer<T> returnValueSerializer )
		{
			ServerTransport.Serialize( this, returnValue, error, returnValueSerializer );
		}

		/// <summary>
		///		Prepares this instance to send response.
		/// </summary>
		internal void Prepare( bool canUseChunkedBuffer )
		{
			Contract.Assert( this.SendingBuffer[ 0 ].Array != null );
			this.SendingBuffer[ 1 ] = this.GetPackedMessageId();
			this.SendingBuffer[ 2 ] = new ArraySegment<byte>( this._errorDataBuffer.GetBuffer(), 0, unchecked( ( int )this._errorDataBuffer.Length ) );
			this.SendingBuffer[ 3 ] = new ArraySegment<byte>( this._returnDataBuffer.GetBuffer(), 0, unchecked( ( int )this._returnDataBuffer.Length ) );
#if MONO
			if ( !canUseChunkedBuffer )
			{
				this._unifiedSendingBuffer.Position = this.SendingBuffer[ 0 ].Count;
				this._unifiedSendingBuffer.SetLength( this.SendingBuffer[ 0 ].Count );
				this._unifiedSendingBuffer.Write( this.SendingBuffer[ 1 ].Array, this.SendingBuffer[ 1 ].Offset, this.SendingBuffer[ 1 ].Count );
				this._unifiedSendingBuffer.Write( this.SendingBuffer[ 2 ].Array, this.SendingBuffer[ 2 ].Offset, this.SendingBuffer[ 2 ].Count );
				this._unifiedSendingBuffer.Write( this.SendingBuffer[ 3 ].Array, this.SendingBuffer[ 3 ].Offset, this.SendingBuffer[ 3 ].Count );
				this.SocketContext.SetBuffer( this._unifiedSendingBuffer.GetBuffer(), 0, unchecked( ( int )this._unifiedSendingBuffer.Length ) );
				this.SocketContext.BufferList = null;
				return;
			}
#endif
			this.SocketContext.SetBuffer( null, 0, 0 );
			this.SocketContext.BufferList = this.SendingBuffer;
		}

		/// <summary>
		///		Clears this instance internal buffers for reuse.
		/// </summary>
		internal sealed override void Clear()
		{
			this.ClearBuffers();
			this._errorDataBuffer.SetLength( 0 );
			this._returnDataBuffer.SetLength( 0 );
			this.SocketContext.BufferList = null;
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
