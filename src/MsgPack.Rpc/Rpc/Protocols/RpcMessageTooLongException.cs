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
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Thrown if incoming MsgPack-RPC message exceeds the quota.
	/// </summary>
#if !SILVERLIGHT
	[Serializable]
#endif
	[SuppressMessage( "Microsoft.Usage", "CA2240:ImplementISerializableCorrectly", Justification = "Using ISafeSerializationData." )]
	[SuppressMessage( "Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Justification = "Using ISafeSerializationData." )]
	public sealed class RpcMessageTooLongException : RpcProtocolException
	{
		/// <summary>
		///		Initializes a new instance of the <see cref="RpcMessageTooLongException"/> class with the default error message.
		/// </summary>
		public RpcMessageTooLongException() : this( null, null ) { }

		/// <summary>
		///		Initializes a new instance of the <see cref="RpcMessageTooLongException"/> class with a specified error message.
		/// </summary>
		/// <param name="message">
		///		Error message to desribe condition. Note that this message should not include security related information.
		///	</param>
		/// <param name="debugInformation">
		///		Debug information of error.
		///		This value can be null for security reason, and its contents are for developers, not end users.
		/// </param>
		public RpcMessageTooLongException( string message, string debugInformation ) : this( message, debugInformation, null ) { }

		/// <summary>
		///		Initializes a new instance of the <see cref="RpcMessageTooLongException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception. 
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
		///		Initializes a new instance of the <see cref="RpcException"/> class with the unpacked data.
		/// </summary>
		/// <param name="unpackedException">
		///		Exception data from remote MessagePack-RPC server.
		///	</param>
		/// <exception cref="SerializationException">
		///		Cannot deserialize instance from <paramref name="unpackedException"/>.
		/// </exception>
		internal RpcMessageTooLongException( MessagePackObject unpackedException ) : base( RpcError.MessageTooLargeError, unpackedException ) { }

#if MONO
		/// <summary>
		///		Initializes a new instance with serialized data. 
		/// </summary>
		/// <param name="info">
		///		The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown. 
		/// </param>
		/// <param name="context">
		///		The <see cref="StreamingContext"/> that contains contextual information about the source or destination.
		/// </param>
		/// <exception cref="T:System.ArgumentNullException">
		///   <paramref name="info"/><paramref name="info"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="T:System.Runtime.Serialization.SerializationException">
		///		The class name is <c>null</c>.
		///		Or <see cref="P:System.Exception.HResult"/> is zero(0).
		/// </exception>
		/// <permission cref="System.Security.Permissions.SecurityPermission"><c>LinkDemand</c>, <c>Flags=SerializationFormatter</c></permission>
		[SecurityPermission( SecurityAction.LinkDemand, SerializationFormatter = true )]
		private RpcMessageTooLongException( SerializationInfo info, StreamingContext context )
			: base( info, context ) { }
#endif
	}
}
