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
using System.Globalization;
using System.Threading.Tasks;
using MsgPack.Rpc.Protocols;
using System.Net;
using System.Threading;
using MsgPack.Collections;
using MsgPack.Rpc.Serialization;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Entry point of MessagePack-RPC client.
	/// </summary>
	/// <remarks>
	///		If you favor implicit (transparent) invocation model, you can use <see cref="PrcProxy"/> instead.
	/// </remarks>
	public sealed class RpcClient : IDisposable
	{
		private readonly ClientTransport _transport;

		public ClientTransport Transport
		{
			get { return this._transport; }
		}

		// TODO: It is too big overhead of ConcurrentDictionary since concurrency not very high.
		//		 If so, using Dictionary and Monitor might improve performance.
		private readonly ConcurrentDictionary<int, RequestMessageAsyncResult> _responseAsyncResults;

		internal RpcClient( ClientTransport transport )
		{
			if ( transport == null )
			{
				throw new ArgumentNullException( "transport" );
			}

			Contract.EndContractBlock();

			this._transport = transport;
			this._responseAsyncResults = new ConcurrentDictionary<int, RequestMessageAsyncResult>();
		}

		public static RpcClient CreateTcp( EndPoint remoteEndPoint, ClientEventLoop eventLoop, RpcClientOptions options )
		{
			RpcTransportException failure = null;
			var transport =
				new TcpClientTransport(
					remoteEndPoint,
					options.ForceIPv4.GetValueOrDefault() ? RpcTransportProtocol.TcpIpV4 : RpcTransportProtocol.TcpIp,
					eventLoop,
					options
				);
			if ( failure != null )
			{
				throw failure;
			}

			return new RpcClient( transport );
		}

		public static RpcClient CreateUdp( EndPoint remoteEndPoint, ClientEventLoop eventLoop, RpcClientOptions options )
		{
			var transport =
				new UdpClientTransport(
					ClientServices.SocketFactory(
						( options.ForceIPv4.GetValueOrDefault() ? RpcTransportProtocol.UdpIpV4 : RpcTransportProtocol.UdpIp ).CreateSocket()
					),
					remoteEndPoint,
					eventLoop,
					options
				);
			return new RpcClient( transport );
		}

		public void Dispose()
		{
			this._transport.Dispose();
		}

		public MessagePackObject? Call( string methodName, params object[] arguments )
		{
			return this.EndCall( this.BeginCall( methodName, arguments, null, null ) );
		}

		public Task<MessagePackObject> CallAsync( string methodName, object[] arguments, object asyncState )
		{
			return Task.Factory.FromAsync<string, object[], MessagePackObject>( this.BeginCall, this.EndCall, methodName, arguments, asyncState, TaskCreationOptions.None );
		}

		public IAsyncResult BeginCall( string methodName, object[] arguments, AsyncCallback asyncCallback, object asyncState )
		{
			var messageId = MessageIdGenerator.Currrent.NextId();
			var asyncResult = new RequestMessageAsyncResult( this, messageId, asyncCallback, asyncState );

			bool isSent = false;
			try
			{
				try { }
				finally
				{
					if ( !this._responseAsyncResults.TryAdd( messageId, asyncResult ) )
					{
						throw new InvalidOperationException( String.Format( CultureInfo.CurrentCulture, "Message ID '{0}' is used.", messageId ) );
					}

					this.Transport.Send(
						MessageType.Request,
						messageId,
						methodName,
						arguments ?? Arrays<object>.Empty,
						( _, error, completedSynchronously ) =>
						{
							if ( error != null )
							{
								RequestMessageAsyncResult ar;
								if ( this._responseAsyncResults.TryRemove( messageId, out ar ) )
								{
									ar.OnError( error, completedSynchronously );
								}
							}
						},
						asyncResult
					);
					isSent = true;
				}
			}
			finally
			{
				if ( !isSent )
				{
					// Remove response handler since sending is failed.
					RequestMessageAsyncResult disposal;
					this._responseAsyncResults.TryRemove( messageId, out disposal );
				}
			}

			return asyncResult;
		}

		public MessagePackObject EndCall( IAsyncResult asyncResult )
		{
			var requestAsyncResult = AsyncResult.Verify<RequestMessageAsyncResult>( asyncResult, this );

			// Wait for completion
			if ( !requestAsyncResult.IsCompleted )
			{
				asyncResult.AsyncWaitHandle.WaitOne();
			}

			var response = requestAsyncResult.Response;
			requestAsyncResult.Finish();
			Contract.Assume( response.HasValue );

			// Fetch message
			if ( response.Value.Error != null )
			{
				throw response.Value.Error;
			}

			// Return it.
			return response.Value.ReturnValue;
		}

		public void Notify( string methodName, params object[] arguments )
		{
			this.EndNotify( this.BeginNotify( methodName, arguments, null, null ) );
		}

		public Task NotifyAsync( string methodName, object[] arguments, object asyncState )
		{
			return Task.Factory.FromAsync<string, object[]>( this.BeginNotify, this.EndNotify, methodName, arguments, asyncState, TaskCreationOptions.None );
		}

		public IAsyncResult BeginNotify( string methodName, object[] arguments, AsyncCallback asyncCallback, object asyncState )
		{
			var asyncResult = new NotificationMessageAsyncResult( this, asyncCallback, asyncState );
			this.Transport.Send(
				MessageType.Notification,
				null,
				methodName,
				arguments ?? Arrays<object>.Empty,
				asyncResult.OnMessageSent,
				asyncResult
			);
			return asyncResult;
		}

		public void EndNotify( IAsyncResult asyncResult )
		{
			var notificationAsyncResult = AsyncResult.Verify<MessageAsyncResult>( asyncResult, this );
			notificationAsyncResult.Finish();
		}

		private sealed class CreateTcpAsyncResult : AsyncResult
		{
			public void OnConnected( ConnectingContext e, bool completedSynchronously )
			{
				Contract.Assume( e != null );
				Contract.Assume( e.Client.SocketContext.ConnectSocket != null );
				base.Complete( completedSynchronously );
			}

			public TcpClientTransport Transport
			{
				get;
				set;
			}

			public CreateTcpAsyncResult( object owner, AsyncCallback asyncCallback, object asyncState )
				: base( owner, asyncCallback, asyncState ) { }
		}
	}


	// TODO via DynamicObject


}
