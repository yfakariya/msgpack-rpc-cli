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

namespace MsgPack.Rpc.Protocols
{
	internal static class SocketErrorCodeExtension
	{
		public static bool? IsError( this SocketError source )
		{
			switch ( source )
			{
				case SocketError.AlreadyInProgress:
				case SocketError.Disconnecting:
				case SocketError.IsConnected:
				case SocketError.Shutdown:
				{
					return null;
				}
				case SocketError.InProgress:
				case SocketError.Interrupted:
				case SocketError.IOPending:
				case SocketError.OperationAborted:
				case SocketError.Success:
				case SocketError.WouldBlock:
				{
					return false;
				}
				default:
				{
					return true;
				}
			}
		}

		public static RpcError ToRpcError( this SocketError source )
		{
			if ( !source.IsError().GetValueOrDefault() )
			{
				return null;
			}

			switch ( source )
			{
				case SocketError.ConnectionRefused:
				{
					// Caller bug
					return RpcError.ConnectionRefusedError;
				}
				case SocketError.HostNotFound:
				case SocketError.HostUnreachable:
				case SocketError.NetworkUnreachable:
				{
					return RpcError.NetworkUnreacheableError;
				}
				case SocketError.MessageSize:
				{
					return RpcError.MessageTooLargeError;
				}
				case SocketError.TimedOut:
				{
					return RpcError.ConnectionTimeoutError;
				}
				default:
				{
					// Caller bug
					return RpcError.TransportError;
				}
			}
		}
	}
}
