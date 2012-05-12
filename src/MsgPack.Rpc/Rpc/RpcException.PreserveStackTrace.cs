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

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MsgPack.Rpc
{
	partial class RpcException : IStackTracePreservable
	{
		private List<string> _preservedStackTrace;

		[SuppressMessage( "Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "Infrastracture." )]
		void IStackTracePreservable.PreserveStackTrace()
		{
			if ( this._preservedStackTrace == null )
			{
				this._preservedStackTrace = new List<string>();
			}

			this._preservedStackTrace.Add(
#if !SILVERLIGHT
				new StackTrace( this, true )
#else
				new StackTrace( this )
#endif
				.ToString() );
		}

		/// <summary>
		///		Gets a string representation of the immediate frames on the call stack.
		/// </summary>
		/// <returns>A string that describes the immediate frames of the call stack.</returns>
		public override string StackTrace
		{
			get
			{
				if ( this._preservedStackTrace == null || this._preservedStackTrace.Count == 0 )
				{
					return base.StackTrace;
				}

				var buffer = new StringBuilder();
				foreach ( var preserved in this._preservedStackTrace )
				{
					buffer.Append( preserved );
					buffer.AppendLine( "   --- End of preserved stack trace ---" );
				}

				buffer.Append( base.StackTrace );
				return buffer.ToString();
			}
		}
	}
}
