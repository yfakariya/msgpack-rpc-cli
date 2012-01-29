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
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace MsgPack.Rpc.Server.Protocols
{
	/// <summary>
	///		Implements common features of <see cref="ServerTransportManager"/>.
	/// </summary>
	/// <typeparam name="TTransport">The type of <see cref="ServerTransport"/>s which are managed by this class.</typeparam>
	public abstract class ServerTransportManager<TTransport> : ServerTransportManager
		where TTransport : ServerTransport
	{
		private readonly ConcurrentDictionary<TTransport, object> _activeTransports;

#if DEBUG
		internal TTransport[] DebugGetActiveTransports()
		{
			return this._activeTransports.Keys.ToArray();
		}
#endif

		private ObjectPool<TTransport> _transportPool;

		/// <summary>
		///		Gets a value indicating whether the transport pool is set.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if the transport pool is set; otherwise, <c>false</c>.
		/// </value>
		/// <remarks>
		///		To set transport pool, invoke <see cref="SetTransportPool"/> method.
		/// </remarks>
		protected bool IsTransportPoolSet
		{
			get { return this._transportPool != null; }
		}

		private int _tranportIsInShutdown;

		/// <summary>
		///		Initializes a new instance of the <see cref="ServerTransportManager{T}"/> class.
		/// </summary>
		/// <param name="server">The server which will host this instance.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="server"/> is <c>null</c>.
		/// </exception>
		protected ServerTransportManager( RpcServer server )
			: base( server )
		{
			this._activeTransports =
				new ConcurrentDictionary<TTransport, object>(
					server.Configuration.MinimumConnection,
					server.Configuration.MaximumConnection
				);
		}

		/// <summary>
		///		Sets the transport pool.
		/// </summary>
		/// <param name="transportPool">The <see cref="ObjectPool{T}"/> of <typeparamref name="TTransport"/>.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="transportPool"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///		<see cref="IsTransportPoolSet"/> is <c>true</c>, this means that you try to replace existent pool.
		/// </exception>
		/// <remarks>
		///		Derviced classes must call this method in end of each constructors.
		/// </remarks>
		protected void SetTransportPool( ObjectPool<TTransport> transportPool )
		{
			if ( transportPool == null )
			{
				throw new ArgumentNullException( "transportPool" );
			}

			if ( this.IsTransportPoolSet )
			{
				throw new InvalidOperationException( "Already set." );
			}

			Contract.Ensures( this.IsTransportPoolSet );

			this._transportPool = transportPool;
		}

		/// <summary>
		///		When overridden in derived class, begins the shutdown process.
		/// </summary>
		/// <remarks>
		///		This method begins shutdown on the all active transports managed by this instance.
		/// </remarks>
		protected override void BeginShutdownCore()
		{
			foreach ( var transport in this._activeTransports )
			{
				try { }
				finally
				{
					transport.Key.ShutdownCompleted += this.OnTransportShutdownCompleted;
					Interlocked.Increment( ref this._tranportIsInShutdown );
				}

				transport.Key.BeginShutdown();
			}

			base.BeginShutdownCore();
		}

		private void OnTransportShutdownCompleted( object sender, EventArgs e )
		{
			var transport = sender as TTransport;
			Contract.Assert( transport != null );
			try { }
			finally
			{
				transport.ShutdownCompleted -= this.OnTransportShutdownCompleted;
				if ( Interlocked.Decrement( ref this._tranportIsInShutdown ) == 0 )
				{
					this.OnShutdownCompleted();
				}
			}
		}

		protected TTransport GetTransport( Socket bindingSocket )
		{
			if ( bindingSocket == null )
			{
				throw new ArgumentNullException( "bindingSocket" );
			}

			if ( !this.IsTransportPoolSet )
			{
				throw new InvalidOperationException( "Transport pool must be set via SetTransportPool()." );
			}

			Contract.Ensures( Contract.Result<TTransport>() != null );
			Contract.Ensures( Contract.Result<TTransport>().BoundSocket == bindingSocket );

			TTransport transport = GetTransportCore();
			transport.BoundSocket = bindingSocket;
			this._activeTransports.TryAdd( transport, null );

			return transport;
		}

		protected virtual TTransport GetTransportCore()
		{
			Contract.Ensures( Contract.Result<TTransport>() != null );

			return this._transportPool.Borrow();
		}

		internal sealed override void ReturnTransport( ServerTransport transport )
		{
			this.ReturnTransport( ( TTransport )transport );
		}

		protected void ReturnTransport( TTransport transport )
		{
			if ( transport == null )
			{
				throw new ArgumentNullException( "transport" );
			}

			if ( !Object.ReferenceEquals( this, transport.Manager ) )
			{
				throw new ArgumentException( "The specified transport is not owned by this manager.", "transport" );
			}

			if ( !this.IsTransportPoolSet )
			{
				throw new InvalidOperationException( "Transport pool must be set via SetTransportPool()." );
			}

			Contract.EndContractBlock();

			object dummy;
			this._activeTransports.TryRemove( transport, out dummy );
			this.ReturnTransportCore( transport );
		}

		protected virtual void ReturnTransportCore( TTransport transport )
		{
			Contract.Requires( transport != null );

			this._transportPool.Return( transport );
		}

		protected ServerRequestContext GetRequestContext( TTransport transport )
		{
			if ( transport == null )
			{
				throw new ArgumentNullException( "transport" );
			}

			Contract.Ensures( Contract.Result<ServerRequestContext>() != null );
			Contract.Ensures( Contract.Result<ServerRequestContext>().BoundTransport == transport );

			var requestContext = this.RequestContextPool.Borrow();
			requestContext.SetTransport( transport );
			return requestContext;
		}
	}
}
