using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
