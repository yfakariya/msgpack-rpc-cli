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

namespace MsgPack.Rpc.Serialization
{
	/// <summary>
	///		Stores context information of request or notification message serialization.
	/// </summary>
	public sealed class RequestMessageSerializationContext : MessageSerializationContext
	{
		private readonly int? _messageId;

		/// <summary>
		///		Get ID of message.
		/// </summary>
		/// <value>ID of message. If this message is notification then null.</value>
		public int? MessageId
		{
			get { return this._messageId; }
		} 


		private readonly string _methodName;

		/// <summary>
		///		Get name of target method.
		/// </summary>
		/// <value>Name of target method.</value>
		public string MethodName
		{
			get { return this._methodName; }
		}

		private readonly IList<object> _arguments;

		/// <summary>
		///		Get arguments to invoke target method.
		/// </summary>
		/// <value>
		///		Arguments to invoke target method.
		/// </value>
		public IList<object> Arguments
		{
			get { return this._arguments; }
		}

		internal RequestMessageSerializationContext( RpcOutputBuffer buffer, int? messageId, string methodName, IList<object> arguments )
			: base( buffer )
		{
			if ( methodName == null )
			{
				throw new ArgumentNullException( "methodName" );
			}

			if ( String.IsNullOrWhiteSpace( methodName ) )
			{
				throw new ArgumentException( "'methodName' cannot be empty.", "methodName" );
			}

			if ( arguments == null )
			{
				throw new ArgumentNullException( "arguments" );
			}

			Contract.EndContractBlock();

			this._messageId = messageId;
			this._methodName = methodName;
			this._arguments = arguments;
		}
	}

}
