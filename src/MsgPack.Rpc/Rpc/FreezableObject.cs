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
	///		The standard implementation of the <see cref="IFreezable"/> interface.
	/// </summary>
	public abstract class FreezableObject : IFreezable, ICloneable
	{
		private bool _isFrozen;

		/// <summary>
		///		Gets a value indicating whether this instance is frozen.
		/// </summary>
		/// <value>
		///   <c>true</c> if this instance is frozen; otherwise, <c>false</c>.
		/// </value>
		public bool IsFrozen
		{
			get { return this._isFrozen; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FreezableObject"/> class.
		/// </summary>
		protected FreezableObject() { }

		/// <summary>
		///		Verifies this instance is not frozen.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		///		This instance is already frozen.
		/// </exception>
		protected void VerifyIsNotFrozen()
		{
			if ( this._isFrozen )
			{
				throw new InvalidOperationException( "This instance is frozen." );
			}
		}

		/// <summary>
		///		Clones all of the fields of this instance.
		/// </summary>
		/// <returns>
		///		The shallow copy of this instance.
		/// </returns>
		protected virtual FreezableObject CloneCore()
		{
			return this.MemberwiseClone() as FreezableObject;
		}

		/// <summary>
		///		Freezes this instance.
		/// </summary>
		/// <returns>
		///		This instance.
		/// </returns>
		protected virtual FreezableObject FreezeCore()
		{
			if ( !this._isFrozen )
			{
				this._isFrozen = true;
				Thread.MemoryBarrier();
			}

			return this;
		}

		/// <summary>
		/// Gets the frozen copy of this instance.
		/// </summary>
		/// <returns>
		/// This instance if it is already frozen.
		/// Otherwise, frozen copy of this instance.
		/// </returns>
		protected virtual FreezableObject AsFrozenCore()
		{
			if ( this.IsFrozen )
			{
				return this;
			}

			return this.CloneCore().FreezeCore();
		}

		object ICloneable.Clone()
		{
			return this.CloneCore();
		}

		/// <summary>
		/// Gets the frozen copy of this instance.
		/// </summary>
		/// <returns>
		/// This instance if it is already frozen.
		/// Otherwise, frozen copy of this instance.
		/// </returns>
		IFreezable IFreezable.AsFrozen()
		{
			return this.AsFrozenCore();
		}

		IFreezable IFreezable.Freeze()
		{
			return this.FreezeCore();
		}
	}
}
