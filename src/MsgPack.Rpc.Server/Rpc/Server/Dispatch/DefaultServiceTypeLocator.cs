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
using System.Collections.ObjectModel;
using System.Linq;

namespace MsgPack.Rpc.Server.Dispatch
{
	/// <summary>
	///		Implements in-memory <see cref="ServiceTypeLocator"/> for mainly testing purposes.
	/// </summary>
	public sealed class DefaultServiceTypeLocator : ServiceTypeLocator
	{
		private readonly HashSet<ServiceDescription> _serviceTypes = new HashSet<ServiceDescription>();

		/// <summary>
		///		Initializes a new instance of the <see cref="DefaultServiceTypeLocator"/> class.
		/// </summary>
		public DefaultServiceTypeLocator() { }

		/// <summary>
		///		Adds the specified service.
		/// </summary>
		/// <param name="serviceType">Type of the service.</param>
		/// <returns>
		///		<c>true</c> if the specified service newly added; otherwise, <c>false</c>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="serviceType"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="serviceType"/> is invalid. 
		///		See <see cref="M:ServiceDescription.FromServiceType"/> for details.
		/// </exception>
		public bool AddService( Type serviceType )
		{
			return this._serviceTypes.Add( ServiceDescription.FromServiceType( serviceType ) );
		}

		/// <summary>
		///		Removes the specified service.
		/// </summary>
		/// <param name="serviceType">Type of the service.</param>
		/// <returns>
		///		<c>true</c> if the specified service successfully removed; otherwise, <c>false</c>.
		/// </returns>
		public bool RemoveService( Type serviceType )
		{
			return this._serviceTypes.RemoveWhere( item => item.ServiceType == serviceType ) > 0;
		}

		/// <summary>
		///		Clears all services.
		/// </summary>
		public void ClearServices()
		{
			this._serviceTypes.Clear();
		}

		/// <summary>
		///		Enumerates all services.
		/// </summary>
		/// <returns>
		///		Read only <see cref="ServiceDescription"/> iterator.
		/// </returns>
		public IEnumerable<ServiceDescription> EnumerateServices()
		{
			foreach ( var item in this._serviceTypes )
			{
				// Enumerate to prevent manipulation via down cast.
				yield return item;
			}
		}

		/// <summary>
		///		Find services types with implementation specific way and returns them as <see cref="ServiceDescription"/>.
		/// </summary>
		/// <returns>
		///		The collection of <see cref="ServiceDescription"/>.
		/// </returns>
		/// <remarks>
		///		This implementation causes collection copying.
		/// </remarks>
		public sealed override IEnumerable<ServiceDescription> FindServices()
		{
			return this._serviceTypes.ToArray();
		}
	}
}
