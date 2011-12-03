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

namespace MsgPack.Rpc.Server.Protocols
{
	/// <summary>
	///		Enable and reset trace source settings.
	/// </summary>
	internal sealed class DebugTraceSourceSetting : IDisposable
	{
		private readonly SourceLevels _originalLevels;
		private readonly TraceListener[] _originalListeners;
		private readonly TraceSource _traceSource;

		/// <summary>
		///		Enable debug trace.
		/// </summary>
		/// <param name="source">Target <see cref="TraceSource"/>.</param>
		/// <param name="isDebug"><c>true</c> to enable debug trace; <c>false</c>, otherwise.</param>
		public DebugTraceSourceSetting( TraceSource source, bool isDebug )
		{
			this._traceSource = source;
			this._originalLevels = source.Switch.Level;
			if ( isDebug )
			{
				this._originalListeners = new TraceListener[ source.Listeners.Count ];
				source.Listeners.CopyTo( this._originalListeners, 0 );
				source.Switch.Level = SourceLevels.All;
				source.Listeners.Clear();
				source.Listeners.Add( new ConsoleTraceListener() { TraceOutputOptions = TraceOptions.DateTime | TraceOptions.Timestamp | TraceOptions.ThreadId } );
			}
			else
			{
				this._originalListeners = null;
			}
		}

		/// <summary>
		///		Reset settings.
		/// </summary>
		public void Dispose()
		{
			if ( this._originalListeners != null )
			{
				this._traceSource.Listeners.Clear();
				this._traceSource.Listeners.AddRange( this._originalListeners );
				this._traceSource.Switch.Level = this._originalLevels;
			}
		}
	}
}
