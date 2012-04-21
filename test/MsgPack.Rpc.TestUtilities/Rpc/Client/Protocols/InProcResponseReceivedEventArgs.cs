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

namespace MsgPack.Rpc.Client.Protocols
{
	/// <summary>
	///		Contains event data for <see cref="InProcClientTransport.ResponseReceived"/> event.
	/// </summary>
	public sealed class InProcResponseReceivedEventArgs : EventArgs
	{
		private readonly byte[] _receivedData;

		/// <summary>
		///		Gets the originally received data.
		/// </summary>
		/// <value>
		///		The originally received data.
		/// </value>
		public byte[] ReceivedData
		{
			get { return this._receivedData; }
		}

		/// <summary>
		///		Gets or sets the chunked received data.
		/// </summary>
		/// <value>
		///		The chunked received data.
		///		If this value is set, chunked data will be used instead of <see cref="ReceivedData"/>.
		/// </value>
		public IEnumerable<byte[]> ChunkedReceivedData { get; set; }

		/// <summary>
		///		Initializes a new instance of the <see cref="InProcResponseReceivedEventArgs"/> class.
		/// </summary>
		/// <param name="receivedData">The received data.</param>
		internal InProcResponseReceivedEventArgs( byte[] receivedData )
		{
			this._receivedData = receivedData;
		}
	}
}
