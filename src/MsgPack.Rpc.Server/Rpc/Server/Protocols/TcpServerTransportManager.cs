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
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;

namespace MsgPack.Rpc.Server.Protocols
{
	/// <summary>
	///		<see cref="ServerTransportManager{T}"/> implementation for the TCP.
	/// </summary>
	public sealed class TcpServerTransportManager : ServerTransportManager<TcpServerTransport>
	{
		private readonly Socket _listeningSocket;

		[Conditional("DEBUG")]
		internal void GetListeningSocket(ref Socket result)
		{
			result = this._listeningSocket;
		}

		private readonly ObjectPool<ListeningContext> _listeningContextPool;

		/// <summary>
		/// Initializes a new instance of the <see cref="TcpServerTransportManager"/> class.
		/// </summary>
		/// <param name="server">The server which will host this instance.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="server"/> is <c>null</c>.
		/// </exception>
		public TcpServerTransportManager( RpcServer server )
			: base( server )
		{
#if !API_SIGNATURE_TEST
			base.SetTransportPool( server.Configuration.TcpTransportPoolProvider( () => new TcpServerTransport( this ), server.Configuration.CreateTransportPoolConfiguration() ) );
#endif

			this._listeningContextPool = server.Configuration.ListeningContextPoolProvider( () => new ListeningContext(), server.Configuration.CreateListeningContextPoolConfiguration() );
			var addressFamily = ( server.Configuration.PreferIPv4 || !Socket.OSSupportsIPv6) ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6;
			var bindingEndPoint = this.Configuration.BindingEndPoint;
#if !API_SIGNATURE_TEST
			if ( bindingEndPoint == null )
			{
				bindingEndPoint =
					new IPEndPoint(
#if MONO
						this.Configuration.PreferIPv4 && Socket.SupportsIPv4
#else
						this.Configuration.PreferIPv4 && Socket.OSSupportsIPv4
#endif
						? IPAddress.Any
						: IPAddress.IPv6Any,
						57129 // arbitrary number
					);
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
					SocketType.Stream,
					ProtocolType.Tcp
				);

#if !MONO
			if ( !this.Configuration.PreferIPv4
				&& Environment.OSVersion.Platform == PlatformID.Win32NT
				&& Environment.OSVersion.Version.Major >= 6
				&& Socket.OSSupportsIPv6
			)
			{
				// Listen both of IPv4 and IPv6
				this._listeningSocket.SetSocketOption( SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, 0 );
			}
#endif // !MONO

			this._listeningSocket.Bind( bindingEndPoint );
			this._listeningSocket.Listen( server.Configuration.ListenBackLog );

#if !API_SIGNATURE_TEST
			MsgPackRpcServerProtocolsTrace.TraceEvent(
				MsgPackRpcServerProtocolsTrace.StartListen,
				"Start listen. {{ \"Socket\" : 0x{0:X}, \"EndPoint\" : \"{1}\", \"ListenBackLog\" : {2} }}",
				this._listeningSocket.Handle,
				bindingEndPoint,
				server.Configuration.ListenBackLog
			);
#endif

			this.StartAccept();
		}

		/// <summary>
		///		Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected sealed override void Dispose( bool disposing )
		{
			if ( disposing )
			{
				this._listeningSocket.Close();
			}

			base.Dispose( disposing );
		}

		private void StartAccept()
		{
			var concurrency = this.Configuration.MinimumConnection;
			for ( int i = 0; i < concurrency; i++ )
			{
				var context = this._listeningContextPool.Borrow();
				context.Completed += this.OnCompleted;
				this.Accept( context );
			}
		}

		private void OnCompleted( object sender, SocketAsyncEventArgs e )
		{
			var socket = sender as Socket;
			if ( !this.HandleSocketError( socket, e ) )
			{
				return;
			}

			switch ( e.LastOperation )
			{
				case SocketAsyncOperation.Accept:
				{
					var context = e as ListeningContext;
					Contract.Assert( context != null );
					this.OnAcceptted( context );
					break;
				}
				default:
				{
#if !API_SIGNATURE_TEST
					MsgPackRpcServerProtocolsTrace.TraceEvent(
						MsgPackRpcServerProtocolsTrace.UnexpectedLastOperation,
						"Unexpected operation. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\", \"LastOperation\" : \"{3}\" }}",
						ServerTransport.GetHandle( socket ),
						ServerTransport.GetRemoteEndPoint( socket, e ),
						ServerTransport.GetLocalEndPoint( socket ),
						e.LastOperation
					);
#endif
					break;
				}
			}
		}

		private void Accept( ListeningContext context )
		{
			// Ensure buffers are cleared to avoid unepxected data feeding on Accept
			context.SetBuffer( null, 0, 0 );
			context.BufferList = null;

			try
			{
				if ( this.IsInShutdown )
				{
					return;
				}

#if !API_SIGNATURE_TEST
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.BeginAccept,
					"Wait for connection. {{ \"Socket\" : 0x{0:X}, \"LocalEndPoint\" : \"{1}\" }}",
					this._listeningSocket.Handle,
					this._listeningSocket.LocalEndPoint
				);
#endif

				if ( !this._listeningSocket.AcceptAsync( context ) )
				{
					// Avoid recursive acceptance and the subsequent request delay.
					// Task is bit heavy here.
					ThreadPool.QueueUserWorkItem( _ => this.OnAcceptted( context ) );
				}
			}
			catch ( ObjectDisposedException )
			{
				if ( !this.IsDisposed )
				{
					throw;
				}
			}
		}

		private void OnAcceptted( ListeningContext context )
		{
			if ( context.AcceptSocket == null || context.AcceptSocket.RemoteEndPoint == null )
			{
				// Canceled due to shutdown.
				return;
			}

#if !API_SIGNATURE_TEST
			MsgPackRpcServerProtocolsTrace.TraceEvent(
				MsgPackRpcServerProtocolsTrace.EndAccept,
				"Accept. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\" }}",
				ServerTransport.GetHandle( context.AcceptSocket ),
				ServerTransport.GetRemoteEndPoint( context.AcceptSocket, context ),
				ServerTransport.GetLocalEndPoint( context.AcceptSocket )
			);
#endif

			Contract.Assert( context.BytesTransferred == 0, context.BytesTransferred.ToString() );

			var transport = this.GetTransport( context.AcceptSocket );
			context.AcceptSocket = null;
			this.Accept( context );
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
		protected sealed override TcpServerTransport GetTransportCore( Socket bindingSocket )
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
