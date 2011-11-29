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
using System.Linq;
using System.Text;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Define common interface of asynchrnous connect operation client.
	/// </summary>
	public interface IAsyncConnectClient
	{
		/// <summary>
		///		Get socket to be connected.
		/// </summary>
		/// <value>
		///		Socket to be connected.
		/// </value>
		RpcSocket Socket { get; }

		/// <summary>
		///		Get socket context that <see cref="Socket"/> will be set.
		/// </summary>
		/// <value>
		///		Socket context that <see cref="Socket"/> will be set.
		/// </value>
		RpcSocketAsyncEventArgs SocketContext { get; }

		/// <summary>
		///		Invoked when connection is completed by error.
		/// </summary>
		/// <param name="rpcError">RPC error data.</param>
		/// <param name="exception">Thrown exception.</param>
		/// <param name="completedSynchronously">If operation is completed syunchrnously then true.</param>
		/// <param name="asyncState">Async state client supplied to async operation.</param>
		void OnConnectError( RpcError rpcError, Exception exception, bool completedSynchronously, object asyncState );

		/// <summary>
		///		Invoked when connection is completed successfully.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <param name="completedSynchronously">If operation is completed syunchrnously then true.</param>
		/// <param name="asyncState">Async state client supplied to async operation.</param>
		void OnConnected( ConnectingContext context, bool completedSynchronously, object asyncState );
	}
}
