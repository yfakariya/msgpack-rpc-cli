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
	/// <typeparam name="TExternal">
	///		The type of privately leased object which holds expensive resource.
	/// </typeparam>
	/// <remarks>
	///		This class is thread-safe, but the derived type might not be thread-safe.
	/// </remarks>
	public sealed class ForgettableObjectLease<TExternal, TInternal> : ObjectLease<TExternal, TInternal>
		where TExternal : class
		where TInternal : class
	{
		/// <summary>
		///		Initializes a new instance of the <see cref="ForgettableObjectLease&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="externalValue">The exposed value.</param>
		/// <param name="initialInternalValue">The initial internal value.</param>
		public ForgettableObjectLease( TExternal externalValue, TInternal initialInternalValue )
			: base( externalValue, initialInternalValue ) { }

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
