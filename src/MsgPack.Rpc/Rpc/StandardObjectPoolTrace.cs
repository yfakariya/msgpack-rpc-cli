
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

namespace MsgPack.Rpc.StandardObjectPoolTracing
{
	/// <summary>
	/// 	Defines trace for MsgPack.Rpc.StandardObjectPoolTracing namespace.
	/// </summary>
	internal static partial class StandardObjectPoolTrace
	{
		private static readonly TraceSource _source = new TraceSource( "MsgPack.Rpc.StandardObjectPoolTracing" );

		private static readonly Dictionary<MessageId, TraceEventType> _typeTable = 
			new Dictionary<MessageId, TraceEventType> ( 18 )
			{
				{ MessageId.InitializedWithDefaultConfiguration, TraceEventType.Verbose },
				{ MessageId.InitializedWithConfiguration, TraceEventType.Verbose },
				{ MessageId.FailedToAddPoolInitially, TraceEventType.Error },
				{ MessageId.Disposed, TraceEventType.Verbose },
				{ MessageId.Finalized, TraceEventType.Verbose },
				{ MessageId.BorrowFromPool, TraceEventType.Verbose },
				{ MessageId.ExpandPool, TraceEventType.Verbose },
				{ MessageId.FailedToExpandPool, TraceEventType.Verbose },
				{ MessageId.PoolIsEmpty, TraceEventType.Verbose },
				{ MessageId.ReturnToPool, TraceEventType.Verbose },
				{ MessageId.FailedToReturnToPool, TraceEventType.Error },
				{ MessageId.EvictingExtraItemsInduced, TraceEventType.Verbose },
				{ MessageId.EvictingExtraItemsPreiodic, TraceEventType.Verbose },
				{ MessageId.EvictedExtraItemsInduced, TraceEventType.Information },
				{ MessageId.EvictedExtraItemsPreiodic, TraceEventType.Verbose },
				{ MessageId.GarbageCollectedWithoutLost, TraceEventType.Verbose },
				{ MessageId.GarbageCollectedWithLost, TraceEventType.Information },
				{ MessageId.FailedToRefreshEvictionTImer, TraceEventType.Error },
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
		/// 	<see cref="MessageId" /> of .InitializedWithDefaultConfiguration (ID:1) message.
		/// </summary>
		public const MessageId InitializedWithDefaultConfiguration = MessageId.InitializedWithDefaultConfiguration;
		/// <summary>
		/// 	<see cref="MessageId" /> of .InitializedWithConfiguration (ID:2) message.
		/// </summary>
		public const MessageId InitializedWithConfiguration = MessageId.InitializedWithConfiguration;
		/// <summary>
		/// 	<see cref="MessageId" /> of .FailedToAddPoolInitially (ID:3) message.
		/// </summary>
		public const MessageId FailedToAddPoolInitially = MessageId.FailedToAddPoolInitially;
		/// <summary>
		/// 	<see cref="MessageId" /> of .Disposed (ID:81) message.
		/// </summary>
		public const MessageId Disposed = MessageId.Disposed;
		/// <summary>
		/// 	<see cref="MessageId" /> of .Finalized (ID:82) message.
		/// </summary>
		public const MessageId Finalized = MessageId.Finalized;
		/// <summary>
		/// 	<see cref="MessageId" /> of .BorrowFromPool (ID:101) message.
		/// </summary>
		public const MessageId BorrowFromPool = MessageId.BorrowFromPool;
		/// <summary>
		/// 	<see cref="MessageId" /> of .ExpandPool (ID:102) message.
		/// </summary>
		public const MessageId ExpandPool = MessageId.ExpandPool;
		/// <summary>
		/// 	<see cref="MessageId" /> of .FailedToExpandPool (ID:103) message.
		/// </summary>
		public const MessageId FailedToExpandPool = MessageId.FailedToExpandPool;
		/// <summary>
		/// 	<see cref="MessageId" /> of .PoolIsEmpty (ID:104) message.
		/// </summary>
		public const MessageId PoolIsEmpty = MessageId.PoolIsEmpty;
		/// <summary>
		/// 	<see cref="MessageId" /> of .ReturnToPool (ID:201) message.
		/// </summary>
		public const MessageId ReturnToPool = MessageId.ReturnToPool;
		/// <summary>
		/// 	<see cref="MessageId" /> of .FailedToReturnToPool (ID:202) message.
		/// </summary>
		public const MessageId FailedToReturnToPool = MessageId.FailedToReturnToPool;
		/// <summary>
		/// 	<see cref="MessageId" /> of .EvictingExtraItemsInduced (ID:301) message.
		/// </summary>
		public const MessageId EvictingExtraItemsInduced = MessageId.EvictingExtraItemsInduced;
		/// <summary>
		/// 	<see cref="MessageId" /> of .EvictingExtraItemsPreiodic (ID:302) message.
		/// </summary>
		public const MessageId EvictingExtraItemsPreiodic = MessageId.EvictingExtraItemsPreiodic;
		/// <summary>
		/// 	<see cref="MessageId" /> of .EvictedExtraItemsInduced (ID:303) message.
		/// </summary>
		public const MessageId EvictedExtraItemsInduced = MessageId.EvictedExtraItemsInduced;
		/// <summary>
		/// 	<see cref="MessageId" /> of .EvictedExtraItemsPreiodic (ID:304) message.
		/// </summary>
		public const MessageId EvictedExtraItemsPreiodic = MessageId.EvictedExtraItemsPreiodic;
		/// <summary>
		/// 	<see cref="MessageId" /> of .GarbageCollectedWithoutLost (ID:311) message.
		/// </summary>
		public const MessageId GarbageCollectedWithoutLost = MessageId.GarbageCollectedWithoutLost;
		/// <summary>
		/// 	<see cref="MessageId" /> of .GarbageCollectedWithLost (ID:312) message.
		/// </summary>
		public const MessageId GarbageCollectedWithLost = MessageId.GarbageCollectedWithLost;
		/// <summary>
		/// 	<see cref="MessageId" /> of .FailedToRefreshEvictionTImer (ID:391) message.
		/// </summary>
		public const MessageId FailedToRefreshEvictionTImer = MessageId.FailedToRefreshEvictionTImer;
		public enum MessageId
		{
			InitializedWithDefaultConfiguration = 1,
			InitializedWithConfiguration = 2,
			FailedToAddPoolInitially = 3,
			Disposed = 81,
			Finalized = 82,
			BorrowFromPool = 101,
			ExpandPool = 102,
			FailedToExpandPool = 103,
			PoolIsEmpty = 104,
			ReturnToPool = 201,
			FailedToReturnToPool = 202,
			EvictingExtraItemsInduced = 301,
			EvictingExtraItemsPreiodic = 302,
			EvictedExtraItemsInduced = 303,
			EvictedExtraItemsPreiodic = 304,
			GarbageCollectedWithoutLost = 311,
			GarbageCollectedWithLost = 312,
			FailedToRefreshEvictionTImer = 391,
		}
	}
}

