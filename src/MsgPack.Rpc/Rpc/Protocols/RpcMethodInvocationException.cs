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
using System.Runtime.Serialization;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Thrown if something wrong during remote method invocation.
	/// </summary>
#if !SILVERLIGHT
	[Serializable]
#endif
	public class RpcMethodInvocationException : RpcException
	{
		private const string _methodNameKey = "MethodName";
		private static readonly MessagePackObject _methodNameKeyUtf8 = MessagePackConvert.EncodeString( "MethodName" );

		private readonly string _methodName;

		/// <summary>
		///		Gets the name of invoking method.
		/// </summary>
		/// <value>
		///		The name of invoking method. This value will not be empty but may be <c>null</c>.
		/// </value>
		public string MethodName
		{
			get
			{
				Contract.Ensures( Contract.Result<string>() != null );
				return this._methodName;
			}
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="RpcMethodInvocationException"/> class with the default error message.
		/// </summary>
		/// <param name="rpcError">
		///		Metadata of error. If you specify null, <see cref="RpcError.CallError"/> is used.
		///	</param>
		///	<param name="methodName">
		///		Name of method which is related to this error.
		///	</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="methodName"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="methodName"/> is empty or blank.
		/// </exception>
		public RpcMethodInvocationException( RpcError rpcError, string methodName ) : this( rpcError, methodName, "Failed to call specified method.", null ) { }

		/// <summary>
		///		Initializes a new instance of the <see cref="RpcMethodInvocationException"/> class with a specified error message.
		/// </summary>
		/// <param name="rpcError">
		///		Metadata of error. If you specify null, <see cref="RpcError.CallError"/> is used.
		///	</param>
		///	<param name="methodName">
		///		Name of method which is related to this error.
		///	</param>
		/// <param name="message">
		///		Error message to desribe condition. Note that this message should not include security related information.
		///	</param>
		/// <param name="debugInformation">
		///		Debug information of error.
		///		This value can be null for security reason, and its contents are for developers, not end users.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="methodName"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="methodName"/> is empty or blank.
		/// </exception>
		/// <remarks>
		///		<para>
		///			For example, if some exception is occurred in server application,
		///			the value of <see cref="Exception.ToString()"/> should specify for <paramref name="debugInformation"/>.
		///			And then, user-friendly, safe message should be specified to <paramref name="message"/> like 'Internal Error."
		///		</para>
		///		<para>
		///			MessagePack-RPC for CLI runtime does not propagate <see cref="RpcException.DebugInformation"/> for remote endpoint.
		///			So you should specify some error handler to instrument it (e.g. logging handler).
		///		</para>
		/// </remarks>
		public RpcMethodInvocationException( RpcError rpcError, string methodName, string message, string debugInformation ) : this( rpcError, methodName, message, debugInformation, null ) { }

		/// <summary>
		///		Initializes a new instance of the <see cref="RpcMethodInvocationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception. 
		/// </summary>
		/// <param name="rpcError">
		///		Metadata of error. If you specify null, <see cref="RpcError.CallError"/> is used.
		///	</param>
		///	<param name="methodName">
		///		Name of method which is related to this error.
		///	</param>
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
		/// <exception cref="ArgumentNullException">
		///		<paramref name="methodName"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="methodName"/> is empty or blank.
		/// </exception>
		/// <remarks>
		///		<para>
		///			For example, if some exception is occurred in server application,
		///			the value of <see cref="Exception.ToString()"/> should specify for <paramref name="debugInformation"/>.
		///			And then, user-friendly, safe message should be specified to <paramref name="message"/> like 'Internal Error."
		///		</para>
		///		<para>
		///			MessagePack-RPC for CLI runtime does not propagate <see cref="RpcException.DebugInformation"/> for remote endpoint.
		///			So you should specify some error handler to instrument it (e.g. logging handler).
		///		</para>
		/// </remarks>
		public RpcMethodInvocationException( RpcError rpcError, string methodName, string message, string debugInformation, Exception inner )
			: base( rpcError, message, debugInformation, inner )
		{
			if ( methodName == null )
			{
				throw new ArgumentNullException( "methodName" );
			}

			if ( String.IsNullOrWhiteSpace( methodName ) )
			{
				throw new ArgumentException( "'methodName' cannot be empty nor blank.", "methodName" );
			}

			Contract.EndContractBlock();

			this._methodName = methodName;
		}

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
		protected RpcMethodInvocationException( SerializationInfo info, StreamingContext context )
			: base( info, context )
		{
			this._methodName = info.GetString( _methodNameKey );
		}

		/// <summary>
		///		Initialize new sintance with unpacked data.
		/// </summary>
		/// <param name="rpcError">
		///		Metadata of error. If you specify null, <see cref="MsgPack.Rpc.RpcError.RemoteRuntimeError"/> is used.
		///		Metadata of error. If you specify null, <see cref="RpcError.RemoteRuntimeError"/> is used.
		///	</param>
		/// <param name="unpackedException">
		///		Exception data from remote MessagePack-RPC server.
		///	</param>
		/// <exception cref="SerializationException">
		///		Cannot deserialize instance from <paramref name="unpackedException"/>.
		/// </exception>
		protected internal RpcMethodInvocationException( RpcError rpcError, MessagePackObject unpackedException )
			: base( rpcError, unpackedException )
		{
			this._methodName = unpackedException.GetString( _methodNameKeyUtf8 );
		}

		/// <summary>
		///		Stores derived type specific information to specified dictionary.
		/// </summary>
		/// <param name="store">
		///		Dictionary to be stored. This value will not be <c>null</c>.
		///	</param>
		/// <param name="includesDebugInformation">
		///		<c>true</c>, when this method should include debug information; otherwise, <c>false</c>.
		///	</param>
		protected override void GetExceptionMessage( IDictionary<MessagePackObject, MessagePackObject> store, bool includesDebugInformation )
		{
			base.GetExceptionMessage( store, includesDebugInformation );
			store.Add( _methodNameKeyUtf8, MessagePackConvert.EncodeString( this._methodName ) );
		}

		/// <summary>
		///		Set up <see cref="SerializationInfo"/> with this instance data.
		/// </summary>
		/// <param name="info"><see cref="SerializationInfo"/> to be set serialized data.</param>
		/// <param name="context"><see cref="StreamingContext"/> which has context information about transport source or destination.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="info"/> is null.
		/// </exception>
		public override void GetObjectData( SerializationInfo info, StreamingContext context )
		{
			base.GetObjectData( info, context );
			info.AddValue( _methodNameKey, this._methodName );
		}
	}
}
