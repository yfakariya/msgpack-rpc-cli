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

namespace MsgPack.Rpc.Server
{
	internal static class Tracer
	{
		private static readonly TraceSource _server = new TraceSource( "MsgPack.Rpc.Server" );
		public static TraceSource Server
		{
			get { return _server; }
		}

		private static readonly TraceSource _dispatch = new TraceSource( "MsgPack.Rpc.Server.Dispatch" );
		public static TraceSource Dispatch
		{
			get { return _dispatch; }
		}

		private static readonly TraceSource _protocols = new TraceSource( "MsgPack.Rpc.Server.Protocols" );
		public static TraceSource Protocols
		{
			get { return _protocols; }
		}

		private static readonly TraceSource _emit = new TraceSource( "MsgPack.Rpc.Server.Dispatch.Emit" );
		public static TraceSource Emit
		{
			get { return _emit; }
		}

		public static class EventType
		{
			public const TraceEventType StartServer = TraceEventType.Start;
			public const TraceEventType StartListen = TraceEventType.Start;
			public const TraceEventType SocketError = TraceEventType.Warning;
			public const TraceEventType UnexpectedLastOperation = TraceEventType.Error;
			public const TraceEventType ErrorWhenSendResponse = TraceEventType.Error;

			public const TraceEventType BoundSocket = TraceEventType.Start;
			public const TraceEventType CloseTransport = TraceEventType.Stop;
			public const TraceEventType DetectClientShutdown = TraceEventType.Information;

			public const TraceEventType BeginAccept = TraceEventType.Verbose;
			public const TraceEventType AcceptInboundTcp = TraceEventType.Verbose;

			public const TraceEventType BeginReceive = TraceEventType.Verbose;
			public const TraceEventType ReceiveCanceledDueToClientShutdown = TraceEventType.Verbose;
			public const TraceEventType ReceiveCanceledDueToServerShutdown = TraceEventType.Verbose;
			public const TraceEventType ReceiveInboundData = TraceEventType.Verbose;

			public const TraceEventType DeserializeRequest = TraceEventType.Verbose;
			public const TraceEventType DispatchRequest = TraceEventType.Verbose;
			public const TraceEventType OperationStart = TraceEventType.Start;
			public const TraceEventType OperationSucceeded = TraceEventType.Stop;
			public const TraceEventType OperationFailed = TraceEventType.Warning;

			public const TraceEventType SerializeResponse = TraceEventType.Verbose;
			public const TraceEventType SendOutboundData = TraceEventType.Verbose;
			public const TraceEventType SentOutboundData = TraceEventType.Verbose;

			public const TraceEventType NeedRequestHeader = TraceEventType.Verbose;
			public const TraceEventType NeedMessageType = TraceEventType.Verbose;
			public const TraceEventType NeedMessageId = TraceEventType.Verbose;
			public const TraceEventType NeedMethodName = TraceEventType.Verbose;
			public const TraceEventType NeedArgumentsArrayHeader = TraceEventType.Verbose;
			public const TraceEventType NeedArgumentsElement = TraceEventType.Verbose;

			public const TraceEventType DumpInvalidRequestHeader = TraceEventType.Verbose;
			public const TraceEventType IgnoreableError = TraceEventType.Verbose;

			public static TraceEventType ForRpcError( RpcError rpcError )
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

			public const TraceEventType DefineType = TraceEventType.Verbose;
			public const TraceEventType ILTrace = TraceEventType.Verbose;
		}

		public static class EventId
		{
			public const int StartServer = 1;
			public const int StartListen = 2;
			public const int BoundSocket = 1001;
			public const int CloseTransport = 1002;
			public const int DetectClientShutdown = 1003;
			public const int SocketError = 1091;
			public const int UnexpectedLastOperation = 1092;
			public const int ErrorWhenSendResponse = 1093;

			public const int BeginAccept = 1201;
			public const int AcceptInboundTcp = 1202;

			public const int BeginReceive = 1101;
			public const int ReceiveCanceledDueToClientShutdown = 1102;
			public const int ReceiveCanceledDueToServerShutdown = 1103;
			public const int ReceiveInboundData = 1104;

			public const int DeserializeRequest = 1111;
			public const int DispatchRequest = 1131;
			public const int OperationStart = 1132;
			public const int OperationSucceeded = 1133;
			public const int OperationFailed = 1134;

			public const int SerializeResponse = 1141;
			public const int SendOutboundData = 1151;
			public const int SentOutboundData = 1152;

			public const int NeedRequestHeader = 1112;
			public const int NeedMessageType = 1113;
			public const int NeedMessageId = 1114;
			public const int NeedMethodName = 1115;
			public const int NeedArgumentsArrayHeader = 1116;
			public const int NeedArgumentsElement = 1117;

			public const int DumpInvalidRequestHeader = 1301;
			public const int IgnoreableError = 1302;

			internal static int ForRpcError( RpcError rpcError )
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

			public const int DefineType = 1401;
			public const int ILTrace = 1499;
		}
	}
}
