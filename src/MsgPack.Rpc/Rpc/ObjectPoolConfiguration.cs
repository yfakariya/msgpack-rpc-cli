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
using System.Diagnostics.CodeAnalysis;

[module: SuppressMessage( "Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Scope = "member", Target = "MsgPack.Rpc.ObjectPoolConfiguration.#ToString`1(!!0,System.Text.StringBuilder)", Justification = "Boolean value should be lower case." )]

namespace MsgPack.Rpc
{
	/// <summary>
	///		Represents configuratin of the <see cref="ObjectPool{T}"/>.
	/// </summary>
	public sealed partial class ObjectPoolConfiguration : FreezableObject
	{
		private static readonly ObjectPoolConfiguration _default = new ObjectPoolConfiguration().AsFrozen();

		/// <summary>
		///		Gets the default frozen instance.
		/// </summary>
		/// <value>
		///		The default frozen instance.
		///		This value will not be <c>null</c>.
		/// </value>
		public static ObjectPoolConfiguration Default
		{
			get { return _default; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectPoolConfiguration"/> class.
		/// </summary>
		public ObjectPoolConfiguration() { }

		/// <summary>
		///		Clones all of the fields of this instance.
		/// </summary>
		/// <returns>
		///		The shallow copy of this instance.
		/// </returns>
		public ObjectPoolConfiguration Clone()
		{
			return this.CloneCore() as ObjectPoolConfiguration;
		}

		/// <summary>
		///		Freezes this instance.
		/// </summary>
		/// <returns>
		///		This instance.
		/// </returns>
		public ObjectPoolConfiguration Freeze()
		{
			return this.FreezeCore() as ObjectPoolConfiguration;
		}

		/// <summary>
		/// Gets the frozen copy of this instance.
		/// </summary>
		/// <returns>
		/// This instance if it is already frozen.
		/// Otherwise, frozen copy of this instance.
		/// </returns>
		public ObjectPoolConfiguration AsFrozen()
		{
			return this.AsFrozenCore() as ObjectPoolConfiguration;
		}
	}
}
