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
	// TODO: Move to NLiblet
	/// <summary>
	///		Basic implementation of <see cref="ILease{T}"/>.
	/// </summary>
	/// <typeparam name="T">
	///		The type of leased object.
	/// </typeparam>
	/// <remarks>
	///		This class is thread-safe, but the derived type might not be thread-safe.
	/// </remarks>
	public abstract class ObjectLease<T> : ILease<T>
			where T : class
	{
		private bool _isDisposed;
		private T _value;

		/// <summary>
		///		Gets the leased object itself.
		/// </summary>
		/// <value>
		///		The leased object itself.
		/// </value>
		/// <exception cref="ObjectDisposedException">
		///		This instance is already disposed.
		/// </exception>
		/// <remarks>
		///		The derived type can set this value via setter.
		/// </remarks>
		public T Value
		{
			get
			{
				this.VerifyIsNotDisposed();
				return this._value;
			}
			protected set
			{
				this.VerifyIsNotDisposed();
				Interlocked.Exchange( ref this._value, value );
			}
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="ObjectLease&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="initialValue">The initial value.</param>
		protected ObjectLease( T initialValue )
		{
			this._value = initialValue;
		}

		/// <summary>
		///		Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			this.Dispose( true );
			GC.SuppressFinalize( this );
		}

		/// <summary>
		///		Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing">
		///		<c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.
		///	</param>
		protected virtual void Dispose( bool disposing )
		{
			if ( !this._isDisposed )
			{
				this._isDisposed = true;
				Thread.MemoryBarrier();
			}
		}

		private void VerifyIsNotDisposed()
		{
			if ( this._isDisposed )
			{
				throw new ObjectDisposedException( this.GetType().FullName );
			}
		}
	}
}
