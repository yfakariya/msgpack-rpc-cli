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

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Represents operation kind in tranportation layer.
	/// </summary>
	public enum RpcTransportOperation
	{
		/// <summary>
		///		Unknown.
		/// </summary>
		None = 0,

		/// <summary>
		///		Connect to remote endpoint.
		/// </summary>
		Connect,

		/// <summary>
		///		Accept connection from remote endpoint.
		/// </summary>
		Accept,

		/// <summary>
		///		Send data to remote endpoint.
		/// </summary>
		Send,

		/// <summary>
		///		Receive data from remote endpoint.
		/// </summary>
		Receive,

		/// <summary>
		///		Serialize message.
		/// </summary>
		Serialize,

		/// <summary>
		///		Deserialize message.
		/// </summary>
		Deserialize,

		/// <summary>
		///		Bind socket to local port.
		/// </summary>
		Bind,

		/// <summary>
		///		Shutdown socket communication.
		/// </summary>
		Shutdown,

		/// <summary>
		///		Unknown operation.
		/// </summary>
		Unknown
	}
}
