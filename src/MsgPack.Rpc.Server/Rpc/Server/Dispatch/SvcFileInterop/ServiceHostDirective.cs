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

namespace MsgPack.Rpc.Server.Dispatch.SvcFileInterop
{
	/// <summary>
	///		Represents WCF &lt;%@ServiceHost %&gt; directive contents.
	/// </summary>
	internal sealed class ServiceHostDirective
	{
		/// <summary>
		///		Gets or sets the decoded Service attribute content.
		/// </summary>
		/// <value>
		/// The decoded Service attribute content.
		/// </value>
		public string Service
		{
			get;
			internal set;
		}

		internal ServiceHostDirective() { }
	}
}
