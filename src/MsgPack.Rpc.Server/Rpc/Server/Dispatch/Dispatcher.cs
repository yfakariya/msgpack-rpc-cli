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
		private readonly SerializationContext _serializationContext;

		protected SerializationContext SerializationContext
		{
			get { return this._serializationContext; }
		}

		protected Dispatcher( RpcServer server )
		{
			if ( server == null )
			{
				throw new ArgumentNullException( "server" );
			}

			this._serializationContext = server.SerializationContext;
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
					requestContext.MessageId,
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
			var sessionState = Tuple.Create( requestContext.SessionId, requestContext.MessageType == MessageType.Request ? requestContext.MessageId : default( int? ) );

#if NET_4_5
			task.ContinueWith( ( previous, state ) =>
				{
					previous.Dispose();
					var tuple = state as Tuple<ServerTransport, ServerResponseContext>
					tuple.Item1.Send( tuple.Item2 );
				},
				Tuple.Create( serverTransport, responseContext )
			).ContinueWith( ( previous, state ) =>
				{
					Tracer.Dispatch.TraceEvent(
						Tracer.EventType.ErrorWhenSendResponse,
						Tracer.EventId.ErrorWhenSendResponse,
						"Failed to send response. [ \"SessionID\" : {0}, \"MessageID\" : {1}, \"Error\" : \"{2}\" ]",
						state.Item1,
						state.Item2,
						previous.Exception
					);

					previous.Dispose();
				},
				TaskContinuationOptions.OnlyOnFaulted,
				sessionState
			);
#else
			task.ContinueWith( previous =>
				{
					previous.Dispose();
					serverTransport.Send( responseContext );
				}
			).ContinueWith( previous =>
				{
					Tracer.Dispatch.TraceEvent(
						Tracer.EventType.ErrorWhenSendResponse,
						Tracer.EventId.ErrorWhenSendResponse,
						"Failed to send response. [ \"SessionID\" : {0}, \"MessageID\" : {1}, \"Error\" : \"{2}\" ]",
						sessionState.Item1,
						sessionState.Item2,
						previous.Exception
					);

					previous.Dispose();
				},
				TaskContinuationOptions.OnlyOnFaulted
			);
#endif
		}

		protected abstract Func<ServerRequestContext, ServerResponseContext, Task> Dispatch( string methodName );

		protected void SetReturnValue<T>( ServerResponseContext context, T returnValue )
		{
			context.Serialize( returnValue, RpcErrorMessage.Success, this._serializationContext.GetSerializer<T>() );
		}

		protected void SetException( ServerResponseContext context, Exception exception )
		{
			context.Serialize<MessagePackObject>( MessagePackObject.Nil, InvocationHelper.HandleInvocationException( exception ), this._serializationContext.GetSerializer<MessagePackObject>() );
		}
	}
}