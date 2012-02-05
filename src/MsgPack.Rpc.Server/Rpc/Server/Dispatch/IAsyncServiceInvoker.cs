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
using MsgPack.Rpc.Server.Protocols;

namespace MsgPack.Rpc.Server.Dispatch
{
	/// <summary>
	///		Defines non-generic interface for asynchronous service invoker.
	/// </summary>
	internal interface IAsyncServiceInvoker
	{
		/// <summary>
		///		Gets the ID of the operation.
		/// </summary>
		/// <value>
		///		The ID of the operation.
		/// </value>
		string OperationId { get; }

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
		///		<see cref="Task"/> to control entire process including sending response.
		/// </returns>
		Task InvokeAsync( ServerRequestContext requestContext, ServerResponseContext responseContext );
	}
}
