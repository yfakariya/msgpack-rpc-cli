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
	///		<strong>This type is intended to be used by the infrastructure. Do not use directly from the application.</strong>
	///		Defines common interface the object which can be leased from <see cref="ObjectPool{T}"/>.
	/// </summary>
	/// <typeparam name="T">
	///		The type of the leasing object.
	/// </typeparam>
	public interface ILeaseable<T>
	{
		/// <summary>
		///		<strong>This member is intended to be used by the infrastructure. Do not use directly from the application.</strong>
		///		Sets the <see cref="ILease{T}"/> to handle graceful returning.
		/// </summary>
		/// <param name="lease">
		///		The <see cref="ILease{T}"/> to handle graceful returning.
		///		The pool will pass <c>null</c> for this parameter when the object is returned to pool.
		/// </param>
		/// <exception cref="InvalidOperationException">
		///		This instance is already leased.
		/// </exception>
		void SetLease( ILease<T> lease );
	}
}