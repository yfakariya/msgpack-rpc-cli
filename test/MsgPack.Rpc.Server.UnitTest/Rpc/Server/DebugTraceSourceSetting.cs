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
using System.Linq;

namespace MsgPack.Rpc.Server
{
	/// <summary>
	///		Enable and reset trace source settings.
	/// </summary>
	internal sealed class DebugTraceSourceSetting : IDisposable
	{
		private readonly SourceLevels[] _originalLevels;
		private readonly TraceListener[][] _originalListeners;
		private readonly TraceSource[] _traceSources;

		/// <summary>
		///		Enable debug trace.
		/// </summary>
		/// <param name="isDebug"><c>true</c> to enable debug trace; <c>false</c>, otherwise.</param>
		/// <param name="sources">Target <see cref="TraceSource"/>.</param>
		public DebugTraceSourceSetting( bool isDebug, params TraceSource[] sources )
		{
			this._traceSources = sources;
			this._originalLevels = sources.Select( x => x.Switch.Level ).ToArray();
			if ( isDebug )
			{
				this._originalListeners = new TraceListener[ sources.Length ][];
				for ( int i = 0; i < sources.Length; i++ )
				{
					this._originalListeners[ i ] = new TraceListener[ sources[ i ].Listeners.Count ];
					sources[ i ].Listeners.CopyTo( this._originalListeners[ i ], 0 );
					sources[ i ].Switch.Level = SourceLevels.All;
					sources[ i ].Listeners.Clear();
					sources[ i ].Listeners.Add( new ConsoleTraceListener() { TraceOutputOptions = TraceOptions.DateTime | TraceOptions.Timestamp | TraceOptions.ThreadId } );
				}
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
				for ( int i = 0; i < this._traceSources.Length; i++ )
				{
					this._traceSources[ i ].Listeners.Clear();
					this._traceSources[ i ].Listeners.AddRange( this._originalListeners[ i ] );
					this._traceSources[ i ].Switch.Level = this._originalLevels[ i ];
				}
			}
		}
	}
}
