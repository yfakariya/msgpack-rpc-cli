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
using System.Threading;
using System.Threading.Tasks;
using MsgPack.Rpc.Client.Protocols;
using MsgPack.Serialization;

namespace MsgPack.Rpc.Client
{
	/// <summary>
	///		Entry point of MessagePack-RPC client.
	/// </summary>
	public sealed class RpcClient : IDisposable
	{
		// TODO: Configurable
		private static int _messageIdGenerator;
		private static int NextId()
		{
			return Interlocked.Increment( ref _messageIdGenerator );
		}

		private readonly SerializationContext _serializationContext;

		internal SerializationContext SerializationContext
		{
			get { return this._serializationContext; }
		}

		private readonly ClientTransport _transport;

		internal ClientTransport Transport
		{
			get { return this._transport; }
		}

		private TaskCompletionSource<object> _transportShutdownCompletionSource;

		public RpcClient( ClientTransport transport, SerializationContext serializationContext )
		{
			if ( transport == null )
			{
				throw new ArgumentNullException( "transport" );
			}

			Contract.EndContractBlock();

			this._transport = transport;
			this._serializationContext = serializationContext ?? new SerializationContext();
		}

		/// <summary>
		///		Creates new <see cref="RpcClient"/> to communicate with specified <see cref="EndPoint"/>
		///		using specified configuration and default serialization context.
		/// </summary>
		/// <param name="targetEndPoint">
		///		<see cref="EndPoint"/> for the target.
		/// </param>
		/// <param name="transportManager">
		///		A <see cref="ClientTransportManager"/> which manages <see cref="ClientTransport"/> to be used to connect to the server.
		/// </param>
		/// <returns>
		///		A new <see cref="RpcClient"/> to communicate with specified <see cref="EndPoint"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="targetEndPoint"/> is <c>null</c>.
		///		Or <paramref name="configuration"/> is <c>null</c>.
		/// </exception>
		public static RpcClient Create( EndPoint targetEndPoint, ClientTransportManager transportManager )
		{
			if ( targetEndPoint == null )
			{
				throw new ArgumentNullException( "targetEndPoint" );
			}

			if ( transportManager == null )
			{
				throw new ArgumentNullException( "transportManager" );
			}

			Contract.Ensures( Contract.Result<RpcClient>() != null );

			return CreateCore( targetEndPoint, transportManager, new SerializationContext() );
		}

		/// <summary>
		///		Creates new <see cref="RpcClient"/> to communicate with specified <see cref="EndPoint"/>
		///		and specified configuration.
		/// </summary>
		/// <param name="targetEndPoint">
		///		<see cref="EndPoint"/> for the target.
		/// </param>
		/// <param name="transportManager">
		///		A <see cref="ClientTransportManager"/> which manages <see cref="ClientTransport"/> to be used to connect to the server.
		/// </param>
		/// <param name="serializationContext">
		///		A <see cref="SerializationContext"/> to holds serializers.
		/// </param>
		/// <returns>
		///		A new <see cref="RpcClient"/> to communicate with specified <see cref="EndPoint"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="targetEndPoint"/> is <c>null</c>.
		///		Or <paramref name="transportManager"/> is <c>null</c>.
		///		Or <paramref name="serializationContext"/> is <c>null</c>.
		/// </exception>
		public static RpcClient Create( EndPoint targetEndPoint, ClientTransportManager transportManager, SerializationContext serializationContext )
		{
			if ( targetEndPoint == null )
			{
				throw new ArgumentNullException( "targetEndPoint" );
			}

			if ( transportManager == null )
			{
				throw new ArgumentNullException( "transportManager" );
			}

			if ( serializationContext == null )
			{
				throw new ArgumentNullException( "serializationContext" );
			}

			Contract.Ensures( Contract.Result<RpcClient>() != null );

			return CreateCore( targetEndPoint, transportManager, serializationContext );
		}

		private static RpcClient CreateCore( EndPoint targetEndPoint, ClientTransportManager transportManager, SerializationContext serializationContext )
		{
			var transport = transportManager.ConnectAsync( targetEndPoint ).Result;
			return new RpcClient( transport, serializationContext );
		}

		/// <summary>
		///		Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			this._transport.Dispose();
		}

		/// <summary>
		///		Initiates shutdown of current connection and wait to complete it.
		/// </summary>
		public void Shutdown()
		{
			using ( var task = this.ShutdownAsync() )
			{
				if ( task != null )
				{
					task.Wait();
				}
			}
		}

		/// <summary>
		///		Initiates shutdown of current connection.
		/// </summary>
		/// <returns>
		///		The <see cref="Task"/> to wait to complete shutdown process.
		///		This value will be <c>null</c> when there is not the active connection.
		/// </returns>
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

		/// <summary>
		///		Calls specified remote method with specified argument and returns its result synchronously. 
		/// </summary>
		/// <param name="methodName">
		///		The name of target method.
		/// </param>
		/// <param name="arguments">
		///		Argument to be passed to the server.
		///		All values must be able to be serialized with MessagePack serializer.
		/// </param>
		/// <returns>
		///		The <see cref="MessagePackObject"/> which represents the return value.
		///		Note that <c>nil</c> object will be returned when the remote method does not return any values (a.k.a. void).
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="methodName"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="methodName"/> is not valid.
		/// </exception>
		/// <exception cref="RpcException">
		///		Failed to execute specified remote method.
		/// </exception>
		public MessagePackObject Call( string methodName, params object[] arguments )
		{
			return this.EndCall( this.BeginCall( methodName, arguments, null, null ) );
		}

		/// <summary>
		///		Calls specified remote method with specified argument asynchronously. 
		/// </summary>
		/// <param name="methodName">
		///		The name of target method.
		/// </param>
		/// <param name="arguments">
		///		Argument to be passed to the server.
		///		All values must be able to be serialized with MessagePack serializer.
		/// </param>
		/// <param name="asyncState">
		///		User supplied state object to be set as <see cref="P:Task.AsyncState"/>.
		/// </param>
		/// <returns>
		///		A <see cref="Task{T}"/> of <see cref="MessagePackObject"/> which represents asynchronous invocation.
		///		The resulting <see cref="MessagePackObject"/> which represents the return value.
		///		Note that <c>nil</c> object will be returned when the remote method does not return any values (a.k.a. void).
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="methodName"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="methodName"/> is not valid.
		/// </exception>
		/// <remarks>
		///		In .NET Framework 4.0, Silverlight 5, or Windows Phone 7.5, 
		///		the exception will be thrown as <see cref="AggregateException"/> in continuation <see cref="Task"/>.
		///		But this is because of runtime limitation, so this behavior will change in the future.
		///		You can <see cref="BeginCall"/> and <see cref="EndCall"/> to get appropriate exception directly.
		/// </remarks>
		public Task<MessagePackObject> CallAsync( string methodName, object[] arguments, object asyncState )
		{
			return Task.Factory.FromAsync<string, object[], MessagePackObject>( this.BeginCall, this.EndCall, methodName, arguments, asyncState, TaskCreationOptions.None );
		}

		/// <summary>
		///		Calls specified remote method with specified argument asynchronously. 
		/// </summary>
		/// <param name="methodName">
		///		The name of target method.
		/// </param>
		/// <param name="arguments">
		///		Argument to be passed to the server.
		///		All values must be able to be serialized with MessagePack serializer.
		/// </param>
		/// <param name="asyncCallback">
		///		The callback method invoked when the notification is sent or the reponse is received.
		///		This value can be <c>null</c>.
		///		Usually this callback get the result of invocation via <see cref="EndCall"/>.
		/// </param>
		/// <param name="asyncState">
		///		User supplied state object which can be gotten via <see cref="IAsyncResult.AsyncState"/> in the <paramref name="asyncCallback"/> callback.
		///		This value can be <c>null</c>.
		/// </param>
		/// <returns>
		///		An <see cref="IAsyncResult" /> which can be passed to <see cref="EndCall"/> method.
		///		Usually, this value will be ignored because same instance will be passed to the <paramref name="asyncCallback"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="methodName"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="methodName"/> is not valid.
		/// </exception>
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
					this._transport.ReturnContext( context );
				}
			}

			return asyncResult;
		}

		/// <summary>
		///		Finishes asynchronous method invocation and returns its result.
		/// </summary>
		/// <param name="asyncResult">
		///		<see cref="IAsyncResult"/> returned from <see cref="BeginCall"/>.
		/// </param>
		/// <returns>
		///		The <see cref="MessagePackObject"/> which represents the return value.
		///		Note that <c>nil</c> object will be returned when the remote method does not return any values (a.k.a. void).
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="asyncResult"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="asyncResult"/> is not valid type.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///		<paramref name="asyncResult"/> is returned from other instance,
		///		or its state is not valid.
		/// </exception>
		/// <exception cref="RpcException">
		///		Failed to execute specified remote method.
		/// </exception>
		/// <remarks>
		///		You must call this method to clean up internal bookkeeping information and
		///		handles communication error
		///		even if the remote method returns <c>void</c>.
		/// </remarks>
		public MessagePackObject EndCall( IAsyncResult asyncResult )
		{
			var requestAsyncResult = AsyncResult.Verify<RequestMessageAsyncResult>( asyncResult, this );
			requestAsyncResult.WaitForCompletion();
			requestAsyncResult.Finish();
			var result = requestAsyncResult.Result;
			Contract.Assert( result != null );
			return result.Value;
		}

		/// <summary>
		///		Sends specified remote method with specified argument as notification message synchronously.
		/// </summary>
		/// <param name="methodName">
		///		The name of target method.
		/// </param>
		/// <param name="arguments">
		///		Argument to be passed to the server.
		///		All values must be able to be serialized with MessagePack serializer.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="methodName"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="methodName"/> is not valid.
		/// </exception>
		/// <exception cref="RpcException">
		///		Failed to send notification message.
		/// </exception>
		public void Notify( string methodName, params object[] arguments )
		{
			this.EndNotify( this.BeginNotify( methodName, arguments, null, null ) );
		}

		/// <summary>
		///		Sends specified remote method with specified argument as notification message asynchronously.
		/// </summary>
		/// <param name="methodName">
		///		The name of target method.
		/// </param>
		/// <param name="arguments">
		///		Argument to be passed to the server.
		///		All values must be able to be serialized with MessagePack serializer.
		/// </param>
		/// <param name="asyncState">
		///		User supplied state object to be set as <see cref="P:Task.AsyncState"/>.
		/// </param>
		/// <returns>
		///		A <see cref="Task"/> which represents asynchronous invocation.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="methodName"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="methodName"/> is not valid.
		/// </exception>
		/// <remarks>
		///		In .NET Framework 4.0, Silverlight 5, or Windows Phone 7.5, 
		///		the exception will be thrown as <see cref="AggregateException"/> in continuation <see cref="Task"/>.
		///		But this is because of runtime limitation, so this behavior will change in the future.
		///		You can <see cref="BeginNotify"/> and <see cref="EndNotify"/> to get appropriate exception directly.
		/// </remarks>
		public Task NotifyAsync( string methodName, object[] arguments, object asyncState )
		{
			return Task.Factory.FromAsync<string, object[]>( this.BeginNotify, this.EndNotify, methodName, arguments, asyncState, TaskCreationOptions.None );
		}

		/// <summary>
		///		Sends specified remote method with specified argument as notification message asynchronously.
		/// </summary>
		/// <param name="methodName">
		///		The name of target method.
		/// </param>
		/// <param name="arguments">
		///		Argument to be passed to the server.
		///		All values must be able to be serialized with MessagePack serializer.
		/// </param>
		/// <param name="asyncCallback">
		///		The callback method invoked when the notification is sent or the reponse is received.
		///		This value can be <c>null</c>.
		///		Usually this callback get the result of invocation via <see cref="EndNotify"/>.
		/// </param>
		/// <param name="asyncState">
		///		User supplied state object which can be gotten via <see cref="IAsyncResult.AsyncState"/> in the <paramref name="asyncCallback"/> callback.
		///		This value can be <c>null</c>.
		/// </param>
		/// <returns>
		///		An <see cref="IAsyncResult" /> which can be passed to <see cref="EndNotify"/> method.
		///		Usually, this value will be ignored because same instance will be passed to the <paramref name="asyncCallback"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="methodName"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="methodName"/> is not valid.
		/// </exception>
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
					this._transport.ReturnContext( context );
				}
			}

			return asyncResult;
		}

		/// <summary>
		///		Finishes asynchronous method invocation.
		/// </summary>
		/// <param name="asyncResult">
		///		<see cref="IAsyncResult"/> returned from <see cref="BeginNotify"/>.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="asyncResult"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="asyncResult"/> is not valid type.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///		<paramref name="asyncResult"/> is returned from other instance,
		///		or its state is not valid.
		/// </exception>
		/// <exception cref="RpcException">
		///		Failed to execute specified remote method.
		/// </exception>
		/// <remarks>
		///		You must call this method to clean up internal bookkeeping information and
		///		handles communication error.
		/// </remarks>
		public void EndNotify( IAsyncResult asyncResult )
		{
			var notificationAsyncResult = AsyncResult.Verify<MessageAsyncResult>( asyncResult, this );
			notificationAsyncResult.WaitForCompletion();
			notificationAsyncResult.Finish();
		}
	}
}