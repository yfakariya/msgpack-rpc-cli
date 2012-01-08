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
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using MsgPack.Rpc.Protocols;
using MsgPack.Rpc.Server.Dispatch;
using MsgPack.Rpc.Server.Protocols;
using MsgPack.Serialization;

namespace MsgPack.Rpc.Server
{
	/// <summary>
	///		Control stand alone server event loop.
	/// </summary>
	public class RpcServer : IDisposable
	{
		private readonly SerializationContext _serializationContext;

		public SerializationContext SerializationContext
		{
			get { return this._serializationContext; }
		}

		private readonly RpcServerConfiguration _configuration;

		public RpcServerConfiguration Configuration
		{
			get { return this._configuration; }
		}


		private EventHandler<RpcClientErrorEventArgs> _clientError;

		public event EventHandler<RpcClientErrorEventArgs> ClientError
		{
			add
			{
				EventHandler<RpcClientErrorEventArgs> oldHandler;
				EventHandler<RpcClientErrorEventArgs> currentHandler = this._clientError;
				do
				{
					oldHandler = currentHandler;
					var newHandler = Delegate.Combine( oldHandler, value ) as EventHandler<RpcClientErrorEventArgs>;
					currentHandler = Interlocked.CompareExchange( ref this._clientError, newHandler, oldHandler );
				} while ( oldHandler != currentHandler );
			}
			remove
			{
				EventHandler<RpcClientErrorEventArgs> oldHandler;
				EventHandler<RpcClientErrorEventArgs> currentHandler = this._clientError;
				do
				{
					oldHandler = currentHandler;
					var newHandler = Delegate.Remove( oldHandler, value ) as EventHandler<RpcClientErrorEventArgs>;
					currentHandler = Interlocked.CompareExchange( ref this._clientError, newHandler, oldHandler );
				} while ( oldHandler != currentHandler );
			}
		}

		protected virtual void OnClientError( RpcClientErrorEventArgs e )
		{
			if ( e == null )
			{
				throw new ArgumentNullException( "e" );
			}

			var handler = Interlocked.CompareExchange( ref this._clientError, null, null );
			if ( handler != null )
			{
				handler( this, e );
			}
		}

		internal void RaiseClientError( ServerRequestContext context, RpcErrorMessage rpcError )
		{
			this.OnClientError( 
				new RpcClientErrorEventArgs( rpcError ) 
				{ 
					RemoteEndPoint = context.RemoteEndPoint, 
					SessionId = context.SessionId,
					MessageId = context.MessageId
				}
			);
		}


		private EventHandler<RpcServerErrorEventArgs> _serverError;

		public event EventHandler<RpcServerErrorEventArgs> ServerError
		{
			add
			{
				EventHandler<RpcServerErrorEventArgs> oldHandler;
				EventHandler<RpcServerErrorEventArgs> currentHandler = this._serverError;
				do
				{
					oldHandler = currentHandler;
					var newHandler = Delegate.Combine( oldHandler, value ) as EventHandler<RpcServerErrorEventArgs>;
					currentHandler = Interlocked.CompareExchange( ref this._serverError, newHandler, oldHandler );
				} while ( oldHandler != currentHandler );
			}
			remove
			{
				EventHandler<RpcServerErrorEventArgs> oldHandler;
				EventHandler<RpcServerErrorEventArgs> currentHandler = this._serverError;
				do
				{
					oldHandler = currentHandler;
					var newHandler = Delegate.Remove( oldHandler, value ) as EventHandler<RpcServerErrorEventArgs>;
					currentHandler = Interlocked.CompareExchange( ref this._serverError, newHandler, oldHandler );
				} while ( oldHandler != currentHandler );
			}
		}

		protected virtual void OnServerError( RpcServerErrorEventArgs e )
		{
			if ( e == null )
			{
				throw new ArgumentNullException( "e" );
			}

			var handler = Interlocked.CompareExchange( ref this._serverError, null, null );
			if ( handler != null )
			{
				handler( this, e );
			}
		}

		internal void RaiseServerError( Exception exception )
		{
			this.OnServerError( new RpcServerErrorEventArgs( exception ) );
		}

		// TODO: auto-scaling
		// _maximumConcurrency, _currentConcurrency, _minimumIdle, _maximumIdle

		private ServerTransportManager _transportManager;
		private Dispatcher _dispatcher;

		public RpcServer() : this( null ) { }

		public RpcServer( RpcServerConfiguration configuration )
		{
			var safeConfiguration = ( configuration ?? RpcServerConfiguration.Default ).AsFrozen();
			this._configuration = safeConfiguration;
			this._serializationContext = new SerializationContext();
		}

		public void Dispose()
		{
			this.Stop();
		}

		private ServerTransportManager CreateTransportManager()
		{
			// TODO: transport factory.
			return new TcpServerTransportManager( this );
		}

		// FIXME : Prohibit reuse.
		public bool Start()
		{
			var currentDispatcher = Interlocked.CompareExchange( ref this._dispatcher, this._configuration.DispatcherProvider( this ), null );
			if ( currentDispatcher != null )
			{
				return false;
			}

			MsgPackRpcServerTrace.TraceEvent( 
				MsgPackRpcServerTrace.StartServer,
				"Start server. {{ \"Configuration\" : {0} }}", 
				this._configuration 
			);

			this._transportManager = this._configuration.TransportManagerProvider( this );
			return true;
		}

		// FIXME: Change to Dispose 
		public bool Stop()
		{
			var currentDispatcher = Interlocked.Exchange( ref this._dispatcher, null );
			if ( currentDispatcher == null )
			{
				return false;
			}

			if ( this._transportManager != null )
			{
				this._transportManager.BeginShutdown();
				this._transportManager.Dispose();
				this._transportManager = null;
			}

			// TODO: ID
			MsgPackRpcServerTrace.TraceEvent(
				MsgPackRpcServerTrace.StopServer,
				"Stop server. {{ \"Configuration\" : {0} }}",
				this._configuration
			);
			return true;
		}
	}
}