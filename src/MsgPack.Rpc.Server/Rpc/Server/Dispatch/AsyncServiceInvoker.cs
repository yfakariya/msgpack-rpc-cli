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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MsgPack.Rpc.Server.Protocols;

namespace MsgPack.Rpc.Server.Dispatch
{
	/// <summary>
	///		Defines non-generic features of <see cref="AsyncServiceInvoker{T}"/>.
	/// </summary>
	/// <remarks>
	///		You cannot create directly derived class of this type.
	///		Inherit <see cref="AsyncServiceInvoker{T}"/> to extend this class instead.
	/// </remarks>
	public abstract class AsyncServiceInvoker : IAsyncServiceInvoker
	{
		private readonly RpcServerRuntime _runtime;

		/// <summary>
		///		Gets a value indicating whether the server is in debug mode.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if the server is in debug mode; otherwise, <c>false</c>.
		/// </value>
		protected bool IsDebugMode
		{
			get { return this._runtime.IsDebugMode; }
		}

		private readonly string _operationId;

		/// <summary>
		///		Gets the ID of the operation.
		/// </summary>
		/// <value>
		///		The ID of the operation.
		/// </value>
		public string OperationId
		{
			get
			{
				Contract.Ensures( !String.IsNullOrEmpty( Contract.Result<string>() ) );

				return this._operationId;
			}
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
			get
			{
				Contract.Ensures( Contract.Result<ServiceDescription>() != null );

				return this._serviceDescription;
			}
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
			get
			{
				Contract.Ensures( Contract.Result<MethodInfo>() != null );
				return this._targetOperation;
			}
		}

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
		internal AsyncServiceInvoker( RpcServerRuntime runtime, ServiceDescription serviceDescription, MethodInfo targetOperation )
		{
			if ( runtime == null )
			{
				throw new ArgumentNullException( "runtime" );
			}

			if ( serviceDescription == null )
			{
				throw new ArgumentNullException( "serviceDescription" );
			}

			if ( targetOperation == null )
			{
				throw new ArgumentNullException( "targetOperation" );
			}

			Contract.EndContractBlock();

			this._runtime = runtime;
			this._serviceDescription = serviceDescription;
			this._targetOperation = targetOperation;
			this._operationId = ServiceIdentifier.TruncateGenericsSuffix( targetOperation.Name );
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
		///   <see cref="Task"/> to control entire process including sending response.
		/// </returns>
		public abstract Task InvokeAsync( ServerRequestContext requestContext, ServerResponseContext responseContext );

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
		protected internal void BeginOperation()
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
		protected internal void EndOperation()
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
		protected internal void HandleThreadAbortException( ThreadAbortException mayBeHardTimeoutException )
		{
			this._runtime.HandleThreadAbortException( mayBeHardTimeoutException );
		}
	}
}
