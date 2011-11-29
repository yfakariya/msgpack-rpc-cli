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
using MsgPack.Rpc.Protocols;
using MsgPack.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Thrown if some arguments are wrong like its type was not match, its value was out of range, its value was null but it is not illegal, so on.
	/// </summary>
	[Serializable]
	public sealed class RpcArgumentException : RpcMethodInvocationException
	{
		private const string _parameterNameKey = "ParameterName";
		private static readonly MessagePackObject _parameterNameKeyUtf8 = MessagePackConvert.EncodeString( _parameterNameKey );

		private readonly string _parameterName;

		/// <summary>
		///		Get name of parameter causing this exception.
		/// </summary>
		/// <value>
		///		Name of parameter causing this exception. This value will not be null nor empty.
		/// </value>
		public string ParameterName
		{
			get { return this._parameterName; }
		}

		/// <summary>
		///		Initialize new instance which represents specified argument error.
		/// </summary>
		///	<param name="methodName">
		///		Name of method which is related to this error.
		///	</param>
		///	<param name="parameterName">
		///		Name of parameter which is invalid.
		///	</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="methodName"/> or <paramref name="parameterName"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="methodName"/> or <paramref name="parameterName"/> is empty or blank.
		/// </exception>
		public RpcArgumentException( string methodName, string parameterName ) : this( methodName, parameterName, "The value of argument is invalid.", null ) { }

		/// <summary>
		///		Initialize new instance which represents specified argument error.
		/// </summary>
		///	<param name="methodName">
		///		Name of method which is related to this error.
		///	</param>
		///	<param name="parameterName">
		///		Name of parameter which is invalid.
		///	</param>
		/// <param name="message">
		///		Error message to desribe condition. Note that this message should not include security related information.
		///	</param>
		/// <param name="debugInformation">
		///		Debug information of error.
		///		This value can be null for security reason, and its contents are for developers, not end users.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="methodName"/> or <paramref name="parameterName"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="methodName"/> or <paramref name="parameterName"/> is empty or blank.
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
		public RpcArgumentException( string methodName, string parameterName, string message, string debugInformation ) : this( methodName, parameterName, message, debugInformation, null ) { }

		/// <summary>
		///		Initialize new instance which represents specified argument error.
		/// </summary>
		///	<param name="methodName">
		///		Name of method which is related to this error.
		///	</param>
		///	<param name="parameterName">
		///		Name of parameter which is invalid.
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
		///		<paramref name="methodName"/> or <paramref name="parameterName"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="methodName"/> or <paramref name="parameterName"/> is empty or blank.
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
		public RpcArgumentException( string methodName, string parameterName, string message, string debugInformation, Exception inner )
			: base( RpcError.ArgumentError, methodName, message, debugInformation, inner )
		{
			if ( parameterName == null )
			{
				throw new ArgumentNullException( "parameterName" );
			}

			if ( String.IsNullOrWhiteSpace( parameterName ) )
			{
				throw new ArgumentException( "'parameterName' cannot be empty.", "parameterName" );
			}

			Contract.EndContractBlock();

			this._parameterName = parameterName;
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
		private RpcArgumentException( SerializationInfo info, StreamingContext context )
			: base( info, context )
		{
			this._parameterName = info.GetString( _parameterNameKey );
		}

		/// <summary>
		///		Initialize new sintance with unpacked data.
		/// </summary>
		/// <param name="unpackedException">
		///		Exception data from remote MessagePack-RPC server.
		///	</param>
		/// <exception cref="SerializationException">
		///		Cannot deserialize instance from <paramref name="unpackedException"/>.
		/// </exception>
		internal RpcArgumentException( MessagePackObject unpackedException )
			: base( RpcError.ArgumentError, unpackedException )
		{
			MessagePackObjectDictionary.TryGetString( unpackedException, _parameterNameKeyUtf8, RpcException.CreateSerializationException, out this._parameterName );
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
		protected sealed override void GetExceptionMessage( IDictionary<MessagePackObject, MessagePackObject> store, bool includesDebugInformation )
		{
			base.GetExceptionMessage( store, includesDebugInformation );
			store.Add( _parameterNameKeyUtf8, MessagePackConvert.EncodeString( this._parameterName ) );
		}

		/// <summary>
		///		Set up <see cref="SerializationInfo"/> with this instance data.
		/// </summary>
		/// <param name="info"><see cref="SerializationInfo"/> to be set serialized data.</param>
		/// <param name="context"><see cref="StreamingContext"/> which has context information about transport source or destination.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="info"/> is null.
		/// </exception>
		public sealed override void GetObjectData( SerializationInfo info, StreamingContext context )
		{
			base.GetObjectData( info, context );
			info.AddValue( _parameterNameKey, this._parameterName );
		}
	}
}
