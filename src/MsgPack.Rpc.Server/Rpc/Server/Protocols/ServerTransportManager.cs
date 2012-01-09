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
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc.Server.Protocols
{
	public abstract class ServerTransportManager : IDisposable
	{
		private readonly RpcServer _server;

		protected internal RpcServer Server
		{
			get { return this._server; }
		}

		private readonly ObjectPool<ServerRequestContext> _requestContextPool;

		public ObjectPool<ServerRequestContext> RequestContextPool
		{
			get { return this._requestContextPool; }
		}

		private readonly ObjectPool<ServerResponseContext> _responseContextPool;

		public ObjectPool<ServerResponseContext> ResponseContextPool
		{
			get { return this._responseContextPool; }
		}

		private readonly RpcServerConfiguration _configuration;

		protected RpcServerConfiguration Configuration
		{
			get { return this._configuration; }
		}

		private bool _isDisposed;

		protected bool IsDisposed
		{
			get { return this._isDisposed; }
		}

		private bool _isInShutdown;

		public bool IsInShutdown
		{
			get { return this._isInShutdown; }
		}

		private EventHandler<EventArgs> _shutdownCompleted;

		public event EventHandler<EventArgs> ShutdownCompleted
		{
			add
			{
				EventHandler<EventArgs> oldHandler;
				EventHandler<EventArgs> currentHandler = this._shutdownCompleted;
				do
				{
					oldHandler = currentHandler;
					var newHandler = Delegate.Combine( oldHandler, value ) as EventHandler<EventArgs>;
					currentHandler = Interlocked.CompareExchange( ref this._shutdownCompleted, newHandler, oldHandler );
				} while ( oldHandler != currentHandler );
			}
			remove
			{
				EventHandler<EventArgs> oldHandler;
				EventHandler<EventArgs> currentHandler = this._shutdownCompleted;
				do
				{
					oldHandler = currentHandler;
					var newHandler = Delegate.Remove( oldHandler, value ) as EventHandler<EventArgs>;
					currentHandler = Interlocked.CompareExchange( ref this._shutdownCompleted, newHandler, oldHandler );
				} while ( oldHandler != currentHandler );
			}
		}

		protected virtual void OnShutdownCompleted()
		{
			var handler = Interlocked.CompareExchange( ref this._shutdownCompleted, null, null );
			if ( handler != null )
			{
				handler( this, EventArgs.Empty );
			}
		}

		internal void RaiseClientError( ServerRequestContext context, RpcErrorMessage rpcError )
		{
			this.Server.RaiseClientError( context, rpcError );
		}

		internal void RaiseServerError( Exception exception )
		{
			this.Server.RaiseServerError( exception );
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerTransportManager"/> class.
		/// </summary>
		/// <param name="server">The server which will host this instance.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="server"/> is <c>null</c>.
		/// </exception>
		protected ServerTransportManager( RpcServer server )
		{
			if ( server == null )
			{
				throw new ArgumentNullException( "server" );
			}

			this._configuration = server.Configuration;
			this._requestContextPool = server.Configuration.RequestContextPoolProvider( () => new ServerRequestContext(), server.Configuration.CreateRequestContextPoolConfiguration() );
			this._responseContextPool = server.Configuration.ResponseContextPoolProvider( () => new ServerResponseContext(), server.Configuration.CreateResponseContextPoolConfiguration() );
			this._server = server;
		}

		public void Dispose()
		{
			this.Dispose( true );
			GC.SuppressFinalize( this );
		}

		protected void Dispose( bool disposing )
		{
			this._isDisposed = true;
			Thread.MemoryBarrier();
			this.OnDisposing( disposing );
			this.DisposeCore( disposing );
			this.OnDisposed( disposing );
		}

		protected virtual void OnDisposing( bool disposing ) { }
		protected virtual void DisposeCore( bool disposing ) { }
		protected virtual void OnDisposed( bool disposing ) { }

		public void BeginShutdown()
		{
			this.BeginShutdownCore();
		}

		protected virtual void BeginShutdownCore()
		{
			this._isInShutdown = true;
			Thread.MemoryBarrier();
		}

		protected internal bool HandleSocketError( Socket socket, SocketAsyncEventArgs context )
		{
			if ( socket == null )
			{
				throw new ArgumentNullException( "socket" );
			}

			if ( context == null )
			{
				throw new ArgumentNullException( "e" );
			}

			bool? isError = context.SocketError.IsError();
			if ( isError == null )
			{
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.IgnoreableError,
					"Ignoreable error. {{ \"Socket\" : 0x{0:X}, \"RemoteEndpoint\" : \"{1}\", \"LocalEndpoint\" : \"{2}\", \"LastOperation\" : \"{3}\", \"SocketError\" : \"{4}\", \"ErrorCode\" : 0x{5:X} }}",
					socket.Handle,
					socket.RemoteEndPoint,
					socket.LocalEndPoint,
					context.LastOperation,
					context.SocketError,
					( int )context.SocketError
				);
				return true;
			}
			else if ( isError.GetValueOrDefault() )
			{
				var errorDetail =
					String.Format(
						CultureInfo.CurrentCulture,
						"Socket error. {{ \"Socket\" : 0x{0:X}, \"RemoteEndpoint\" : \"{1}\", \"LocalEndpoint\" : \"{2}\", \"LastOperation\" : \"{3}\", \"SocketError\" : \"{4}\", \"ErrorCode\" : 0x{5:X} }}",
						socket.Handle,
						socket.RemoteEndPoint,
						socket.LocalEndPoint,
						context.LastOperation,
						context.SocketError,
						( int )context.SocketError
					);
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.SocketError,
					errorDetail
				);

				this.RaiseServerError( new RpcTransportException( RpcError.TransportError, "Socket error.", errorDetail, new SocketException( ( int )context.SocketError ) ) );
				return false;
			}

			return true;
		}

		internal abstract void ReturnTransport( ServerTransport transport );
	}
}
