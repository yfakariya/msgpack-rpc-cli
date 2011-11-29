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
using System.Runtime.Serialization;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Thrown if incoming MsgPack-RPC message exceeds the quota.
	/// </summary>
	[Serializable]
	public sealed class RpcMessageTooLongException : RpcProtocolException
	{
		/// <summary>
		///		Initialize new instance with system default message.
		/// </summary>
		public RpcMessageTooLongException() : this( "Message is too large.", null ) { }

		/// <summary>
		///		Initialize new instance which represents specified error.
		/// </summary>
		/// <param name="message">
		///		Error message to desribe condition. Note that this message should not include security related information.
		///	</param>
		/// <param name="debugInformation">
		///		Debug information of error.
		///		This value can be null for security reason, and its contents are for developers, not end users.
		/// </param>
		public RpcMessageTooLongException( string message, string debugInformation ) : base( RpcError.MessageTooLargeError, message, debugInformation ) { }

		/// <summary>
		///		Initialize new instance which represents specified error.
		/// </summary>
		/// <param name="message">
		///		Error message to desribe condition. Note that this message should not include security related information.
		///	</param>
		/// <param name="debugInformation">
		///		Debug information of error.
		///		This value can be null for security reason, and its contents are for developers, not end users.
		/// </param>
		/// <param name="inner">
		///		Exception which caused this error.
		/// </param>		
		public RpcMessageTooLongException( string message, string debugInformation, Exception inner ) : base( RpcError.MessageTooLargeError, message, debugInformation, inner ) { }

		/// <summary>
		///		Initialize new instance with serialized data.
		/// </summary>
		/// <param name="info"><see cref="SerializationInfo"/> which has serialized data.</param>
		/// <param name="context"><see cref="StreamingContext"/> which has context information about transport source or destination.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="info"/> is null.
		/// </exception>
		/// <exception cref="SerializationException">
		///		Cannot deserialize instance from <paramref name="info"/>.
		/// </exception>
		private RpcMessageTooLongException( SerializationInfo info, StreamingContext context ) : base( info, context ) { }

		/// <summary>
		///		Initialize new sintance with unpacked data.
		/// </summary>
		/// <param name="unpackedException">
		///		Exception data from remote MessagePack-RPC server.
		///	</param>
		/// <exception cref="SerializationException">
		///		Cannot deserialize instance from <paramref name="unpackedException"/>.
		/// </exception>
		internal RpcMessageTooLongException( MessagePackObject unpackedException ) : base( RpcError.MessageTooLargeError, unpackedException ) { }
	}
}
