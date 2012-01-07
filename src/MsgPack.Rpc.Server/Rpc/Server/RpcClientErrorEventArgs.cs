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

namespace MsgPack.Rpc.Server
{
	public sealed class RpcClientErrorEventArgs : EventArgs
	{
		private readonly RpcErrorMessage _rpcError;

		public RpcErrorMessage RpcError
		{
			get { return _rpcError; }
		}

		public EndPoint RemoteEndPoint { get; set; }

		public long SessionId { get; set; }

		public int? MessageId { get; set; }

		public RpcClientErrorEventArgs( RpcErrorMessage rpcError )
		{
			this._rpcError = rpcError;
		}
	}
}
