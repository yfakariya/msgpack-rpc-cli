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
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MsgPack.Rpc.Client.Protocols
{
	/// <summary>
	///		Implements <see cref="ClientTransportManager{T}"/> for <see cref="TcpClientTransport"/>.
	/// </summary>
	public sealed class TcpClientTransportManager : ClientTransportManager<TcpClientTransport>
	{
		/// <summary>
		///		Initializes a new instance of the <see cref="TcpClientTransportManager"/> class.
		/// </summary>
		/// <param name="configuration">
		///		The <see cref="RpcClientConfiguration"/> which describes transport configuration.
		/// </param>
		public TcpClientTransportManager( RpcClientConfiguration configuration )
			: base( configuration )
		{
#if !API_SIGNATURE_TEST
			base.SetTransportPool( configuration.TcpTransportPoolProvider( () => new TcpClientTransport( this ), configuration.CreateTransportPoolConfiguration() ) );
#endif
		}

		/// <summary>
		///		Establishes logical connection, which specified to the managed transport protocol, for the server.
		/// </summary>
		/// <param name="targetEndPoint">The end point of target server.</param>
		/// <returns>
		///		<see cref="Task{T}"/> of <see cref="ClientTransport"/> which represents asynchronous establishment process specific to the managed transport.
		///		This value will not be <c>null</c>.
		/// </returns>
		protected sealed override Task<ClientTransport> ConnectAsyncCore( EndPoint targetEndPoint )
		{
			TaskCompletionSource<ClientTransport> source = new TaskCompletionSource<ClientTransport>();
			var context = new SocketAsyncEventArgs();
			context.RemoteEndPoint = targetEndPoint;
			context.UserToken = source;
			context.Completed += this.OnCompleted;

#if !API_SIGNATURE_TEST
			MsgPackRpcClientProtocolsTrace.TraceEvent(
				MsgPackRpcClientProtocolsTrace.BeginConnect,
				"Connecting. {{ \"EndPoint\" : \"{0}\", \"AddressFamily\" : {1}, \"PreferIPv4\" : {2}, \"OSSupportsIPv6\" : {3} }}",
				targetEndPoint,
				targetEndPoint.AddressFamily,
				this.Configuration.PreferIPv4,
				Socket.OSSupportsIPv6
			);
#endif
			context.UserToken = this.BeginConnectTimeoutWatch( () => Socket.CancelConnectAsync( context ) );
			if ( !Socket.ConnectAsync( SocketType.Stream, ProtocolType.Tcp, context ) )
			{
				this.OnCompleted( null, context );
			}

			return source.Task;
		}

		private void OnCompleted( object sender, SocketAsyncEventArgs e )
		{
			var socket = sender as Socket;
			var taskCompletionSource = e.UserToken as TaskCompletionSource<ClientTransport>;
			var watcher = e.UserToken as ConnectTimeoutWatcher;
			if ( watcher != null )
			{
				this.EndConnectTimeoutWatch( watcher );
			}

			var error = this.HandleSocketError( e.ConnectSocket ?? socket, e );
			if ( error != null )
			{
				taskCompletionSource.SetException( error.Value.ToException() );
				return;
			}

			switch ( e.LastOperation )
			{
				case SocketAsyncOperation.Connect:
				{
					this.OnConnected( e, taskCompletionSource );
					break;
				}
				default:
				{
#if !API_SIGNATURE_TEST
					MsgPackRpcClientProtocolsTrace.TraceEvent(
						MsgPackRpcClientProtocolsTrace.UnexpectedLastOperation,
						"Unexpected operation. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\", \"LastOperation\" : \"{3}\" }}",
						socket.Handle,
						socket.RemoteEndPoint,
						socket.LocalEndPoint,
						e.LastOperation
					);
#endif
					taskCompletionSource.SetException( new ApplicationException( String.Format( CultureInfo.CurrentCulture, "Unknown socket operation : {0}", e.LastOperation ) ) );
					break;
				}
			}
		}

		private void OnConnected( SocketAsyncEventArgs context, TaskCompletionSource<ClientTransport> taskCompletionSource )
		{
#if !API_SIGNATURE_TEST
			MsgPackRpcClientProtocolsTrace.TraceEvent(
				MsgPackRpcClientProtocolsTrace.EndConnect,
				"Connected. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\" }}",
				context.ConnectSocket.Handle,
				context.ConnectSocket.RemoteEndPoint,
				context.ConnectSocket.LocalEndPoint
			);
#endif

			taskCompletionSource.SetResult( this.GetTransport( context.ConnectSocket ) );
			context.Dispose();
		}

		/// <summary>
		///		Gets the transport managed by this instance.
		/// </summary>
		/// <param name="bindingSocket">The <see cref="Socket"/> to be bind the returning transport.</param>
		/// <returns>
		///		The transport managed by this instance.
		///		This implementation binds a valid <see cref="Socket"/> to the returning transport.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		///		<see cref="P:IsTransportPoolSet"/> is <c>false</c>.
		///		Or <paramref name="bindingSocket"/> is <c>null</c>.
		/// </exception>
		protected sealed override TcpClientTransport GetTransportCore( Socket bindingSocket )
		{
			if ( bindingSocket == null )
			{
				throw new InvalidOperationException( String.Format( CultureInfo.CurrentCulture, "'bindingSocket' is required in {0}.", this.GetType() ) );
			}

			var transport = base.GetTransportCore( bindingSocket );
			this.BindSocket( transport, bindingSocket );
			return transport;
		}
	}
}
