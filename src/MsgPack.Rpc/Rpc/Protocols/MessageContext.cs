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
using System.Globalization;
using System.Net.Sockets;
using System.Threading;

namespace MsgPack.Rpc.Protocols
{
	// FIXME: MessageContext should be decoupled from SocketAsyncEventArgs. Use UserToken instead.
	/// <summary>
	///		Represents context information of asynchronous MesagePack-RPC operation.
	/// </summary>
	public abstract class MessageContext : SocketAsyncEventArgs
	{
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
			this._completedSynchronously = true;
		}

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
			this.AcceptSocket = transport.BoundSocket;
			var oldBoundTransport = Interlocked.CompareExchange( ref this._boundTransport, transport, null );
			if ( oldBoundTransport != null )
			{
				throw new InvalidOperationException( String.Format( CultureInfo.CurrentCulture, "This context is already bounded to '{0}'(Socket: 0x{1:X}).", transport.GetType(), transport.BoundSocket == null ? IntPtr.Zero : transport.BoundSocket.Handle ) );
			}

			this.Completed += transport.OnSocketOperationCompleted;
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="MessageContext"/> class.
		/// </summary>
		protected MessageContext() { }

		/// <summary>
		///		Renews the session id and start time.
		/// </summary>
		public void RenewSessionId()
		{
			this._sessionId = Interlocked.Increment( ref _lastSessionId );
			this._sessionStartedAt = DateTimeOffset.Now;
		}

		/// <summary>
		///		Clears this instance internal buffers for reuse.
		/// </summary>
		internal virtual void Clear()
		{
			var boundTransport = Interlocked.Exchange( ref this._boundTransport, null );
			if ( boundTransport != null )
			{
				this.Completed -= boundTransport.OnSocketOperationCompleted;
			}

			this._completedSynchronously = false;
		}
	}
}
