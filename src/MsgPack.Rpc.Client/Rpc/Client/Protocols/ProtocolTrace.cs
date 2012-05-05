
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

namespace MsgPack.Rpc.Client.Protocols
{
	/// <summary>
	/// 	Defines trace for MsgPack.Rpc.Client.Protocols namespace.
	/// </summary>
	internal static partial class MsgPackRpcClientProtocolsTrace
	{
		private static readonly TraceSource _source = new TraceSource( "MsgPack.Rpc.Client.Protocols" );

		private static readonly Dictionary<MessageId, TraceEventType> _typeTable = 
			new Dictionary<MessageId, TraceEventType> ( 25 )
			{
				{ MessageId.DetectServerShutdown, TraceEventType.Information },
				{ MessageId.OrphanError, TraceEventType.Error },
				{ MessageId.SocketError, TraceEventType.Warning },
				{ MessageId.IgnoreableError, TraceEventType.Verbose },
				{ MessageId.UnexpectedLastOperation, TraceEventType.Critical },
				{ MessageId.WaitTimeout, TraceEventType.Warning },
				{ MessageId.SerializeRequest, TraceEventType.Verbose },
				{ MessageId.SendOutboundData, TraceEventType.Verbose },
				{ MessageId.SentOutboundData, TraceEventType.Verbose },
				{ MessageId.BeginConnect, TraceEventType.Verbose },
				{ MessageId.EndConnect, TraceEventType.Verbose },
				{ MessageId.ConnectTimeout, TraceEventType.Warning },
				{ MessageId.DeserializeResponse, TraceEventType.Verbose },
				{ MessageId.NeedRequestHeader, TraceEventType.Verbose },
				{ MessageId.NeedMessageType, TraceEventType.Verbose },
				{ MessageId.NeedMessageId, TraceEventType.Verbose },
				{ MessageId.NeedError, TraceEventType.Verbose },
				{ MessageId.NeedResult, TraceEventType.Verbose },
				{ MessageId.DumpInvalidResponseHeader, TraceEventType.Verbose },
				{ MessageId.BeginReceive, TraceEventType.Verbose },
				{ MessageId.ReceiveInboundData, TraceEventType.Verbose },
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
		/// 	<see cref="MessageId" /> of .DetectServerShutdown (ID:11) message.
		/// </summary>
		public const MessageId DetectServerShutdown = MessageId.DetectServerShutdown;
		/// <summary>
		/// 	<see cref="MessageId" /> of .OrphanError (ID:91) message.
		/// </summary>
		public const MessageId OrphanError = MessageId.OrphanError;
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
		/// 	<see cref="MessageId" /> of .WaitTimeout (ID:104) message.
		/// </summary>
		public const MessageId WaitTimeout = MessageId.WaitTimeout;
		/// <summary>
		/// 	<see cref="MessageId" /> of .SerializeRequest (ID:1101) message.
		/// </summary>
		public const MessageId SerializeRequest = MessageId.SerializeRequest;
		/// <summary>
		/// 	<see cref="MessageId" /> of .SendOutboundData (ID:1201) message.
		/// </summary>
		public const MessageId SendOutboundData = MessageId.SendOutboundData;
		/// <summary>
		/// 	<see cref="MessageId" /> of .SentOutboundData (ID:1202) message.
		/// </summary>
		public const MessageId SentOutboundData = MessageId.SentOutboundData;
		/// <summary>
		/// 	<see cref="MessageId" /> of .BeginConnect (ID:1301) message.
		/// </summary>
		public const MessageId BeginConnect = MessageId.BeginConnect;
		/// <summary>
		/// 	<see cref="MessageId" /> of .EndConnect (ID:1302) message.
		/// </summary>
		public const MessageId EndConnect = MessageId.EndConnect;
		/// <summary>
		/// 	<see cref="MessageId" /> of .ConnectTimeout (ID:1303) message.
		/// </summary>
		public const MessageId ConnectTimeout = MessageId.ConnectTimeout;
		/// <summary>
		/// 	<see cref="MessageId" /> of .DeserializeResponse (ID:2001) message.
		/// </summary>
		public const MessageId DeserializeResponse = MessageId.DeserializeResponse;
		/// <summary>
		/// 	<see cref="MessageId" /> of .NeedRequestHeader (ID:2112) message.
		/// </summary>
		public const MessageId NeedRequestHeader = MessageId.NeedRequestHeader;
		/// <summary>
		/// 	<see cref="MessageId" /> of .NeedMessageType (ID:2113) message.
		/// </summary>
		public const MessageId NeedMessageType = MessageId.NeedMessageType;
		/// <summary>
		/// 	<see cref="MessageId" /> of .NeedMessageId (ID:2114) message.
		/// </summary>
		public const MessageId NeedMessageId = MessageId.NeedMessageId;
		/// <summary>
		/// 	<see cref="MessageId" /> of .NeedError (ID:2115) message.
		/// </summary>
		public const MessageId NeedError = MessageId.NeedError;
		/// <summary>
		/// 	<see cref="MessageId" /> of .NeedResult (ID:2116) message.
		/// </summary>
		public const MessageId NeedResult = MessageId.NeedResult;
		/// <summary>
		/// 	<see cref="MessageId" /> of .DumpInvalidResponseHeader (ID:2130) message.
		/// </summary>
		public const MessageId DumpInvalidResponseHeader = MessageId.DumpInvalidResponseHeader;
		/// <summary>
		/// 	<see cref="MessageId" /> of .BeginReceive (ID:2201) message.
		/// </summary>
		public const MessageId BeginReceive = MessageId.BeginReceive;
		/// <summary>
		/// 	<see cref="MessageId" /> of .ReceiveInboundData (ID:2202) message.
		/// </summary>
		public const MessageId ReceiveInboundData = MessageId.ReceiveInboundData;
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
			DetectServerShutdown = 11,
			OrphanError = 91,
			SocketError = 101,
			IgnoreableError = 102,
			UnexpectedLastOperation = 103,
			WaitTimeout = 104,
			SerializeRequest = 1101,
			SendOutboundData = 1201,
			SentOutboundData = 1202,
			BeginConnect = 1301,
			EndConnect = 1302,
			ConnectTimeout = 1303,
			DeserializeResponse = 2001,
			NeedRequestHeader = 2112,
			NeedMessageType = 2113,
			NeedMessageId = 2114,
			NeedError = 2115,
			NeedResult = 2116,
			DumpInvalidResponseHeader = 2130,
			BeginReceive = 2201,
			ReceiveInboundData = 2202,
			TransportShutdownCompleted = 3012,
			ShutdownSending = 3013,
			ShutdownReceiving = 3014,
			DisposeTransport = 3019,
		}
	}
}

