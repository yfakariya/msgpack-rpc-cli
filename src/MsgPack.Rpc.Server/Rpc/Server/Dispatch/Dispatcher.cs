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
using System.Threading;
using System.Threading.Tasks;
using MsgPack.Rpc.Protocols;
using MsgPack.Rpc.Server.Protocols;
using MsgPack.Serialization;

namespace MsgPack.Rpc.Server.Dispatch
{
	/// <summary>
	///		Dispatches the RPC message to specific operation.
	/// </summary>
	[ContractClass( typeof( DispatcherContracts ) )]
	public abstract class Dispatcher
	{
		private readonly RpcServer _server;
		private readonly RpcServerRuntime _runtime;

		/// <summary>
		///		Gets <see cref="RpcServerRuntime"/> which provides runtime services.
		/// </summary>
		/// <value>
		///		The <see cref="RpcServerRuntime"/> which provides runtime services.
		/// </value>
		public RpcServerRuntime Runtime
		{
			get
			{
				Contract.Ensures( Contract.Result<RpcServerRuntime>() != null );

				return this._runtime;
			}
		}

		/// <summary>
		///		Gets a value indicating whether the server is in debug mode.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if the server is in debug mode; otherwise, <c>false</c>.
		/// </value>
		public bool IsDebugMode
		{
			get { return this._runtime.IsDebugMode; }
		}

		/// <summary>
		///		Gets the serialization context to store serializer for request arguments and response values.
		/// </summary>
		/// <value>
		///		The serialization context to store serializer for request arguments and response values.
		///		This value will not be <c>null</c>.
		/// </value>
		public SerializationContext SerializationContext
		{
			get
			{
				Contract.Ensures( Contract.Result<SerializationContext>() != null );

				return this._runtime.SerializationContext;
			}
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="Dispatcher"/> class.
		/// </summary>
		/// <param name="server">The server which will hold this instance.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="server"/> is <c>null</c>.
		/// </exception>
		protected Dispatcher( RpcServer server )
		{
			if ( server == null )
			{
				throw new ArgumentNullException( "server" );
			}

			Contract.EndContractBlock();

			this._server = server;
			this._runtime = new RpcServerRuntime( server.Configuration, server.SerializationContext );
		}

		/// <summary>
		///		Dispatches the specified request, and dispatches the response to the specified transport.
		/// </summary>
		/// <param name="serverTransport">The server transport the response to be dispatched.</param>
		/// <param name="requestContext">The request context.</param>
		internal void Dispatch( ServerTransport serverTransport, ServerRequestContext requestContext )
		{
			Contract.Requires( serverTransport != null );
			Contract.Requires( requestContext != null );

			ServerResponseContext responseContext = null;
			if ( requestContext.MessageType == MessageType.Request )
			{
				responseContext = serverTransport.Manager.GetResponseContext( requestContext );
			}

			Task task;
			var operation = this.Dispatch( requestContext.MethodName );
			if ( operation == null )
			{
				var error = new RpcErrorMessage( RpcError.NoMethodError, "Operation does not exist.", null );
				InvocationHelper.TraceInvocationResult<object>(
					requestContext.SessionId,
					requestContext.MessageType,
					requestContext.MessageId.GetValueOrDefault(),
					requestContext.MethodName,
					error,
					null
				);

				if ( responseContext != null )
				{
					task = Task.Factory.StartNew( () => responseContext.Serialize<object>( null, error, null ) );
				}
				else
				{
					return;
				}
			}
			else
			{
				task = operation( requestContext, responseContext );
			}

			var sessionState = Tuple.Create( this._server, requestContext.SessionId, requestContext.MessageType == MessageType.Request ? requestContext.MessageId : default( int? ), requestContext.MethodName );

#if NET_4_5
			task.ContinueWith( ( previous, state ) =>
				{
					var tuple = state as Tuple<ServerTransport, ServerResponseContext, Tuple<RpcServer, long, int?, string>;
					SendResponse( previous, tuple.Item1, tuple.Item2, tuple.Item3 )
				},
				Tuple.Create( serverTransport, responseContext, sessionState )
			).ContinueWith( ( previous, state ) =>
				{
					HandleSendFailure( previous, state as Tuple<RpcServer, long, int?, string> );
				},
				TaskContinuationOptions.OnlyOnFaulted,
				sessionState
			);
#else
			task.ContinueWith( previous =>
				{
					SendResponse( previous, serverTransport, responseContext, sessionState );
				}
			).ContinueWith( previous =>
				{
					HandleSendFailure( previous, sessionState );
				},
				TaskContinuationOptions.OnlyOnFaulted
			);
#endif
		}

		private static void SendResponse( Task previous, ServerTransport transport, ServerResponseContext context, Tuple<RpcServer, long, int?, string> sessionState )
		{
			if ( context == null )
			{
				if ( previous.IsFaulted )
				{
					try
					{
						previous.Exception.Handle( inner => inner is OperationCanceledException );
					}
					catch ( AggregateException exception )
					{
						InvocationHelper.HandleInvocationException(
							sessionState.Item2,
							MessageType.Notification,
							null,
							sessionState.Item4,
							exception,
							sessionState.Item1.Configuration.IsDebugMode
						);
					}
				}

				previous.Dispose();
				return;
			}

			switch ( previous.Status )
			{
				case TaskStatus.Canceled:
				{
					context.Serialize<object>( null, new RpcErrorMessage( RpcError.TimeoutError, "Server task exceeds execution timeout.", null ), null );
					break;
				}
				case TaskStatus.Faulted:
				{
					context.Serialize<object>( null, new RpcErrorMessage( RpcError.RemoteRuntimeError, "Dispatcher throws exception.", previous.Exception.ToString() ), null );
					break;
				}
			}

			previous.Dispose();
			transport.Send( context );
		}

		private static void HandleSendFailure( Task previous, Tuple<RpcServer, long, int?, string> sessionState )
		{
			try
			{
				previous.Exception.Handle( inner => inner is OperationCanceledException );
			}
			catch ( AggregateException exception )
			{
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.ErrorWhenSendResponse,
					"Failed to send response. {{ \"SessionID\" : {0}, \"MessageID\" : {1}, \"Error\" : {2} }}",
					sessionState.Item2,
					sessionState.Item3,
					exception
				);

				sessionState.Item1.RaiseServerError( exception );
			}
			finally
			{
				previous.Dispose();
			}
		}

		/// <summary>
		///		When overriden in the derived classes, dispatches the specified RPC method to the operation.
		/// </summary>
		/// <param name="methodName">Name of the method.</param>
		/// <returns>
		///		<para>
		///			The <see cref="Func{T1,T2,TReturn}"/> which is entity of the operation.
		///		</para>
		///		<para>
		///			The 1st argument is <see cref="ServerRequestContext"/> which holds any data related to the request. 
		///			This value will not be <c>null</c>.
		///		</para>
		///		<para>
		///			The 2nd argument is <see cref="ServerResponseContext"/> which handles any response related behaviors including error response.
		///			This value will not be <c>null</c>.
		///		</para>
		///		<para>
		///			The return value is <see cref="Task"/> which encapselates asynchronous target invocation.
		///			This value cannot be <c>null</c>.
		///		</para>
		/// </returns>
		/// <exception cref="Exception">
		///		The derived class faces unexpected failure.
		/// </exception>
		/// <remarks>
		///		Using <see cref="AsyncServiceInvoker{T}"/> derviced class is recommended approach.
		/// </remarks>
		protected abstract Func<ServerRequestContext, ServerResponseContext, Task> Dispatch( string methodName );

		/// <summary>
		///		Sets the return value to the <see cref="ServerResponseContext"/>.
		/// </summary>
		/// <typeparam name="T">The type of the return value.</typeparam>
		/// <param name="context">The <see cref="ServerResponseContext"/> to be set the return value.</param>
		/// <param name="returnValue">The return value to be set.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="context"/> is <c>null</c>.
		/// </exception>
		protected void SetReturnValue<T>( ServerResponseContext context, T returnValue )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			Contract.EndContractBlock();

			context.Serialize( returnValue, RpcErrorMessage.Success, this.SerializationContext.GetSerializer<T>() );
		}

		/// <summary>
		///		Sets the exception to the <see cref="ServerResponseContext"/> as called method failure.
		/// </summary>
		/// <param name="context">The <see cref="ServerResponseContext"/> to be set the error.</param>
		/// <param name="operationId">The ID of operation which causes <paramref name="exception"/>.</param>
		/// <param name="exception">The exception to be set as the RPC error.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="context"/> is <c>null</c>.
		///		Or, <paramref name="exception"/> is <c>null</c>.
		/// </exception>
		/// <remarks>
		///		You should use <see cref="RpcException"/> derived class to represent application error.
		///		The runtime does not interpret other exception types except <see cref="ArgumentException"/> derviced class,
		///		so they are represented as <see cref="RpcError.CallError"/> in the lump.
		///		(<see cref="ArgumentException"/> derviced class is transformed to <see cref="P:RpcError.ArgumentError"/>.
		/// </remarks>
		protected void SetException( ServerResponseContext context, string operationId, Exception exception )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			if ( exception == null )
			{
				throw new ArgumentNullException( "exception" );
			}

			Contract.EndContractBlock();

			context.Serialize<MessagePackObject>( MessagePackObject.Nil, InvocationHelper.HandleInvocationException( exception, operationId, this.IsDebugMode ), this.SerializationContext.GetSerializer<MessagePackObject>() );
		}

		/// <summary>
		///		Notifies to RPC runtime to begin service operation.
		/// </summary>
		/// <remarks>
		///		Currently, this method performs following:
		///		<list type="bullet">
		///			<item>Sets <see cref="RpcApplicationContext"/>.</item>
		///			<item>Starts execution timeout wathing.</item>
		///		</list>
		/// </remarks>
		protected void BeginOperation()
		{
			this._runtime.BeginOperation();
		}

		/// <summary>
		///		Notifies to RPC runtime to end service operation.
		/// </summary>
		/// <remarks>
		///		Currently, this method performs following:
		///		<list type="bullet">
		///			<item>Clear <see cref="RpcApplicationContext"/>.</item>
		///			<item>Stop execution timeout wathing.</item>
		///		</list>
		/// </remarks>
		protected void EndOperation()
		{
			this._runtime.EndOperation();
		}

		/// <summary>
		///		Handles a <see cref="ThreadAbortException"/> if it is thrown because hard execution timeout.
		/// </summary>
		/// <param name="mayBeHardTimeoutException">A <see cref="ThreadAbortException"/> if it may be thrown because hard execution timeout.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="mayBeHardTimeoutException"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ThreadStateException">
		///		<paramref name="mayBeHardTimeoutException"/> is not thrown on the current thread.
		/// </exception>
		protected void HandleThreadAbortException( ThreadAbortException mayBeHardTimeoutException )
		{
			this._runtime.HandleThreadAbortException( mayBeHardTimeoutException );
		}
	}

	internal abstract class DispatcherContracts : Dispatcher
	{
		public DispatcherContracts( RpcServer server ) : base( server ) { }

		protected override Func<ServerRequestContext, ServerResponseContext, Task> Dispatch( string methodName )
		{
			Contract.Requires( !String.IsNullOrEmpty( methodName ) );
			Contract.Ensures( Contract.Result<Func<ServerRequestContext, ServerResponseContext, Task>>() != null );

			return null;
		}
	}

}