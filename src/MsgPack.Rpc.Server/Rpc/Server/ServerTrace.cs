
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

namespace MsgPack.Rpc.Server
{
	/// <summary>
	/// 	Defines trace for MsgPack.Rpc.Server namespace.
	/// </summary>
	internal static partial class MsgPackRpcServerTrace
	{
		private static readonly TraceSource _source = new TraceSource( "MsgPack.Rpc.Server" );

		private static readonly Dictionary<MessageId, TraceEventType> _typeTable = 
			new Dictionary<MessageId, TraceEventType> ( 2 )
			{
				{ MessageId.StartServer, TraceEventType.Start },
				{ MessageId.StopServer, TraceEventType.Stop },
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
		/// 	<see cref="MessageId" /> of .StartServer (ID:1) message.
		/// </summary>
		public const MessageId StartServer = MessageId.StartServer;
		/// <summary>
		/// 	<see cref="MessageId" /> of .StopServer (ID:2) message.
		/// </summary>
		public const MessageId StopServer = MessageId.StopServer;
		public enum MessageId
		{
			StartServer = 1,
			StopServer = 2,
		}
	}
}

