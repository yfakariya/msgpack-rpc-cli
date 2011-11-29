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
using System.Diagnostics.Contracts;
using MsgPack.Collections;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Represents MsgPack-RPC request message or notification message from client to server.
	/// </summary>
	public struct RequestMessage
	{
		/// <summary>
		///		Get message type of this message.
		/// </summary>
		/// <value>Message type of this message.</value>
		public MessageType MessageType
		{
			get { return this._messageId == null ? Protocols.MessageType.Notification : Protocols.MessageType.Request; }
		}

		private readonly int? _messageId;

		/// <summary>
		///		Get id of this request/response message.
		/// </summary>
		/// <value>
		///		ID of this request/response message.
		///		If this message is <see cref="Protocols.MessageType.Notification"/> then this value is null.
		/// </value>
		public int MessageId
		{
			get { return this._messageId.Value; }
		}

		private readonly string _method;

		/// <summary>
		///		Get identifier of calling method.
		/// </summary>
		/// <value>
		///		Identifier of calling method. 
		/// </value>
		public string Method
		{
			get { return this._method; }
		}

		private readonly IList<MessagePackObject> _arguments;

		/// <summary>
		///		Get list of arguments.
		/// </summary>
		/// <value>
		///		List of arguments. This value will not be null.
		/// </value>
		public IList<MessagePackObject> Arguments
		{
			get { return this._arguments; }
		}

		/// <summary>
		///		Initialize new instance.
		/// </summary>
		/// <param name="messageId">ID of message. If this message is notification message, specify null.</param>
		/// <param name="methodName">Name of method which this message is calling.</param>
		/// <param name="arguments">Arguments of method to be passed.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="methodName"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="methodName"/> is illegal.
		/// </exception>
		public RequestMessage( int? messageId, string methodName, IList<MessagePackObject> arguments )
		{
			if ( methodName == null )
			{
				throw new ArgumentNullException( "methodName" );
			}

			// TODO: Validate more strictly?
			if ( String.IsNullOrWhiteSpace( methodName ) )
			{
				throw new ArgumentException( "'methodName' cannot be empty.", "methodName" );
			}

			Contract.EndContractBlock();

			this._messageId = messageId;
			this._method = methodName;
			this._arguments = arguments ?? Arrays<MessagePackObject>.Empty;
		}
	}
}
