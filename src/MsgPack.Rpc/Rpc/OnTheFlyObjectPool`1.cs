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

namespace MsgPack.Rpc
{
	/// <summary>
	///		The dummy implementation of the <see cref="ObjectPool{T}"/> for mainly testing purposes.
	/// </summary>
	/// <typeparam name="T">
	///		The type of objects to be pooled.
	/// </typeparam>
	/// <remarks>
	///		This object actually does not pool any objects, simply creates and returns <typeparamref name="T"/> type instances.
	/// </remarks>
	public sealed class OnTheFlyObjectPool<T> : ObjectPool<T>
		where T : class
	{
		private readonly Func<ObjectPoolConfiguration, T> _factory;
		private readonly ObjectPoolConfiguration _configuration;

		/// <summary>
		///		Initializes a new instance of the <see cref="OnTheFlyObjectPool&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="factory">
		///		The factory delegate to create <typeparamref name="T"/> type instance using <see cref="ObjectPoolConfiguration"/>.
		///	</param>
		/// <param name="configuration">
		///		The <see cref="ObjectPoolConfiguration"/> which contains various settings of this object pool.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="factory"/> is <c>null</c>.
		///		Or <paramref name="configuration"/> is <c>null</c>.
		/// </exception>
		public OnTheFlyObjectPool( Func<ObjectPoolConfiguration, T> factory, ObjectPoolConfiguration configuration )
		{
			if ( factory == null )
			{
				throw new ArgumentNullException( "factory" );
			}

			if ( configuration == null )
			{
				throw new ArgumentNullException( "configuration" );
			}

			Contract.EndContractBlock();

			this._factory = factory;
			this._configuration = configuration;
		}

		/// <summary>
		///		Borrows the item from this pool.
		/// </summary>
		/// <returns>
		///		The item borrowed.
		///		This value cannot be <c>null</c>.
		/// </returns>
		protected sealed override T BorrowCore()
		{
			var result = this._factory( this._configuration );
			Contract.Assume( result != null );
			return result;
		}

		/// <summary>
		///		Returns the specified borrowed item.
		/// </summary>
		/// <param name="value">The borrowed item. This value will not be <c>null</c>.</param>
		protected sealed override void ReturnCore( T value )
		{
			// nop.
		}
	}
}
