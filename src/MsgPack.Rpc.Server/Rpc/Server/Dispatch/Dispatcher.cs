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
using System.Threading.Tasks;
using MsgPack.Rpc.Protocols;
using MsgPack.Rpc.Server.Dispatch;
using MsgPack.Rpc.Server.Protocols;
using MsgPack.Serialization;

namespace MsgPack.Rpc.Server
{
	public abstract class Dispatcher
	{
		private readonly RpcServer _server;

		protected SerializationContext SerializationContext
		{
			get { return this._server.SerializationContext; }
		}

		protected Dispatcher( RpcServer server )
		{
			if ( server == null )
			{
				throw new ArgumentNullException( "server" );
			}

			this._server = server;
		}

		internal void Dispatch( ServerTransport serverTransport, ServerRequestContext requestContext )
		{
			ServerResponseContext responseContext = null;
			if ( requestContext.MessageType == MessageType.Request )
			{
				responseContext = serverTransport.Manager.ResponseContextPool.Borrow();
				responseContext.MessageId = requestContext.MessageId;
				responseContext.SessionId = requestContext.SessionId;
				responseContext.SetTransport( serverTransport );
			}

			var operation = this.Dispatch( requestContext.MethodName );
			if ( operation == null )
			{
				var error = new RpcErrorMessage( RpcError.NoMethodError, "Operation does not exist.", null );
				InvocationHelper.TraceInvocationResult<object>(
					responseContext.SessionId,
					requestContext.MessageType,
					requestContext.MessageId.GetValueOrDefault(),
					requestContext.MethodName,
					error,
					null
				);

				if ( responseContext != null )
				{
					responseContext.Serialize<object>( null, error, null );
				}

				return;
			}

			var task = operation( requestContext, responseContext );
			var sessionState = Tuple.Create( this._server, requestContext.SessionId, requestContext.MessageType == MessageType.Request ? requestContext.MessageId : default( int? ) );

			// FIXME: Execution timeout
#if NET_4_5
			task.ContinueWith( ( previous, state ) =>
				{
					var tuple = state as Tuple<ServerTransport, ServerResponseContext>
					SendResponse( previous, tuple.Item1, tuple.Item2 )
				},
				Tuple.Create( serverTransport, responseContext )
			).ContinueWith( ( previous, state ) =>
				{
					HandleSendFailure( previous, state as Tuple<RpcServer, long, int?> );
				},
				TaskContinuationOptions.OnlyOnFaulted,
				sessionState
			);
#else
			task.ContinueWith( previous =>
				{
					SendResponse( previous, serverTransport, responseContext );
				}
			).ContinueWith( previous =>
				{
					HandleSendFailure( previous, sessionState );
				},
				TaskContinuationOptions.OnlyOnFaulted
			);
#endif
		}

		private static void SendResponse( Task previous, ServerTransport transport, ServerResponseContext context )
		{
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

		private static void HandleSendFailure( Task previous, Tuple<RpcServer, long, int?> sessionState )
		{
			var exception = previous.Exception;
			Tracer.Dispatch.TraceEvent(
				Tracer.EventType.ErrorWhenSendResponse,
				Tracer.EventId.ErrorWhenSendResponse,
				"Failed to send response. [ \"SessionID\" : {0}, \"MessageID\" : {1}, \"Error\" : \"{2}\" ]",
				sessionState.Item2,
				sessionState.Item3,
				exception
			);

			previous.Dispose();
			sessionState.Item1.RaiseServerError( exception );
		}

		protected abstract Func<ServerRequestContext, ServerResponseContext, Task> Dispatch( string methodName );

		protected void SetReturnValue<T>( ServerResponseContext context, T returnValue )
		{
			context.Serialize( returnValue, RpcErrorMessage.Success, this.SerializationContext.GetSerializer<T>() );
		}

		protected void SetException( ServerResponseContext context, Exception exception )
		{
			context.Serialize<MessagePackObject>( MessagePackObject.Nil, InvocationHelper.HandleInvocationException( exception ), this.SerializationContext.GetSerializer<MessagePackObject>() );
		}
	}
}