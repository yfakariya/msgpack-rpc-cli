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
using System.Collections.Generic;

namespace MsgPack.Rpc.Server.Dispatch
{
	/// <summary>
	///		Implements MessagePack-RPC method catalog without scope and version concepts.
	/// </summary>
	/// <remarks>
	///		This class supports flat and single namespace.
	///		<note>
	///			A <c>method</c> should be compliant with UAX-31(Unicode Identifier and Pattern Syntax)
	///			with allowing to start with underscopre ('_', U+005F).
	///		</note>
	/// </remarks>
	public class FlatOperationCatalog : OperationCatalog
	{
		private readonly Dictionary<string, OperationDescription> _catalog;

		/// <summary>
		///		Initializes a new instance of the <see cref="FlatOperationCatalog"/> class.
		/// </summary>
		public FlatOperationCatalog()
		{
			this._catalog = new Dictionary<string, OperationDescription>();
		}

		/// <summary>
		/// Gets the <see cref="OperationDescription"/> for the specified method description.
		/// </summary>
		/// <param name="methodDescription">The method description.
		/// The format is derived class specific.
		/// This value is not null nor empty.</param>
		/// <returns>
		/// The <see cref="OperationDescription"/> if the item for the specified method description  is found;
		/// <c>null</c>, otherwise.
		/// </returns>
		/// <exception cref="ArgumentException">
		///		<paramref name="methodDescription"/> is invalid.
		/// </exception>
		protected override OperationDescription GetCore( string methodDescription )
		{
			OperationDescription result;
			this._catalog.TryGetValue( methodDescription, out result );
			return result;
		}

		/// <summary>
		/// Adds the specified <see cref="OperationDescription"/> to this catalog.
		/// </summary>
		/// <param name="operation">The <see cref="OperationDescription"/> to be added.
		/// This value is not <c>null</c>.</param>
		/// <returns>
		///   <c>true</c> if the <paramref name="operation"/> is added successfully;
		/// <c>false</c>, if it is not added because the operation which has same id already exists.
		/// </returns>
		protected override bool AddCore( OperationDescription operation )
		{
			try
			{
				this._catalog.Add( operation.Id, operation );
				return true;
			}
			catch ( ArgumentException )
			{
				// It is more efficient because dictionary may be big.
				return false;
			}
		}

		/// <summary>
		/// Removes the specified <see cref="OperationDescription"/> from this catalog.
		/// </summary>
		/// <param name="operation">The <see cref="OperationDescription"/> to be removed.
		/// This value is not <c>null</c>.</param>
		/// <returns>
		///   <c>true</c> if the <paramref name="operation"/> is removed successfully;
		/// <c>false</c>, if it is not removed because the operation does not exist in this catalog.
		/// </returns>
		protected override bool RemoveCore( OperationDescription operation )
		{
			return this._catalog.Remove( operation.Id );
		}

		/// <summary>
		/// Clears all operations from this catalog.
		/// </summary>
		public override void Clear()
		{
			this._catalog.Clear();
		}

		/// <summary>
		/// Returns an enumerator to iterate all operations in this catalog.
		/// </summary>
		/// <returns>
		///   <see cref="T:System.Collections.Generic.IEnumerator`1"/> which can be used to interate all operations in this catalog.
		/// </returns>
		public override IEnumerator<OperationDescription> GetEnumerator()
		{
			return this._catalog.Values.GetEnumerator();
		}
	}
}
