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
using System.Net.Sockets;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Reprsents event data of <see cref="EventLoop.TransportError"/> event.
	/// </summary>
	public sealed class RpcTransportErrorEventArgs : EventArgs
	{
		private readonly int? _messageId;

		/// <summary>
		///		Get message ID of transporting message.
		/// </summary>
		/// <value>
		///		Message ID of transporting message.
		///		If message ID is unknown or message is notification message, this value is null.
		/// </value>
		public int? MessageId
		{
			get { return this._messageId; }
		}

		private readonly RpcTransportOperation _operation;

		/// <summary>
		///		Get operation type which caused this error.
		/// </summary>
		/// <value>
		///		Operation type which caused this error.
		/// </value>
		public RpcTransportOperation Operation
		{
			get { return this._operation; }
		}

		private readonly SocketError? _socketErrorCode;

		/// <summary>
		///		Get underlying socket error.
		/// </summary>
		/// <value>
		///		Underlying socket error of this error.
		/// </value>
		public SocketError? SocketErrorCode
		{
			get { return _socketErrorCode; }
		}

		private readonly RpcErrorMessage? _rpcError;

		/// <summary>
		///		Get RPC error message.
		/// </summary>
		/// <value>
		///		RPC error of this error.
		/// </value>
		public RpcErrorMessage? RpcError
		{
			get { return this._rpcError; }
		}

		/// <summary>
		///		Initialize new instance which represents socket level error.
		/// </summary>
		/// <param name="operation">Last operation.</param>
		/// <param name="socketErrorCode">Error code.</param>
		public RpcTransportErrorEventArgs( SocketAsyncOperation operation, SocketError socketErrorCode )
		{
			this._operation = ToRpcTransportOperation( operation );
			this._socketErrorCode = socketErrorCode;
		}

		/// <summary>
		///		Initialize new instance which represents socket level error.
		/// </summary>
		/// <param name="operation">Last operation.</param>
		/// <param name="messageId">ID of message.</param>
		/// <param name="socketErrorCode">Error code.</param>
		public RpcTransportErrorEventArgs( SocketAsyncOperation operation, int messageId, SocketError socketErrorCode )
		{
			this._operation = ToRpcTransportOperation( operation );
			this._messageId = messageId;
			this._socketErrorCode = socketErrorCode;
		}

		private static RpcTransportOperation ToRpcTransportOperation( SocketAsyncOperation operation )
		{
			switch ( operation )
			{
				case SocketAsyncOperation.Accept:
				{
					return RpcTransportOperation.Accept;
				}
				case SocketAsyncOperation.Connect:
				{
					return RpcTransportOperation.Connect;
				}
				case SocketAsyncOperation.Receive:
				case SocketAsyncOperation.ReceiveFrom:
				case SocketAsyncOperation.ReceiveMessageFrom:
				{
					return RpcTransportOperation.Receive;
				}
				case SocketAsyncOperation.Send:
				case SocketAsyncOperation.SendPackets:
				case SocketAsyncOperation.SendTo:
				{
					return RpcTransportOperation.Send;
				}
				default:
				{
					Debug.WriteLine( "Unepcted SocketAsyncOperation:" + operation );
					return RpcTransportOperation.Unknown;
				}
			}
		}

		/// <summary>
		///		Initialize new instance which represents RPC level error.
		/// </summary>
		/// <param name="operation">Last operation.</param>
		/// <param name="rpcError">Error.</param>
		public RpcTransportErrorEventArgs( RpcTransportOperation operation, RpcErrorMessage rpcError )
		{
			this._operation = operation;
			this._rpcError = rpcError;
		}

		/// <summary>
		///		Initialize new instance which represents RPC level error.
		/// </summary>
		/// <param name="operation">Last operation.</param>
		/// <param name="messageId">ID of message.</param>
		/// <param name="rpcError">Error.</param>
		public RpcTransportErrorEventArgs( RpcTransportOperation operation, int messageId, RpcErrorMessage rpcError )
		{
			this._operation = operation;
			this._messageId = messageId;
			this._rpcError = rpcError;
		}
	}
}
