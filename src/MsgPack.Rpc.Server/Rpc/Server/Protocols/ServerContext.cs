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
using System.Diagnostics.Contracts;
using System.Net.Sockets;
using System.Threading;

namespace MsgPack.Rpc.Server.Protocols
{
	/// <summary>
	///		Represents context information of asynchronous server operation.
	/// </summary>
	public abstract class ServerContext : SocketAsyncEventArgs, ILeaseable<ServerContext>
	{
		private static long _lastSessionId;

		private long _sessionId;
		public long SessionId
		{
			get { return this._sessionId; }
			internal set { this._sessionId = value; }
		}

		/// <summary>
		///		Gets or sets the listening socket.
		/// </summary>
		/// <value>
		///		The listening socket. This value can be <c>null</c>.
		/// </value>
		public Socket ListeningSocket { get; set; }

		/// <summary>
		///		Gets or sets the message id.
		/// </summary>
		/// <value>
		///		The message id. 
		///		This value will be undefined for the notification message.
		/// </value>
		internal int MessageId
		{
			get;
			set;
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

		[Obsolete( "Use more stable way" )]
		public bool IsClientShutdowned
		{
			get;
			internal set;
		}

		private ILease<ServerContext> _asLease;

		void ILeaseable<ServerContext>.SetLease( ILease<ServerContext> lease )
		{
			this.SetLease( lease );
		}

		protected void SetLease( ILease<ServerContext> lease )
		{
			this._asLease = lease;
		}

		private ServerTransport _boundTransport;

		internal ServerTransport BoundTransport
		{
			get { return this._boundTransport; }
		}

		private EventHandler<SocketAsyncEventArgs> _boundEventHandler;

		internal virtual void SetTransport( ServerTransport transport )
		{
			this.AcceptSocket = transport.BoundSocket;
			EventHandler<SocketAsyncEventArgs> newHandler = transport.OnSocketOperationCompleted;
			var oldHandler = Interlocked.Exchange( ref this._boundEventHandler, newHandler );
			Contract.Assert( oldHandler == null, "Bounded: " + oldHandler );
			if ( oldHandler != null )
			{
				this.Completed -= oldHandler;
			}

			this.Completed += this._boundEventHandler;
			this._boundTransport = transport;
		}

		protected ServerContext()
		{
			// TODO: Configurable
			this._receivingBuffer = new byte[ 65536 ];
			// TODO: ArrayDeque is preferred.
			this._receivedData = new List<ArraySegment<byte>>( 1 );
		}

		public void RenewSessionId()
		{
			this._sessionId = Interlocked.Increment( ref _lastSessionId );
		}

		internal void ReturnLease()
		{
			try { }
			finally
			{
				var handler = Interlocked.Exchange( ref this._boundEventHandler, null );
				if ( handler != null )
				{
					this.Completed -= handler;
					this._boundTransport = null;
				}

				var asLease = Interlocked.Exchange( ref this._asLease, null );
				if ( asLease != null )
				{
					asLease.Dispose();
				}
			}
		}

		internal virtual void Clear()
		{
			this.ReturnLease();
		}
	}
}
