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
	public sealed class UdpServerTransport : ServerTransport, ILeaseable<UdpServerTransport>
	{
		public UdpServerTransport( UdpServerTransportManager manager ) : base( manager ) { }

		protected sealed override void ReceiveCore( ServerRequestSocketAsyncEventArgs context )
		{
			// Manager stores the socket which is dedicated socket to this transport in the AcceptSocket property.
			if ( !context.AcceptSocket.ReceiveFromAsync( context ) )
			{
				this.OnReceived( context );
			}
		}

		protected sealed override void SendCore( ServerResponseSocketAsyncEventArgs context )
		{
			// Manager stores the socket which is dedicated socket to this transport in the AcceptSocket property.
			if ( !context.AcceptSocket.SendToAsync( context ) )
			{
				this.OnSent( context );
			}
		}

		void ILeaseable<UdpServerTransport>.SetLease( ILease<UdpServerTransport> lease )
		{
			base.SetLease( lease );
		}
	}
}
