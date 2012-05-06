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
using System.Threading.Tasks;
using MsgPack.Rpc.Server.Dispatch;
using MsgPack.Rpc.Server.Protocols;

namespace MsgPack.Rpc.Server.Dispatch
{
	/// <summary>
	///		Implements <see cref="Dispatcher"/> which uses <see cref="ServiceTypeLocator"/>, <see cref="ServiceDescription"/> and <see cref="OperationDescription"/>.
	/// </summary>
	/// <remarks>
	///		This class can be thought as the façade component.
	/// </remarks>
	public sealed class LocatorBasedDispatcher : Dispatcher
	{
		private readonly ServiceTypeLocator _locator;
		private readonly OperationCatalog _descriptionTable;

		/// <summary>
		/// Initializes a new instance of the <see cref="LocatorBasedDispatcher"/> class.
		/// </summary>
		/// <param name="server">The server which will hold this instance.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="server"/> is <c>null</c>.
		/// </exception>
		public LocatorBasedDispatcher( RpcServer server )
			: base( server )
		{
			this._locator = server.Configuration.ServiceTypeLocatorProvider( server.Configuration );
			this._descriptionTable = server.Configuration.UseFullMethodName ? new VersionedOperationCatalog() : ( OperationCatalog )new FlatOperationCatalog();

			foreach ( var service in this._locator.FindServices() )
			{
				foreach ( var operation in OperationDescription.FromServiceDescription( this.Runtime, service ) )
				{
					this._descriptionTable.Add( operation );
				}
			}
		}

		/// <summary>
		/// When overriden in the derived classes, dispatches the specified RPC method to the operation.
		/// </summary>
		/// <param name="methodName">Name of the method.</param>
		/// <returns>
		///   <para>
		/// The <see cref="Func{T1,T2,TReturn}"/> which is entity of the operation.
		///   </para>
		///   <para>
		/// The 1st argument is <see cref="ServerRequestContext"/> which holds any data related to the request.
		/// This value will not be <c>null</c>.
		///   </para>
		///   <para>
		/// The 2nd argument is <see cref="ServerResponseContext"/> which handles any response related behaviors including error response.
		/// This value will not be <c>null</c>.
		///   </para>
		///   <para>
		/// The return value is <see cref="Task"/> which encapselates asynchronous target invocation.
		/// This value cannot be <c>null</c>.
		///   </para>
		/// </returns>
		/// <exception cref="Exception">
		/// The derived class faces unexpected failure.
		///   </exception>
		protected sealed override Func<ServerRequestContext, ServerResponseContext, Task> Dispatch( string methodName )
		{
			OperationDescription description = this._descriptionTable.Get( methodName );
			if ( description == null )
			{
				return null;
			}

			return description.Operation;
		}
	}
}
