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
using System.Threading;

namespace MsgPack.Rpc
{
	/// <summary>
	///		The adapter to <see cref="ObjectPool{T}"/> for the singleton object.
	/// </summary>
	public sealed class SingletonObjectPool<T> : ObjectPool<T>
		where T : class
	{
		private readonly T _singletonInstance;


		/// <summary>
		///		Occurs when the object returned to this pool.
		/// </summary>
		public event EventHandler<ObjectReturnedToPoolEventArgs<T>> ObjectReturned;

		/// <summary>
		///		Initializes a new instance of the <see cref="SingletonObjectPool&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="singletonInstance">The singleton instance.</param>
		public SingletonObjectPool( T singletonInstance )
		{
			this._singletonInstance = singletonInstance;
		}

		/// <summary>
		///		Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing">
		///		<c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.
		///	</param>
		protected override void Dispose( bool disposing )
		{
			if ( disposing )
			{
				var asDisposable = this._singletonInstance as IDisposable;
				if ( asDisposable != null )
				{
					asDisposable.Dispose();
				}
			}

			base.Dispose( disposing );
		}

		/// <summary>
		///		Borrows the item from this pool.
		/// </summary>
		/// <returns>
		///		The item borrowed.
		///		This value cannot be <c>null</c>.
		/// </returns>
		protected override T BorrowCore()
		{
			return this._singletonInstance;
		}

		/// <summary>
		///		Returns the specified borrowed item.
		/// </summary>
		/// <param name="value">The borrowed item. This value will not be <c>null</c>.</param>
		protected override void ReturnCore( T value )
		{
			var handler = Interlocked.CompareExchange( ref this.ObjectReturned, null, null );

			if ( handler != null )
			{
				handler( this, new ObjectReturnedToPoolEventArgs<T>( value ) );
			}
		}
	}
}
