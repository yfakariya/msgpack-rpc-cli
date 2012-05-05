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
	///		Defines common interface for freezable objects.
	/// </summary>
	[ContractClass( typeof( IFreezableContract ) )]
	public interface IFreezable
	{
		/// <summary>
		///		Gets a value indicating whether this instance is frozen.
		/// </summary>
		/// <value>
		///   <c>true</c> if this instance is frozen; otherwise, <c>false</c>.
		/// </value>
		bool IsFrozen { get; }

		/// <summary>
		///		Freezes this instance.
		/// </summary>
		/// <returns>
		///		This instance.
		/// </returns>
		IFreezable Freeze();

		/// <summary>
		///		Gets the frozen copy of this instance.
		/// </summary>
		/// <returns>
		///		This instance if it is already frozen.
		///		Otherwise, frozen copy of this instance.
		/// </returns>
		IFreezable AsFrozen();
	}

	[ContractClassFor( typeof( IFreezable ) )]
	internal abstract class IFreezableContract : IFreezable
	{
		public bool IsFrozen
		{
			get { return false; }
		}

		public IFreezable Freeze()
		{
			Contract.Ensures( Contract.Result<IFreezable>() != null );
			Contract.Ensures( Contract.ReferenceEquals( Contract.Result<IFreezable>(), this ) );
			Contract.Ensures( this.IsFrozen );

			return null;
		}

		public IFreezable AsFrozen()
		{
			Contract.Ensures( Contract.Result<IFreezable>() != null );
			Contract.Ensures( !Object.ReferenceEquals( Contract.Result<IFreezable>(), this ) );
			Contract.Ensures( Contract.Result<IFreezable>().IsFrozen );
			Contract.Ensures( this.IsFrozen == Contract.OldValue( this.IsFrozen ) );
			
			throw new NotImplementedException();
		}
	}
}
