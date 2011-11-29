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

namespace MsgPack.Rpc.Dispatch
{
	/// <summary>
	///		Represents result of method invocation, which is void, any return value, or exception.
	/// </summary>
	public struct InvocationResult
	{
		private readonly object _returnValue;

		public object ReturnValue
		{
			get { return this._returnValue; }
		}

		private readonly bool _isVoid;

		public bool IsVoid
		{
			get { return this._isVoid; }
		}

		private readonly Exception _exception;

		public Exception Exception
		{
			get { return this._exception; }
		}

		public InvocationResult( object returnValue, bool isVoid )
		{
			this._returnValue = returnValue;
			this._isVoid = isVoid;
			this._exception = null;
		}

		public InvocationResult( Exception exception, bool isVoid )
		{
			if ( exception == null )
			{
				throw new ArgumentNullException( "exception" );
			}

			this._returnValue = null;
			this._isVoid = isVoid;
			this._exception = exception;
		}
	}
}
