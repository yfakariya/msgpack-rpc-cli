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
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using MsgPack.Rpc.Protocols;
using MsgPack.Serialization;
using MsgPack.Rpc.Server.Protocols;
using System.Diagnostics.Contracts;

namespace MsgPack.Rpc.Server.Dispatch
{
	/// <summary>
	///		Defines common features of emitting asynchronous service invokers.
	/// </summary>
	/// <typeparam name="T">
	///		Type of the return value of the target method.
	///		<see cref="Missing"/> when the traget method returns void.
	/// </typeparam>
	internal abstract class AsyncServiceInvoker<T> : IAsyncServiceInvoker
	{
		internal static readonly MethodInfo InvokeCoreMethod =
		typeof( AsyncServiceInvoker<T> ).GetMethod( "InvokeCore", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );

		private readonly MessagePackSerializer<T> _returnValueSerializer;
		private readonly string _operationId;

		/// <summary>
		/// Gets the ID of the operation.
		/// </summary>
		/// <value>
		/// The ID of the operation.
		/// </value>
		public string OperationId
		{
			get { return this._operationId; }
		} 

		private readonly ServiceDescription _serviceDescription;

		/// <summary>
		///		Gets the <see cref="ServiceDescription"/> of target service.
		/// </summary>
		/// <value>
		///		The <see cref="ServiceDescription"/> of target service.
		///		This value will not be <c>null</c>.
		/// </value>
		public ServiceDescription ServiceDescription
		{
			get { return this._serviceDescription; }
		}

		private readonly MethodInfo _targetOperation;

		/// <summary>
		///		Gets the <see cref="MethodInfo"/> of target method.
		/// </summary>
		/// <value>
		///		The <see cref="MethodInfo"/> of target method.
		///		This value will not be <c>null</c>.
		/// </value>
		public MethodInfo TargetOperation
		{
			get { return this._targetOperation; }
		}

		protected AsyncServiceInvoker( SerializationContext context, ServiceDescription serviceDescription, MethodInfo targetOperation )
		{
			Contract.Assert( context != null );
			Contract.Assert( serviceDescription != null );
			Contract.Assert( targetOperation != null );

			this._serviceDescription = serviceDescription;
			this._targetOperation = targetOperation;
			this._operationId = serviceDescription.ToString() + "::" + targetOperation.Name;
			this._returnValueSerializer = context.GetSerializer<T>();
		}
		
		public Task InvokeAsync( ServerRequestContext requestContext, ServerResponseContext responseContext )
		{
			if ( requestContext == null )
			{
				throw new ArgumentNullException( "requestContext" );
			}

			var messageId = requestContext.MessageId;
			var arguments = requestContext.ArgumentsUnpacker;

			Trace.CorrelationManager.StartLogicalOperation();
			if ( Tracer.Dispatch.Switch.ShouldTrace( Tracer.EventType.OperationStart ) )
			{
				Tracer.Dispatch.TraceData(
					Tracer.EventType.OperationStart,
					Tracer.EventId.OperationStart,
					responseContext == null ? MessageType.Notification : MessageType.Request,
					messageId,
					this._operationId
				);
			}

			Task task;
			RpcErrorMessage error;
			this.InvokeCore( arguments, out task, out error );
			var tuple = Tuple.Create( this, requestContext.SessionId, messageId, responseContext, error );
			if ( task == null )
			{
				return Task.Factory.StartNew( state => HandleInvocationResult( null, state as Tuple<AsyncServiceInvoker<T>, long, int, ServerResponseContext, RpcErrorMessage> ), tuple );
			}
			else
			{
#if NET_4_5
					return
						task.ContinueWith(
							( previous, state ) => HandleInvocationResult( 
								previous, 
								state as Tuple<AsyncServiceInvoker<T>, int, Packer, RpcErrorMessage>
							),
							tuple
						);
#else
				return
					task.ContinueWith(
						previous => HandleInvocationResult(
							previous,
							tuple
						)
					);
#endif
			}
		}

		private static void HandleInvocationResult( Task previous, Tuple<AsyncServiceInvoker<T>, long, int, ServerResponseContext, RpcErrorMessage> closureState )
		{
			var @this = closureState.Item1;
			var sessionId = closureState.Item2;
			var messageId = closureState.Item3;
			var responseContext = closureState.Item4;
			var error = closureState.Item5;

			T result = default( T );
			try
			{
				if ( previous != null )
				{
					if ( error.IsSuccess )
					{
						if ( previous.Exception != null )
						{
							error = InvocationHelper.HandleInvocationException( previous.Exception.InnerException );
						}
						else if ( @this._returnValueSerializer != null )
						{
							result = ( previous as Task<T> ).Result;
						}
					}

					previous.Dispose();
				}

				InvocationHelper.TraceInvocationResult( sessionId, responseContext == null ? MessageType.Notification : MessageType.Request, messageId, @this._operationId, error, result );
			}
			finally
			{
				Trace.CorrelationManager.StopLogicalOperation();
			}

			if ( responseContext != null )
			{
				responseContext.Serialize( result, error, @this._returnValueSerializer );
			}
		}

		/// <summary>
		///		Invokes target service operation asynchronously.
		/// </summary>
		/// <param name="arguments"><see cref="Unpacker"/> to unpack arguments.</param>
		/// <param name="task">The <see cref="Task"/> to control asynchronous target invocation will be stored.</param>
		/// <param name="error">The RPC error will be stored.</param>
		protected abstract void InvokeCore( Unpacker arguments, out Task task, out RpcErrorMessage error );
	}
}
