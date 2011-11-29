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

namespace MsgPack.Rpc.Dispatch
{
	/// <summary>
	///		Defines interfaces of error handler of method invocation via <see cref="MethodInvoker"/>.
	/// </summary>
	/// <remarks>
	///		InvocationErrorHandler can swap thrown exception to another exception or non-exceptional result (e.g. error code).
	///		But InvocationErrorHandlers typically do:
	///		<list type="bullet">
	///			<item>Filter internal error information to secure error message to respond.</item>
	///			<item>Log error information for trouble shooting.</item>
	///		</list>
	/// </remarks>
	public abstract class InvocationErrorHandler
	{
		internal InvocationResult? Handle( MethodInfo targetMethod, Exception error )
		{
			Contract.Assert( targetMethod != null );
			Contract.Assert( error != null );

			return this.HandleCore( targetMethod, error );
		}

		protected abstract InvocationResult? HandleCore( MethodInfo targetMethod, Exception error );
	}
}
