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
using System.Net;

namespace MsgPack.Rpc.Server
{
	/// <summary>
	///		Represents event data for the <see cref="RpcServer.ClientError"/> event.
	/// </summary>
	public sealed class RpcClientErrorEventArgs : EventArgs
	{
		private readonly RpcErrorMessage _rpcError;

		/// <summary>
		///		Gets the <see cref="RpcErrorMessage"/> represents client error.
		/// </summary>
		/// <value>
		///		The <see cref="RpcErrorMessage"/> represents client error.
		/// </value>
		public RpcErrorMessage RpcError
		{
			get { return this._rpcError; }
		}

		/// <summary>
		///		Gets or sets the remote end point.
		/// </summary>
		/// <value>
		///		The remote end point.
		///		This value may be <c>null</c>.
		/// </value>
		public EndPoint RemoteEndPoint { get; set; }

		/// <summary>
		///		Gets or sets the session id.
		/// </summary>
		/// <value>
		///		The session id.
		/// </value>
		public long SessionId { get; set; }

		/// <summary>
		///		Gets or sets the message id.
		/// </summary>
		/// <value>
		///		The message id.
		///		This value will be <c>null</c> for the notification.
		/// </value>
		public int? MessageId { get; set; }

		/// <summary>
		///		Initializes a new instance of the <see cref="RpcClientErrorEventArgs"/> class.
		/// </summary>
		/// <param name="rpcError">The <see cref="RpcErrorMessage"/> represents client error.</param>
		public RpcClientErrorEventArgs( RpcErrorMessage rpcError )
		{
			this._rpcError = rpcError;
		}
	}
}
