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
#if !WINDOWS_PHONE
using System.Threading.Tasks;
#endif
using MsgPack.Rpc.Protocols;

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
#if MONO
			var connectingSocket = new Socket( targetEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp );
#endif
			context.UserToken =
				Tuple.Create(
					source,
					this.BeginConnectTimeoutWatch(
						() =>
						{
#if !API_SIGNATURE_TEST
							MsgPackRpcClientProtocolsTrace.TraceEvent(
								MsgPackRpcClientProtocolsTrace.ConnectTimeout,
								"Connect timeout. {{ \"EndPoint\" : \"{0}\", \"AddressFamily\" : {1}, \"PreferIPv4\" : {2}, \"OSSupportsIPv6\" : {3}, \"ConnectTimeout\" : {4} }}",
								targetEndPoint,
								targetEndPoint.AddressFamily,
								this.Configuration.PreferIPv4,
								Socket.OSSupportsIPv6,
								this.Configuration.ConnectTimeout
							);
#endif
#if MONO
							// Cancel ConnectAsync.
							connectingSocket.Close();
#else
							Socket.CancelConnectAsync( context );
#endif
						}
					)
#if MONO
					, connectingSocket
#endif
				);

#if MONO
			if( !connectingSocket.ConnectAsync( context ) )
#else
			if ( !Socket.ConnectAsync( SocketType.Stream, ProtocolType.Tcp, context ) )
#endif
			{
				this.OnCompleted( null, context );
			}

			return source.Task;
		}

		private void OnCompleted( object sender, SocketAsyncEventArgs e )
		{
			var socket = sender as Socket;
#if MONO
			var userToken = e.UserToken as Tuple<TaskCompletionSource<ClientTransport>, ConnectTimeoutWatcher, Socket>;
#else
			var userToken = e.UserToken as Tuple<TaskCompletionSource<ClientTransport>, ConnectTimeoutWatcher>;
#endif
			var taskCompletionSource = userToken.Item1;
			var watcher = userToken.Item2;
			if ( watcher != null )
			{
				this.EndConnectTimeoutWatch( watcher );
			}
			
#if MONO
			var error = this.HandleSocketError( userToken.Item3 ?? socket, e );
#else
			var error = this.HandleSocketError( e.ConnectSocket ?? socket, e );
#endif
			if ( error != null )
			{
				taskCompletionSource.SetException( error.Value.ToException() );
				return;
			}

			switch ( e.LastOperation )
			{
				case SocketAsyncOperation.Connect:
				{
#if MONO
					this.OnConnected( userToken.Item3, e, taskCompletionSource );
#else
					this.OnConnected( e.ConnectSocket, e, taskCompletionSource );
#endif
					break;
				}
				default:
				{
#if !API_SIGNATURE_TEST
					MsgPackRpcClientProtocolsTrace.TraceEvent(
						MsgPackRpcClientProtocolsTrace.UnexpectedLastOperation,
						"Unexpected operation. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\", \"LastOperation\" : \"{3}\" }}",
						ClientTransport.GetHandle( socket ),
						ClientTransport.GetRemoteEndPoint( socket, e ),
						ClientTransport.GetLocalEndPoint( socket ),
						e.LastOperation
					);
#endif
					taskCompletionSource.SetException( new InvalidOperationException( String.Format( CultureInfo.CurrentCulture, "Unknown socket operation : {0}", e.LastOperation ) ) );
					break;
				}
			}
		}

		private void OnConnected( Socket connectSocket, SocketAsyncEventArgs context, TaskCompletionSource<ClientTransport> taskCompletionSource )
		{
			try
			{
				if ( connectSocket == null || !connectSocket.Connected )
				{
					// canceled.
					taskCompletionSource.SetException(
						new RpcTransportException(
							RpcError.ConnectionTimeoutError,
							"Connect timeout.",
							String.Format( CultureInfo.CurrentCulture, "Timeout: {0}", this.Configuration.ConnectTimeout )
						)
					);
					return;
				}

#if !API_SIGNATURE_TEST
				MsgPackRpcClientProtocolsTrace.TraceEvent(
					MsgPackRpcClientProtocolsTrace.EndConnect,
					"Connected. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\" }}",
					ClientTransport.GetHandle( connectSocket ),
					ClientTransport.GetRemoteEndPoint( connectSocket, context ),
					ClientTransport.GetLocalEndPoint( connectSocket )
				);
#endif

				taskCompletionSource.SetResult( this.GetTransport( connectSocket ) );
			}
			finally
			{
				context.Dispose();
			}
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

#if WINDOWS_PHONE
		private static class Tuple
		{
			public static Tuple<T1, T2> Create<T1, T2>( T1 item1, T2 item2 )
			{
				return new Tuple<T1, T2>( item1, item2 );
			}
		}

		private sealed class Tuple<T1, T2>
		{
			public readonly T1 Item1;
			public readonly T2 Item2;

			public Tuple( T1 item1, T2 item2 )
			{
				this.Item1 = item1;
				this.Item2 = item2;
			}
		}
#endif
	}
}
