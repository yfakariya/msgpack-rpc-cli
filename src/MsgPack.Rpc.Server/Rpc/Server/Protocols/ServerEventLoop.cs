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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MsgPack.Rpc.Protocols
{

	/// <summary>
	///		Provides common interfaces and basic features of server side <see cref="EventLoop"/>.
	/// </summary>
	public abstract class ServerEventLoop : EventLoop, IDisposable
	{
		public static readonly int DefaultInitialSendBufferSize = 32 * 1024;
		public static readonly int DefaultReceiveBufferSize = 32 * 1024;
		public static readonly TimeSpan DefaultTimeoutWatchPeriod = TimeSpan.FromSeconds( 10 );
		public static readonly TimeSpan DefaultExecutionTimeout = TimeSpan.FromSeconds( 300 );

		private static readonly bool _canUseIOCompletionPortOnCli = DetermineCanUseIOCompletionPortOnCli();

		private static bool DetermineCanUseIOCompletionPortOnCli()
		{
			// TODO: silverlight/moonlight path...
			string windir = Environment.GetEnvironmentVariable( "windir" );
			if ( String.IsNullOrEmpty( windir ) )
			{
				return false;
			}

			string clrMSCorLibPath =
				Path.Combine(
					windir,
					"Microsoft.NET",
					"Framework" + ( IntPtr.Size == 8 ? "64" : String.Empty ),
					"v" + Environment.Version.Major + Environment.Version.Minor + Environment.Version.Build,
					"mscorlib.dll"
				);

			return String.Equals( typeof( object ).Assembly.Location, clrMSCorLibPath, StringComparison.OrdinalIgnoreCase );
		}

		private struct WorkerThreadInfo
		{
			public readonly DateTime StartTime;
			public readonly Thread WorkerThread;

			public WorkerThreadInfo( DateTime startTime, Thread workerThread )
			{
				this.StartTime = startTime;
				this.WorkerThread = workerThread;
			}
		}

		private static readonly object _executionTimeoutToken = new object();

		private bool _isRunning;
		private readonly ConcurrentDictionary<int, WorkerThreadInfo> _exeuctingWorkerThreadTable;
		private int _exeuctingWorkerThreadCount;
		private readonly Timer _timeoutWatchDog;
		private readonly ConcurrentDictionary<EndPoint, RpcServerSession> _sessionPool;
		private readonly Func<ServerSessionContext, RpcServerSession> _sessionFactory;
		private readonly RpcServerOptions _options;
		private readonly RpcTransportProtocol _protocol;

		public int AcceptConcurrency
		{
			get
			{
				return
					this._options == null
					? Environment.ProcessorCount
					: this._options.AcceptConcurrency ?? Environment.ProcessorCount;
			}
		}

		public int ReceiveBufferSize
		{
			get
			{
				return
					this._options == null
					? ServerEventLoop.DefaultReceiveBufferSize
					: ( this._options.ReceiveBufferSize ?? ServerEventLoop.DefaultReceiveBufferSize );
			}
		}

		public TimeSpan TimeoutWatchPeriod
		{
			get
			{
				return
					this._options == null
					? ServerEventLoop.DefaultTimeoutWatchPeriod
					: ( this._options.TimeoutWatchPeriod ?? ServerEventLoop.DefaultTimeoutWatchPeriod );
			}
		}

		public TimeSpan ExecutionTimeout
		{
			get
			{
				return
					this._options == null
					? ServerEventLoop.DefaultExecutionTimeout
					: ( this._options.ExecutionTimeout ?? ServerEventLoop.DefaultExecutionTimeout );
			}
		}

		public DateTime ExecutionTimeLimit
		{
			get
			{
				WorkerThreadInfo info;
				if ( !this._exeuctingWorkerThreadTable.TryGetValue( Thread.CurrentThread.ManagedThreadId, out info ) )
				{
					throw new InvalidOperationException( "Current thread is not worker thread." );
				}

				return info.StartTime;
			}
		}

		protected ServerEventLoop( Func<ServerSessionContext, RpcServerSession> sessionFactory, RpcTransportProtocol protocol, RpcServerOptions options, EventHandler<RpcTransportErrorEventArgs> errorHandler )
			: base( errorHandler )
		{
			if ( sessionFactory == null )
			{
				throw new ArgumentNullException( "sessionFactory" );
			}

			Contract.EndContractBlock();

			this._sessionFactory = sessionFactory;
			this._options = options;
			this._exeuctingWorkerThreadTable = new ConcurrentDictionary<int, WorkerThreadInfo>();
			this._sessionPool = new ConcurrentDictionary<EndPoint, RpcServerSession>();
			this._timeoutWatchDog = new Timer( this.CheckTimeout, null, ( long )this.TimeoutWatchPeriod.TotalMilliseconds, Timeout.Infinite );
		}

		protected override void Dispose( bool disposing )
		{
			this._timeoutWatchDog.Dispose();
			base.Dispose( disposing );
		}

		private void CheckTimeout( object state )
		{
			this._timeoutWatchDog.Change( Timeout.Infinite, Timeout.Infinite );
			var now = DateTime.UtcNow;
			List<int> targets = new List<int>();

			// take keys snapshot to timeout.
			foreach ( var entry in this._exeuctingWorkerThreadTable )
			{
				if ( entry.Value.StartTime - now > this.ExecutionTimeout )
				{
					targets.Add( entry.Key );
				}
			}

			foreach ( var targetThreadId in targets )
			{
				WorkerThreadInfo targetWorkerThread;
				if ( this._exeuctingWorkerThreadTable.TryRemove( targetThreadId, out targetWorkerThread ) )
				{
					if ( targetWorkerThread.StartTime - now > this.ExecutionTimeout )
					{
						targetWorkerThread.WorkerThread.Abort( _executionTimeoutToken );
					}
					else
					{
						// race condition, restore
						this._exeuctingWorkerThreadTable[ targetWorkerThread.WorkerThread.ManagedThreadId ] = targetWorkerThread;
					}
				}
			}
		}

		protected RpcServerSession GetSession( EndPoint remoteEndPoint )
		{
			if ( remoteEndPoint == null )
			{
				throw new ArgumentNullException( "remoteEndPoint" );
			}

			Contract.EndContractBlock();

			RpcServerSession value;
			if ( !this._sessionPool.TryGetValue( remoteEndPoint, out value ) )
			{
				return null;
			}

			return value;
		}

		protected RpcServerSession CreateSession( EndPoint remoteEndPoint, ServerSessionContext context )
		{
			if ( remoteEndPoint == null )
			{
				throw new ArgumentNullException( "remoteEndPoint" );
			}

			if ( !context.IsValid )
			{
				throw new ArgumentException( "'context' is invalid.", "context" );
			}

			Contract.EndContractBlock();

			var newSesson = this._sessionFactory( context );
			var result = this._sessionPool.AddOrUpdate( remoteEndPoint, newSesson, ( oldKey, oldValue ) => oldValue );
			//if ( Object.ReferenceEquals( newSesson, result ) )
			//{
			//    newSesson.Start();
			//}
			//else
			//{
			//    newSesson.Dispose();
			//}
			return result;
		}

		protected RpcServerSession RemoveSession( EndPoint remoteEndPoint )
		{
			if ( remoteEndPoint == null )
			{
				throw new ArgumentNullException( "remoteEndPoint" );
			}

			Contract.EndContractBlock();

			RpcServerSession session;
			if ( this._sessionPool.TryRemove( remoteEndPoint, out session ) )
			{
				return session;
			}
			else
			{
				return null;
			}
		}

		public void Start( EndPoint localEndpoint )
		{
			int acceptConcurrency = this.AcceptConcurrency;

			this._isRunning = true;

			for ( int i = 0; i < acceptConcurrency; i++ )
			{
				var socket = new Socket( this._protocol.AddressFamily, this._protocol.SocketType, this._protocol.ProtocolType );
				socket.Bind( localEndpoint );
				var e = new ServerSocketAsyncEventArgs( this.OnAcceptted, this.OnReceived, this.HandleError );
				this.StartCore( socket, e );
			}
		}

		protected virtual void StartCore( Socket socket, ServerSocketAsyncEventArgs context )
		{
			this.AcceptAsync( socket, context );
		}

		public void Stop()
		{
			this._isRunning = false;
		}

		protected virtual void AcceptAsync( Socket socket, ServerSocketAsyncEventArgs context )
		{
			context.AcceptSocket = socket;
			// TODO: use streaming buffer
			var buffer = new byte[ this.ReceiveBufferSize ];
			context.SetBuffer( buffer, 0, buffer.Length );
			context.SendingContext = new SocketAsyncEventArgs();
			if ( !context.AcceptSocket.AcceptAsync( context ) )
			{
				this.OnAcceptted( context );
			}
		}

		protected virtual void OnAcceptted( ServerSocketAsyncEventArgs e )
		{
			do
			{
				this.HandleError( e.LastOperation, e.SocketError );
				// Drain
				e.ResetReceivingBuffer();
				e.AppendRecivingBuffer( e.Buffer, e.Offset, e.BytesTransferred );
				while ( e.AcceptSocket.Available > 0 )
				{
					// TODO: use buffer manager
					SocketError error;
					e.AcceptSocket.Receive( e.Buffer, e.Offset, e.Count, e.SocketFlags, out error );
					this.HandleError( SocketAsyncOperation.Receive, error );

					var feeding = new byte[ e.Buffer.Length - e.Offset ];
					Buffer.BlockCopy( e.Buffer, e.Offset, feeding, 0, e.BytesTransferred );
					e.AppendRecivingBuffer( feeding, 0, e.BytesTransferred );
				}

				ThreadPool.QueueUserWorkItem(
					state => this.OnReceived( e as ServerSocketAsyncEventArgs ),
					e
				);

				if ( !this._isRunning )
				{
					return;
				}

			} while ( !e.AcceptSocket.AcceptAsync( e ) );
		}

		protected virtual void OnReceived( ServerSocketAsyncEventArgs e )
		{
			var session = this.GetSession( e.RemoteEndPoint );
			if ( session == null )
			{
				session = this.CreateSession( e.RemoteEndPoint, new ServerSessionContext( e ) );
			}

			var workerThredInfo = new WorkerThreadInfo( DateTime.UtcNow, Thread.CurrentThread );
			if ( !this._exeuctingWorkerThreadTable.TryAdd( Thread.CurrentThread.ManagedThreadId, workerThredInfo ) )
			{
				Environment.FailFast( String.Format( CultureInfo.CurrentCulture, "Startig worker thread {0} is marked running.", Thread.CurrentThread.ManagedThreadId ) );
			}

			try { }
			finally
			{
				if ( Interlocked.Increment( ref this._exeuctingWorkerThreadCount ) == 1 )
				{
					this._timeoutWatchDog.Change( ( long )this.TimeoutWatchPeriod.TotalMilliseconds, Timeout.Infinite );
				}
			}

			try
			{
				session.Transport.OnReceived( session );
			}
			catch ( ThreadAbortException ex )
			{
				if ( ex.ExceptionState == _executionTimeoutToken )
				{
					Thread.ResetAbort();
				}
			}
			finally
			{
				if ( Interlocked.Decrement( ref this._exeuctingWorkerThreadCount ) == 0 )
				{
					this._timeoutWatchDog.Change( Timeout.Infinite, Timeout.Infinite );
				}

				WorkerThreadInfo disposal;
				this._exeuctingWorkerThreadTable.TryRemove( Thread.CurrentThread.ManagedThreadId, out disposal );
				Contract.Assert( disposal.WorkerThread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId );
			}
		}

		public void SendAsync( ServerSessionContext context, IEnumerable<byte> response )
		{
			Contract.Assert( context.Underlying.SendingContext != null );
			// TODO: streaming.
			var buffer = response.ToArray();
			context.Underlying.SendingContext.SetBuffer( buffer, 0, buffer.Length );
			if ( !context.Underlying.ConnectSocket.SendAsync( context.Underlying.SendingContext ) )
			{
				// Just check error.
				this.HandleError( context.Underlying.LastOperation, context.Underlying.SocketError );
			}
		}

		public void HandleError( RpcTransportErrorEventArgs e )
		{
			throw new NotImplementedException();
		}
	}
}
