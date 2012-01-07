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
	/// <typeparam name="T">
	/// </typeparam>
	public sealed class FinalizableObjectLease<T> : ObjectLease<T>
			where T : class
	{
		private Action<T> _returning;

		public FinalizableObjectLease( T initialValue, Action<T> returning )
			: base( null )
		{
			if ( returning == null )
			{
				GC.SuppressFinalize( this );
				throw new ArgumentNullException( "returning" );
			}

			RuntimeHelpers.PrepareConstrainedRegions();
			try { }
			finally
			{
				this._returning = returning;
				this.Value = initialValue;
			}
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
					var value = this.Value;
					this.Value = null;
					returning( value );
					base.Dispose( disposing );
				}
			}
		}
	}
}
