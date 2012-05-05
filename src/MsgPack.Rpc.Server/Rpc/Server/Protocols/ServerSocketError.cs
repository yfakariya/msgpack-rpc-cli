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

namespace MsgPack.Rpc.Server.Protocols
{
	/// <summary>
	///		Defines extension methods to help server side socket error handling.
	/// </summary>
	public static class ServerSocketError
	{
		/// <summary>
		///		Gets the <see cref="RpcErrorMessage"/> corresponds to the specified <see cref="SocketError"/>.
		/// </summary>
		/// <param name="socketError">The <see cref="SocketError"/>.</param>
		/// <returns>
		///		The <see cref="RpcErrorMessage"/> corresponds to the specified <see cref="SocketError"/>.
		/// </returns>
		public static RpcErrorMessage ToServerRpcError( this SocketError socketError )
		{
			if ( socketError.IsError().GetValueOrDefault() )
			{
				RpcError rpcError = socketError.ToRpcError();
				return new RpcErrorMessage( rpcError, rpcError == RpcError.TransportError ? "Unexpected socket error." : rpcError.DefaultMessageInvariant, new SocketException( ( int )socketError ).Message );
			}
			else
			{
				return RpcErrorMessage.Success;
			}
		}
	}
}
