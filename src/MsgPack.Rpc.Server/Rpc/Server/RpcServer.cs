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
using System.Threading;
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

		/// <summary>
		///		Gets the <see cref="SerializationContext"/> which holds serializers used in this server stack.
		/// </summary>
		/// <value>
		///		The <see cref="SerializationContext"/> which holds serializers used in this server stack.
		///		This value will not be <c>null</c>.
		/// </value>
		public SerializationContext SerializationContext
		{
			get
			{
				Contract.Ensures( Contract.Result<SerializationContext>() != null );

				return this._serializationContext;
			}
		}

		private readonly RpcServerConfiguration _configuration;

		/// <summary>
		///		Gets the <see cref="RpcServerConfiguration"/> for this server stack.
		/// </summary>
		/// <value>
		///		The <see cref="RpcServerConfiguration"/> for this server stack.
		///		This value will not be <c>null</c>.
		/// </value>
		public RpcServerConfiguration Configuration
		{
			get
			{
				Contract.Ensures( Contract.Result<RpcServerConfiguration>() != null );

				return this._configuration;
			}
		}

		/// <summary>
		///		Gets or sets the id of this server for debugging.
		/// </summary>
		/// <value>
		///		The id of this server.
		/// </value>
		public string Id
		{
			get;
			set;
		}

		private EventHandler<RpcClientErrorEventArgs> _clientError;

		/// <summary>
		///		Occurs when the client causes some error.
		/// </summary>
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

		/// <summary>
		///		Raises the <see cref="E:ClientError"/> event.
		/// </summary>
		/// <param name="e">The <see cref="MsgPack.Rpc.Server.RpcClientErrorEventArgs"/> instance containing the event data.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="e"/> is <c>null</c>.
		/// </exception>
		protected virtual void OnClientError( RpcClientErrorEventArgs e )
		{
			if ( e == null )
			{
				throw new ArgumentNullException( "e" );
			}

			Contract.EndContractBlock();

			var handler = Interlocked.CompareExchange( ref this._clientError, null, null );
			if ( handler != null )
			{
				handler( this, e );
			}
		}

		/// <summary>
		///		Raises the <see cref="E:ClientError"/> event.
		/// </summary>
		/// <param name="context">The context information.</param>
		/// <param name="rpcError">The RPC error.</param>
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

		/// <summary>
		///		Occurs when the server stack causes some errors.
		/// </summary>
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

		/// <summary>
		///		Raises the <see cref="E:ServerError"/> event.
		/// </summary>
		/// <param name="e">The <see cref="MsgPack.Rpc.Server.RpcServerErrorEventArgs"/> instance containing the event data.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="e"/> is <c>null</c>.
		/// </exception>
		protected virtual void OnServerError( RpcServerErrorEventArgs e )
		{
			if ( e == null )
			{
				throw new ArgumentNullException( "e" );
			}

			Contract.EndContractBlock();

			var handler = Interlocked.CompareExchange( ref this._serverError, null, null );
			if ( handler != null )
			{
				handler( this, e );
			}
		}

		/// <summary>
		///		Raises the <see cref="E:ServerError"/> event.
		/// </summary>
		/// <param name="exception">The occurred exception.</param>
		internal void RaiseServerError( Exception exception )
		{
			this.OnServerError( new RpcServerErrorEventArgs( exception ) );
		}

		private ServerTransportManager _transportManager;
		private Dispatcher _dispatcher;
		private int _state;

		/// <summary>
		///		Initializes a new instance of the <see cref="RpcServer"/> class with default configuration.
		/// </summary>
		public RpcServer() : this( null ) { }

		/// <summary>
		///		Initializes a new instance of the <see cref="RpcServer"/> class with specified configuration.
		/// </summary>
		/// <param name="configuration">
		///		The <see cref="RpcServerConfiguration"/>.
		///		Or <c>null</c> to use default configuration.
		///	</param>
		public RpcServer( RpcServerConfiguration configuration )
		{
			var safeConfiguration = ( configuration ?? RpcServerConfiguration.Default ).AsFrozen();
			this._configuration = safeConfiguration;
			this._serializationContext = new SerializationContext();
		}

		/// <summary>
		///		Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			this.Dispose( true );
			GC.SuppressFinalize( this );
		}

		/// <summary>
		///		Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing">
		///		<c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		protected virtual void Dispose( bool disposing )
		{
			this.Stop();
		}

		/// <summary>
		///		Starts the server stack.
		/// </summary>
		/// <returns>
		///		<c>true</c> if the server starts operation; otherwise, <c>false</c>.
		///		If the server is already started, this method returns <c>false</c>.
		/// </returns>
		public bool Start()
		{
			if ( Interlocked.CompareExchange( ref this._state, 0, 0 ) != 0 )
			{
				throw new ObjectDisposedException( this.ToString() );
			}

			var currentDispatcher = Interlocked.CompareExchange( ref this._dispatcher, this._configuration.DispatcherProvider( this ), null );
			if ( currentDispatcher != null )
			{
				return false;
			}

			MsgPackRpcServerTrace.TraceEvent(
				MsgPackRpcServerTrace.StartServer,
				"Start server. {{ \"Configuration\" : {0}, \"Id\" : \"{1}\" }}",
				this._configuration,
				this
			);

			this._transportManager = this._configuration.TransportManagerProvider( this );
			return true;
		}

		private void Stop()
		{
			var currentDispatcher = Interlocked.Exchange( ref this._dispatcher, null );
			if ( currentDispatcher == null )
			{
				return;
			}

			if ( Interlocked.Exchange( ref _state, 1 ) != 0 )
			{
				return;
			}

			if ( this._transportManager != null )
			{
				using ( var tracker = new ShutdownTracker() )
				{
					this._transportManager.ShutdownCompleted += tracker.OnTransportManagerShutdownCompleted;
					try
					{
						if ( this._transportManager.BeginShutdown() )
						{
							tracker.Wait();
						}
					}
					finally
					{
						this._transportManager.ShutdownCompleted -= tracker.OnTransportManagerShutdownCompleted;
					}

				}
				this._transportManager.Dispose();
				this._transportManager = null;
			}

			MsgPackRpcServerTrace.TraceEvent(
				MsgPackRpcServerTrace.StopServer,
				"Stop server. {{ \"Configuration\" : {0}, \"Id\" : \"{1}\" }}",
				this._configuration,
				this
			);
		}

		/// <summary>
		///		Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		///		A <see cref="System.String"/> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return String.IsNullOrWhiteSpace( this.Id ) ? this.GetHashCode().ToString( "X", CultureInfo.InvariantCulture ) : this.Id;
		}

		private sealed class ShutdownTracker : IDisposable
		{
			private readonly ManualResetEventSlim _waitHandle;

			public ShutdownTracker()
			{
				this._waitHandle = new ManualResetEventSlim();
			}

			public void Dispose()
			{
				this._waitHandle.Dispose();
			}

			public void Wait()
			{
				this._waitHandle.Wait();
			}

			public void OnTransportManagerShutdownCompleted( object sender, EventArgs e )
			{
				this._waitHandle.Set();
			}
		}

	}
}