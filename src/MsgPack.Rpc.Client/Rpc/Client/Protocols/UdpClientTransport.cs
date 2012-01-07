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
using System.Linq;
using System.Text;
using System.Net;

namespace MsgPack.Rpc.Client.Protocols
{
	public sealed class UdpClientTransport : ClientTransport, ILeaseable<UdpClientTransport>
	{
		public EndPoint RemoteEndPoint { get; internal set; }

		public UdpClientTransport( UdpClientTransportManager manager ) : base( manager ) { }

		public sealed override ClientRequestContext GetClientRequestContext()
		{
			if ( this.RemoteEndPoint == null )
			{
				throw new InvalidOperationException( "RemoteEndPoint must be set. UdpClientTransport must be retrieved from UdpTClientransportManager.GetTransport." );
			}

			var result = base.GetClientRequestContext();
			result.RemoteEndPoint = this.RemoteEndPoint;
			return result;
		}

		protected sealed override void SendCore( ClientRequestContext context )
		{
			// Manager stores the socket which is dedicated socket to this transport in the AcceptSocket property.
			if ( !this.BoundSocket.SendToAsync( context ) )
			{
				context.SetCompletedSynchronously();
				this.OnSent( context );
			}
		}

		protected sealed override void ReceiveCore( ClientResponseContext context )
		{
			// Manager stores the socket which is dedicated socket to this transport in the AcceptSocket property.
			if ( !this.BoundSocket.ReceiveFromAsync( context ) )
			{
				context.SetCompletedSynchronously();
				this.OnReceived( context );
			}
		}

		void ILeaseable<UdpClientTransport>.SetLease( ILease<UdpClientTransport> lease )
		{
			base.SetLease( lease );
		}
	}
}
