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
	///		Represents MsgPack-RPC response message from server to client.
	/// </summary>
	public struct ResponseMessage
	{
		private readonly int _messageId;

		/// <summary>
		///		Get id of this message.
		/// </summary>
		/// <value>
		///		Id of this message.
		/// </value>
		public int MessageId
		{
			get { return this._messageId; }
		}

		private readonly RpcException _error;

		/// <summary>
		///		Get detected error in server.
		/// </summary>
		/// <value>
		///		Detected error in server.
		///		If request was processed successfully, then null.
		///		When <see cref="ReturnValue"/> is not <see cref="MessagePackObject.IsNil">nil</see>,
		///		then this value will be null.
		/// </value>
		public RpcException Error
		{
			get { return this._error; }
		}

		private readonly MessagePackObject _returnValue;

		/// <summary>
		///		Get return value of previous request.
		/// </summary>
		/// <value>
		///		Return value of previous request.
		///		When <see cref="Error"/> is not nil, this value is <see cref="MessagePackObject.IsNil">nil</see>.
		///		Detailed error information might be contained by <see cref="Error"/>.
		/// </value>
		public MessagePackObject ReturnValue
		{
			get { return this._returnValue; }
		}

		/// <summary>
		///		Initialize new instance for response of 'void' method call request.
		/// </summary>
		/// <param name="messageId">Id of call to correspond request and response.</param>
		public ResponseMessage( int messageId )
			: this( messageId, MessagePackObject.Nil ) { }

		/// <summary>
		///		Initialize new instance for response of 'non void' method call request.
		/// </summary>
		/// <param name="messageId">Id of call to correspond request and response</param>
		/// <param name="returnValue">Return value of method call.</param>
		public ResponseMessage( int messageId, MessagePackObject returnValue )
			: this( messageId, returnValue, null ) { }

		/// <summary>
		///		Initialize new instance for error of method call.
		/// </summary>
		/// <param name="messageId">Id of call to correspond request and response</param>
		/// <param name="error">Error of method.</param>
		public ResponseMessage( int messageId, RpcException error )
			: this( messageId, MessagePackObject.Nil, error ) { }

		private ResponseMessage( int messageId, MessagePackObject returnValue, RpcException error )
		{
			this._messageId = messageId;
			this._returnValue = returnValue;
			this._error = error;
		}
	}
}
