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
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using MsgPack.Rpc.Protocols;
using MsgPack.Rpc.Server.Dispatch.Reflection;

namespace MsgPack.Rpc.Server.Dispatch
{
	/// <summary>
	///		<strong>This class is intended to MessagePack-RPC internal use.</strong>
	///		Defines helper methods for current service invoker implementation.
	/// </summary>
	[EditorBrowsable( EditorBrowsableState.Never )]
	public static class InvocationHelper
	{
		/// <summary>
		///		<see cref="MethodInfo"/> of <see cref="HandleArgumentDeserializationException"/>.
		/// </summary>
		internal static readonly MethodInfo HandleArgumentDeserializationExceptionMethod =
			FromExpression.ToMethod( ( Exception exception, string parameterName, bool isDebugMode ) => HandleArgumentDeserializationException( exception, parameterName, isDebugMode ) );

		/// <summary>
		///		<strong>This member is intended to MessagePack-RPC internal use.</strong>
		///		Convert specified argument deserialization error to the RPC error.
		/// </summary>
		/// <param name="exception">The exception thrown by unpacker.</param>
		/// <param name="parameterName">The parameter name failed to deserialize.</param>
		/// <param name="isDebugMode"><c>true</c>, if the server stack is in debug mode; otherwise, <c>false</c>.</param>
		/// <returns>
		///		<see cref="RpcErrorMessage"/>.
		/// </returns>
		[EditorBrowsable( EditorBrowsableState.Never )]
		public static RpcErrorMessage HandleArgumentDeserializationException( Exception exception, string parameterName, bool isDebugMode )
		{
			return
				new RpcErrorMessage(
					RpcError.ArgumentError,
					String.Format( CultureInfo.CurrentCulture, "Argument '{0}' is invalid.", parameterName ),
					( isDebugMode && exception != null ) ? exception.ToString() : null
				);
		}

		/// <summary>
		///		<strong>This member is intended to MessagePack-RPC internal use.</strong>
		///		Convert the exception thrown by target method to the RPC error.
		/// </summary>
		/// <param name="sessionId">The ID of the current session.</param>
		/// <param name="messageType">The type of the inbound message.</param>
		/// <param name="messageId">The ID of the request message. Specify <c>null</c> for the notification message.</param>
		/// <param name="operationId">The ID of the target operation. This value is usually method name.</param>
		/// <param name="exception">The exception thrown by target method.</param>
		/// <param name="isDebugMode"><c>true</c>, if the server stack is in debug mode; otherwise, <c>false</c>.</param>
		/// <returns>
		///		<see cref="RpcErrorMessage"/>.
		/// </returns>
		[EditorBrowsable( EditorBrowsableState.Never )]
		public static RpcErrorMessage HandleInvocationException( long sessionId, MessageType messageType, int? messageId, string operationId, Exception exception, bool isDebugMode )
		{
			MsgPackRpcServerDispatchTrace.TraceEvent(
				MsgPackRpcServerDispatchTrace.OperationThrewException,
				"Operation threw exception. {{ \"SessionId\" : {0}, \"MessageType\" : \"{1}\", \"MessageID\" : {2}, \"OperationID\" : \"{3}\", \"Exception\" : \"{4}\" }}",
				sessionId,
				messageType,
				messageId,
				operationId,
				exception
			);

			return HandleInvocationException( exception, operationId, isDebugMode );
		}

		internal static RpcErrorMessage HandleInvocationException( Exception exception, string operationId, bool isDebugMode )
		{
			ArgumentException asArgumentException;
			RpcException rpcException;
			if ( ( asArgumentException = exception as ArgumentException ) != null )
			{
				return
					new RpcErrorMessage(
						RpcError.ArgumentError,
						String.Format( CultureInfo.CurrentCulture, "Argument '{0}' is invalid.", asArgumentException.ParamName ),
						isDebugMode ? exception.ToString() : null
					);
			}
			else if ( ( rpcException = exception as RpcException ) != null )
			{
				return new RpcErrorMessage( rpcException.RpcError, rpcException.GetExceptionMessage( isDebugMode ) );
			}
			else
			{
				var details =
					new MessagePackObjectDictionary( isDebugMode ? 3 : 1 )
					{
						{ RpcException.MessageKeyUtf8, isDebugMode ? exception.Message : RpcError.CallError.DefaultMessageInvariant } 
					};

				if ( isDebugMode )
				{
					details.Add( RpcException.DebugInformationKeyUtf8, exception.ToString() );
					details.Add( RpcMethodInvocationException.MethodNameKeyUtf8, operationId );
				}

				return
					new RpcErrorMessage(
						RpcError.CallError,
						new MessagePackObject( details, true )
					);
			}
		}

		internal static void TraceInvocationResult<T>( long sessionId, MessageType messageType, int messageId, string operationId, RpcErrorMessage error, T result )
		{
			if ( error.IsSuccess )
			{
				if ( MsgPackRpcServerDispatchTrace.ShouldTrace( MsgPackRpcServerDispatchTrace.OperationSucceeded ) )
				{
					MsgPackRpcServerDispatchTrace.TraceEvent(
						MsgPackRpcServerDispatchTrace.OperationSucceeded,
						"Operation succeeded. {{ \"SessionId\" : {0}, \"MessageType\" : \"{1}\", \"MessageID\" : {2}, \"OperationID\" : \"{3}\", \"Result\" : \"{4}\" }}",
						sessionId,
						messageType,
						messageId,
						operationId,
						result
					);
				}
			}
			else
			{
				MsgPackRpcServerDispatchTrace.TraceEvent(
					MsgPackRpcServerDispatchTrace.OperationFailed,
					"Operation failed. {{ \"SessionId\" : {0}, \"MessageType\" : \"{1}\", \"MessageID\" : {2}, \"OperationID\" : \"{3}\", \"RpcError\" : {4} }}",
					sessionId,
					messageType,
					messageId,
					operationId,
					error
				);
			}
		}
	}
}
