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
using System.Net;
using System.Net.Sockets;
using System.Threading;
#if !WINDOWS_PHONE
using System.Threading.Tasks;
#endif
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc.Client.Protocols
{
	/// <summary>
	///		Defines non-generic interface of <see cref="ClientTransportManager{T}"/> and provides related features.
	/// </summary>
	public abstract class ClientTransportManager : IDisposable
	{
		private readonly ObjectPool<ClientRequestContext> _requestContextPool;

		/// <summary>
		///		Gets the <see cref="ObjectPool{T}"/> of <see cref="ClientRequestContext"/>.
		/// </summary>
		/// <value>
		///		The <see cref="ObjectPool{T}"/> of <see cref="ClientRequestContext"/>.
		///		This value will not be <c>null</c>.
		/// </value>
		protected ObjectPool<ClientRequestContext> RequestContextPool
		{
			get
			{
				Contract.Ensures( Contract.Result<ObjectPool<ClientRequestContext>>() != null );

				return this._requestContextPool;
			}
		}

		private readonly ObjectPool<ClientResponseContext> _responseContextPool;

		/// <summary>
		///		Gets the <see cref="ObjectPool{T}"/> of <see cref="ClientResponseContext"/>.
		/// </summary>
		/// <value>
		///		The <see cref="ObjectPool{T}"/> of <see cref="ClientResponseContext"/>.
		///		This value will not be <c>null</c>.
		/// </value>
		protected ObjectPool<ClientResponseContext> ResponseContextPool
		{
			get
			{
				Contract.Ensures( Contract.Result<ObjectPool<ClientResponseContext>>() != null );

				return this._responseContextPool;
			}
		}

		private readonly RpcClientConfiguration _configuration;

		/// <summary>
		///		Gets the <see cref="RpcClientConfiguration"/> which describes transport configuration.
		/// </summary>
		/// <value>
		///		The <see cref="RpcClientConfiguration"/> which describes transport configuration.
		///		This value will not be <c>null</c>.
		/// </value>
		protected internal RpcClientConfiguration Configuration
		{
			get
			{
				Contract.Ensures( Contract.Result<RpcClientConfiguration>() != null );

				return this._configuration;
			}
		}

		private int _isDisposed;

		/// <summary>
		///		Gets a value indicating whether this instance is disposed.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is disposed; otherwise, <c>false</c>.
		/// </value>
		public bool IsDisposed
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

		private EventHandler<ShutdownCompletedEventArgs> _shutdownCompleted;

		/// <summary>
		///		Occurs when client shutdown is completed.
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

			var handler = Interlocked.CompareExchange( ref this._shutdownCompleted, null, null );
			if ( handler != null )
			{
				handler( this, e );
			}

			Interlocked.Exchange( ref this._isInShutdown, 0 );
		}

		/// <summary>
		///		Occurs when unknown response received.
		/// </summary>
		/// <remarks>
		///		When the client restart between the server accepts request and sends response,
		///		the orphan message might be occurred.
		/// </remarks>
		public event EventHandler<UnknownResponseReceivedEventArgs> UnknownResponseReceived;

		/// <summary>
		///		Raises the <see cref="E:UnknownResponseReceived"/> event.
		/// </summary>
		/// <param name="e">The <see cref="MsgPack.Rpc.Client.Protocols.UnknownResponseReceivedEventArgs"/> instance containing the event data.</param>
		protected virtual void OnUnknownResponseReceived( UnknownResponseReceivedEventArgs e )
		{
			if ( e == null )
			{
				throw new ArgumentNullException( "e" );
			}

			Contract.EndContractBlock();

			var handler = Interlocked.CompareExchange( ref this.UnknownResponseReceived, null, null );
			if ( handler != null )
			{
				handler( this, e );
			}
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="ClientTransportManager"/> class.
		/// </summary>
		/// <param name="configuration">
		///		The <see cref="RpcClientConfiguration"/> which contains various configuration information.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="configuration"/> is <c>null</c>.
		/// </exception>
		protected ClientTransportManager( RpcClientConfiguration configuration )
		{
			if ( configuration == null )
			{
				throw new ArgumentNullException( "configuration" );
			}

			Contract.EndContractBlock();

			this._configuration = configuration;
			this._requestContextPool = configuration.RequestContextPoolProvider( () => new ClientRequestContext( configuration ), configuration.CreateRequestContextPoolConfiguration() );
			this._responseContextPool = configuration.ResponseContextPoolProvider( () => new ClientResponseContext( configuration ), configuration.CreateResponseContextPoolConfiguration() );
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
		///		Releases unmanaged and - optionally - managed resources.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		private void DisposeOnce( bool disposing )
		{
			if ( Interlocked.CompareExchange( ref this._isDisposed, 1, 0 ) == 0 )
			{
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
		///		Initiates client shutdown.
		/// </summary>
		/// <returns>
		///		If shutdown process is initiated, then <c>true</c>.
		///		If shutdown is already initiated or completed, then <c>false</c>.
		/// </returns>
		public bool BeginShutdown()
		{
			if ( Interlocked.Exchange( ref this._isInShutdown, 1 ) == 0 )
			{
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
		protected virtual void BeginShutdownCore()
		{
			// nop
		}

		/// <summary>
		///		Establishes logical connection, which specified to the managed transport protocol, for the server.
		/// </summary>
		/// <param name="targetEndPoint">The end point of target server.</param>
		/// <returns>
		///		<see cref="Task{T}"/> of <see cref="ClientTransport"/> which represents asynchronous establishment process
		///		specific to the managed transport.
		///		This value will not be <c>null</c>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="targetEndPoint"/> is <c>null</c>.
		/// </exception>
		public Task<ClientTransport> ConnectAsync( EndPoint targetEndPoint )
		{
			if ( targetEndPoint == null )
			{
				throw new ArgumentNullException( "targetEndPoint" );
			}

			Contract.Ensures( Contract.Result<Task<ClientTransport>>() != null );

			return this.ConnectAsyncCore( targetEndPoint );
		}

		/// <summary>
		///		Establishes logical connection, which specified to the managed transport protocol, for the server.
		/// </summary>
		/// <param name="targetEndPoint">The end point of target server.</param>
		/// <returns>
		///		<see cref="Task{T}"/> of <see cref="ClientTransport"/> which represents asynchronous establishment process
		///		specific to the managed transport.
		///		This value will not be <c>null</c>.
		/// </returns>
		protected abstract Task<ClientTransport> ConnectAsyncCore( EndPoint targetEndPoint );

		/// <summary>
		///		Handles socket error.
		/// </summary>
		/// <param name="socket">The <see cref="Socket"/> which might cause socket error.</param>
		/// <param name="context">The <see cref="SocketAsyncEventArgs"/> which holds actual error information.</param>
		/// <returns>
		///		<see cref="RpcErrorMessage"/> corresponds for the socket error.
		///		<c>null</c> if the operation result is not socket error.
		/// </returns>
		[SuppressMessage( "Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Logically be instance method." )]
		protected internal RpcErrorMessage? HandleSocketError( Socket socket, SocketAsyncEventArgs context )
		{
			if ( context.SocketError.IsError() == false )
			{
				return null;
			}

			MsgPackRpcClientProtocolsTrace.TraceEvent(
				MsgPackRpcClientProtocolsTrace.SocketError,
				"Socket error. {{ \"Socket\" : 0x{0:X}, \"RemoteEndpoint\" : \"{1}\", \"LocalEndpoint\" : \"{2}\", \"LastOperation\" : \"{3}\", \"SocketError\" : \"{4}\", \"ErrorCode\" : 0x{5:X} }}",
				ClientTransport.GetHandle( socket ),
				ClientTransport.GetRemoteEndPoint( socket, context ),
				ClientTransport.GetLocalEndPoint( socket ),
				context.LastOperation,
				context.SocketError,
				( int )context.SocketError
			);

			return context.SocketError.ToClientRpcError();
		}

		/// <summary>
		///		Returns specified <see cref="ClientTransport"/> to the internal pool.
		/// </summary>
		/// <param name="transport">The <see cref="ClientTransport"/> to be returned.</param>
		internal abstract void ReturnTransport( ClientTransport transport );

		internal void HandleOrphan( int? messageId, RpcErrorMessage rpcError, MessagePackObject? returnValue )
		{
			this.OnUnknownResponseReceived( new UnknownResponseReceivedEventArgs( messageId, rpcError, returnValue ) );
		}

		internal ClientRequestContext GetRequestContext( ClientTransport transport )
		{
			Contract.Requires( transport != null );
			Contract.Ensures( Contract.Result<ClientRequestContext>() != null );

			var result = this.RequestContextPool.Borrow();
			result.SetTransport( transport );
			result.RenewSessionId();
			return result;
		}

		internal ClientResponseContext GetResponseContext( ClientTransport transport, EndPoint remoteEndPoint )
		{
			Contract.Requires( transport != null );
			Contract.Requires( remoteEndPoint != null );
			Contract.Ensures( Contract.Result<ClientResponseContext>() != null );

			var result = this.ResponseContextPool.Borrow();
			result.RenewSessionId();
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
		protected internal void ReturnRequestContext( ClientRequestContext context )
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
		protected internal void ReturnResponseContext( ClientResponseContext context )
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

		/// <summary>
		///		Starts the connect timeout watching.
		/// </summary>
		/// <param name="onTimeout">A callback to be invoked when the timeout occurrs.</param>
		/// <returns>A <see cref="ConnectTimeoutWatcher"/> for connect timeout watching.</returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="onTimeout"/> is <c>null</c>.
		/// </exception>
		protected ConnectTimeoutWatcher BeginConnectTimeoutWatch( Action onTimeout )
		{
			if ( onTimeout == null )
			{
				throw new ArgumentNullException( "onTimeout" );
			}

			Contract.Ensures( Contract.Result<ConnectTimeoutWatcher>() != null );

			if ( this._configuration.ConnectTimeout == null )
			{
				return NullConnectTimeoutWatcher.Instance;
			}
			else
			{
				return new DefaultConnectTimeoutWatcher( this._configuration.ConnectTimeout.Value, onTimeout );
			}
		}

		/// <summary>
		///		Ends the connect timeout watching.
		/// </summary>
		/// <param name="watcher">The <see cref="ConnectTimeoutWatcher"/>.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="watcher"/> is <c>null</c>.
		/// </exception>
		[SuppressMessage( "Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Logically be instance method." )]
		protected void EndConnectTimeoutWatch( ConnectTimeoutWatcher watcher )
		{
			if ( watcher == null )
			{
				throw new ArgumentNullException( "watcher" );
			}

			Contract.EndContractBlock();

			watcher.Dispose();
		}

		/// <summary>
		///		Helps connection timeout watching.
		/// </summary>
		protected abstract class ConnectTimeoutWatcher : IDisposable
		{
			internal ConnectTimeoutWatcher() { }

			/// <summary>
			///		Stops watching and release internal resources.
			/// </summary>
			public void Dispose()
			{
				this.Dispose( true );
				GC.SuppressFinalize( this );
			}

			/// <summary>
			/// Releases unmanaged and - optionally - managed resources
			/// </summary>
			/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
			protected virtual void Dispose( bool disposing )
			{
				// nop
			}
		}

		private sealed class NullConnectTimeoutWatcher : ConnectTimeoutWatcher
		{
			public static readonly NullConnectTimeoutWatcher Instance = new NullConnectTimeoutWatcher();

			private NullConnectTimeoutWatcher() { }
		}

		private sealed class DefaultConnectTimeoutWatcher : ConnectTimeoutWatcher
		{
			private readonly TimeoutWatcher _watcher;

			public DefaultConnectTimeoutWatcher( TimeSpan timeout, Action onTimeout )
			{
				var watcher = new TimeoutWatcher();
				watcher.Timeout += ( sender, e ) => onTimeout();
				Interlocked.Exchange( ref this._watcher, watcher );
				watcher.Start( timeout );
			}

			protected override void Dispose( bool disposing )
			{
				if ( disposing )
				{
					this._watcher.Stop();
					this._watcher.Dispose();
				}

				base.Dispose( disposing );
			}
		}
	}
}
