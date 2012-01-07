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
using System.Net;
using System.Threading.Tasks;
using MsgPack.Rpc.Protocols;
using MsgPack.Rpc.Client.Protocols;
using System.Threading;
using MsgPack.Serialization;

namespace MsgPack.Rpc.Client
{
	/// <summary>
	///		Entry point of MessagePack-RPC client.
	/// </summary>
	/// <remarks>
	///		If you favor implicit (transparent) invocation model, you can use <see cref="PrcProxy"/> instead.
	/// </remarks>
	public sealed class RpcClient : IDisposable
	{
		// TODO: Configurable
		private static int _messageIdGenerator;
		private static int NextId()
		{
			return Interlocked.Increment( ref _messageIdGenerator );
		}

		private readonly SerializationContext _serializationContext;
		private readonly ClientTransport _transport;
		private TaskCompletionSource<object> _transportShutdownCompletionSource;

		internal RpcClient( ClientTransport transport, SerializationContext serializationContext )
		{
			if ( transport == null )
			{
				throw new ArgumentNullException( "transport" );
			}

			Contract.EndContractBlock();

			this._transport = transport;
			this._serializationContext = serializationContext ?? new SerializationContext();
		}
		public static RpcClient Create( EndPoint targetEndPoint )
		{
			return Create( targetEndPoint, RpcClientConfiguration.Default );
		}

		public static RpcClient Create( EndPoint targetEndPoint, RpcClientConfiguration configuration )
		{
			return Create( targetEndPoint, configuration, new SerializationContext() );
		}

		public static RpcClient Create( EndPoint targetEndPoint, SerializationContext serializationContext )
		{
			return Create( targetEndPoint, RpcClientConfiguration.Default, serializationContext );
		}

		public static RpcClient Create( EndPoint targetEndPoint, RpcClientConfiguration configuration, SerializationContext serializationContext )
		{
			if ( targetEndPoint == null )
			{
				throw new ArgumentNullException( "targetEndPoint" );
			}

			if ( configuration == null )
			{
				throw new ArgumentNullException( "configuration" );
			}

			var manager = configuration.TransportManagerProvider( configuration );
			var transport = manager.ConnectAsync( targetEndPoint ).Result;
			return RpcClient.Create( transport, serializationContext );
		}

		public static RpcClient Create( ClientTransport transport, SerializationContext serializationContext )
		{
			return new RpcClient( transport, serializationContext );
		}

		public void Dispose()
		{
			this._transport.Dispose();
		}

		// FIXME: Shutdown
		public void Shutdown()
		{
			this.ShutdownAsync().Wait();
		}

		public Task ShutdownAsync()
		{
			if ( this._transportShutdownCompletionSource != null )
			{
				return null;
			}

			var taskCompletionSource = new TaskCompletionSource<object>();
			if ( Interlocked.CompareExchange( ref this._transportShutdownCompletionSource, taskCompletionSource, null ) != null )
			{
				return null;
			}

			this._transport.ShutdownCompleted += this.OnTranportShutdownComplete;
			this._transport.BeginShutdown();
			taskCompletionSource.Task.Start();
			return taskCompletionSource.Task;
		}

		private void OnTranportShutdownComplete( object sender, EventArgs e )
		{
			var taskCompletionSource = Interlocked.CompareExchange( ref this._transportShutdownCompletionSource, null, this._transportShutdownCompletionSource );
			if ( taskCompletionSource != null )
			{
				var transport = sender as ClientTransport;
				transport.ShutdownCompleted -= this.OnTranportShutdownComplete;
				taskCompletionSource.SetResult( null );
			}
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
			var messageId = NextId();
			var asyncResult = new RequestMessageAsyncResult( this, messageId, asyncCallback, asyncState );

			bool isSucceeded = false;
			var context = this._transport.GetClientRequestContext();
			try
			{
				context.SetRequest( messageId, methodName, asyncResult.OnCompleted );
				if ( arguments == null )
				{
					context.ArgumentsPacker.Pack( new MessagePackObject[ 0 ] );
				}
				else
				{
					context.ArgumentsPacker.PackArrayHeader( arguments.Length );
					foreach ( var arg in arguments )
					{
						if ( arg == null )
						{
							context.ArgumentsPacker.PackNull();
						}
						else
						{
							this._serializationContext.GetSerializer( arg.GetType() ).PackTo( context.ArgumentsPacker, arg );
						}
					}
				}

				this._transport.Send( context );
				isSucceeded = true;
			}
			finally
			{
				if ( !isSucceeded )
				{
					context.Clear();
					context.ReturnLease();
				}
			}

			return asyncResult;
		}

		public MessagePackObject EndCall( IAsyncResult asyncResult )
		{
			var requestAsyncResult = AsyncResult.Verify<RequestMessageAsyncResult>( asyncResult, this );
			requestAsyncResult.Finish();
			ClientResponseContext responseContext = requestAsyncResult.ResponseContext;
			try
			{
				return Unpacking.UnpackObject( responseContext.ResultBuffer );
			}
			finally
			{
				responseContext.Clear();
			}
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

			bool isSucceeded = false;
			var context = this._transport.GetClientRequestContext();
			try
			{
				context.SetNotification( methodName, asyncResult.OnCompleted );
				if ( arguments == null )
				{
					context.ArgumentsPacker.Pack( new MessagePackObject[ 0 ] );
				}
				else
				{
					context.ArgumentsPacker.PackArrayHeader( arguments.Length );
					foreach ( var arg in arguments )
					{
						if ( arg == null )
						{
							context.ArgumentsPacker.PackNull();
						}
						else
						{
							this._serializationContext.GetSerializer( arg.GetType() ).PackTo( context.ArgumentsPacker, arg );
						}
					}
				}

				this._transport.Send( context );
				isSucceeded = true;
			}
			finally
			{
				if ( !isSucceeded )
				{
					context.Clear();
					context.ReturnLease();
				}
			}

			return asyncResult;
		}

		public void EndNotify( IAsyncResult asyncResult )
		{
			var notificationAsyncResult = AsyncResult.Verify<MessageAsyncResult>( asyncResult, this );
			notificationAsyncResult.Finish();
		}
	}


	// TODO via DynamicObject


}
