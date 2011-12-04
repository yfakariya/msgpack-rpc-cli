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
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using MsgPack.Serialization;
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc.Server.Protocols
{
	/// <summary>
	///		Represents context information of asynchronous server operation.
	/// </summary>
	public sealed class ServerSocketAsyncEventArgs : SocketAsyncEventArgs
	{
		// TODO: ?
		private readonly WeakReference _server;

		/// <summary>
		///		Gets the server reference to obtain global settings.
		/// </summary>
		/// <value>
		///		The server reference to obtain global settings.
		///		This value can be <c>null</c>.
		/// </value>
		internal RpcServer Server
		{
			get
			{
				if ( this._server.IsAlive )
				{
					try
					{
						return this._server.Target as RpcServer;
					}
					catch ( InvalidOperationException ) { }
				}

				return null;
			}
		}

		/// <summary>
		///		Gets the serialization context to obtain serializer.
		/// </summary>
		/// <value>
		///		The serialization context to obtain serializer.
		///		This value will be <c>null</c> when <see cref="Server"/> is <c>null</c>.
		/// </value>
		internal SerializationContext SerializationContext
		{
			get
			{
				var server = this.Server;
				return server == null ? null : server.SerializationContext;
			}
		}

		/// <summary>
		///		Gets or sets the listening socket.
		/// </summary>
		/// <value>
		///		The listening socket. This value can be <c>null</c>.
		/// </value>
		public Socket ListeningSocket { get; set; }

		private readonly ArraySegment<byte>[] _sendingBuffer;

		/// <summary>
		///		Gets the sending buffer.
		/// </summary>
		/// <value>
		///		The sending buffer.
		/// </value>
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
		internal ArraySegment<byte>[] SendingBuffer
		{
			get { return this._sendingBuffer; }
		}
		
		/// <summary>
		///		Gets or sets the message id.
		/// </summary>
		/// <value>
		///		The message id. 
		///		This value will be undefined for the notification message.
		/// </value>
		internal uint Id
		{
			get;
			set;
		}
		
		private readonly MemoryStream _idBuffer;

		/// <summary>
		///		Gets the reusable buffer to pack <see cref="Id"/>.
		/// </summary>
		/// <value>
		///		The reusable buffer to pack <see cref="Id"/>.
		///		This value will not be <c>null</c>.
		/// </value>
		internal MemoryStream IdBuffer
		{
			get { return this._idBuffer; }
		}

		private readonly MemoryStream _errorDataBuffer;

		/// <summary>
		///		Gets the reusable buffer to pack error ID.
		/// </summary>
		/// <value>
		///		The reusable buffer to pack error ID.
		///		This value will not be <c>null</c>.
		/// </value>
		internal MemoryStream ErrorDataBuffer
		{
			get { return this._errorDataBuffer; }
		}

		private readonly MemoryStream _returnDataBuffer;

		/// <summary>
		///		Gets the reusable buffer to pack return value or error detail.
		/// </summary>
		/// <value>
		///		The reusable buffer to pack return value or error detail.
		///		This value will not be <c>null</c>.
		/// </value>
		internal MemoryStream ReturnDataBuffer
		{
			get { return this._returnDataBuffer; }
		}

		private byte[] _receivingBuffer;

		/// <summary>
		///		Gets the buffer to receive data.
		/// </summary>
		/// <value>
		///		The buffer to receive data.
		///		This value will not be <c>null</c>.
		///		Available section is started with _receivingBufferOffset.
		/// </value>
		internal byte[] ReceivingBuffer
		{
			get { return this._receivingBuffer; }
		}

		private int _receivingBufferOffset;

		/// <summary>
		///		Sets the receiving buffer offset as shifted by specified value.
		/// </summary>
		/// <param name="shift">The shifting value.s</param>
		internal void SetReceivingBufferOffset( int shift )
		{
			this._receivingBufferOffset += shift;
			if ( this._receivingBufferOffset == this._receivingBuffer.Length )
			{
				this._receivingBuffer = new byte[ this._receivingBuffer.Length ];
				this._receivingBufferOffset = 0;
			}

			this.SetBuffer( this._receivingBuffer, this._receivingBufferOffset, this._receivingBuffer.Length - this._receivingBufferOffset );
		}

		private readonly List<ArraySegment<byte>> _receivedData;

		/// <summary>
		///		Gets the received data.
		/// </summary>
		/// <value>
		///		The received data.
		///		This value wlll not be <c>null</c>.
		/// </value>
		internal List<ArraySegment<byte>> ReceivedData
		{
			get { return this._receivedData; }
		}

		private static readonly ArraySegment<byte> _responseHeader =
			new ArraySegment<byte>( new byte[] { 0x94, 0x01 } ); // [FixArray4], [Response:1]

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerSocketAsyncEventArgs"/> class.
		/// </summary>
		/// <param name="server">The server.</param>
		public ServerSocketAsyncEventArgs( RpcServer server )
		{
			this._server = new WeakReference( server );
			this._idBuffer = new MemoryStream( 5 );
			// TODO: Configurable
			this._receivingBuffer = new byte[ 65536 ];
			this._receivedData = new List<ArraySegment<byte>>( 1 );
			// TODO: Configurable
			this._errorDataBuffer = new MemoryStream( 128 );
			// TODO: Configurable
			this._returnDataBuffer = new MemoryStream( 65536 );
			this._sendingBuffer = new ArraySegment<byte>[ 4 ];
			this._sendingBuffer[ 0 ] = _responseHeader;
		}
	}
}
