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
using System.Net.Sockets;

namespace MsgPack.Rpc.Server.Protocols
{
	/// <summary>
	///		<see cref="ServerTransport"/> implementation for TCP/IP.
	/// </summary>
	public sealed class TcpServerTransport : ServerTransport
	{
		/// <summary>
		/// Gets a value indicating whether this instance can resume receiving.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance can resume receiving; otherwise, <c>false</c>.
		/// </value>
		protected override bool CanResumeReceiving
		{
			get { return true; }
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="TcpServerTransport"/> class.
		/// </summary>
		/// <param name="manager">The manager which will manage this instance.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="manager"/> is <c>null</c>.
		/// </exception>
		public TcpServerTransport( TcpServerTransportManager manager )
			: base( manager ) { }

		/// <summary>
		///		Shutdown receiving on this transport.
		/// </summary>
		protected override void ShutdownReceiving()
		{
			if ( this.IsDisposed )
			{
				return;
			}

			Contract.Assert( this.BoundSocket != null );

			try
			{
				this.BoundSocket.Shutdown( SocketShutdown.Receive );
			}
			catch( SocketException ex )
			{
				if( ex.SocketErrorCode != SocketError.NotConnected )
				{
					throw;
				}
			}

			base.ShutdownReceiving();
		}

		/// <summary>
		///		Shutdown sending on this transport.
		/// </summary>
		protected override void ShutdownSending()
		{
			if ( this.IsDisposed )
			{
				return;
			}

			Contract.Assert( this.BoundSocket != null );
			
			try
			{
				this.BoundSocket.Shutdown( SocketShutdown.Send );
			}
			catch( SocketException ex )
			{
				if( ex.SocketErrorCode != SocketError.NotConnected )
				{
					throw;
				}
			}

			base.ShutdownSending();
		}

		/// <summary>
		///		Performs protocol specific asynchronous 'Receive' operation.
		/// </summary>
		/// <param name="context">Context information.</param>
		protected sealed override void ReceiveCore( ServerRequestContext context )
		{
			bool isAsyncOperationStarted;
			try
			{
				isAsyncOperationStarted = this.BoundSocket.ReceiveAsync( context.SocketContext );
			}
			catch( ObjectDisposedException )
			{
				// Canceled.
				return;
			}

			if ( !isAsyncOperationStarted )
			{
				context.SetCompletedSynchronously();
				this.OnReceived( context );
			}
		}

		/// <summary>
		///		Performs protocol specific asynchronous 'Send' operation.
		/// </summary>
		/// <param name="context">Context information.</param>
		protected sealed override void SendCore( ServerResponseContext context )
		{
			Contract.Assert( this.BoundSocket != null );

			if ( !this.BoundSocket.SendAsync( context.SocketContext ) )
			{
				context.SetCompletedSynchronously();
				this.OnSent( context );
			}
		}
	}
}
