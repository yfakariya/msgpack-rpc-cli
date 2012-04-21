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
using System.Net.Sockets;
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc.Client.Protocols
{
	/// <summary>
	///		Defines utility methods to handle <see cref="SocketError"/>.
	/// </summary>
	public static class ClientSocketError
	{
		/// <summary>
		///		Creates a <see cref="RpcErrorMessage"/> based on the specified <see cref="SocketError"/>.
		/// </summary>
		/// <param name="socketError">The underlying <see cref="SocketError"/>.</param>
		/// <returns>
		///		A <see cref="RpcErrorMessage"/> based on the specified <see cref="SocketError"/>.
		/// </returns>
		public static RpcErrorMessage ToClientRpcError( this SocketError socketError )
		{
			if ( socketError.IsError().GetValueOrDefault() )
			{
				return new RpcErrorMessage( socketError.ToRpcError(), new SocketException( ( int )socketError ).Message, String.Empty );
			}
			else
			{
				return RpcErrorMessage.Success;
			}
		}
	}
}
