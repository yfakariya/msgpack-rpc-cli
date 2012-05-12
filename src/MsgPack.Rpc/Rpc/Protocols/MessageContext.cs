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
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Represents context information of asynchronous MesagePack-RPC operation.
	/// </summary>
	public abstract class MessageContext : IDisposable
	{
		#region -- Async Socket Context --

		private readonly SocketAsyncEventArgs _socketContext;

		/// <summary>
		///		Gets the socket context for asynchronous socket.
		/// </summary>
		/// <value>
		///		The <see cref="SocketAsyncEventArgs"/> for asynchronous socket.
		/// </value>
		public SocketAsyncEventArgs SocketContext
		{
			get { return this._socketContext; }
		}

		#endregion

		#region -- Session Management --

		private static long _lastSessionId;

		private long _sessionId;

		/// <summary>
		///		Gets the ID of the session.
		/// </summary>
		/// <value>
		///		The ID of the session.
		/// </value>
		/// <remarks>
		///		DO NOT use this information for the security related feature.
		///		This information is intented for the tracking of session processing in debugging.
		/// </remarks>
		public long SessionId
		{
			get { return this._sessionId; }
			internal set { this._sessionId = value; }
		}

		private DateTimeOffset _sessionStartedAt;

		/// <summary>
		///		Gets the session start time.
		/// </summary>
		/// <value>
		///		The session start time.
		/// </value>
		public DateTimeOffset SessionStartedAt
		{
			get { return this._sessionStartedAt; }
		}

		/// <summary>
		///		Gets or sets the message id.
		/// </summary>
		/// <value>
		///		The message id. 
		///		This value will be undefined for the notification message.
		/// </value>
		public int? MessageId
		{
			get;
			internal set;
		}

		#endregion

		#region -- CompletedSynchronously --

		private bool _completedSynchronously;

		/// <summary>
		///		Gets a value indicating whether the operation has been completed synchronously.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if the operation has been completed synchronously; otherwise, <c>false</c>.
		/// </value>
		public bool CompletedSynchronously
		{
			get { return this._completedSynchronously; }
		}

		/// <summary>
		///		Sets the operation has been completed synchronously.
		/// </summary>
		public void SetCompletedSynchronously()
		{
			Contract.Ensures( this.CompletedSynchronously == true );

			this._completedSynchronously = true;
		}

		#endregion

		#region -- Transport --

		private IContextBoundableTransport _boundTransport;

		/// <summary>
		///		Gets the bound <see cref="IContextBoundableTransport"/>.
		/// </summary>
		internal IContextBoundableTransport BoundTransport
		{
			get { return this._boundTransport; }
		}

		/// <summary>
		///		Sets the bound <see cref="IContextBoundableTransport"/>.
		/// </summary>
		/// <param name="transport">The <see cref="IContextBoundableTransport"/>.</param>
		internal virtual void SetTransport( IContextBoundableTransport transport )
		{
			Contract.Requires( transport != null );
			Contract.Requires( this.BoundTransport == null );
			Contract.Ensures( this.BoundTransport != null );

			var oldBoundTransport = Interlocked.CompareExchange( ref this._boundTransport, transport, null );
			if ( oldBoundTransport != null )
			{
#if !SILVERLIGHT
				throw new InvalidOperationException( String.Format( CultureInfo.CurrentCulture, "This context is already bounded to '{0}'(Socket: 0x{1:X}).", transport.GetType(), transport.BoundSocket == null ? IntPtr.Zero : transport.BoundSocket.Handle ) );
#else
				throw new InvalidOperationException( String.Format( CultureInfo.CurrentCulture, "This context is already bounded to '{0}'(Socket: 0x{1:X}).", transport.GetType(), transport.BoundSocket == null ? 0 : transport.BoundSocket.GetHashCode() ) );
#endif
			}

			this.SocketContext.Completed += transport.OnSocketOperationCompleted;
		}

		private readonly TimeoutWatcher _timeoutWatcher;
		private int _isTimeout;

		/// <summary>
		///		Gets a value indicating whether the watched operation is timed out.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if the watched operation is timed out; otherwise, <c>false</c>.
		/// </value>
		internal bool IsTimeout
		{
			get { return Interlocked.CompareExchange( ref this._isTimeout, 0, 0 ) != 0; }
		}

		/// <summary>
		///		Occurs when the watched operation is timed out.
		/// </summary>
		internal event EventHandler Timeout;

		private void OnTimeout()
		{
			Interlocked.Exchange( ref this._isTimeout, 1 );

			var handler = this.Timeout;
			if ( handler != null )
			{
				handler( this, EventArgs.Empty );
			}
		}

		#endregion

		#region -- Communication --

		private int? _bytesTransferred;

		/// <summary>
		///		Gets the bytes count of transferred data.
		/// </summary>
		/// <returns>The bytes count of transferred data..</returns>
		public int BytesTransferred
		{
			get { return this._bytesTransferred ?? this.SocketContext.BytesTransferred; }
		}

		/// <summary>
		///		Gets or sets the remote end point.
		/// </summary>
		/// <value>
		///		The remote end point.
		/// </value>
		public EndPoint RemoteEndPoint
		{
			get { return this._socketContext.RemoteEndPoint; }
			set { this._socketContext.RemoteEndPoint = value; }
		}

		/// <summary>
		///		Gets the last asynchronous operation.
		/// </summary>
		/// <value>
		///		The <see cref="SocketAsyncOperation"/> which represents the last asynchronous operation.
		/// </value>
		public SocketAsyncOperation LastOperation
		{
			get { return this._socketContext.LastOperation; }
		}

		/// <summary>
		///		Gets or sets the asynchronous socket operation result.
		/// </summary>
		/// <value>
		///		The <see cref="SocketError"/> which represents the asynchronous socket operation result.
		/// </value>
		public SocketError SocketError
		{
			get { return this._socketContext.SocketError; }
			set { this._socketContext.SocketError = value; }
		}

		#endregion

		#region -- In-Proc support fakes --

		/// <summary>
		///		Sets <see cref="BytesTransferred"/> property value with specified value for testing purposes.
		/// </summary>
		/// <param name="value">The value.</param>
		internal void SetBytesTransferred( int value )
		{
			this._bytesTransferred = value;
		}

		internal byte[] Buffer
		{
			get { return this._socketContext.Buffer; }
		}

		internal IList<ArraySegment<byte>> BufferList
		{
			get { return this._socketContext.BufferList; }
		}

		internal int Offset
		{
			get { return this._socketContext.Offset; }
		}

		#endregion


		/// <summary>
		///		Initializes a new instance of the <see cref="MessageContext"/> class.
		/// </summary>
		protected MessageContext()
		{
			this._socketContext = new SocketAsyncEventArgs();
			this._socketContext.UserToken = this;
			this._timeoutWatcher = new TimeoutWatcher();
			this._timeoutWatcher.Timeout += ( sender, e ) => this.OnTimeout();
		}

		/// <summary>
		///		Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			this.Dispose( true );
			GC.SuppressFinalize( this );
		}

		/// <summary>
		///		Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing">
		///		<c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.
		///	</param>
		protected virtual void Dispose( bool disposing )
		{
			if ( disposing )
			{
				this._socketContext.Dispose();
				this._timeoutWatcher.Dispose();
			}
		}

		/// <summary>
		///		Starts timeout watch.
		/// </summary>
		/// <param name="timeout">The timeout.</param>
		internal virtual void StartWatchTimeout( TimeSpan timeout )
		{
			Interlocked.Exchange( ref this._isTimeout, 0 );
			this._timeoutWatcher.Start( timeout );
		}

		/// <summary>
		///		Stops timeout watch.
		/// </summary>
		internal virtual void StopWatchTimeout()
		{
			this._timeoutWatcher.Stop();
			this._timeoutWatcher.Reset();
		}

		/// <summary>
		///		Renews the session id and start time.
		/// </summary>
		public void RenewSessionId()
		{
			Contract.Ensures( this.SessionId > 0 );
			Contract.Ensures( this.SessionStartedAt >= DateTimeOffset.Now );

			this._sessionId = Interlocked.Increment( ref _lastSessionId );
			this._sessionStartedAt = DateTimeOffset.Now;
		}

		/// <summary>
		///		Clears the session id.
		/// </summary>
		internal void ClearSessionId()
		{
			Interlocked.Exchange( ref this._sessionId, 0 );
		}

		/// <summary>
		///		Clears this instance internal buffers for reuse.
		/// </summary>
		internal virtual void Clear()
		{
			Contract.Ensures( this.CompletedSynchronously == false );
			Contract.Ensures( this.MessageId == null );
			Contract.Ensures( this.SessionId == 0 );
			Contract.Ensures( this.SessionStartedAt == default( DateTimeOffset ) );

			this._completedSynchronously = false;
			this.MessageId = null;
			this._sessionId = 0;
			this._sessionStartedAt = default( DateTimeOffset );
			this._bytesTransferred = null;
			this._timeoutWatcher.Reset();
			Interlocked.Exchange( ref this._isTimeout, 0 );
		}

		internal void UnboundTransport()
		{
			Contract.Ensures( this.BoundTransport == null );

			var boundTransport = Interlocked.Exchange( ref this._boundTransport, null );
			if ( boundTransport != null )
			{
				this.SocketContext.Completed -= boundTransport.OnSocketOperationCompleted;
			}
		}
	}
}