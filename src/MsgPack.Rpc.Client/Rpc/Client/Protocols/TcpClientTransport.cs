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
using System.Net.Sockets;

namespace MsgPack.Rpc.Client.Protocols
{
	/// <summary>
	///		Implements <see cref="ClientTransport"/> for TCP/IP protocol.
	/// </summary>
	public sealed class TcpClientTransport : ClientTransport
	{
		/// <summary>
		/// Gets a value indicating whether the protocol used by this class can resume receiving.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance can resume receiving; otherwise, <c>false</c>.
		/// </value>
		protected override bool CanResumeReceiving
		{
			get { return true; }
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="TcpClientTransport"/> class.
		/// </summary>
		/// <param name="manager">The manager which will manage this instance.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="manager"/> is <c>null</c>.
		/// </exception>
		public TcpClientTransport( TcpClientTransportManager manager )
			: base( manager ) { }

		/// <summary>
		///		Shutdowns the sending.
		/// </summary>
		protected sealed override void ShutdownSending()
		{
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
		///		Shutdowns the receiving.
		/// </summary>
		protected sealed override void ShutdownReceiving()
		{
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
		///		Performs protocol specific asynchronous 'Send' operation.
		/// </summary>
		/// <param name="context">Context information.</param>
		protected sealed override void SendCore( ClientRequestContext context )
		{
			if ( !this.BoundSocket.SendAsync( context.SocketContext ) )
			{
				context.SetCompletedSynchronously();
				this.OnSent( context );
			}
		}

		/// <summary>
		///		Performs protocol specific asynchronous 'Receive' operation.
		/// </summary>
		/// <param name="context">Context information.</param>
		protected sealed override void ReceiveCore( ClientResponseContext context )
		{
			if ( !this.BoundSocket.ReceiveAsync( context.SocketContext ) )
			{
				context.SetCompletedSynchronously();
				this.OnReceived( context );
			}
		}
	}
}
