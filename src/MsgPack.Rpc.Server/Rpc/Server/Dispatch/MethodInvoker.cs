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
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace MsgPack.Rpc.Dispatch
{
	/// <summary>
	///		Responsible for method invocation.
	/// </summary>
	public abstract class MethodInvoker
	{
		private readonly RuntimeMethodHandle _targetMethod;

		public RuntimeMethodHandle TargetMethod
		{
			get { return this._targetMethod; }
		}

		private readonly Func<object> _instanceFactory;

		public object GetInstance()
		{
			return this._instanceFactory();
		}

		protected MethodInvoker( RuntimeMethodHandle targetMethod, Func<object> instanceFactory )
		{
			if ( instanceFactory == null )
			{
				throw new ArgumentNullException( "instanceFactory" );
			}

			Contract.EndContractBlock();

			this._targetMethod = targetMethod;
			this._instanceFactory = instanceFactory;
		}

		public InvocationResult Invoke( IList<MessagePackObject> arguments )
		{
			if ( arguments == null )
			{
				throw new ArgumentNullException( "arguments" );
			}

			Contract.EndContractBlock();

			return this.InvokeCore( arguments );
		}

		protected abstract InvocationResult InvokeCore( IList<MessagePackObject> arguments );

		protected static object ConvertEnumerable( IEnumerable<MessagePackObject> enumerable, Type itemType )
		{
			// TODO: write tt
			throw new NotImplementedException();
		}

		protected static object ConvertList( IList<MessagePackObject> list, Type itemType )
		{
			// TODO: write tt
			throw new NotImplementedException();
		}

		protected static object ConvertEnumerable( IDictionary<MessagePackObject, MessagePackObject> dictionary, Type keyType, Type valueType )
		{
			// TODO: write tt
			throw new NotImplementedException();
		}
	}
}
