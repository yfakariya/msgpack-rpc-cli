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

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Wraps standard BSD <see cref="Socket"/>.
	/// </summary>
	/// <remarks>
	///		For testability, you can create test double which inhertis this type.
	/// </remarks>
	public abstract class RpcSocket : IDisposable
	{
		/// <summary>
		///		Get <see cref="RpcTransportProtocol"/> for this socket.
		/// </summary>
		/// <value>
		///		<see cref="RpcTransportProtocol"/> for this socket.
		/// </value>
		public abstract RpcTransportProtocol Protocol { get; }

		/// <summary>
		///		Get <see cref="EndPoint"/> of remote endpoint.
		/// </summary>
		/// <value>
		///		<see cref="EndPoint"/> of remote endpoint.
		/// </value>
		public abstract EndPoint RemoteEndPoint { get; }

		/// <summary>
		///		Get <see cref="EndPoint"/> of local endpoint.
		/// </summary>
		/// <value>
		///		<see cref="EndPoint"/> of locla endpoint.
		/// </value>
		public abstract EndPoint LocalEndPoint { get; }

		/// <summary>
		///		Initialize new instance.
		/// </summary>
		protected RpcSocket() { }

		/// <summary>
		///		Clean up internal resources.
		/// </summary>
		public void Dispose()
		{
			this.Dispose( true );
			GC.SuppressFinalize( this );
		}

		/// <summary>
		///		Clean up internal unmanaged resouces, optionally clean up unmanged resources.
		/// </summary>
		/// <param name="disposing">
		///		Also clean up managed resources then true, otherwise false.
		/// </param>
		protected virtual void Dispose( bool disposing ) { }

		/// <summary>
		///		Connect remote endpoint asynchronously.
		/// </summary>
		/// <param name="e">Context information.</param>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="e"/> is null.
		/// </exception>
		/// <seealso cref="Socket.ConnectAsync(SocketAsyncEventArgs)"/>.
		public bool ConnectAsync( RpcSocketAsyncEventArgs e )
		{
			if ( e == null )
			{
				throw new ArgumentNullException( "e" );
			}

			Contract.EndContractBlock();

			return this.ConnectAsyncCore( e );
		}

		/// <summary>
		///		Connect remote endpoint asynchronously.
		/// </summary>
		/// <param name="e">Context information.</param>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		/// <seealso cref="Socket.ConnectAsync(SocketAsyncEventArgs)"/>.		
		protected abstract bool ConnectAsyncCore( RpcSocketAsyncEventArgs e );

		/// <summary>
		///		Accept connection request from remote endpoint asynchronously.
		/// </summary>
		/// <param name="e">Context information.</param>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="e"/> is null.
		/// </exception>
		/// <seealso cref="Socket.AcceptAsync"/>.
		public bool AcceptAsync( RpcSocketAsyncEventArgs e )
		{
			if ( e == null )
			{
				throw new ArgumentNullException( "e" );
			}

			Contract.EndContractBlock();

			return this.AcceptAsyncCore( e );
		}

		/// <summary>
		///		Accept connection request from remote endpoint asynchronously.
		/// </summary>
		/// <param name="e">Context information.</param>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		/// <seealso cref="Socket.AcceptAsync"/>.
		protected abstract bool AcceptAsyncCore( RpcSocketAsyncEventArgs e );

		/// <summary>
		///		Send data to connected remote endpoint asynchronously.
		/// </summary>
		/// <param name="e">Context information.</param>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="e"/> is null.
		/// </exception>
		/// <seealso cref="Socket.SendAsync"/>.
		public bool SendAsync( RpcSocketAsyncEventArgs e )
		{
			if ( e == null )
			{
				throw new ArgumentNullException( "e" );
			}

			Contract.EndContractBlock();

			return this.SendAsyncCore( e );
		}

		/// <summary>
		///		Send data to connected remote endpoint asynchronously.
		/// </summary>
		/// <param name="e">Context information.</param>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		/// <seealso cref="Socket.SendAsync"/>.
		protected abstract bool SendAsyncCore( RpcSocketAsyncEventArgs e );

		/// <summary>
		///		Send data to specified remote endpoint asynchronously.
		/// </summary>
		/// <param name="e">Context information.</param>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="e"/> is null.
		/// </exception>
		/// <seealso cref="Socket.SendToAsync"/>.
		public bool SendToAsync( RpcSocketAsyncEventArgs e )
		{
			if ( e == null )
			{
				throw new ArgumentNullException( "e" );
			}

			Contract.EndContractBlock();

			return this.SendToAsyncCore( e );
		}

		/// <summary>
		///		Send data to specified remote endpoint asynchronously.
		/// </summary>
		/// <param name="e">Context information.</param>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		/// <seealso cref="Socket.SendToAsync"/>.
		protected abstract bool SendToAsyncCore( RpcSocketAsyncEventArgs e );

		/// <summary>
		///		Receive data from connected remote endpoint asynchronously.
		/// </summary>
		/// <param name="e">Context information.</param>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="e"/> is null.
		/// </exception>
		/// <seealso cref="Socket.ReceiveAsync"/>.
		public bool ReceiveAsync( RpcSocketAsyncEventArgs e )
		{
			if ( e == null )
			{
				throw new ArgumentNullException( "e" );
			}

			Contract.EndContractBlock();

			return this.ReceiveAsyncCore( e );
		}

		/// <summary>
		///		Receive data from connected remote endpoint asynchronously.
		/// </summary>
		/// <param name="e">Context information.</param>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		/// <seealso cref="Socket.ReceiveAsync"/>.
		protected abstract bool ReceiveAsyncCore( RpcSocketAsyncEventArgs e );

		/// <summary>
		///		Receive data from specified remote endpoint asynchronously.
		/// </summary>
		/// <param name="e">Context information.</param>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="e"/> is null.
		/// </exception>
		/// <seealso cref="Socket.ReceiveFromAsync"/>.
		public bool ReceiveFromAsync( RpcSocketAsyncEventArgs e )
		{
			if ( e == null )
			{
				throw new ArgumentNullException( "e" );
			}

			Contract.EndContractBlock();

			return this.ReceiveFromAsyncCore( e );
		}

		/// <summary>
		///		Receive data from specified remote endpoint asynchronously.
		/// </summary>
		/// <param name="e">Context information.</param>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		/// <seealso cref="Socket.ReceiveFromAsync"/>.
		protected abstract bool ReceiveFromAsyncCore( RpcSocketAsyncEventArgs e );

		/// <summary>
		///		Receive data from connected remote endpoint synchronously.
		/// </summary>
		/// <param name="e">Context information.</param>
		/// <returns>
		///		Transferred bytes length.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="e"/> is null.
		/// </exception>
		/// <exception cref="SocketException">
		///		Some socket error is ocurred.
		/// </exception>
		/// <seealso cref="Socket.ReceiveAsync"/>.
		public int Receive( RpcSocketAsyncEventArgs e )
		{
			if ( e == null )
			{
				throw new ArgumentNullException( "e" );
			}

			Contract.EndContractBlock();

			return this.ReceiveCore( e );
		}

		/// <summary>
		///		Receive data from connected remote endpoint synchronously.
		/// </summary>
		/// <param name="e">Context information.</param>
		/// <returns>
		///		Transferred bytes length.
		/// </returns>
		/// <exception cref="SocketException">
		///		Some socket error is ocurred.
		/// </exception>
		/// <seealso cref="Socket.ReceiveAsync"/>.
		protected abstract int ReceiveCore( RpcSocketAsyncEventArgs e );

		/// <summary>
		///		Shutdown socket.
		/// </summary>
		/// <param name="how">
		///		Operation to be disabled.
		/// </param>
		public abstract void Shutdown( SocketShutdown how );

		public abstract int Available { get; }
	}
}
