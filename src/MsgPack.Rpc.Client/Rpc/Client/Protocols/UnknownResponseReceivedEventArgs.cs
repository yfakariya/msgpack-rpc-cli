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
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace MsgPack.Rpc.Client.Protocols
{
	/// <summary>
	///		Represents event data of <see cref="E:ClientTransportManager.UnknownResponseReceived"/> event.
	/// </summary>
	public sealed class UnknownResponseReceivedEventArgs : EventArgs
	{
		private readonly int? _messageId;

		/// <summary>
		///		Gets the message ID.
		/// </summary>
		/// <value>
		///		The received message ID.
		/// </value>
		public int? MessageId
		{
			get { return this._messageId; }
		}

		private readonly RpcErrorMessage _error;

		/// <summary>
		///		Gets the received error.
		/// </summary>
		/// <value>
		///		The received error.
		///		This value will not be <c>null</c>, will be <see cref="RpcErrorMessage.IsSuccess"/> is <c>true</c> when no error.
		/// </value>
		public RpcErrorMessage Error
		{
			get { return this._error; }
		}

		private readonly MessagePackObject? _returnValue;

		/// <summary>
		///		Gets the received return value.
		/// </summary>
		/// <value>
		///		The received return value.
		///		Note that this value will be <c>null</c> when <see cref="RpcErrorMessage.IsSuccess"/> property of <see cref="Error"/> is <c>true</c>.
		/// </value>
		public MessagePackObject? ReturnValue
		{
			get { return this._returnValue; }
		}

		internal UnknownResponseReceivedEventArgs( int? messageId, RpcErrorMessage error, MessagePackObject? returnValue )
		{
			this._messageId = messageId;
			this._error = error;
			this._returnValue = returnValue;
		}

		[ContractInvariantMethod]
		[SuppressMessage( "Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "ObjectInvariant." )]
		[SuppressMessage( "Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "ObjectInvariant." )]
		private void ObjectInvariant()
		{
			Contract.Invariant( ( this.Error.IsSuccess && Contract.Result<MessagePackObject?>() != null )
				|| ( !this.Error.IsSuccess && Contract.Result<MessagePackObject?>() == null ) );
		}
	}
}
