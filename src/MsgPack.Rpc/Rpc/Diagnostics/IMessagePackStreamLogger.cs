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
using System.Net;

namespace MsgPack.Rpc.Diagnostics
{
	/// <summary>
	///		Defines common interface of loggers for MessagePack stream.
	/// </summary>
	public interface IMessagePackStreamLogger
	{
		/// <summary>
		///		Writes the specified data to log sink.
		/// </summary>
		/// <param name="sessionStartTime">The <see cref="DateTimeOffset"/> when session was started.</param>
		/// <param name="remoteEndPoint">The <see cref="EndPoint"/> which is data source of the <paramref name="stream"/>.</param>
		/// <param name="stream">The MessagePack data stream. This value might be corrupted or actually not a MessagePack stream.</param>
		void Write( DateTimeOffset sessionStartTime, EndPoint remoteEndPoint, IEnumerable<byte> stream );
	}
}
