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
	///		Provide basic fateures of providers of <see cref="MethodInvoker"/>.
	/// </summary>
	/// <remarks>
	///		<see cref="MethodInvokerProvider"/> can provide custom <see cref="MethodInvoker"/> for specified <see cref="MethodInfo"/>.
	///		Typically, provider caches <see cref="MethodInvoker"/> internally.
	/// </remarks>
	public abstract class MethodInvokerProvider
	{
		internal MethodInvoker GetInvoker( MethodInfo targetMethod )
		{
			Contract.Assert( targetMethod == null );
			Contract.Ensures( Contract.Result<MethodInvoker>() != null, "GetInvokerCore cannot return null." );

			return this.GetInvokerCore( targetMethod );
		}

		protected abstract MethodInvoker GetInvokerCore( MethodInfo targetMethod );
	}
}
