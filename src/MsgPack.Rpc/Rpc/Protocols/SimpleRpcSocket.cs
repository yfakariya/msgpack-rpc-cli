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
	///		Simple <see cref="RpcSocket"/> implementation for real use.
	/// </summary>
	public sealed class SimpleRpcSocket : RpcSocket
	{
		private readonly Socket _socket;

		public override int Available
		{
			get { return this._socket.Available; }
		}

		/// <summary>
		///		Get <see cref="RpcTransportProtocol"/> for this socket.
		/// </summary>
		/// <value>
		///		<see cref="RpcTransportProtocol"/> for this socket.
		/// </value>
		public sealed override RpcTransportProtocol Protocol
		{
			get { return RpcTransportProtocol.ForSocket( this._socket ); }
		}

		/// <summary>
		///		Get <see cref="EndPoint"/> of remote endpoint.
		/// </summary>
		/// <value>
		///		<see cref="EndPoint"/> of remote endpoint.
		/// </value>
		public sealed override EndPoint RemoteEndPoint
		{
			get { return this._socket.RemoteEndPoint; }
		}

		/// <summary>
		///		Get <see cref="EndPoint"/> of local endpoint.
		/// </summary>
		/// <value>
		///		<see cref="EndPoint"/> of local endpoint.
		/// </value>
		public sealed override EndPoint LocalEndPoint
		{
			get { return this._socket.LocalEndPoint; }
		}

		/// <summary>
		///		Initialize new instance.
		/// </summary>
		/// <param name="socket">
		///		<see cref="Socket"/> to be wrapped.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="socket"/> is null.
		/// </exception>
		public SimpleRpcSocket( Socket socket )
		{
			if ( socket == null )
			{
				throw new ArgumentNullException( "socket" );
			}

			Contract.EndContractBlock();

			this._socket = socket;
		}

		/// <summary>
		///		Clean up internal unmanaged resouces, optionally clean up unmanged resources.
		/// </summary>
		/// <param name="disposing">
		///		Also clean up managed resources then true, otherwise false.
		/// </param>
		protected sealed override void Dispose( bool disposing )
		{
			this._socket.Close( 500 );
			base.Dispose( disposing );
		}

		/// <summary>
		///		Connect remote endpoint asynchronously.
		/// </summary>
		/// <param name="e">Context information.</param>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		/// <remarks>
		///		This method delegates to <see cref="Socket.ConnectAsync(SocketAsyncEventArgs)"/>.
		/// </remarks>
		protected sealed override bool ConnectAsyncCore( RpcSocketAsyncEventArgs e )
		{
			return this._socket.ConnectAsync( e );
		}

		/// <summary>
		///		Accept connection request from remote endpoint asynchronously.
		/// </summary>
		/// <param name="e">Context information.</param>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		/// <remarks>
		///		This method delegates to <see cref="Socket.AcceptAsync"/>.
		/// </remarks>
		protected sealed override bool AcceptAsyncCore( RpcSocketAsyncEventArgs e )
		{
			return this._socket.AcceptAsync( e );
		}

		/// <summary>
		///		Send data to connected remote endpoint asynchronously.
		/// </summary>
		/// <param name="e">Context information.</param>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		/// <remarks>
		///		This method delegates to <see cref="Socket.SendAsync"/>.
		/// </remarks>
		protected sealed override bool SendAsyncCore( RpcSocketAsyncEventArgs e )
		{
			Contract.Assume( e.BufferList != null );
			Contract.Assume( e.BufferList.Count > 0, e.BufferList.Count.ToString() );
			Contract.Assume( ( e as SocketAsyncEventArgs ).ConnectSocket == this._socket );
			Contract.Assume( e.RemoteEndPoint != null );
			return this._socket.SendAsync( e );
		}


		/// <summary>
		///		Send data to specified remote endpoint asynchronously.
		/// </summary>
		/// <param name="e">Context information.</param>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		/// <remarks>
		///		This method delegates to <see cref="Socket.SendToAsync"/>.
		/// </remarks>
		protected sealed override bool SendToAsyncCore( RpcSocketAsyncEventArgs e )
		{
			return this._socket.SendToAsync( e );
		}

		/// <summary>
		///		Receive data from connected remote endpoint asynchronously.
		/// </summary>
		/// <param name="e">Context information.</param>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		/// <remarks>
		///		This method delegates to <see cref="Socket.ReceiveAsync"/>.
		/// </remarks>
		protected sealed override bool ReceiveAsyncCore( RpcSocketAsyncEventArgs e )
		{
			return this._socket.ReceiveAsync( e );
		}

		/// <summary>
		///		Receive data from specified remote endpoint asynchronously.
		/// </summary>
		/// <param name="e">Context information.</param>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		/// <remarks>
		///		This method delegates to <see cref="Socket.ReceiveFromAsync"/>.
		/// </remarks>
		protected sealed override bool ReceiveFromAsyncCore( RpcSocketAsyncEventArgs e )
		{
			return this._socket.ReceiveFromAsync( e );
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
		/// <remarks>
		///		This method delegates to <see cref="Socket.ReceiveAsync"/>.
		/// </remarks>
		protected sealed override int ReceiveCore( RpcSocketAsyncEventArgs e )
		{
			return this._socket.Receive( e.BufferList, e.SocketFlags );
		}

		/// <summary>
		///		Shutdown socket.
		/// </summary>
		/// <param name="how">
		///		Operation to be disabled.
		/// </param>
		/// <remarks>
		///		This method delegates to <see cref="Socket.Shutdown"/>.
		/// </remarks>
		public sealed override void Shutdown( SocketShutdown how )
		{
			this._socket.Shutdown( how );
		}
	}
}
