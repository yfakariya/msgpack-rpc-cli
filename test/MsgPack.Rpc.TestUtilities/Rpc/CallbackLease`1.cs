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
	// TODO: NLiblet

	/// <summary>
	///		Implements callback based <see cref="ObjectLease{T}"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public sealed class CallbackLease<T> : ObjectLease<T>
			where T : class
	{
		private readonly Action<bool> _disposed;

		/// <summary>
		/// Initializes a new instance of the <see cref="CallbackLease&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="initialValue">The initial value.</param>
		/// <param name="disposed">
		///		The callback invoked when this instance is disposed.
		///		The first argument is <c>disposing</c> parameter of <see cref="Dispose(bool)"/> method.
		///	</param>
		public CallbackLease( T initialValue, Action<bool> disposed )
			: base( initialValue )
		{
			this._disposed = disposed;
		}

		~CallbackLease()
		{
			this.Dispose( false );
		}

		/// <summary>
		///		Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected sealed override void Dispose( bool disposing )
		{
			base.Dispose( disposing );

			if ( this._disposed != null )
			{
				this._disposed( disposing );
			}
		}
	}
}
