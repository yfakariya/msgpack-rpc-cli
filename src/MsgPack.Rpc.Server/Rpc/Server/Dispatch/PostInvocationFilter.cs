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
	///		Defines interfaces of post invocation filter of method invocation via <see cref="MethodInvoker"/>.
	/// </summary>
	/// <remarks>
	///		<para>
	///			PreInvocationFilters are typically used to trace method invocation.
	///		</para>
	///		<para>
	///			If you want to collaborate with PreInvocationHandler, you can use <see cref="System.Diagnostics.CorrelationManager"/>
	///			via <see cref="System.Diagnostics.Trace.CorrelationManager"/>.
	///			<see cref="System.Diagnostics.CorrelationManager"/> stores logical operation stack in <see cref="System.Runtime.Remoting.Messaging.CallContext"/> of current logical thread.
	///			Logical thread can across AppDomains.
	///		</para>
	///		<note>
	///			MessagePack-RPC does not transfer CallContext as default.
	///		</note>
	/// </remarks>
	public abstract class PostInvocationFilter
	{
		internal InvocationResult Process( MethodInfo targetMethod, InvocationResult result )
		{
			Contract.Assert( targetMethod != null );

			return this.HandleCore( targetMethod, result );
		}

		protected abstract InvocationResult HandleCore( MethodInfo targetMethod, InvocationResult result );
	}
}
