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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using MsgPack.Collections.Concurrent;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		<see cref="ClientEventLoop"/> implementation using message queing and <see cref="Socket.Poll"/>.
	/// </summary>
	public sealed class PollingClientEventLoop : ClientEventLoop
	{
		private readonly BlockingCollection<ClientSocketAsyncEventArgs> _dummy;
		private readonly NotifiableBlockingCollection<ClientSocketAsyncEventArgs> _connectingQueue;
		private readonly NotifiableBlockingCollection<ClientSocketAsyncEventArgs> _sendingQueue;
		private readonly Thread _inBoundPollingThread;
		private readonly CancellationTokenSource _cancellationTokenSource;

		public PollingClientEventLoop( Func<ClientSessionContext, RpcClient> sessionFactory, RpcClientOptions options, EventHandler<RpcTransportErrorEventArgs> errorHandler )
			: base( sessionFactory, options, errorHandler )
		{
			this._cancellationTokenSource = new CancellationTokenSource();

			int connectingConcurrency =
				options == null
				? Environment.ProcessorCount / 5 + 1
				: ( options.ConnectingConcurrency ?? Environment.ProcessorCount / 5 + 1 );
			int sendingConcurrency =
				options == null
				? ( Environment.ProcessorCount / 5 + 1 ) * 2
				: ( options.SendingConcurrency ?? ( Environment.ProcessorCount / 5 + 1 ) * 2 );
			int receivingConcurrency =
				options == null
				? ( Environment.ProcessorCount / 5 + 1 ) * 2
				: ( options.ReceivingConcurrency ?? ( Environment.ProcessorCount / 5 + 1 ) * 2 );

			this._connectingQueue =
				new NotifiableBlockingCollection<ClientSocketAsyncEventArgs>(
					new ConcurrentQueue<ClientSocketAsyncEventArgs>(),
					options == null
					? connectingConcurrency * 4
					: ( options.ConnectingQueueLength ?? connectingConcurrency * 4 )
				);
			this._sendingQueue =
				new NotifiableBlockingCollection<ClientSocketAsyncEventArgs>(
					new ConcurrentQueue<ClientSocketAsyncEventArgs>(),
					options == null
					? sendingConcurrency * 4
					: ( options.SendingQueueLength ?? sendingConcurrency * 4 )
				);

			for ( int i = 0; i < connectingConcurrency; i++ )
			{
				this.BeginWaitForOnConnecting();
			}

			for ( int i = 0; i < sendingConcurrency; i++ )
			{
				this.BeginWaitForOnSending();
			}

			this._inBoundPollingThread = new Thread( this.PollInBound );
			this._inBoundPollingThread.IsBackground = true;
			this._inBoundPollingThread.Name =
				String.Format( CultureInfo.InvariantCulture, "{0}({1}).InBoundPollingThread", this.GetType().Name, this.GetHashCode() );
			this._inBoundPollingThread.Start( this._cancellationTokenSource.Token );
		}

		private void BeginWaitForOnConnecting()
		{
			var cancellationTokenHolder = new CancellationTokenHolder( this._cancellationTokenSource.Token );
			var registeredWaitHandle =
				ThreadPool.RegisterWaitForSingleObject(
					this._connectingQueue.ConsumerWaitHandle,
					this.OnConnecting,
					cancellationTokenHolder,
					Timeout.Infinite,
					false
				);
			var token = cancellationTokenHolder.Token;
			token.Register(
				handle => ( handle as RegisteredWaitHandle ).Unregister( null ),
				registeredWaitHandle
			);
			cancellationTokenHolder.Token = token;
		}

		private void BeginWaitForOnSending()
		{
			var cancellationTokenHolder = new CancellationTokenHolder( this._cancellationTokenSource.Token );
			var registeredWaitHandle =
				ThreadPool.RegisterWaitForSingleObject(
					this._sendingQueue.ConsumerWaitHandle,
					this.OnSending,
					cancellationTokenHolder,
					Timeout.Infinite,
					false
				);
			var token = cancellationTokenHolder.Token;
			token.Register(
				handle => ( handle as RegisteredWaitHandle ).Unregister( null ),
				registeredWaitHandle
			);
		}

		protected sealed override void ConnectCore( ClientSocketAsyncEventArgs context, Action<ClientSocketAsyncEventArgs, object> callerOnConnected, object asyncState )
		{
			context.UserToken = Tuple.Create( context, callerOnConnected, asyncState );
			bool dummy;
			this._connectingQueue.Add( context, this._cancellationTokenSource.Token, out dummy );
		}

		private void OnConnecting( object state, bool timedOut )
		{
			var cancellationToken = state as CancellationTokenHolder;
			try
			{
				if ( cancellationToken.Token.IsCancellationRequested )
				{
					return;
				}

				ClientSocketAsyncEventArgs e;
				this._connectingQueue.Take( cancellationToken.Token, out e );
				var tuple = e.UserToken as Tuple<ClientSocketAsyncEventArgs, Action<ClientSocketAsyncEventArgs, object>, object>;
				try
				{
					tuple.Item1.Connect( e.RemoteEndPoint );
					e.ContextSocket = tuple.Item1;
					Contract.Assert( Object.ReferenceEquals( e.UserToken, tuple ) );
					e.LastOperation = SocketAsyncOperation.Connect;
				}
				catch ( SocketException ex )
				{
					e.SocketError = ex.SocketErrorCode;
				}

				if ( cancellationToken.Token.IsCancellationRequested )
				{
					return;
				}

				this.OnConnected( e );
			}
			catch ( OperationCanceledException ) { }
		}

		protected sealed override void OnConnected( ClientSocketAsyncEventArgs e )
		{
			Contract.Assert( e != null );
			var tuple = e.UserToken as Tuple<Socket, Action<RpcClient, object>, object>;
			Contract.Assert( tuple != null );
			base.OnConnectedCore( e, tuple.Item2, tuple.Item3 );
		}

		//protected override void SendCore( ClientSocketAsyncEventArgs e, int? messageId, ClientTransport transport, IAsyncSessionErrorSink errorSink )
		protected sealed override void SendCore( RpcClient session, int? messageId, IAsyncSessionErrorSink errorSink )
		{
			Contract.Assert( session != null );
			bool added = false;
			try
			{
				//Contract.Assert( session.SendingContext != null );
				session.SendingContext.UserToken = Tuple.Create( messageId, session, errorSink );
				this._sendingQueue.Add( session, session.CancellationToken, out added );
			}
			finally
			{
				if ( added )
				{
					session.ReceivingContext.SetBuffer( new byte[ this.ReceiveBufferSize ], 0, this.ReceiveBufferSize );
					//session.ReceivingContext.UserToken = this._cancellationTokenSource.Token;

					//lock ( this._pendingContexts )
					lock ( this._pendingSession )
					{
						this._pendingSession.Add( session );
						//this._pendingContexts.Add( e );
					}
				}
			}
		}

		private readonly List<RpcClient> _pendingSession;
		private readonly List<ClientSocketAsyncEventArgs> _pendingContexts;
		private static readonly Socket[] _emptySockets = new Socket[ 0 ];

		private void OnSending( object state, bool timedOut )
		{
			var cancellationToken = state as CancellationTokenHolder;
			Contract.Assert( cancellationToken != null );
			try
			{
				if ( cancellationToken.Token.IsCancellationRequested )
				{
					// shutdown
					return;
				}

				ClientSocketAsyncEventArgs e;
				this._connectingQueue.Take( cancellationToken.Token, out e );
				var tuple = e.SendingContext.UserToken as Tuple<int?, IAsyncSessionErrorSink>;
				e.UserToken = null;
				try
				{
					SocketError error;
					e.ContextSocket.Send( e.SendingContext.Buffer, e.SendingContext.Offset, e.SendingContext.Count, e.SendingContext.SocketFlags, out error );
					e.SendingContext.SocketError = error;
				}
				catch ( SocketException ex )
				{
					e.SendingContext.SocketError = ex.SocketErrorCode;
				}

				if ( e.SendingContext.SocketError != System.Net.Sockets.SocketError.Success )
				{
					base.OnSendError( e, tuple.Item1, tuple.Item2, false );
				}
			}
			catch ( OperationCanceledException ) { } // shutdown
		}

		private void PollInBound( object state )
		{
			try
			{
				var cancellationToken = ( CancellationToken )state;
				while ( !cancellationToken.IsCancellationRequested )
				{
					List<Socket> pendingSockets;
					lock ( this._pendingContexts )
					{
						pendingSockets = this._pendingContexts.Select( context => context.ContextSocket ).ToList();
					}

					if ( cancellationToken.IsCancellationRequested )
					{
						// shutdown
						return;
					}

					try
					{
						Socket.Select( pendingSockets, _emptySockets, _emptySockets, Timeout.Infinite );
					}
					catch ( ThreadInterruptedException )
					{
						// shutdown
						return;
					}


					if ( cancellationToken.IsCancellationRequested )
					{
						// shutdown
						return;
					}

					foreach ( var context in this._pendingContexts )
					{
						if ( context.ContextSocket.Available > 0 )
						{
							ThreadPool.QueueUserWorkItem( this.OnReceiving, Tuple.Create( context, cancellationToken ) );
						}
					}
				}
			}
			catch ( OperationCanceledException ) { }
		}

		private void OnReceiving( object state )
		{
			var tuple = state as Tuple<ClientSocketAsyncEventArgs, CancellationToken>;
			Contract.Assert( tuple != null );

			try
			{
				tuple.Item1.ResetReceivingBuffer();

				do
				{
					if ( tuple.Item2.IsCancellationRequested )
					{
						// shutdown
						return;
					}

					SocketError error;
					tuple.Item1.ContextSocket.Receive( tuple.Item1.Buffer, tuple.Item1.Offset, tuple.Item1.Count, tuple.Item1.SocketFlags, out error );
					tuple.Item1.SocketError = error;
					tuple.Item1.LastOperation = SocketAsyncOperation.Receive;

					if ( tuple.Item2.IsCancellationRequested )
					{
						// shutdown
						return;
					}

					if ( tuple.Item1.SocketError != System.Net.Sockets.SocketError.Success )
					{
						this.HandleError( tuple.Item1.LastOperation, tuple.Item1.SocketError );
						return;
					}

					// TODO: streaming w/ BlockingCollection?( base.OnReceived invoke via ThreadPool)
					var feeding = new byte[ tuple.Item1.Buffer.Length - tuple.Item1.Offset ];
					Buffer.BlockCopy( tuple.Item1.Buffer, tuple.Item1.Offset, feeding, 0, tuple.Item1.BytesTransferred );
					tuple.Item1.AppendRecivingBuffer( feeding, 0, tuple.Item1.BytesTransferred );

				} while ( tuple.Item1.ContextSocket.Available > 0 );

				this.OnReceived( false, tuple.Item1 );
			}
			catch ( OperationCanceledException ) { }
		}

		private sealed class CancellationTokenHolder
		{
			public CancellationToken Token { get; set; }

			public CancellationTokenHolder( CancellationToken token )
			{
				this.Token = token;
			}
		}
	}

}
