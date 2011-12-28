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
	///		Defines common interfaces and basic features of the object pool.
	/// </summary>
	/// <typeparam name="T">
	///		The type of objects to be pooled.
	/// </typeparam>
	public abstract class ObjectPool<T> : IDisposable
		where T : class, ILeaseable<T>
	{
		/// <summary>
		///		Initializes a new instance of the <see cref="ObjectPool&lt;T&gt;"/> class.
		/// </summary>
		protected ObjectPool() { }

		/// <summary>
		///		Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			this.Dispose( true );
			GC.SuppressFinalize( this );
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected virtual void Dispose( bool disposing )
		{
			// nop
		}

		/// <summary>
		///		Evicts the extra items from current pool.
		/// </summary>
		public virtual void EvictExtraItems() { }

		/// <summary>
		///		Borrows the item from this pool.
		/// </summary>
		/// <returns>
		///		The item borrowed.
		///		This value will not be <c>null</c>.
		/// </returns>
		public T Borrow()
		{
			var result = this.BorrowCore();
			var lease = this.Lease( result );
			result.SetLease( lease );
			return result;
		}

		/// <summary>
		///		Borrows the item from this pool.
		/// </summary>
		/// <returns>
		///		The item borrowed.
		///		This value cannot be <c>null</c>.
		/// </returns>
		protected abstract T BorrowCore();

		/// <summary>
		///		Leases the specified item.
		/// </summary>
		/// <param name="result">The item to be returned from the <see cref="Borrow()"/> method.</param>
		/// <returns>
		///		<see cref="ILease{T}"/> for the <paramref name="result"/>.
		/// </returns>
		protected abstract ILease<T> Lease( T result );

		/// <summary>
		///		Returns the specified borrowed item.
		/// </summary>
		/// <param name="value">The borrowed item.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="value"/> is <c>null</c>.
		/// </exception>
		public void Return( T value )
		{
			if ( value == null )
			{
				throw new ArgumentNullException( "value" );
			}

			this.ReturnCore( value );
		}

		/// <summary>
		///		Returns the specified borrowed item.
		/// </summary>
		/// <param name="value">The borrowed item. This value will not be <c>null</c>.</param>
		protected abstract void ReturnCore( T value );
	}
}
