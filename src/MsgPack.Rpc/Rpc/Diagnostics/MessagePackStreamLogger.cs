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
using System.IO;
using System.Net;
using System.Security;
using System.Threading;

namespace MsgPack.Rpc.Diagnostics
{
	/// <summary>
	///		Implements basic common features for <see cref="IMessagePackStreamLogger"/>s.
	/// </summary>
	public abstract class MessagePackStreamLogger : IMessagePackStreamLogger, IDisposable
	{
		private static readonly ThreadLocal<TraceEventCache> _traceEventCache = new ThreadLocal<TraceEventCache>( () => new TraceEventCache() );
		private static readonly DateTime _processStartTimeUtc = GetProcessStartTimeUtc();
		private static readonly string _processName = GetProcessName();

		private static DateTime GetProcessStartTimeUtc()
		{
#if !SILVERLIGHT
			try
			{
				return PrivilegedGetProcessStartTimeUtc();
			}
			catch ( SecurityException )
			{
				// This value ensures that resulting process identifier is unique.
				return DateTime.UtcNow;
			}
			catch ( MemberAccessException )
			{
				// This value ensures that resulting process identifier is unique.
				return DateTime.UtcNow;
			}
#else
			return DateTime.UtcNow;
#endif
		}

#if !SILVERLIGHT
		[SecuritySafeCritical]
		private static DateTime PrivilegedGetProcessStartTimeUtc()
		{
			using ( var process = Process.GetCurrentProcess() )
			{
				return process.StartTime.ToUniversalTime();
			}
		}
#endif

		private static string GetProcessName()
		{
#if !SILVERLIGHT
			try
			{
				return PrivilegedGetProcessName();
			}
			catch ( SecurityException )
			{
				return String.Empty;
			}
			catch ( MemberAccessException )
			{
				return String.Empty;
			}
#else
			return String.Empty;
#endif
		}

#if !SILVERLIGHT
		[SecuritySafeCritical]
		private static string PrivilegedGetProcessName()
		{
			using ( var process = Process.GetCurrentProcess() )
			{
				return Path.GetFileNameWithoutExtension( process.MainModule.ModuleName );
			}
		}
#endif

		/// <summary>
		///		Gets the current process id.
		/// </summary>
		/// <value>
		///		The current process id.
		/// </value>
		protected static int ProcessId
		{
			get { return _traceEventCache.Value.ProcessId; }
		}

		/// <summary>
		///		Gets the current process start time in UTC.
		/// </summary>
		/// <value>
		///		The current process start time in UTC. 
		/// </value>
		protected static DateTime ProcessStartTimeUtc
		{
			get { return _processStartTimeUtc; }
		}

		/// <summary>
		///		Gets the name of the current process.
		/// </summary>
		/// <value>
		///		The name of the current process.
		/// </value>
		protected static string ProcessName
		{
			get { return _processName; }
		}

		/// <summary>
		///		Gets the managed thread identifier.
		/// </summary>
		/// <value>
		///		The managed thread identifier.
		/// </value>
		protected static string ThreadId
		{
			get { return _traceEventCache.Value.ThreadId; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MessagePackStreamLogger"/> class.
		/// </summary>
		protected MessagePackStreamLogger() { }

		/// <summary>
		///		Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			this.Dispose( true );
			GC.SuppressFinalize( this );
		}

		/// <summary>
		///		Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected virtual void Dispose( bool disposing )
		{
			// nop
		}

		/// <summary>
		///		Writes the specified data to log sink.
		/// </summary>
		/// <param name="sessionStartTime">The <see cref="DateTimeOffset"/> when session was started.</param>
		/// <param name="remoteEndPoint">The <see cref="EndPoint"/> which is data source of the <paramref name="stream"/>.</param>
		/// <param name="stream">The MessagePack data stream. This value might be corrupted or actually not a MessagePack stream.</param>
		public abstract void Write( DateTimeOffset sessionStartTime, EndPoint remoteEndPoint, IEnumerable<byte> stream );
	}
}
