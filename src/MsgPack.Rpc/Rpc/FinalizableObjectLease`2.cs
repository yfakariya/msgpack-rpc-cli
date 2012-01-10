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
using System.Runtime.CompilerServices;
using System.Threading;

namespace MsgPack.Rpc
{
	// TODO: Move to NLiblet
	/// <summary>
	///		The simple implementation of the <see cref="ObjectLease{T}"/>.
	/// </summary>
	/// <typeparam name="TExternal">
	///		The type of privately leased object which holds expensive resource.
	/// </typeparam>
	/// <remarks>
	///		This class is thread-safe, but the derived type might not be thread-safe.
	/// </remarks>
	public sealed class FinalizableObjectLease<TExternal, TInternal> : ObjectLease<TExternal, TInternal>
		where TExternal : class
		where TInternal : class
	{
		private Action<TInternal> _returning;

		/// <summary>
		/// Initializes a new instance of the <see cref="FinalizableObjectLease&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="externalValue">The exposed value.</param>
		/// <param name="initialInternalValue">The initial internal value.</param>
		/// <param name="returning">The returning.</param>
		public FinalizableObjectLease( TExternal externalValue, TInternal initialInternalValue, Action<TInternal> returning )
			: base( externalValue, initialInternalValue )
		{
			if ( returning == null )
			{
				GC.SuppressFinalize( this );
				throw new ArgumentNullException( "returning" );
			}

			this._returning = returning;
		}

		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="FinalizableObjectLease&lt;T&gt;"/> is reclaimed by garbage collection.
		/// </summary>
		~FinalizableObjectLease()
		{
			this.Dispose( false );
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected sealed override void Dispose( bool disposing )
		{
			try { }
			finally
			{
				var returning = Interlocked.Exchange( ref this._returning, null );
				if ( returning != null )
				{
					var value = this.InternalValue;
					this.InternalValue = null;
					returning( value );
				}
			}

			base.Dispose( disposing );
		}
	}
}
