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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net;

namespace MsgPack.Rpc.Diagnostics
{
	/// <summary>
	///		In proc ring buffer based <see cref="MessagePackStreamLogger"/>.
	/// </summary>
	public sealed class InProcMessagePackStreamLogger : MessagePackStreamLogger
	{
		private readonly int? _limit;

		/// <summary>
		///		Gets the limit of internal buffer. <c>0</c> means inifinite.
		/// </summary>
		/// <value>
		///		The limit of internal buffer. <c>0</c> means inifinite.
		/// </value>
		public int? Limit
		{
			get { return this._limit; }
		}

		private readonly BlockingCollection<InProcMessagePackStreamLoggerEntry> _entries;

		/// <summary>
		///		Gets the written entries.
		/// </summary>
		/// <value>
		///		The written entries.
		/// </value>
		public IEnumerable<InProcMessagePackStreamLoggerEntry> Entries
		{
			get { return this._entries; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="InProcMessagePackStreamLogger"/> class without limit.
		/// </summary>
		public InProcMessagePackStreamLogger() : this( null ) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="InProcMessagePackStreamLogger"/> class.
		/// </summary>
		/// <param name="limit">The limit of internal buffer. <c>null</c> means infinite.</param>
		public InProcMessagePackStreamLogger( int? limit )
		{
			if ( limit != null && limit.GetValueOrDefault() <= 0 )
			{
				throw new ArgumentOutOfRangeException( "limit" );
			}

			Contract.EndContractBlock();

			this._limit = limit;
			var queue = new ConcurrentQueue<InProcMessagePackStreamLoggerEntry>();
			if ( limit == null )
			{
				this._entries = new BlockingCollection<InProcMessagePackStreamLoggerEntry>( queue );
			}
			else
			{
				this._entries = new BlockingCollection<InProcMessagePackStreamLoggerEntry>( queue, limit.Value );
			}
		}

		/// <summary>
		/// Writes the specified data to log sink.
		/// </summary>
		/// <param name="sessionStartTime">The <see cref="DateTimeOffset"/> when session was started.</param>
		/// <param name="remoteEndPoint">The <see cref="EndPoint"/> which is data source of the <paramref name="stream"/>.</param>
		/// <param name="stream">The MessagePack data stream. This value might be corrupted or actually not a MessagePack stream.</param>
		public override void Write( DateTimeOffset sessionStartTime, EndPoint remoteEndPoint, IEnumerable<byte> stream )
		{
			while ( !this._entries.TryAdd( new InProcMessagePackStreamLoggerEntry( sessionStartTime, remoteEndPoint, stream ) ) )
			{
				this._entries.Take();
			}
		}
	}
}