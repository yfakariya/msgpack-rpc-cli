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
	public sealed class LocatorBasedDispatcher : Dispatcher
	{
		private readonly ServiceTypeLocator _locator;
		private readonly Dictionary<string, OperationDescription> _descriptionTable;

		public LocatorBasedDispatcher( RpcServer server )
			: base( server )
		{
			if ( server == null )
			{
				throw new ArgumentNullException( "server" );
			}

			this._locator = server.Configuration.ServiceTypeLocatorProvider( server.Configuration );
			this._descriptionTable = new Dictionary<string, OperationDescription>();

			foreach ( var service in this._locator.FindServices() )
			{
				foreach ( var operation in OperationDescription.FromServiceDescription( server.SerializationContext, service ) )
				{
					this._descriptionTable.Add( operation.Id, operation );
				}
			}
		}

		protected sealed override Func<ServerRequestContext, ServerResponseContext, Task> Dispatch( string methodName )
		{
			OperationDescription description;
			if ( !this._descriptionTable.TryGetValue( methodName, out description ) )
			{
				return null;
			}

			return description.Operation;
		}
	}
}
