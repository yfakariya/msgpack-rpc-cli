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
using System.Threading.Tasks;
using System.Globalization;
using System.Diagnostics.Contracts;

namespace MsgPack.Rpc.Client.Protocols
{
	public sealed class UdpClientTransportManager : ClientTransportManager<UdpClientTransport>
	{
		public UdpClientTransportManager( RpcClientConfiguration configuration )
			: base( configuration )
		{
#if !API_SIGNATURE_TEST
			base.SetTransportPool( configuration.UdpTransportPoolProvider( () => new UdpClientTransport( this ), configuration.CreateUdpTransportPoolConfiguration() ) );
#endif
		}

		protected sealed override Task<ClientTransport> ConnectAsyncCore(EndPoint targetEndPoint )
		{
			var task = new Task<ClientTransport>( this.CreateTransport, targetEndPoint );
			task.RunSynchronously( TaskScheduler.Default );
			return task;
		}

		private UdpClientTransport CreateTransport( object state )
		{
			var socket =
				new Socket(
					( this.Configuration.PreferIPv4 || !Socket.OSSupportsIPv6 ) ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6,
					SocketType.Dgram,
					ProtocolType.Udp
				);

			var transport = this.GetTransport( socket );
			transport.RemoteEndPoint = state as EndPoint;
			return transport;
		}

		protected sealed override UdpClientTransport GetTransportCore( Socket bindingSocket )
		{
			if ( bindingSocket == null )
			{
				throw new InvalidOperationException( String.Format( CultureInfo.CurrentCulture, "'bindingSocket' is required in {0}.", this.GetType() ) );
			}

			var transport = base.GetTransportCore( bindingSocket );
			this.BindSocket( transport, bindingSocket );
			return transport;
		}
	}
}
