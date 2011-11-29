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
using System.Net;
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Control stand alone server event loop.
	/// </summary>
	public sealed class RpcServer : IDisposable
	{
		private readonly ServerEventLoop _eventLoop;

		public RpcServer( ServerEventLoop eventLoop )
		{
			if ( eventLoop == null )
			{
				throw new ArgumentNullException( "eventLoop" );
			}

			Contract.EndContractBlock();

			this._eventLoop = eventLoop;
		}

		public void Dispose()
		{
			this._eventLoop.Dispose();
		}

		public void Start( EndPoint bindingEndPoint )
		{
			this._eventLoop.Start( bindingEndPoint );
		}

		public void Stop()
		{
			this._eventLoop.Stop();
		}

	}
}
