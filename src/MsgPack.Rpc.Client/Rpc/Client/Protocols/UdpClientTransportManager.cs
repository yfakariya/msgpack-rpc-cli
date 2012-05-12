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
using System.Net;
using System.Net.Sockets;
#if !WINDOWS_PHONE
using System.Threading.Tasks;
#endif

namespace MsgPack.Rpc.Client.Protocols
{
	/// <summary>
	///		Implements <see cref="ClientTransportManager{T}"/> for <see cref="TcpClientTransport"/>.
	/// </summary>
	public sealed class UdpClientTransportManager : ClientTransportManager<UdpClientTransport>
	{
		/// <summary>
		///		Initializes a new instance of the <see cref="UdpClientTransportManager"/> class.
		/// </summary>
		/// <param name="configuration">
		///		The <see cref="RpcClientConfiguration"/> which describes transport configuration.
		/// </param>
		public UdpClientTransportManager( RpcClientConfiguration configuration )
			: base( configuration )
		{
#if !API_SIGNATURE_TEST
			base.SetTransportPool( configuration.UdpTransportPoolProvider( () => new UdpClientTransport( this ), configuration.CreateTransportPoolConfiguration() ) );
#endif
		}

		/// <summary>
		///		Establishes logical connection, which specified to the managed transport protocol, for the server.
		/// </summary>
		/// <param name="targetEndPoint">The end point of target server.</param>
		/// <returns>
		///		<see cref="Task{T}"/> of <see cref="ClientTransport"/> which represents asynchronous establishment process specific to the managed transport.
		///		This value will not be <c>null</c>.
		/// </returns>
		protected sealed override Task<ClientTransport> ConnectAsyncCore( EndPoint targetEndPoint )
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

		/// <summary>
		///		Gets the transport managed by this instance.
		/// </summary>
		/// <param name="bindingSocket">The <see cref="Socket"/> to be bind the returning transport.</param>
		/// <returns>
		///		The transport managed by this instance.
		///		This implementation binds a valid <see cref="Socket"/> to the returning transport.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		///		<see cref="P:IsTransportPoolSet"/> is <c>false</c>.
		///		Or <paramref name="bindingSocket"/> is <c>null</c>.
		/// </exception>
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
