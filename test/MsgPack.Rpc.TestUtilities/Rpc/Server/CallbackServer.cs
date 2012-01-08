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
using System.Net;
using MsgPack.Rpc.Server.Dispatch;
using System.Net.Sockets;

namespace MsgPack.Rpc.Server
{
	public static class CallbackServer
	{
		public const int PortNumber = 57319;

		public static RpcServer Create( Func<int?, MessagePackObject[], MessagePackObject> callback )
		{
			return Create( callback, new IPEndPoint( IPAddress.Any, PortNumber ), true );
		}

		public static RpcServer Create( Func<int?, MessagePackObject[], MessagePackObject> callback, EndPoint endPoint, bool preferIPv4 )
		{
			return
				new RpcServer(
					new RpcServerConfiguration()
					{
						PreferIPv4 = preferIPv4,
						BindingEndPoint = endPoint,
						MinimumConcurrentRequest = 1,
						MaximumConcurrentRequest = 10,
						MinimumConnection = 1,
						MaximumConnection = 1,
						DispatcherProvider = server => new CallbackDispatcher( server, callback )
					}
				);
		}

	}
}
