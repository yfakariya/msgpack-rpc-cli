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
using System.Net;
using System.Net.Sockets;

namespace MsgPack.Rpc.Server.Protocols
{
	/// <summary>
	///		<see cref="ServerTransport"/> implementation for UDP/IP.
	/// </summary>
	public sealed class UdpServerTransport : ServerTransport
	{
		private Socket CurrentSocket
		{
			get;
			set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UdpServerTransport"/> class.
		/// </summary>
		/// <param name="context">The context information.</param>
		/// <exception cref="ArgumentNullException">
		///   <paramref name="context"/> is <c>null</c>.
		///   </exception>
		public UdpServerTransport( ServerSocketAsyncEventArgs context ) : base( context ) { }

		protected sealed override void InitializeCore( ServerSocketAsyncEventArgs context, EndPoint bindingEndPoint )
		{
			// TODO: IPv6
			// TODO: BackLog-Configuration
			var socket = new Socket( AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp );
			socket.Bind( bindingEndPoint );
			socket.Listen( 10 );
			context.ListeningSocket = socket;
			//context.RemoteEndPoint = 
			this.Receive( context );
		}

		protected sealed override void ReceiveCore( ServerSocketAsyncEventArgs context )
		{
			if ( !this.CurrentSocket.ReceiveFromAsync( context ) )
			{
				this.OnReceived( context );
			}
		}

		protected sealed override void SendCore( ServerSocketAsyncEventArgs context )
		{
			if ( !CurrentSocket.SendToAsync( context ) )
			{
				this.OnSent( context );
			}
		}
	}
}
