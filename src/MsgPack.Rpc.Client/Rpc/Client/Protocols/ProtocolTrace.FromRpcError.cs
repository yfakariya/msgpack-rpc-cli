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
using System.Diagnostics;

namespace MsgPack.Rpc.Client.Protocols
{
	partial class MsgPackRpcClientProtocolsTrace
	{
		internal static void TraceRpcError( RpcError rpcError, string format, params object[] args )
		{
			_source.TraceEvent( GetTypeForRpcError( rpcError ), GetIdForRpcError( rpcError ), format, args );
		}

		private static TraceEventType GetTypeForRpcError( RpcError rpcError )
		{
			if ( 0 < rpcError.ErrorCode || rpcError.ErrorCode == -31 )
			{
				return TraceEventType.Warning;
			}

			switch ( rpcError.ErrorCode % 10 )
			{
				case -2:
				case -4:
				{
					return TraceEventType.Warning;
				}
				case -1:
				case -3:
				{
					return TraceEventType.Critical;
				}
				default:
				{
					return TraceEventType.Error;
				}
			}
		}

		private static int GetIdForRpcError( RpcError rpcError )
		{
			if ( 0 < rpcError.ErrorCode )
			{
				return 20000;
			}
			else
			{
				return 10000 + ( rpcError.ErrorCode * -1 );
			}
		}
	}
}
