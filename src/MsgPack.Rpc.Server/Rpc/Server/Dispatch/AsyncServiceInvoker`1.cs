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
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Threading.Tasks;
using MsgPack.Rpc.Protocols;
using MsgPack.Rpc.Server.Protocols;
using MsgPack.Serialization;
using System.Security;
using System.Security.Permissions;

namespace MsgPack.Rpc.Server.Dispatch
{

	/// <summary>
	///		Defines common features of emitting asynchronous service invokers.
	/// </summary>
	/// <typeparam name="T">
	///		Type of the return value of the target method.
	///		<see cref="Missing"/> when the traget method returns void.
	/// </typeparam>
	[ContractClass( typeof( AsyncServiceInvokerContract<> ) )]
	public abstract class AsyncServiceInvoker<T> : AsyncServiceInvoker
	{
		private static readonly SecurityPermission _unmanagedCodePermission = new SecurityPermission( SecurityPermissionFlag.UnmanagedCode );

		private readonly MessagePackSerializer<T> _returnValueSerializer;

		/// <summary>
		///		Initializes a new instance of the <see cref="AsyncServiceInvoker&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="runtime">The <see cref="RpcServerRuntime"/> which provides runtime services.</param>
		/// <param name="serviceDescription">The service description which defines target operation.</param>
		/// <param name="targetOperation">The target operation to be invoked.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="runtime"/> is <c>null</c>.
		///		Or, <paramref name="serviceDescription"/> is <c>null</c>.
		///		Or, <paramref name="targetOperation"/> is <c>null</c>.
		/// </exception>
		protected AsyncServiceInvoker( RpcServerRuntime runtime, ServiceDescription serviceDescription, MethodInfo targetOperation )
			: base( runtime, serviceDescription, targetOperation )
		{
			this._returnValueSerializer = typeof( T ) == typeof( Missing ) ? null : runtime.SerializationContext.GetSerializer<T>();
		}

		/// <summary>
		///		Invokes target service operation asynchronously.
		/// </summary>
		/// <param name="requestContext">
		///		The context object to hold request data.
		///		Note that properties of the context is only valid until this method returns.
		///		That is, it will be unpredectable state in the asynchronous operation.
		///	</param>
		/// <param name="responseContext">
		///		The context object to pack response value or error.
		///		This is <c>null</c> for the notification messages.
		///	</param>
		/// <returns>
		///		The <see cref="Task"/> to control entire process including sending response.
		/// </returns>
		[SuppressMessage( "Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Transfers the exception." )]
		public sealed override Task InvokeAsync( ServerRequestContext requestContext, ServerResponseContext responseContext )
		{
			if ( requestContext == null )
			{
				throw new ArgumentNullException( "requestContext" );
			}

			Contract.Ensures( Contract.Result<Task>() != null );

			var messageId = requestContext.MessageId;
			var arguments = requestContext.ArgumentsUnpacker;

			bool readMustSuccess = arguments.Read();
			Contract.Assert( readMustSuccess, "Arguments is not an array." );
			Contract.Assert( arguments.IsArrayHeader );

			SafeStartLogicalOperation();
			if ( MsgPackRpcServerDispatchTrace.ShouldTrace( MsgPackRpcServerDispatchTrace.OperationStart ) )
			{
				MsgPackRpcServerDispatchTrace.TraceData(
					MsgPackRpcServerDispatchTrace.OperationStart,
					"Operation starting.",
					responseContext == null ? MessageType.Notification : MessageType.Request,
					messageId,
					this.OperationId
				);
			}

			AsyncInvocationResult result;
			try
			{
				result = this.InvokeCore( arguments );
			}
			catch ( Exception ex )
			{
				result = new AsyncInvocationResult( InvocationHelper.HandleInvocationException( requestContext.SessionId, requestContext.MessageType, requestContext.MessageId, this.OperationId, ex, this.IsDebugMode ) );
			}

			var tuple = Tuple.Create( this, requestContext.SessionId, messageId.GetValueOrDefault(), this.OperationId, responseContext, result.InvocationError );
			if ( result.AsyncTask == null )
			{
				return Task.Factory.StartNew( state => HandleInvocationResult( null, state as Tuple<AsyncServiceInvoker<T>, long, int, string, ServerResponseContext, RpcErrorMessage> ), tuple );
			}
			else
			{
#if NET_4_5
					return
						result.AsyncTask.ContinueWith(
							( previous, state ) => HandleInvocationResult( 
								previous, 
								state as Tuple<AsyncServiceInvoker<T>, long, int, string, ServerResponseContext, RpcErrorMessage>
							),
							tuple
						);
#else
				return
					result.AsyncTask.ContinueWith(
						previous => HandleInvocationResult(
							previous,
							tuple
						)
					);
#endif
			}
		}

		private static void HandleInvocationResult( Task previous, Tuple<AsyncServiceInvoker<T>, long, int, string, ServerResponseContext, RpcErrorMessage> closureState )
		{
			var @this = closureState.Item1;
			var sessionId = closureState.Item2;
			var messageId = closureState.Item3;
			var operationId = closureState.Item4;
			var responseContext = closureState.Item5;
			var error = closureState.Item6;

			T result = default( T );
			try
			{
				if ( previous != null )
				{
					if ( error.IsSuccess )
					{
						if ( previous.Exception != null )
						{
							error =
								InvocationHelper.HandleInvocationException(
									sessionId,
									responseContext == null ? MessageType.Notification : MessageType.Request,
									responseContext == null ? default( int? ) : messageId,
									operationId,
									previous.Exception.InnerException,
									@this.IsDebugMode
								);
						}
						else if ( @this._returnValueSerializer != null )
						{
							result = ( previous as Task<T> ).Result;
						}
					}

					previous.Dispose();
				}

				InvocationHelper.TraceInvocationResult( sessionId, responseContext == null ? MessageType.Notification : MessageType.Request, messageId, @this.OperationId, error, result );
			}
			finally
			{
				SafeStopLogicalOperation();
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
		/// <returns>
		///		An <see cref="AsyncInvocationResult"/> which represents the result of asynchronopus operation invocation.
		/// </returns>
		/// <remarks>
		///		<paramref name="arguments"/> will be disposed asynchronously after this method returns.
		///		So, you must deserialize all arguments synchronously, or copy its content as <see cref="MessagePackObject"/> tree.
		/// </remarks>
		protected abstract AsyncInvocationResult InvokeCore( Unpacker arguments );

		private static void SafeStartLogicalOperation()
		{
			try
			{
				PrivilegedStartLogicalOperation();
			}
			catch ( SecurityException ) { }
			catch ( MemberAccessException ) { }
		}

		[SecuritySafeCritical]
		[SuppressMessage( "Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands" )]
		[SuppressMessage( "Microsoft.Security", "CA2106:SecureAsserts" )]
		private static void PrivilegedStartLogicalOperation()
		{
			_unmanagedCodePermission.Assert();
			try
			{
				Trace.CorrelationManager.StartLogicalOperation();
			}
			finally
			{
				SecurityPermission.RevertAssert();
			}
		}

		private static void SafeStopLogicalOperation()
		{
			PrivilegedStopLogicalOperation();
		}

		[SecuritySafeCritical]
		[SuppressMessage( "Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands" )]
		private static void PrivilegedStopLogicalOperation()
		{
			_unmanagedCodePermission.Assert();
			try
			{
				Trace.CorrelationManager.StopLogicalOperation();
			}
			finally
			{
				SecurityPermission.RevertAssert();
			}
		}
	}

	[ContractClassFor( typeof( AsyncServiceInvoker<> ) )]
	internal abstract class AsyncServiceInvokerContract<T> : AsyncServiceInvoker<T>
	{
		protected AsyncServiceInvokerContract() : base( null, null, null ) { }

		protected override AsyncInvocationResult InvokeCore( Unpacker arguments )
		{
			Contract.Requires( arguments != null );
			Contract.Requires( arguments.IsArrayHeader );
			Contract.Ensures( Contract.Result<AsyncInvocationResult>() != null );
			Contract.Ensures( Contract.Result<AsyncInvocationResult>().AsyncTask != null || !Contract.Result<AsyncInvocationResult>().InvocationError.IsSuccess );

			return null;
		}
	}
}