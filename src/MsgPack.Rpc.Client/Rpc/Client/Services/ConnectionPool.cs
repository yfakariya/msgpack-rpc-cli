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
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Globalization;

namespace MsgPack.Rpc.Protocols.Services
{
#warning TODO: Prevent connection leak using finalizer, or session pool might be more useful.
	public sealed class ConnectionPool : IDisposable
	{
		private readonly EndPoint _destination;
		private readonly RpcTransportProtocol _protocol;
		private readonly ClientEventLoop _eventLoop;
		// FIXME: Use internal socket-buffer pair (connection) instead of raw context.
		private readonly ConcurrentDictionary<RpcSocketAsyncEventArgs, LeaseStatus> _statusTable = new ConcurrentDictionary<RpcSocketAsyncEventArgs, LeaseStatus>();
		private readonly ConcurrentDictionary<RpcSocketAsyncEventArgs, Exception> _connectErrors = new ConcurrentDictionary<RpcSocketAsyncEventArgs, Exception>();
		private int _initializingSocketCount = 0;
		private int _isDisposed = 0;
		private readonly BlockingCollection<RpcSocketAsyncEventArgs> _availableSockets;
		private readonly CancellationTokenSource _cancellationTokenSource;
		private readonly int _maximum;
		private readonly int _minimum;
		private readonly object _allocationLock;

		public ConnectionPool( EndPoint destination, RpcTransportProtocol protocol, ClientEventLoop eventLoop, int minimumCount, int maximumCount )
		{
			if ( destination == null )
			{
				throw new ArgumentNullException( "destination" );
			}

			if ( eventLoop == null )
			{
				throw new ArgumentNullException( "eventLoop" );
			}

			if ( minimumCount < 0 )
			{
				throw new ArgumentOutOfRangeException( "minimumCount", "'minumumCount' cannot be negative." );
			}

			if ( maximumCount < 1 )
			{
				throw new ArgumentOutOfRangeException( "maximumCount", "'maximumCount' must be positive." );
			}

			if ( maximumCount < minimumCount )
			{
				throw new ArgumentException( "'maximumCount' cannot be lessor than 'minimumCount'.", "maximumCount" );
			}

			Contract.EndContractBlock();

			this._allocationLock = new object();
			this._cancellationTokenSource = new CancellationTokenSource();
			this._destination = destination;
			this._protocol = protocol;
			this._eventLoop = eventLoop;
			this._minimum = minimumCount;
			this._maximum = maximumCount;
			this._availableSockets = new BlockingCollection<RpcSocketAsyncEventArgs>( new ConcurrentQueue<RpcSocketAsyncEventArgs>(), maximumCount );
			this.AllocateMore();
		}

		public void Dispose()
		{
			if ( Interlocked.CompareExchange( ref this._isDisposed, 1, 0 ) == 0 )
			{
				try { }
				finally
				{

					this._cancellationTokenSource.Cancel();
					this._cancellationTokenSource.Dispose();
				}
			}
		}

		public sealed override string ToString()
		{
			return String.Format( CultureInfo.CurrentCulture, "{0}:{1}({2})", this.GetType(), this._destination, this._protocol );
		}

		public RpcSocketAsyncEventArgs Borrow( TimeSpan? timeout, CancellationToken cancellationToken )
		{
			if ( timeout.HasValue && timeout.Value.TotalMilliseconds < 0.0 )
			{
				throw new ArgumentOutOfRangeException( "timeout" );
			}

			if ( this._isDisposed != 0 )
			{
				throw new ObjectDisposedException( this.ToString() );
			}

			Contract.EndContractBlock();

			// TODO: stat based strategy
			var linkedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource( this._cancellationTokenSource.Token, cancellationToken );
			RpcSocketAsyncEventArgs result = null;

			// Initially, just take it without blocking.
			int milliSecondsTimeout = 0;
			do
			{
				try { }
				finally
				{
					if ( this._availableSockets.TryTake( out result, milliSecondsTimeout, linkedCancellationSource.Token ) )
					{
						if ( !this._statusTable.TryUpdate( result, LeaseStatus.InUse, LeaseStatus.Ready ) )
						{
							if ( this._statusTable[ result ] == LeaseStatus.Error )
							{
								try { }
								finally
								{
									LeaseStatus dummy;
									this._statusTable.TryRemove( result, out dummy );
									Exception error;
									if ( !this._connectErrors.TryRemove( result, out error ) )
									{
										throw new InvalidOperationException( "ConectionPool is in inconsistent state. Error detail of error socket is not found." );
									}
									else
									{
										throw error;
									}
								}
							}
							else
							{
								throw new InvalidOperationException( "ConectionPool is in inconsistent state. Bollowing socket is in use." );
							}
						}
					}
					else if ( linkedCancellationSource.Token.IsCancellationRequested )
					{
						throw new OperationCanceledException( linkedCancellationSource.Token );
					}
				}

				if ( result == null )
				{
					if ( !this.AllocateMore() )
					{
						// Retry with blocking using client specified timeout.
						milliSecondsTimeout = timeout == null ? Timeout.Infinite : ( int )timeout.Value.TotalMilliseconds;
					}
				}
			} while ( result == null );

			Contract.Assume( result.ConnectSocket != null );
			Contract.Assume( ( result as SocketAsyncEventArgs ).ConnectSocket.Connected );
			return result;
		}

		private bool AllocateMore()
		{
			if ( this._initializingSocketCount > 0 )
			{
				// Should wait initializing.
				return false;
			}

			int currentSocketCount = this._statusTable.Count;
			if ( currentSocketCount < this._minimum )
			{
				lock ( this._allocationLock )
				{
					if ( currentSocketCount < this._minimum )
					{
						for ( int i = 0; i < this._minimum; i++ )
						{
							this.IniaializeSocket();
						}

						return true;
					}
					else
					{
						// Should wait initializing.
						return false;
					}
				}
			}

			if ( currentSocketCount < this._maximum )
			{
				lock ( this._allocationLock )
				{
					if ( currentSocketCount < this._maximum )
					{
						// Allocate equals to "_minimum" new socket up to _maximum.
						for ( int i = 0; i < this._minimum && ( i + currentSocketCount ) < this._maximum; i++ )
						{
							this.IniaializeSocket();
						}

						return true;
					}
					else
					{
						// Should wait re-allocating.
						return false;
					}
				}
			}

			// Pool is max.
			return false;
		}

		private void IniaializeSocket()
		{
			bool success = false;
			RpcSocketAsyncEventArgs context = this._eventLoop.CreateSocketContext( this._destination );
			try
			{
				bool mustSuccess = this._statusTable.TryAdd( context, LeaseStatus.Uninitialized );
				Contract.Assert( mustSuccess );
				this._eventLoop.Connect(
					new ConnectingContext(
						new AsynchronousConnectHandler( context, new SimpleRpcSocket( this._protocol.CreateSocket() ) ),
						this
					)
				);
				success = true;
			}
			finally
			{
				if ( success )
				{
					bool mustSuccess = this._statusTable.TryUpdate( context, LeaseStatus.Initializing, LeaseStatus.Uninitialized );
					Contract.Assert( mustSuccess );
					Interlocked.Increment( ref this._initializingSocketCount );
				}
				else
				{
					LeaseStatus lastStatus;
					bool mustSuccess = this._statusTable.TryRemove( context, out lastStatus );
					Contract.Assert( mustSuccess );
					Contract.Assert( lastStatus == LeaseStatus.Uninitialized );
				}
			}
		}

		public void Return( RpcSocketAsyncEventArgs context )
		{
			this.Return( context, false );
		}

		private void Return( RpcSocketAsyncEventArgs context, bool isInFinalizer )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			if ( !this._statusTable.ContainsKey( context ) )
			{
				throw new ArgumentException( "Specified socket does not belong to this pool.", "context" );
			}

			Contract.EndContractBlock();

			// FIXME: Shrink buffer in context.

			try { }
			finally
			{
				if ( this._statusTable.TryUpdate( context, LeaseStatus.Ready, LeaseStatus.InUse ) )
				{
					this._availableSockets.Add( context );
				}
				else
				{
					if ( !isInFinalizer )
					{
						throw new InvalidOperationException( "ConectionPool is in inconsistent state. Returning socket is not in use." );
					}
				}
			}
		}

		private enum LeaseStatus
		{
			Uninitialized = 0,
			Initializing,
			Ready,
			InUse,
			Error
		}

		private sealed class AsynchronousConnectHandler : IAsyncConnectClient
		{
			private readonly RpcSocket _socketToConnect;
			private readonly RpcSocketAsyncEventArgs _context;

			public RpcSocket Socket
			{
				get { return this._socketToConnect; }
			}

			public RpcSocketAsyncEventArgs SocketContext
			{
				get { return this._context; }
			}

			public AsynchronousConnectHandler( RpcSocketAsyncEventArgs context, RpcSocket socketToConnect )
			{
				this._context = context;
				this._socketToConnect = socketToConnect;
			}

			public void OnConnectError( RpcError rpcError, Exception exception, bool completedSynchronously, object asyncState )
			{
				Contract.Assume( exception != null );

				var lastError = new RpcProtocolException( rpcError, exception.Message, null, exception );
				if ( completedSynchronously )
				{
					throw lastError;
				}
				else
				{
					var pool = asyncState as ConnectionPool;
					// TODO: trace
					try { }
					finally
					{
						if ( !pool._statusTable.TryUpdate( this._context, LeaseStatus.Error, LeaseStatus.Initializing ) )
						{
							throw new InvalidOperationException( "ConectionPool is in inconsistent state. Initialized socket is not ininitializing." );
						}

						if ( !pool._connectErrors.TryAdd( this._context, lastError ) )
						{
							throw new InvalidOperationException( "ConectionPool is in inconsistent state. Initialized socket is already in error." );
						}

						Interlocked.Decrement( ref pool._initializingSocketCount );
					}
				}

			}

			public void OnConnected( ConnectingContext context, bool completedSynchronously, object asyncState )
			{
				Contract.Assume( context.Client.SocketContext == this._context );
				Contract.Assume( context.Client.SocketContext.ConnectSocket != null );

				var pool = asyncState as ConnectionPool;
				try { }
				finally
				{
					if ( !pool._statusTable.TryUpdate( this._context, LeaseStatus.Ready, LeaseStatus.Initializing ) )
					{
						LeaseStatus lastStatus;
						if ( pool._statusTable.TryGetValue( this._context, out lastStatus ) )
						{
							throw new InvalidOperationException( "ConectionPool is in inconsistent state. Initialized socket is not ininitializing. Actual is :" + lastStatus );
						}
						else
						{
							throw new InvalidOperationException( "ConectionPool is in inconsistent state. Initialized socket is not recognized." );
						}
					}

					if ( !pool._availableSockets.TryAdd( this._context, 0 ) )
					{
						// overflow.
						this._socketToConnect.Shutdown( SocketShutdown.Both );
						this._socketToConnect.Dispose();
					}

					Interlocked.Decrement( ref pool._initializingSocketCount );
				}
			}
		}
	}
}
