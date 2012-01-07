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

namespace MsgPack.Rpc
{
	// TODO: Move to NLiblet
	/// <summary>
	///		The dummy implementation of the <see cref="ObjectLease{T}"/> for <see cref="OnTheFlyObjectPool{T}"/>.
	/// </summary>
	/// <typeparam name="T">
	///		The type of the leased object.
	/// </typeparam>
	public sealed class ForgettableObjectLease<T> : ObjectLease<T>
			where T : class
	{
		/// <summary>
		///		Initializes a new instance of the <see cref="ForgettableObjectLease&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="initialValue">The initial value.</param>
		public ForgettableObjectLease( T initialValue ) : base( initialValue ) { }

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected sealed override void Dispose( bool disposing )
		{
			base.Dispose( disposing );
			// nop.
		}
	}
}
