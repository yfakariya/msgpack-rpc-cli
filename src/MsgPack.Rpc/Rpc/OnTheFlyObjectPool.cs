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
	///		The dummy implementation of the <see cref="ObjectPool{T}"/> for mainly testing purposes.
	/// </summary>
	/// <typeparam name="T">
	///		The type of the 'pooled' objects.
	/// </typeparam>
	/// <remarks>
	///		This object actually does not pool any objects, simply creates and returns <typeparamref name="T"/> type instances.
	/// </remarks>
	internal sealed class OnTheFlyObjectPool<T> : ObjectPool<T>
			where T : class, ILeaseable<T>
	{
		private readonly Func<T> _factory;

		/// <summary>
		///		Initializes a new instance of the <see cref="OnTheFlyObjectPool&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="factory">The factory delegate to create <typeparamref name="T"/> type instance.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="factory"/> is <c>null</c>.
		/// </exception>
		public OnTheFlyObjectPool( Func<T> factory )
		{
			if ( factory == null )
			{
				throw new ArgumentNullException( "factory" );
			}

			this._factory = factory;
		}

		protected sealed override T BorrowCore()
		{
			return this._factory();
		}

		protected sealed override ILease<T> Lease( T result )
		{
			return new ForgettableObjectLease<T>( result );
		}

		protected sealed override void ReturnCore( T value )
		{
			// nop
		}
	}
}
