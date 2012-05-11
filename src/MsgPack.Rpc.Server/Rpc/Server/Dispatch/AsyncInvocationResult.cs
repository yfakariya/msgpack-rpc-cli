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
using System.Threading.Tasks;

namespace MsgPack.Rpc.Server.Dispatch
{
	/// <summary>
	///		Represents the result of asynchronous operation invocation in <see cref="AsyncServiceInvoker{T}"/> derived classes.
	/// </summary>
	public sealed class AsyncInvocationResult
	{
		private readonly Task _asyncTask;

		/// <summary>
		///		Gets the <see cref="Task"/> which represents asynchronous operation invocation itself.
		/// </summary>
		/// <value>
		///		The <see cref="Task"/> which represents asynchronous operation invocation itself.
		///		This value will be <c>null</c> when invocation itself was failed.
		/// </value>
		public Task AsyncTask
		{
			get { return this._asyncTask; }
		}

		private readonly RpcErrorMessage _invocationError;

		/// <summary>
		///		Gets the <see cref="RpcErrorMessage"/> which represents the error of invocation itself.
		/// </summary>
		/// <value>
		///		The <see cref="RpcErrorMessage"/> which represents the error of invocation itself.
		/// </value>
		public RpcErrorMessage InvocationError
		{
			get { return this._invocationError; }
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="AsyncInvocationResult"/> class.
		/// </summary>
		/// <param name="asyncTask">
		///		The <see cref="Task"/> which represents asynchronous operation invocation itself.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="asyncTask"/> is <c>null</c>.
		/// </exception>
		public AsyncInvocationResult( Task asyncTask )
		{
			if ( asyncTask == null )
			{
				throw new ArgumentNullException( "asyncTask" );
			}

			Contract.EndContractBlock();

			this._asyncTask = asyncTask;
			this._invocationError = RpcErrorMessage.Success;
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="AsyncInvocationResult"/> class as error result.
		/// </summary>
		/// <param name="invocationError">
		///		The <see cref="RpcErrorMessage"/> which represents the error of invocation itself.
		/// </param>
		public AsyncInvocationResult( RpcErrorMessage invocationError )
		{
			this._asyncTask = null;
			this._invocationError = invocationError;
		}
	}
}
