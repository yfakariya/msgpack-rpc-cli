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

namespace MsgPack.Rpc.Server.Protocols
{
	/// <summary>
	///		<see cref="ServerTransport"/> implementation for UDP/IP.
	/// </summary>
	public sealed class UdpServerTransport : ServerTransport
	{
		/// <summary>
		///		Initializes a new instance of the <see cref="UdpServerTransport"/> class.
		/// </summary>
		/// <param name="manager">The manager which will manage this instance.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="manager"/> is <c>null</c>.
		/// </exception>
		public UdpServerTransport( UdpServerTransportManager manager ) : base( manager ) { }

		/// <summary>
		///		Performs protocol specific asynchronous 'Send' operation.
		/// </summary>
		/// <param name="context">Context information.</param>
		protected sealed override void SendCore( ServerResponseContext context )
		{
			// Manager stores the socket which is dedicated socket to this transport in the AcceptSocket property.
			if ( !this.BoundSocket.SendToAsync( context ) )
			{
				context.SetCompletedSynchronously();
				this.OnSent( context );
			}
		}

		/// <summary>
		///		Performs protocol specific asynchronous 'Receive' operation.
		/// </summary>
		/// <param name="context">Context information.</param>
		protected sealed override void ReceiveCore( ServerRequestContext context )
		{
			// Manager stores the socket which is dedicated socket to this transport in the AcceptSocket property.
			if ( !this.BoundSocket.ReceiveFromAsync( context ) )
			{
				context.SetCompletedSynchronously();
				this.OnReceived( context );
			}
		}
	}
}
