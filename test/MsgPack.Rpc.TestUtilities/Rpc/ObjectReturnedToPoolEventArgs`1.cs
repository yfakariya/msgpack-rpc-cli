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
using System.Linq;
using System.Text;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Holds event information for <see cref="E:SingletonObjectPool{T}.ObjectReturned"/> event.
	/// </summary>
	/// <typeparam name="T">The type of pooled object.</typeparam>
	public sealed class ObjectReturnedToPoolEventArgs<T> : EventArgs
	{
		private readonly T _returnedObject;

		/// <summary>
		///		Gets the returned object.
		/// </summary>
		/// <value>
		///		The returned object.
		/// </value>
		public T ReturnedObject
		{
			get
			{
				Contract.Ensures( Contract.Result<T>() != null );

				return _returnedObject;
			}
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="ObjectReturnedToPoolEventArgs&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="returnedObject">The returned object.</param>
		public ObjectReturnedToPoolEventArgs( T returnedObject )
		{
			if ( returnedObject == null )
			{
				throw new ArgumentNullException( "returnedObject" );
			}

			Contract.EndContractBlock();

			this._returnedObject = returnedObject;
		}
	}
}
