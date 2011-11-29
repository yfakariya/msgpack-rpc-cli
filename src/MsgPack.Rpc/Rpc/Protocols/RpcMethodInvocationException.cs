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
using System.Diagnostics.Contracts;
using MsgPack.Collections;
using System.Collections.Generic;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Thrown if something wrong during remote method invocation.
	/// </summary>
	[Serializable]
	public class RpcMethodInvocationException : RpcException
	{
		private const string _methodNameKey = "MethodName";
		private static readonly MessagePackObject _methodNameKeyUtf8 = MessagePackConvert.EncodeString( "MethodName" );

		private readonly string _methodName;

		/// <summary>
		///		Get a name of invoking method.
		/// </summary>
		/// <value>
		///		Name of invoking method. This value will not be null nor empty.
		/// </value>
		public string MethodName
		{
			get { return this._methodName; }
		}

		/// <summary>
		///		Initialize new instance which represents specified error.
		/// </summary>
		/// <param name="rpcError">
		///		Metadata of error. If you specify null, <see cref="RpcError.RemoteRuntimeError"/> is used.
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
		///		Initialize new instance which represents specified error.
		/// </summary>
		/// <param name="rpcError">
		///		Metadata of error. If you specify null, <see cref="RpcError.RemoteRuntimeError"/> is used.
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
		///		Initialize new instance which represents specified error.
		/// </summary>
		/// <param name="rpcError">
		///		Metadata of error. If you specify null, <see cref="RpcError.RemoteRuntimeError"/> is used.
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
				throw new ArgumentException( "'methodName' cannot be empty not blank.", "methodName" );
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
			MessagePackObjectDictionary.TryGetString( unpackedException, _methodNameKeyUtf8, message => new SerializationException( message ), out this._methodName );
		}

		/// <summary>
		///		Store derived type specific information to specified dictionary.
		/// </summary>
		/// <param name="store">
		///		Dictionary to be stored.
		///	</param>
		/// <param name="includesDebugInformation">
		///		If this method should include debug information then true.
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
