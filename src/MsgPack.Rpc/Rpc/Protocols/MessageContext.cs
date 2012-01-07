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

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Represents context information of asynchronous MesagePack-RPC operation.
	/// </summary>
	public abstract class MessageContext : SocketAsyncEventArgs, ILeaseable<MessageContext>
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

		internal DateTimeOffset SessionStartedAt
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
		internal int? MessageId
		{
			get;
			set;
		}

		private bool _completedSynchronously;

		internal bool CompletedSynchronously
		{
			get { return this._completedSynchronously; }
		}

		public void SetCompletedSynchronously()
		{
			this._completedSynchronously = true;
		}

		private ILease<MessageContext> _asLease;

		void ILeaseable<MessageContext>.SetLease( ILease<MessageContext> lease )
		{
			this.SetLease( lease );
		}

		protected void SetLease( ILease<MessageContext> lease )
		{
			this._asLease = lease;
		}

		private IContextBoundableTransport _boundTransport;

		internal IContextBoundableTransport BoundTransport
		{
			get { return this._boundTransport; }
		}

		private EventHandler<SocketAsyncEventArgs> _boundEventHandler;

		internal virtual void SetTransport( IContextBoundableTransport transport )
		{
			this.AcceptSocket = transport.BoundSocket;
			Contract.Assert( this._boundEventHandler == null, "Bounded: " + this._boundEventHandler );
			if ( this._boundEventHandler != null )
			{
				this.Completed -= this._boundEventHandler;
			}

			this._boundEventHandler = transport.OnSocketOperationCompleted;
			this.Completed += this._boundEventHandler;
			this._boundTransport = transport;
		}

		protected MessageContext() { }

		public void RenewSessionId()
		{
			this._sessionId = Interlocked.Increment( ref _lastSessionId );
			this._sessionStartedAt = DateTimeOffset.Now;
		}

		internal void ReturnLease()
		{
			try { }
			finally
			{
				if ( this._boundEventHandler != null )
				{
					this.Completed -= this._boundEventHandler;
					this._boundTransport = null;
				}

				this._completedSynchronously = false;

				if ( this._asLease != null )
				{
					this._asLease.Dispose();
					this._asLease = null;
				}
			}
		}

		internal virtual void Clear()
		{
			this.ReturnLease();
		}
	}
}
