
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
using System.Diagnostics;

namespace MsgPack.Rpc.Server.Protocols
{
	/// <summary>
	/// 	Defines trace for MsgPack.Rpc.Server.Protocols namespace.
	/// </summary>
	internal static partial class MsgPackRpcServerProtocolsTrace
	{
		private static readonly TraceSource _source = new TraceSource( "MsgPack.Rpc.Server.Protocols" );

		private static readonly Dictionary<MessageId, TraceEventType> _typeTable = 
			new Dictionary<MessageId, TraceEventType> ( 37 )
			{
				{ MessageId.DetectClientShutdown, TraceEventType.Information },
				{ MessageId.SocketError, TraceEventType.Warning },
				{ MessageId.IgnoreableError, TraceEventType.Verbose },
				{ MessageId.UnexpectedLastOperation, TraceEventType.Critical },
				{ MessageId.DefaultEndPoint, TraceEventType.Information },
				{ MessageId.UdpServerManagerListenerThreadCrash, TraceEventType.Error },
				{ MessageId.FailedToStopUdpServerManagerListenerThread, TraceEventType.Error },
				{ MessageId.BeginReceive, TraceEventType.Verbose },
				{ MessageId.ReceiveInboundData, TraceEventType.Verbose },
				{ MessageId.ReceiveCanceledDueToClientShutdown, TraceEventType.Verbose },
				{ MessageId.ReceiveCanceledDueToServerShutdown, TraceEventType.Verbose },
				{ MessageId.DeserializeRequest, TraceEventType.Verbose },
				{ MessageId.NewSession, TraceEventType.Verbose },
				{ MessageId.NeedRequestHeader, TraceEventType.Verbose },
				{ MessageId.NeedMessageType, TraceEventType.Verbose },
				{ MessageId.NeedMessageId, TraceEventType.Verbose },
				{ MessageId.NeedMethodName, TraceEventType.Verbose },
				{ MessageId.NeedArgumentsArrayHeader, TraceEventType.Verbose },
				{ MessageId.NeedArgumentsElement, TraceEventType.Verbose },
				{ MessageId.DumpInvalidRequestHeader, TraceEventType.Verbose },
				{ MessageId.ReceiveTimeout, TraceEventType.Warning },
				{ MessageId.StartListen, TraceEventType.Information },
				{ MessageId.BeginAccept, TraceEventType.Verbose },
				{ MessageId.EndAccept, TraceEventType.Verbose },
				{ MessageId.ErrorWhenSendResponse, TraceEventType.Verbose },
				{ MessageId.SerializeResponse, TraceEventType.Verbose },
				{ MessageId.SendOutboundData, TraceEventType.Verbose },
				{ MessageId.SentOutboundData, TraceEventType.Verbose },
				{ MessageId.SendTimeout, TraceEventType.Warning },
				{ MessageId.BeginShutdownManager, TraceEventType.Verbose },
				{ MessageId.ManagerShutdownCompleted, TraceEventType.Verbose },
				{ MessageId.DisposeManager, TraceEventType.Verbose },
				{ MessageId.BeginShutdownTransport, TraceEventType.Verbose },
				{ MessageId.TransportShutdownCompleted, TraceEventType.Verbose },
				{ MessageId.ShutdownSending, TraceEventType.Verbose },
				{ MessageId.ShutdownReceiving, TraceEventType.Verbose },
				{ MessageId.DisposeTransport, TraceEventType.Verbose },
			};

		/// <summary>
		/// 	Gets the <see cref="TraceSource" />.
		/// </summary>
		/// <value>
		/// 	The <see cref="TraceSource" />.
		/// </value>
		public static TraceSource Source
		{
			get { return _source; }
		}

		/// <summary>
		/// 	Returns the value whether the specified message should be traced in current configuration.
		/// </summary>
		/// <param name="id">
		/// 	<see cref="MessageId" /> for the trace message.
		/// </param>
		/// <returns>
		/// 	<c>true</c> if the specified message should be traced; otherwise, <c>false</c>.
		/// </returns>
		public static bool ShouldTrace ( MessageId id )
		{
			return _source.Switch.ShouldTrace( _typeTable[ id ] );
		}

		/// <summary>
		/// 	Outputs the trace message for the interesting event.
		/// </summary>
		/// <param name="id">
		/// 	<see cref="MessageId" /> for the trace message.
		/// </param>
		/// <param name="format">
		/// 	The format string of the descriptive message.
		/// </param>
		/// <param name="args">
		/// 	The format arguments of the descriptive message.
		/// </param>
		public static void TraceEvent ( MessageId id, string format, params object[] args )
		{
			if ( args == null || args.Length == 0 )
			{
				_source.TraceEvent( _typeTable[ id ], ( int )id, format );
			}
			else
			{
				_source.TraceEvent( _typeTable[ id ], ( int )id, format, args );
			}
		}

		/// <summary>
		/// 	Outputs the raw data for the interesting event.
		/// </summary>
		/// <param name="id">
		/// 	<see cref="MessageId" /> for the trace data.
		/// </param>
		/// <param name="data">
		/// 	The raw data for this event.
		/// </param>
		public static void TraceData ( MessageId id, params object[] data )
		{
			_source.TraceData( _typeTable[ id ], ( int )id, data );
		}

		/// <summary>
		/// 	<see cref="MessageId" /> of .DetectClientShutdown (ID:11) message.
		/// </summary>
		public const MessageId DetectClientShutdown = MessageId.DetectClientShutdown;
		/// <summary>
		/// 	<see cref="MessageId" /> of .SocketError (ID:101) message.
		/// </summary>
		public const MessageId SocketError = MessageId.SocketError;
		/// <summary>
		/// 	<see cref="MessageId" /> of .IgnoreableError (ID:102) message.
		/// </summary>
		public const MessageId IgnoreableError = MessageId.IgnoreableError;
		/// <summary>
		/// 	<see cref="MessageId" /> of .UnexpectedLastOperation (ID:103) message.
		/// </summary>
		public const MessageId UnexpectedLastOperation = MessageId.UnexpectedLastOperation;
		/// <summary>
		/// 	<see cref="MessageId" /> of .DefaultEndPoint (ID:104) message.
		/// </summary>
		public const MessageId DefaultEndPoint = MessageId.DefaultEndPoint;
		/// <summary>
		/// 	<see cref="MessageId" /> of .UdpServerManagerListenerThreadCrash (ID:201) message.
		/// </summary>
		public const MessageId UdpServerManagerListenerThreadCrash = MessageId.UdpServerManagerListenerThreadCrash;
		/// <summary>
		/// 	<see cref="MessageId" /> of .FailedToStopUdpServerManagerListenerThread (ID:202) message.
		/// </summary>
		public const MessageId FailedToStopUdpServerManagerListenerThread = MessageId.FailedToStopUdpServerManagerListenerThread;
		/// <summary>
		/// 	<see cref="MessageId" /> of .BeginReceive (ID:1001) message.
		/// </summary>
		public const MessageId BeginReceive = MessageId.BeginReceive;
		/// <summary>
		/// 	<see cref="MessageId" /> of .ReceiveInboundData (ID:1002) message.
		/// </summary>
		public const MessageId ReceiveInboundData = MessageId.ReceiveInboundData;
		/// <summary>
		/// 	<see cref="MessageId" /> of .ReceiveCanceledDueToClientShutdown (ID:1003) message.
		/// </summary>
		public const MessageId ReceiveCanceledDueToClientShutdown = MessageId.ReceiveCanceledDueToClientShutdown;
		/// <summary>
		/// 	<see cref="MessageId" /> of .ReceiveCanceledDueToServerShutdown (ID:1004) message.
		/// </summary>
		public const MessageId ReceiveCanceledDueToServerShutdown = MessageId.ReceiveCanceledDueToServerShutdown;
		/// <summary>
		/// 	<see cref="MessageId" /> of .DeserializeRequest (ID:1101) message.
		/// </summary>
		public const MessageId DeserializeRequest = MessageId.DeserializeRequest;
		/// <summary>
		/// 	<see cref="MessageId" /> of .NewSession (ID:1102) message.
		/// </summary>
		public const MessageId NewSession = MessageId.NewSession;
		/// <summary>
		/// 	<see cref="MessageId" /> of .NeedRequestHeader (ID:1111) message.
		/// </summary>
		public const MessageId NeedRequestHeader = MessageId.NeedRequestHeader;
		/// <summary>
		/// 	<see cref="MessageId" /> of .NeedMessageType (ID:1112) message.
		/// </summary>
		public const MessageId NeedMessageType = MessageId.NeedMessageType;
		/// <summary>
		/// 	<see cref="MessageId" /> of .NeedMessageId (ID:1113) message.
		/// </summary>
		public const MessageId NeedMessageId = MessageId.NeedMessageId;
		/// <summary>
		/// 	<see cref="MessageId" /> of .NeedMethodName (ID:1114) message.
		/// </summary>
		public const MessageId NeedMethodName = MessageId.NeedMethodName;
		/// <summary>
		/// 	<see cref="MessageId" /> of .NeedArgumentsArrayHeader (ID:1115) message.
		/// </summary>
		public const MessageId NeedArgumentsArrayHeader = MessageId.NeedArgumentsArrayHeader;
		/// <summary>
		/// 	<see cref="MessageId" /> of .NeedArgumentsElement (ID:1116) message.
		/// </summary>
		public const MessageId NeedArgumentsElement = MessageId.NeedArgumentsElement;
		/// <summary>
		/// 	<see cref="MessageId" /> of .DumpInvalidRequestHeader (ID:1190) message.
		/// </summary>
		public const MessageId DumpInvalidRequestHeader = MessageId.DumpInvalidRequestHeader;
		/// <summary>
		/// 	<see cref="MessageId" /> of .ReceiveTimeout (ID:1201) message.
		/// </summary>
		public const MessageId ReceiveTimeout = MessageId.ReceiveTimeout;
		/// <summary>
		/// 	<see cref="MessageId" /> of .StartListen (ID:1301) message.
		/// </summary>
		public const MessageId StartListen = MessageId.StartListen;
		/// <summary>
		/// 	<see cref="MessageId" /> of .BeginAccept (ID:1302) message.
		/// </summary>
		public const MessageId BeginAccept = MessageId.BeginAccept;
		/// <summary>
		/// 	<see cref="MessageId" /> of .EndAccept (ID:1303) message.
		/// </summary>
		public const MessageId EndAccept = MessageId.EndAccept;
		/// <summary>
		/// 	<see cref="MessageId" /> of .ErrorWhenSendResponse (ID:2001) message.
		/// </summary>
		public const MessageId ErrorWhenSendResponse = MessageId.ErrorWhenSendResponse;
		/// <summary>
		/// 	<see cref="MessageId" /> of .SerializeResponse (ID:2101) message.
		/// </summary>
		public const MessageId SerializeResponse = MessageId.SerializeResponse;
		/// <summary>
		/// 	<see cref="MessageId" /> of .SendOutboundData (ID:2201) message.
		/// </summary>
		public const MessageId SendOutboundData = MessageId.SendOutboundData;
		/// <summary>
		/// 	<see cref="MessageId" /> of .SentOutboundData (ID:2202) message.
		/// </summary>
		public const MessageId SentOutboundData = MessageId.SentOutboundData;
		/// <summary>
		/// 	<see cref="MessageId" /> of .SendTimeout (ID:2203) message.
		/// </summary>
		public const MessageId SendTimeout = MessageId.SendTimeout;
		/// <summary>
		/// 	<see cref="MessageId" /> of .BeginShutdownManager (ID:3001) message.
		/// </summary>
		public const MessageId BeginShutdownManager = MessageId.BeginShutdownManager;
		/// <summary>
		/// 	<see cref="MessageId" /> of .ManagerShutdownCompleted (ID:3002) message.
		/// </summary>
		public const MessageId ManagerShutdownCompleted = MessageId.ManagerShutdownCompleted;
		/// <summary>
		/// 	<see cref="MessageId" /> of .DisposeManager (ID:3009) message.
		/// </summary>
		public const MessageId DisposeManager = MessageId.DisposeManager;
		/// <summary>
		/// 	<see cref="MessageId" /> of .BeginShutdownTransport (ID:3011) message.
		/// </summary>
		public const MessageId BeginShutdownTransport = MessageId.BeginShutdownTransport;
		/// <summary>
		/// 	<see cref="MessageId" /> of .TransportShutdownCompleted (ID:3012) message.
		/// </summary>
		public const MessageId TransportShutdownCompleted = MessageId.TransportShutdownCompleted;
		/// <summary>
		/// 	<see cref="MessageId" /> of .ShutdownSending (ID:3013) message.
		/// </summary>
		public const MessageId ShutdownSending = MessageId.ShutdownSending;
		/// <summary>
		/// 	<see cref="MessageId" /> of .ShutdownReceiving (ID:3014) message.
		/// </summary>
		public const MessageId ShutdownReceiving = MessageId.ShutdownReceiving;
		/// <summary>
		/// 	<see cref="MessageId" /> of .DisposeTransport (ID:3019) message.
		/// </summary>
		public const MessageId DisposeTransport = MessageId.DisposeTransport;
		public enum MessageId
		{
			DetectClientShutdown = 11,
			SocketError = 101,
			IgnoreableError = 102,
			UnexpectedLastOperation = 103,
			DefaultEndPoint = 104,
			UdpServerManagerListenerThreadCrash = 201,
			FailedToStopUdpServerManagerListenerThread = 202,
			BeginReceive = 1001,
			ReceiveInboundData = 1002,
			ReceiveCanceledDueToClientShutdown = 1003,
			ReceiveCanceledDueToServerShutdown = 1004,
			DeserializeRequest = 1101,
			NewSession = 1102,
			NeedRequestHeader = 1111,
			NeedMessageType = 1112,
			NeedMessageId = 1113,
			NeedMethodName = 1114,
			NeedArgumentsArrayHeader = 1115,
			NeedArgumentsElement = 1116,
			DumpInvalidRequestHeader = 1190,
			ReceiveTimeout = 1201,
			StartListen = 1301,
			BeginAccept = 1302,
			EndAccept = 1303,
			ErrorWhenSendResponse = 2001,
			SerializeResponse = 2101,
			SendOutboundData = 2201,
			SentOutboundData = 2202,
			SendTimeout = 2203,
			BeginShutdownManager = 3001,
			ManagerShutdownCompleted = 3002,
			DisposeManager = 3009,
			BeginShutdownTransport = 3011,
			TransportShutdownCompleted = 3012,
			ShutdownSending = 3013,
			ShutdownReceiving = 3014,
			DisposeTransport = 3019,
		}
	}
}

