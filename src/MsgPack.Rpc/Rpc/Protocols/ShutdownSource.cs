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

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Represents the source which caused transport shutdown.
	/// </summary>
	public enum ShutdownSource
	{
		/// <summary>
		///		Unknown. This might indicate internal runtime error.
		/// </summary>
		Unknown = 0,

		/// <summary>
		///		Client initiated the shutdown, it might be done in normal shutdown sequence, or client failure.
		/// </summary>
		Client = 1,

		/// <summary>
		///		Server initiated the shutdown, it might indicates server maintenance or failure.
		/// </summary>
		Server = 2,

		/// <summary>
		///		Disposing current transport causes rudely shutdown.
		/// </summary>
		Disposing = 3
	}
}