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
using System.Net.Sockets;
using System.Threading;
using MsgPack.Rpc.Protocols;

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
		///		Initiates protocol specific shutdown process.
		/// </summary>
		/// <remarks>
		///		This method begins shutdown on the all active transports managed by this instance.
		/// </remarks>
		protected override void BeginShutdownCore()
		{
			int registered = 0;
			foreach ( var transport in this._activeTransports )
			{
				try { }
				finally
				{
					transport.Key.ShutdownCompleted += this.OnTransportShutdownCompleted;
					Interlocked.Increment( ref this._tranportIsInShutdown );
					registered++;
				}

				if ( !transport.Key.BeginShutdown() )
				{
					try { }
					finally
					{
						transport.Key.ShutdownCompleted -= this.OnTransportShutdownCompleted;
						Interlocked.Decrement( ref this._tranportIsInShutdown );
						registered--;
					}
				}
			}

			base.BeginShutdownCore();

			if ( registered == 0 )
			{
				this.OnShutdownCompleted( new ShutdownCompletedEventArgs( ShutdownSource.Server ) );
			}
		}

		private void OnTransportClientShutdown( object sender, ClientShutdownEventArgs e )
		{
			this.OnClientShutdown( e );
		}

		private void OnTransportShutdownCompleted( object sender, ShutdownCompletedEventArgs e )
		{
			var transport = sender as TTransport;
			Contract.Assert( transport != null );
			try { }
			finally
			{
				transport.ShutdownCompleted -= this.OnTransportShutdownCompleted;
				if ( Interlocked.Decrement( ref this._tranportIsInShutdown ) == 0 )
				{
					this.OnShutdownCompleted( e );
				}
			}
		}

		/// <summary>
		///		Gets the transport managed by this instance.
		/// </summary>
		/// <param name="bindingSocket">The <see cref="Socket"/> to be bind the returning transport.</param>
		/// <returns>
		///		The transport managed by this instance.
		///		Note that <see cref="ServerTransport.BoundSocket"/> might be <c>null</c> depends on <see cref="GetTransportCore"/> implementation.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		///		<see cref="IsTransportPoolSet"/> is <c>false</c>.
		///		Or <paramref name="bindingSocket"/> cannot be <c>null</c> for the current transport.
		/// </exception>
		protected TTransport GetTransport( Socket bindingSocket )
		{
			Contract.Ensures( Contract.Result<TTransport>() != null );
			Contract.Ensures( Contract.Result<TTransport>().BoundSocket == null || Contract.Result<TTransport>().BoundSocket == bindingSocket );

			TTransport transport = this.GetTransportCore( bindingSocket );
			this._activeTransports.TryAdd( transport, null );
			transport.ClientShutdown += this.OnTransportClientShutdown;

			return transport;
		}

		/// <summary>
		///		Gets the transport managed by this instance.
		/// </summary>
		/// <param name="bindingSocket">The <see cref="Socket"/> to be bind the returning transport.</param>
		/// <returns>
		///		The transport managed by this instance.
		///		Note that <see cref="ServerTransport.BoundSocket"/> might be <c>null</c> depends on <see cref="GetTransportCore"/> implementation.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		///		<see cref="IsTransportPoolSet"/> is <c>false</c>.
		///		Or <paramref name="bindingSocket"/> cannot be <c>null</c> for the current transport.
		/// </exception>
		/// <remarks>
		///		This implementation does not bind <paramref name="bindingSocket"/> to the returning transport.
		///		The derived class which uses <see cref="Socket"/> for its communication, must set the transport via <see cref="BindSocket"/> method.
		/// </remarks>
		protected virtual TTransport GetTransportCore( Socket bindingSocket )
		{
			Contract.Ensures( Contract.Result<TTransport>() != null );
			Contract.Ensures( Contract.Result<TTransport>().BoundSocket == null || Contract.Result<TTransport>().BoundSocket == bindingSocket );

			if ( !this.IsTransportPoolSet )
			{
				throw new InvalidOperationException( "Transport pool must be set via SetTransportPool()." );
			}

			var transport = this._transportPool.Borrow();
			return transport;
		}

		/// <summary>
		///		Binds the specified socket to the specified transport.
		/// </summary>
		/// <param name="transport">The transport to be bound.</param>
		/// <param name="bindingSocket">The socket to be bound.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="transport"/> is <c>null</c>.
		///		Or <paramref name="bindingSocket"/> is <c>null</c>.
		/// </exception>
		protected void BindSocket( TTransport transport, Socket bindingSocket )
		{
			if ( transport == null )
			{
				throw new ArgumentNullException( "transport" );
			}

			if ( bindingSocket == null )
			{
				throw new ArgumentNullException( "bindingSocket" );
			}

			Contract.EndContractBlock();

			transport.BoundSocket = bindingSocket;
		}

		/// <summary>
		///		Invoked from the <see cref="ServerTransport"/> which was created by this manager,
		///		returns the transport to this manager.
		/// </summary>
		/// <param name="transport">The <see cref="ServerTransport"/> which was created by this manager.</param>
		internal sealed override void ReturnTransport( ServerTransport transport )
		{
			this.ReturnTransport( ( TTransport )transport );
		}

		/// <summary>
		///		Invoked from the <see cref="ServerTransport"/> which was created by this manager,
		///		returns the transport to this manager.
		/// </summary>
		/// <param name="transport">The <see cref="ServerTransport"/> which was created by this manager.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="transport"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="transport"/> is not managed by this manager.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///		<see cref="IsTransportPoolSet"/> is <c>false</c>.
		/// </exception>
		protected void ReturnTransport( TTransport transport )
		{
			if ( transport == null )
			{
				throw new ArgumentNullException( "transport" );
			}

			if ( !Object.ReferenceEquals( this, transport.Manager ) )
			{
				throw new ArgumentException( "The specified transport is not managed by this manager.", "transport" );
			}

			if ( !this.IsTransportPoolSet )
			{
				throw new InvalidOperationException( "Transport pool must be set via SetTransportPool()." );
			}

			Contract.EndContractBlock();

			transport.ClientShutdown -= this.OnTransportClientShutdown;
			object dummy;
			this._activeTransports.TryRemove( transport, out dummy );
			this.ReturnTransportCore( transport );
		}

		/// <summary>
		///		Invoked from <see cref="ReturnTransport(TTransport)"/>, returns the transport to this manager.
		/// </summary>
		/// <param name="transport">The <see cref="ServerTransport"/> which was created by this manager.</param>
		protected virtual void ReturnTransportCore( TTransport transport )
		{
			Contract.Requires( transport != null );
			Contract.Requires( Object.ReferenceEquals( this, transport.Manager ) );
			Contract.Requires( this.IsTransportPoolSet );

			if ( !transport.IsDisposed && !transport.IsServerShutdown && !transport.IsClientShutdown )
			{
				this._transportPool.Return( transport );
			}
		}

		/// <summary>
		///		Gets the <see cref="ServerRequestContext"/> to store context information.
		/// </summary>
		/// <param name="transport">The transport to be bound the returning context.</param>
		/// <returns>
		///		The <see cref="ServerRequestContext"/> to store context information.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="transport"/> is <c>null</c>.
		/// </exception>
		/// <remarks>
		///		You can pass the returning value to the <see cref="ServerTransport.Receive"/> method.
		/// </remarks>
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
