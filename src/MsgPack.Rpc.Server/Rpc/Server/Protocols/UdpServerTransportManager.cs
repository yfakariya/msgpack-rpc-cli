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
using System.Diagnostics.Contracts;
using System.Net;

namespace MsgPack.Rpc.Server.Protocols
{
	/// <summary>
	///		<see cref="ServerTransportManager{T}"/> implementation for the UDP.
	/// </summary>
	public sealed class UdpServerTransportManager : ServerTransportManager<UdpServerTransport>
	{
		private readonly Socket _listeningSocket;
		private readonly ListeningContext _listeningContext;

		/// <summary>
		///		Initializes a new instance of the <see cref="UdpServerTransportManager"/> class.
		/// </summary>
		/// <param name="server">The server which will host this instance.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="server"/> is <c>null</c>.
		/// </exception>
		public UdpServerTransportManager( RpcServer server )
			: base( server )
		{
#if !API_SIGNATURE_TEST
			base.SetTransportPool( server.Configuration.UdpTransportPoolProvider( () => new UdpServerTransport( this ), server.Configuration.CreateTransportPoolConfiguration() ) );
#endif

			this._listeningContext = new ListeningContext();
			var addressFamily = ( server.Configuration.PreferIPv4 || !Socket.OSSupportsIPv6 ) ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6;
			var bindingEndPoint = this.Configuration.BindingEndPoint;
#if !API_SIGNATURE_TEST
			if ( bindingEndPoint == null )
			{
				bindingEndPoint = NetworkEnvironment.GetDefaultEndPoint( 57319, server.Configuration.PreferIPv4 );
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.DefaultEndPoint,
					"Default end point is selected. {{ \"EndPoint\" : \"{0}\", \"AddressFamily\" : {1}, \"PreferIPv4\" : {2}, \"OSSupportsIPv6\" : {3} }}",
					bindingEndPoint,
					addressFamily,
					server.Configuration.PreferIPv4,
					Socket.OSSupportsIPv6
				);
			}
#endif
			this._listeningSocket =
				new Socket(
					bindingEndPoint.AddressFamily,
					SocketType.Dgram,
					ProtocolType.Udp
				);

			this._listeningSocket.Bind( bindingEndPoint );
			this._listeningContext.RemoteEndPoint = bindingEndPoint;

#if !API_SIGNATURE_TEST
			MsgPackRpcServerProtocolsTrace.TraceEvent(
				MsgPackRpcServerProtocolsTrace.StartListen,
				"Start listen. {{ \"Socket\" : 0x{0:X}, \"EndPoint\" : \"{1}\", \"ListenBackLog\" : {2} }}",
				this._listeningSocket.Handle,
				bindingEndPoint,
				server.Configuration.ListenBackLog
			);
#endif

			//FIXME: Receive chain.
			this.PollArrival();
		}

		private void PollArrival()
		{
			// FIXME: Configurable
			this._listeningContext.SetBuffer( new byte[ 65536 ], 0, 65536 );
			this._listeningContext.BufferList = null;

			// FIXME: Use multicast to establish virtual connection.
			if ( !this._listeningSocket.ReceiveFromAsync( this._listeningContext ) )
			{
				this.OnArrived( this._listeningContext );
			}
		}

		private void OnCompleted( object sender, SocketAsyncEventArgs e )
		{
			if ( !this.HandleSocketError( sender as Socket, e ) )
			{
				return;
			}

			switch ( e.LastOperation )
			{
				case SocketAsyncOperation.ReceiveFrom:
				{
					var context = e as ListeningContext;
					Contract.Assert( context != null );
					this.OnArrived( context );
					break;
				}
				default:
				{
#if !API_SIGNATURE_TEST
					var socket = sender as Socket;
					MsgPackRpcServerProtocolsTrace.TraceEvent(
						MsgPackRpcServerProtocolsTrace.UnexpectedLastOperation,
						"Unexpected operation. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\", \"LastOperation\" : \"{3}\" }}",
						socket.Handle,
						socket.RemoteEndPoint,
						socket.LocalEndPoint,
						e.LastOperation
					);
#endif
					break;
				}
			}
		}


		private void OnArrived( ListeningContext context )
		{
#if !API_SIGNATURE_TEST
			MsgPackRpcServerProtocolsTrace.TraceEvent(
				MsgPackRpcServerProtocolsTrace.EndAccept,
				"Accept. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\" }}",
				context.AcceptSocket.Handle,
				context.AcceptSocket.RemoteEndPoint,
				context.AcceptSocket.LocalEndPoint
			);
#endif

			Contract.Assert( context.BytesTransferred == 0, context.BytesTransferred.ToString() );

			var transport = this.GetTransport( context.AcceptSocket );
			context.AcceptSocket = null;
			this.Accept( context );
			// FIXME: Remove context pooling
			transport.Receive( this.GetRequestContext( transport ) );
		}

		/// <summary>
		///		Gets the transport managed by this instance.
		/// </summary>
		/// <param name="bindingSocket">The <see cref="Socket"/> to be bind the returning transport.</param>
		/// <returns>
		///		The transport managed by this instance.
		///		Note that <see cref="ServerTransport.BoundSocket"/> might be <c>null</c> depends on <see cref="GetTransportCore"/> implementation.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		///		<paramref name="bindingSocket"/> is <c>null</c>.
		/// </exception>
		protected sealed override UdpServerTransport GetTransportCore( Socket bindingSocket )
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
