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
using System.Globalization;
using MsgPack.Collections;

namespace MsgPack.Rpc.Serialization
{
	// TODO: cleanup
	/// <summary>
	///		Stores context information of request or notification message deserialization.
	/// </summary>
	public sealed class RequestMessageDeserializationContext : MessageDeserializationContext
	{
		private int? _messageId;

		/// <summary>
		///		Get ID of deserializing message.
		/// </summary>
		/// <value>
		///		ID of deserializing message.
		///		When message has not been deserialized yet, then null.
		///		Else, if message is notification message, then null.
		/// </value>
		public int? MessageId
		{
			get { return this._messageId; }
			internal set { this._messageId = value; }
		}

		private string _methodName;

		/// <summary>
		///		Get name of method to be called.
		/// </summary>
		/// <value>
		///		Name of method to be called.
		/// </value>
		public string MethodName
		{
			get { return this._methodName; }
			internal set
			{
				if ( value == null )
				{
					throw new ArgumentNullException( "value" );
				}

				if ( String.IsNullOrWhiteSpace( value ) )
				{
					throw new ArgumentException( "'value' cannot be empty.", "value" );
				}

				this._methodName = value;
			}
		}

		private IList<MessagePackObject> _arguments;

		/// <summary>
		///		Get arguments of method to be called.
		/// </summary>
		/// <value>
		///		Arguments of method to be called. This value will not be null.
		/// </value>
		public IList<MessagePackObject> Arguments
		{
			get { return this._arguments ?? Arrays<MessagePackObject>.Empty; }
			internal set { this._arguments = value; }
		}

		internal RequestMessageDeserializationContext( IEnumerable<byte> buffer, int? maxLength )
			: base( buffer, maxLength ) { }
	}
}
