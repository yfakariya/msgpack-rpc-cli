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
using System.Net.Sockets;
using System.Threading;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Wraps <see cref="SocketAsyncEventArgs"/> for RPC.
	/// </summary>
	public sealed class RpcSocketAsyncEventArgs : SocketAsyncEventArgs, IDisposable
	{
		private readonly Func<Socket, RpcSocket> _socketFactory;
		private readonly Action<RpcSocketAsyncEventArgs, bool> _onConnected;
		private readonly Action<RpcSocketAsyncEventArgs, bool> _onAcceptted;
		private readonly Action<RpcSocketAsyncEventArgs, bool> _onSent;
		private readonly Action<RpcSocketAsyncEventArgs, bool> _onReceived;
		private readonly Action<SocketAsyncOperation, SocketError> _onError;
		private readonly Action<RpcSocketAsyncEventArgs, SocketError, bool> _onConnectError;
		
		private RpcSocket _acceptSocket;

		/// <summary>
		///		Get <see cref="RpcSocket"/> which is set in <see cref="RpcSocket.AcceptAsync"/>.
		/// </summary>
		/// <value>
		///		<see cref="RpcSocket"/> which is set in <see cref="RpcSocket.AcceptAsync"/>.
		/// </value>
		public new RpcSocket AcceptSocket
		{
			get
			{
				if ( this._acceptSocket == null )
				{
					if ( base.AcceptSocket == null )
					{
						return null;
					}

					this._acceptSocket = this._socketFactory( base.AcceptSocket );
				}

				return this._acceptSocket;
			}
			internal set
			{
				this._acceptSocket = value;
			}
		}

		private readonly CancellationToken _cancellationToken;

		/// <summary>
		///		Get cancellation token to cancel asynchronous invocation.
		/// </summary>
		/// <value>
		///		Cancellation token to cancel asynchronous invocation.
		/// </value>
		public CancellationToken CancellationToken
		{
			get { return this._cancellationToken; }
		}

		private RpcSocket _connectSocket;

		/// <summary>
		///		Get <see cref="RpcSocket"/> which is set in <see cref="RpcSocket.ConnectAsync"/>.
		/// </summary>
		/// <value>
		///		<see cref="RpcSocket"/> which is set in <see cref="RpcSocket.ConnectAsync"/>.
		/// </value>
		public new RpcSocket ConnectSocket
		{
			get
			{
				if ( this._connectSocket == null )
				{
					if ( base.ConnectSocket == null )
					{
						return null;
					}

					this._connectSocket = this._socketFactory( base.ConnectSocket );
				}

				return this._connectSocket;
			}
			internal set
			{
				this._connectSocket = value;
			}
		}

		/// <summary>
		///		Initialize new instance.
		/// </summary>
		/// <param name="onConnected">
		///		Callback when asynchronous connect operation is completed.
		/// </param>
		/// <param name="onAcceptted">
		///		Callback when asynchronous accept operation is completed.
		/// </param>
		/// <param name="onSent">
		///		Callback when asynchronous send operation is completed.
		/// </param>
		/// <param name="onReceived">
		///		Callback when asynchronous receive operation is completed.
		/// </param>
		/// <param name="onError">
		///		Callback when asynchronous operation is failed.
		/// </param>
		/// <param name="cancellationToken">
		///		Cancellation token to cancel asynchronous socket callback.
		/// </param>
		/// <param name="socketFactory">
		///		Factory method to wrap <see cref="Socket"/> as <see cref="RpcSocket"/>.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="socketFactory"/> is null.
		/// </exception>
		public RpcSocketAsyncEventArgs(
			Action<RpcSocketAsyncEventArgs, bool> onConnected,
			Action<RpcSocketAsyncEventArgs, bool> onAcceptted,
			Action<RpcSocketAsyncEventArgs, bool> onSent,
			Action<RpcSocketAsyncEventArgs, bool> onReceived,
			Action<RpcSocketAsyncEventArgs, SocketError, bool> onConnectError,
			Action<SocketAsyncOperation, SocketError> onError,
			CancellationToken cancellationToken,
			Func<Socket, RpcSocket> socketFactory
		)
		{
			if ( socketFactory == null )
			{
				throw new ArgumentNullException( "socketFactory" );
			}

			Contract.EndContractBlock();

			this._onConnected = onConnected;
			this._onConnectError = onConnectError;
			this._onAcceptted = onAcceptted;
			this._onSent = onSent;
			this._onReceived = onReceived;
			this._onError = onError;
			this._cancellationToken = cancellationToken;
			this._socketFactory = socketFactory;
		}

		/// <summary>
		///		Raise appropriate event.
		/// </summary>
		/// <param name="e">Event informartion.</param>
		protected sealed override void OnCompleted( SocketAsyncEventArgs e )
		{
			if ( e.SocketError != System.Net.Sockets.SocketError.Success )
			{
				if ( e.LastOperation == SocketAsyncOperation.Connect )
				{
					var handler = this._onConnectError; ;
					if ( handler == null )
					{
						throw new SocketException( ( int )e.SocketError );
					}

					handler( this, e.SocketError, false );
				}
				else
				{
					var handler = this._onError;
					if ( handler == null )
					{
						throw new SocketException( ( int )e.SocketError );
					}

					handler( e.LastOperation, e.SocketError );
				}

				return;
			}

			switch ( e.LastOperation )
			{
				case SocketAsyncOperation.Accept:
				{
					var handler = this._onAcceptted;
					if ( handler != null )
					{
						handler( this, false );
					}

					return;
				}
				case SocketAsyncOperation.Connect:
				{
					var handler = this._onConnected;
					if ( handler != null )
					{
						handler( this, false );
					}

					return;
				}
				case SocketAsyncOperation.Send:
				case SocketAsyncOperation.SendTo:
				{
					var handler = this._onSent;
					if ( handler != null )
					{
						handler( this, false );
					}

					return;
				}
				case SocketAsyncOperation.Receive:
				case SocketAsyncOperation.ReceiveFrom:
				{
					var handler = this._onReceived;
					if ( handler != null )
					{
						handler( this, false );
					}

					return;
				}
			}

			base.OnCompleted( e );
		}

		/// <summary>
		///		Invoke sending callback to client.
		/// </summary>
		public void OnSent( bool completedSynchronously )
		{
			this._onSent( this, completedSynchronously );
		}

		/// <summary>
		///		Send data to connected remote endpoint asynchronously.
		/// </summary>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		public bool SendAsync()
		{
			Contract.Assume( this.ConnectSocket != null );
			return this._connectSocket.SendAsync( this );
		}

		/// <summary>
		///		Send data to specific remote endpoint asynchronously.
		/// </summary>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		public bool SendToAsync()
		{
			Contract.Assume( this.RemoteEndPoint != null );
			return this._connectSocket.SendToAsync( this );
		}

		/// <summary>
		///		Invoke receiving callback to client.
		/// </summary>
		public void OnReceived( bool completedSynchrnously )
		{
			this._onReceived( this, completedSynchrnously );
		}

		/// <summary>
		///		Receive data from connected remote endpoint asynchronously.
		/// </summary>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		public bool ReceiveAsync()
		{
			Contract.Assume( this.ConnectSocket != null );
			return this._connectSocket.ReceiveAsync( this );
		}

		/// <summary>
		///		Receive data from specified remote endpoint asynchronously.
		/// </summary>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		public bool ReceiveFromAsync()
		{
			Contract.Assume( this.RemoteEndPoint != null );
			return this._connectSocket.ReceiveFromAsync( this );
		}
	}
}
