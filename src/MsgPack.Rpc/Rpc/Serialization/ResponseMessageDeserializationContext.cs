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
using MsgPack.Collections;
using System.Collections.Generic;

namespace MsgPack.Rpc.Serialization
{
	/// <summary>
	///		Stores context information of response message deserialization.
	/// </summary>
	public sealed class ResponseMessageDeserializationContext : MessageDeserializationContext
	{
		private int _messageId;

		/// <summary>
		///		Get message ID of this message.
		/// </summary>
		/// <value>
		///		Message ID of this message.
		/// </value>
		public int MessageId
		{
			get { return this._messageId; }
			internal set { this._messageId = value; }
		}

		private MessagePackObject _error;

		/// <summary>
		///		Get error of this message.
		/// </summary>
		/// <value>
		///		Error of this message.
		/// </value>
		public MessagePackObject Error
		{
			get { return this._error; }
			internal set { this._error = value; }
		}

		private MessagePackObject _deserializedResult;

		/// <summary>
		///		Get return value or error detail of this message.
		/// </summary>
		/// <value>
		///		Return value or error detail of this message.
		/// </value>
		public MessagePackObject DeserializedResult
		{
			get { return this._deserializedResult; }
			internal set { this._deserializedResult = value; }
		}

		/// <summary>
		///		Initialize new instance.
		/// </summary>
		/// <param name="buffer">Buffer which contains packed stream.</param>
		/// <param name="maxLength">Maximum quota.</param>
		internal ResponseMessageDeserializationContext( IEnumerable<byte> buffer, int? maxLength )
			: base( buffer, maxLength ) { }
	}
}
