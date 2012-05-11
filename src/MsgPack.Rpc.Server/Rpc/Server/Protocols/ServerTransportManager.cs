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
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc.Server.Protocols
{
	/// <summary>
	///		Defines non-generic interfaces for manager object which manages <see cref="ServerTransport"/> instances lifetime.
	/// </summary>
	/// <threadsafety instance="true" static="true" />
	[ContractClass( typeof( ServerTransportManagerContract ) )]
	public abstract class ServerTransportManager : IDisposable
	{
		private readonly RpcServer _server;

		/// <summary>
		///		Gets the <see cref="RpcServer"/> which activated this instance.
		/// </summary>
		/// <value>
		///		The <see cref="RpcServer"/> which activated this instance.
		///		This value will not be <c>null</c>.
		/// </value>
		protected internal RpcServer Server
		{
			get
			{
				Contract.Ensures( Contract.Result<RpcServer>() != null );

				return this._server;
			}
		}

		private readonly ObjectPool<ServerRequestContext> _requestContextPool;

		/// <summary>
		///		Gets the <see cref="ObjectPool{T}"/> to pool <see cref="ServerRequestContext"/>s.
		/// </summary>
		/// <value>
		///		The <see cref="ObjectPool{T}"/> to pool <see cref="ServerRequestContext"/>s.
		///		This value will not be <c>null</c>.
		/// </value>
		protected ObjectPool<ServerRequestContext> RequestContextPool
		{
			get
			{
				Contract.Ensures( Contract.Result<ObjectPool<ServerRequestContext>>() != null );

				return this._requestContextPool;
			}
		}

		private readonly ObjectPool<ServerResponseContext> _responseContextPool;

		/// <summary>
		///		Gets the <see cref="ObjectPool{T}"/> to pool <see cref="ServerResponseContext"/>s.
		/// </summary>
		/// <value>
		///		The <see cref="ObjectPool{T}"/> to pool <see cref="ServerResponseContext"/>s.
		///		This value will not be <c>null</c>.
		/// </value>
		protected ObjectPool<ServerResponseContext> ResponseContextPool
		{
			get
			{
				Contract.Ensures( Contract.Result<ObjectPool<ServerResponseContext>>() != null );

				return this._responseContextPool;
			}
		}

		/// <summary>
		///		Gets the <see cref="RpcServerConfiguration"/> associated to this instance.
		/// </summary>
		/// <value>
		///		The <see cref="RpcServerConfiguration"/> associated to this instance.
		///		This value will not be <c>null</c>.
		/// </value>
		protected RpcServerConfiguration Configuration
		{
			get
			{
				Contract.Ensures( Contract.Result<RpcServerConfiguration>() != null );

				return this._server.Configuration;
			}
		}

		private int _isDisposed;

		/// <summary>
		///		Gets a value indicating whether this instance is disposed.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is disposed; otherwise, <c>false</c>.
		/// </value>
		protected bool IsDisposed
		{
			get { return Interlocked.CompareExchange( ref this._isDisposed, 0, 0 ) != 0; }
		}

		private int _isInShutdown;

		/// <summary>
		///		Gets a value indicating whether this instance is in shutdown.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is in shutdown; otherwise, <c>false</c>.
		/// </value>
		public bool IsInShutdown
		{
			get { return Interlocked.CompareExchange( ref this._isInShutdown, 0, 0 ) != 0; }
		}

		private EventHandler<ClientShutdownEventArgs> _clientShutdown;

		/// <summary>
		///		Occurs when the shutdown process is initiated on the client of any managed transport.
		/// </summary>
		public event EventHandler<ClientShutdownEventArgs> ClientShutdown
		{
			add
			{
				EventHandler<ClientShutdownEventArgs> oldHandler;
				EventHandler<ClientShutdownEventArgs> currentHandler = this._clientShutdown;
				do
				{
					oldHandler = currentHandler;
					var newHandler = Delegate.Combine( oldHandler, value ) as EventHandler<ClientShutdownEventArgs>;
					currentHandler = Interlocked.CompareExchange( ref this._clientShutdown, newHandler, oldHandler );
				} while ( oldHandler != currentHandler );
			}
			remove
			{
				EventHandler<ClientShutdownEventArgs> oldHandler;
				EventHandler<ClientShutdownEventArgs> currentHandler = this._clientShutdown;
				do
				{
					oldHandler = currentHandler;
					var newHandler = Delegate.Remove( oldHandler, value ) as EventHandler<ClientShutdownEventArgs>;
					currentHandler = Interlocked.CompareExchange( ref this._clientShutdown, newHandler, oldHandler );
				} while ( oldHandler != currentHandler );
			}
		}

		/// <summary>
		///		Raises <see cref="ClientShutdown"/> event.
		/// </summary>
		/// <param name="e">The <see cref="MsgPack.Rpc.Server.Protocols.ClientShutdownEventArgs"/> instance containing the event data.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="e"/> is <c>null</c>.
		/// </exception>
		protected virtual void OnClientShutdown( ClientShutdownEventArgs e )
		{
			if ( e == null )
			{
				throw new ArgumentNullException( "e" );
			}

			Contract.EndContractBlock();

			var handler = Interlocked.CompareExchange( ref this._clientShutdown, null, null );
			if ( handler != null )
			{
				handler( this, e );
			}
		}

		private EventHandler<ShutdownCompletedEventArgs> _shutdownCompleted;

		/// <summary>
		///		Occurs when the shutdown process is completed.
		/// </summary>
		public event EventHandler<ShutdownCompletedEventArgs> ShutdownCompleted
		{
			add
			{
				EventHandler<ShutdownCompletedEventArgs> oldHandler;
				EventHandler<ShutdownCompletedEventArgs> currentHandler = this._shutdownCompleted;
				do
				{
					oldHandler = currentHandler;
					var newHandler = Delegate.Combine( oldHandler, value ) as EventHandler<ShutdownCompletedEventArgs>;
					currentHandler = Interlocked.CompareExchange( ref this._shutdownCompleted, newHandler, oldHandler );
				} while ( oldHandler != currentHandler );
			}
			remove
			{
				EventHandler<ShutdownCompletedEventArgs> oldHandler;
				EventHandler<ShutdownCompletedEventArgs> currentHandler = this._shutdownCompleted;
				do
				{
					oldHandler = currentHandler;
					var newHandler = Delegate.Remove( oldHandler, value ) as EventHandler<ShutdownCompletedEventArgs>;
					currentHandler = Interlocked.CompareExchange( ref this._shutdownCompleted, newHandler, oldHandler );
				} while ( oldHandler != currentHandler );
			}
		}

		/// <summary>
		///		Raises <see cref="ShutdownCompleted"/> event.
		/// </summary>
		/// <param name="e">The <see cref="MsgPack.Rpc.Protocols.ShutdownCompletedEventArgs"/> instance containing the event data.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="e"/> is <c>null</c>.
		/// </exception>
		protected virtual void OnShutdownCompleted( ShutdownCompletedEventArgs e )
		{
			if ( e == null )
			{
				throw new ArgumentNullException( "e" );
			}

			Contract.EndContractBlock();

			MsgPackRpcServerProtocolsTrace.TraceEvent(
				MsgPackRpcServerProtocolsTrace.ManagerShutdownCompleted,
				"Manager shutdown is completed. {{ \"Manager\" : \"{0}\" }}",
				this
			);

			var handler = Interlocked.CompareExchange( ref this._shutdownCompleted, null, null );
			if ( handler != null )
			{
				handler( this, e );
			}
		}

		/// <summary>
		///		Raises <see cref="E:RpcServer.ClientError"/> event on the hosting <see cref="RpcServer"/>.
		/// </summary>
		/// <param name="context">The <see cref="ServerRequestContext"/> which holds client information.</param>
		/// <param name="rpcError">The <see cref="RpcErrorMessage"/> representing the error.</param>
		internal void RaiseClientError( ServerRequestContext context, RpcErrorMessage rpcError )
		{
			Contract.Requires( context != null );
			Contract.Requires( !rpcError.IsSuccess );

			this.Server.RaiseClientError( context, rpcError );
		}

		/// <summary>
		///		Raises <see cref="E:RpcServer.ServerError"/> event on the hosting <see cref="RpcServer"/>.
		/// </summary>
		/// <param name="exception">The <see cref="Exception"/> representing error.</param>
		internal void RaiseServerError( Exception exception )
		{
			Contract.Requires( exception != null );

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

			Contract.EndContractBlock();

			this._requestContextPool = server.Configuration.RequestContextPoolProvider( () => new ServerRequestContext( server.Configuration ), server.Configuration.CreateRequestContextPoolConfiguration() );
			this._responseContextPool = server.Configuration.ResponseContextPoolProvider( () => new ServerResponseContext( server.Configuration ), server.Configuration.CreateResponseContextPoolConfiguration() );

			if ( this._requestContextPool == null )
			{
				throw new InvalidOperationException( "Configuration.RequestContextPoolProvider returns null." );
			}

			if ( this._responseContextPool == null )
			{
				throw new InvalidOperationException( "Configuration.ResponseContextPoolProvider returns null." );
			}

			this._server = server;
		}

		/// <summary>
		///		Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		[SuppressMessage( "Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Must ensure exactly once." )]
		public void Dispose()
		{
			this.DisposeOnce( true );
			GC.SuppressFinalize( this );
		}

		/// <summary>
		///		Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing">
		///		<c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.
		///	</param>
		private void DisposeOnce( bool disposing )
		{
			if ( Interlocked.CompareExchange( ref this._isDisposed, 1, 0 ) == 0 )
			{
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.DisposeManager,
					"Dispose. {{ \"Manager\" : \"{0}\" }}",
					this
				);
				this.Dispose( disposing );
			}
		}

		/// <summary>
		///		When overridden in derived class, releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing">
		///		<c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.
		///	</param>
		/// <remarks>
		///		This method is guaranteed that this is invoked exactly once and after <see cref="IsDisposed"/> changed <c>true</c>.
		/// </remarks>
		protected virtual void Dispose( bool disposing ) { }

		/// <summary>
		///		Initiates server shutdown process.
		/// </summary>
		/// <returns>
		///		If shutdown process is initiated, then <c>true</c>.
		///		If shutdown is already initiated or completed, then <c>false</c>.
		/// </returns>
		/// <remarks>
		///		To observe shutdown completion, subscribe <see cref="ShutdownCompleted"/> event.
		/// </remarks>
		public bool BeginShutdown()
		{
			if ( Interlocked.Exchange( ref this._isInShutdown, 1 ) == 0 )
			{
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.BeginShutdownManager,
					"Begin shutdown. {{ \"Manager\" : \"{0}\" }}",
					this
				);
				this.BeginShutdownCore();
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		///		When overridden in derived class, initiates protocol specific shutdown process.
		/// </summary>
		/// <remarks>
		///		This method might be called more than once.
		/// </remarks>
		protected virtual void BeginShutdownCore() { }

		/// <summary>
		///		Handles the socket error as server error.
		/// </summary>
		/// <param name="socket">The <see cref="Socket"/> caused error.</param>
		/// <param name="context">The <see cref="SocketAsyncEventArgs"/> instance containing the asynchronous operation data.</param>
		/// <returns>
		///		<c>true</c>, if the error can be ignore, it is in shutdown which is initiated by another thread, for example; otherwise, <c>false</c>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="socket"/> is <c>null</c>.
		///		Or <paramref name="context"/> is <c>null</c>.
		/// </exception>
		/// <remarks>
		///		When this method returns <c>false</c>, <see cref="RpcServer.ServerError"/> event will be also ocurred.
		/// </remarks>
		protected internal bool HandleSocketError( Socket socket, SocketAsyncEventArgs context )
		{
			if ( socket == null )
			{
				throw new ArgumentNullException( "socket" );
			}

			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			Contract.EndContractBlock();

			bool? isError = context.SocketError.IsError();
			if ( isError == null )
			{
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.IgnoreableError,
					"Ignoreable error. {{ \"Socket\" : 0x{0:X}, \"RemoteEndpoint\" : \"{1}\", \"LocalEndpoint\" : \"{2}\", \"LastOperation\" : \"{3}\", \"SocketError\" : \"{4}\", \"ErrorCode\" : 0x{5:X} }}",
					ServerTransport.GetHandle( socket ),
					ServerTransport.GetRemoteEndPoint( socket, context ),
					ServerTransport.GetLocalEndPoint( socket ),
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
						ServerTransport.GetHandle( socket ),
						ServerTransport.GetRemoteEndPoint( socket, context ),
						ServerTransport.GetLocalEndPoint( socket ),
						context.LastOperation,
						context.SocketError,
						( int )context.SocketError
					);
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.SocketError,
					errorDetail
				);

				this.RaiseServerError( new RpcTransportException( context.SocketError.ToRpcError(), "Socket error.", errorDetail, new SocketException( ( int )context.SocketError ) ) );
				return false;
			}

			return true;
		}

		/// <summary>
		///		Invoked from the <see cref="ServerTransport"/> which was created by this manager,
		///		returns the transport to this manager.
		/// </summary>
		/// <param name="transport">The <see cref="ServerTransport"/> which was created by this manager.</param>
		internal abstract void ReturnTransport( ServerTransport transport );

		internal ServerResponseContext GetResponseContext( ServerRequestContext requestContext )
		{
			Contract.Requires( requestContext != null );
			Contract.Requires( requestContext.MessageId != null );
			Contract.Requires( requestContext.BoundTransport != null );
			Contract.Ensures( Contract.Result<ServerResponseContext>() != null );

			return this.GetResponseContext( requestContext.BoundTransport, requestContext.RemoteEndPoint, requestContext.SessionId, requestContext.MessageId.Value );
		}

		internal ServerResponseContext GetResponseContext( IContextBoundableTransport transport, EndPoint remoteEndPoint, long sessionId, int messageId )
		{
			Contract.Requires( transport != null );
			Contract.Requires( remoteEndPoint != null );
			Contract.Ensures( Contract.Result<ServerResponseContext>() != null );

			var result = this.ResponseContextPool.Borrow();
			result.MessageId = messageId;
			result.SessionId = sessionId;
			result.SetTransport( transport );
			result.RemoteEndPoint = remoteEndPoint;
			return result;
		}

		/// <summary>
		///		Returns the request context to the pool.
		/// </summary>
		/// <param name="context">The context to the pool.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="context"/> is  <c>null</c>.
		/// </exception>
		protected internal void ReturnRequestContext( ServerRequestContext context )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" ); 
			}

			Contract.EndContractBlock();

			context.Clear();
			context.UnboundTransport();
			this.RequestContextPool.Return( context );
		}

		/// <summary>
		///		Returns the response context to the pool.
		/// </summary>
		/// <param name="context">The response to the pool.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="context"/> is  <c>null</c>.
		/// </exception>
		protected internal void ReturnResponseContext( ServerResponseContext context )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			Contract.EndContractBlock();

			context.Clear();
			context.UnboundTransport();
			this.ResponseContextPool.Return( context );
		}
	}

	[ContractClassFor( typeof( ServerTransportManager ) )]
	internal abstract class ServerTransportManagerContract : ServerTransportManager
	{
		protected ServerTransportManagerContract() : base( null ) { }

		internal override void ReturnTransport( ServerTransport transport )
		{
			Contract.Requires( transport != null );
			Contract.Requires( Object.ReferenceEquals( transport.Manager, this ) );
		}
	}

}
