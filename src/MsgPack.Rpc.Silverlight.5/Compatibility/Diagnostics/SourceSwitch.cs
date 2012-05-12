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

#if SILVERLIGHT
using System;

namespace MsgPack
{
	/// <summary>
	///		System.Diagnostics.SourceSwitch alternative.
	/// </summary>
	internal sealed class SourceSwitch
	{
		public SourceLevels Level
		{
			get;
			set;
		}

		internal SourceSwitch() { }

		public bool ShouldTrace( TraceEventType eventType )
		{
			switch ( eventType )
			{
				case TraceEventType.Critical:
				{
					return ( this.Level & SourceLevels.Critical ) != 0;
				}
				case TraceEventType.Error:
				{
					return ( this.Level & SourceLevels.Error ) != 0;
				}
				case TraceEventType.Warning:
				{
					return ( this.Level & SourceLevels.Warning ) != 0;
				}
				case TraceEventType.Information:
				{
					return ( this.Level & SourceLevels.Information ) != 0;
				}
				case TraceEventType.Verbose:
				{
					return ( this.Level & SourceLevels.Verbose ) != 0;
				}
				default:
				{
					return false;
				}
			}
		}
	}
}
#endif