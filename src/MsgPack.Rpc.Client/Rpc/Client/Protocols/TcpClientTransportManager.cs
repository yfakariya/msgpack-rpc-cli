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
	public sealed class TcpClientTransportManager : ClientTransportManager<TcpClientTransport>
	{
		public TcpClientTransportManager( RpcClientConfiguration configuration )
			: base( configuration )
		{
#if !API_SIGNATURE_TEST
			base.SetTransportPool( configuration.TcpTransportPoolProvider( () => new TcpClientTransport( this ), configuration.CreateTcpTransportPoolConfiguration() ) );
#endif
		}

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

		protected sealed override TcpClientTransport GetTransportCore( Socket bindingSocket )
		{
			if ( bindingSocket == null )
			{
				throw new InvalidOperationException( String.Format( CultureInfo.CurrentCulture, "'bindingSocket' is required in {0}.", this.GetType() ) );
			}

			var transport = base.GetTransportCore( bindingSocket );
			transport.BoundSocket = bindingSocket;
			return transport;
		}
	}
}
