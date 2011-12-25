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
using MsgPack.Rpc.Protocols;
using MsgPack.Serialization;
using System.Threading.Tasks;

namespace MsgPack.Rpc.Server.Dispatch
{
	/// <summary>
	///		Defines common features of emitting service invokers.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal abstract class ServiceInvoker<T> : IServiceInvoker
	{
		internal static readonly MethodInfo InvokeCoreMethod =
			typeof( ServiceInvoker<T> ).GetMethod( "InvokeCore", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );

		private readonly MessagePackSerializer<T> _returnValueSerializer;
		private readonly string _operationId;
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

		protected ServiceInvoker( SerializationContext context, ServiceDescription serviceDescription, MethodInfo targetOperation )
		{
			Debug.Assert( context != null );
			Debug.Assert( serviceDescription != null );
			Debug.Assert( targetOperation != null );

			this._serviceDescription = serviceDescription;
			this._targetOperation = targetOperation;
			this._operationId = serviceDescription.ToString() + "::" + targetOperation.Name;
			this._returnValueSerializer = context.GetSerializer<T>();
		}

		/// <summary>
		///		Invokes target service operation.
		/// </summary>
		/// <param name="arguments"><see cref="Unpacker"/> to unpack arguments.</param>
		/// <param name="messageId">Id of the current request message.
		/// This value is not defined for the notification messages.</param>
		/// <param name="responsePacker"><see cref="Packer"/> to pack return values and errors to the request message.
		/// This is <c>null</c> for the notification messages.</param>
		public void Invoke( Unpacker arguments, int messageId, Packer responsePacker )
		{
			if ( arguments == null )
			{
				throw new ArgumentNullException( "arguments" );
			}

			T returnValue;
			RpcErrorMessage error;
			Trace.CorrelationManager.StartLogicalOperation();
			if ( Tracer.Server.Switch.ShouldTrace( Tracer.EventType.OperationStart ) )
			{
				Tracer.Server.TraceData(
					Tracer.EventType.OperationStart,
					Tracer.EventId.OperationStart,
					responsePacker == null ? MessageType.Notification : MessageType.Request,
					messageId,
					this._operationId
				);
			}

			try
			{
				this.InvokeCore( arguments, out returnValue, out error );

				if ( error.IsSuccess )
				{
					if ( Tracer.Server.Switch.ShouldTrace( Tracer.EventType.OperationSucceeded ) )
					{
						// FIXME: Formatting
						Tracer.Server.TraceData(
							Tracer.EventType.OperationSucceeded,
							Tracer.EventId.OperationSucceeded,
							responsePacker == null ? MessageType.Notification : MessageType.Request,
							messageId,
							this._operationId,
							returnValue
						);
					}
				}
				else
				{
					Tracer.Server.TraceData(
						Tracer.EventType.OperationFailed,
						Tracer.EventId.OperationFailed,
						responsePacker == null ? MessageType.Notification : MessageType.Request,
						messageId,
						this._operationId,
						error.ToString()
					);
				}
			}
			finally
			{
				Trace.CorrelationManager.StopLogicalOperation();
			}

			if ( responsePacker != null )
			{
				this.SerializeResponse( responsePacker, messageId, returnValue, error );
			}
		}

		private void SerializeResponse( Packer responsePacker, int messageId, T returnValue, RpcErrorMessage error )
		{
			// Not notification
			responsePacker.PackArrayHeader( 4 );
			responsePacker.Pack( 1 );
			responsePacker.Pack( messageId );
			responsePacker.Pack( error.Error.Identifier );
			if ( error.IsSuccess )
			{
				if ( this._returnValueSerializer == null )
				{
					// void
					responsePacker.PackNull();
				}
				else
				{
					this._returnValueSerializer.PackTo( responsePacker, returnValue );
				}
			}
			else
			{
				responsePacker.Pack( error.Detail );
			}
		}

		/// <summary>
		///		Invokes target service operation.
		/// </summary>
		/// <param name="arguments"><see cref="Unpacker"/> to unpack arguments.</param>
		/// <param name="returnValue">The return value will be stored.</param>
		/// <param name="error">The RPC error will be stored.</param>
		protected abstract void InvokeCore( Unpacker arguments, out T returnValue, out RpcErrorMessage error );
	}

	internal struct RpcMethodInvocationResult<T>
	{
		public readonly T ReturnValue;
		public readonly RpcErrorMessage Error;

		public RpcMethodInvocationResult( T returnValue, RpcErrorMessage error )
		{
			this.ReturnValue = returnValue;
			this.Error = error;
		}
	}
}
