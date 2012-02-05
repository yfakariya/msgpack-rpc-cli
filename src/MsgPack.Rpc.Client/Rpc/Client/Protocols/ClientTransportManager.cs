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
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using MsgPack.Rpc.Protocols;
using System.Threading.Tasks;
using System.Net;

namespace MsgPack.Rpc.Client.Protocols
{
	public abstract class ClientTransportManager :  IDisposable
	{
		private readonly ObjectPool<ClientRequestContext> _requestContextPool;

		public ObjectPool<ClientRequestContext> RequestContextPool
		{
			get { return this._requestContextPool; }
		}

		private readonly ObjectPool<ClientResponseContext> _responseContextPool;

		public ObjectPool<ClientResponseContext> ResponseContextPool
		{
			get { return this._responseContextPool; }
		}

		private readonly RpcClientConfiguration _configuration;

		protected RpcClientConfiguration Configuration
		{
			get { return this._configuration; }
		}

		private int _isDisposed;

		protected bool IsDisposed
		{
			get { return Interlocked.CompareExchange( ref this._isDisposed, 0, 0 ) != 0; }
		}

		private int _isInShutdown;

		public bool IsInShutdown
		{
			get { return Interlocked.CompareExchange( ref this._isInShutdown, 0, 0 ) != 0; }
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

			this._configuration = configuration;
			this._requestContextPool = configuration.RequestContextPoolProvider( () => new ClientRequestContext(), configuration.CreateRequestContextPoolConfiguration() );
			this._responseContextPool = configuration.ResponseContextPoolProvider( () => new ClientResponseContext(), configuration.CreateResponseContextPoolConfiguration() );
		}

		public void Dispose()
		{
			this.Dispose( true );
			GC.SuppressFinalize( this );
		}

		protected void Dispose( bool disposing )
		{
			this.OnDisposing( disposing );

			if ( Interlocked.CompareExchange( ref this._isDisposed, 1, 0 ) == 0 )
			{
				this.DisposeCore( disposing );
			}

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
			Interlocked.Exchange( ref this._isInShutdown, 1 );
		}

		public Task<ClientTransport> ConnectAsync( EndPoint targetEndPoint )
		{
			if ( targetEndPoint == null )
			{
				throw new ArgumentNullException( "targetEndPoint" );
			}

			return this.ConnectAsyncCore( targetEndPoint );
		}

		protected abstract Task<ClientTransport> ConnectAsyncCore( EndPoint targetEndPoint );


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

		internal abstract void ReturnTransport( ClientTransport transport );
	}
}
