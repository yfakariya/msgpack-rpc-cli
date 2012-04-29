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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
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
		public ObjectPool<ClientRequestContext> RequestContextPool
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
		public ObjectPool<ClientResponseContext> ResponseContextPool
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
			this._requestContextPool = configuration.RequestContextPoolProvider( () => new ClientRequestContext(), configuration.CreateRequestContextPoolConfiguration() );
			this._responseContextPool = configuration.ResponseContextPoolProvider( () => new ClientResponseContext(), configuration.CreateResponseContextPoolConfiguration() );
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
		///		Releases unmanaged and - optionally - managed resources.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected void Dispose( bool disposing )
		{
			if ( Interlocked.CompareExchange( ref this._isDisposed, 1, 0 ) == 0 )
			{
				this.DisposeCore( disposing );
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
		protected virtual void DisposeCore( bool disposing ) { }

		/// <summary>
		///		Initiates client shutdown.
		/// </summary>
		public void BeginShutdown()
		{
			if ( Interlocked.Exchange( ref this._isInShutdown, 1 ) == 0 )
			{
				this.BeginShutdownCore();
			}
		}

		/// <summary>
		///		When overridden in derived class, initiates protocol specific shutdown process.
		/// </summary>
		protected virtual void BeginShutdownCore()
		{
			
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
		protected internal RpcErrorMessage? HandleSocketError( Socket socket, SocketAsyncEventArgs context )
		{
			if ( context.SocketError.IsError() == false )
			{
				return null;
			}

			EndPoint remoteEndPoint = null;
			try
			{
				if ( socket != null )
				{
					remoteEndPoint = socket.RemoteEndPoint;
				}
			}
			catch ( SocketException ) { }

			MsgPackRpcClientProtocolsTrace.TraceEvent(
				MsgPackRpcClientProtocolsTrace.SocketError,
				"Socket error. {{ \"Socket\" : 0x{0:X}, \"RemoteEndpoint\" : \"{1}\", \"LocalEndpoint\" : \"{2}\", \"LastOperation\" : \"{3}\", \"SocketError\" : \"{4}\", \"ErrorCode\" : 0x{5:X} }}",
				socket == null ? IntPtr.Zero : socket.Handle,
				remoteEndPoint,
				socket == null ? null : socket.LocalEndPoint,
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

		internal void HandleOrphan( int? messageId, long sessionId, RpcErrorMessage rpcError, MessagePackObject? returnValue )
		{
			this.OnUnknownResponseReceived( new UnknownResponseReceivedEventArgs( messageId, rpcError, returnValue ) );
		}
	}
}
