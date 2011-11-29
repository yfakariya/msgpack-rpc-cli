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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization;
using MsgPack.Rpc.Protocols;
using System.Text;
using MsgPack.Collections;
using System.Runtime.InteropServices;
using System.Diagnostics.Contracts;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Rperesents MessagePack-RPC related exception.
	/// </summary>
	/// <remarks>
	///		<para>
	///		</para>
	///		<para>
	///			There is no specification to represent error in MessagePack-RPC,
	///			but de-facto is map which has following structure:
	///			<list type="table">
	///				<listheader>
	///					<term>Key</term>
	///					<description>Value</description>
	///				</listheader>
	///				<item>
	///					<term>ErrorCode</term>
	///					<description>
	///						<para><strong>Type:</strong><see cref="Int32"/></para>
	///						<para><strong>Value:</strong>
	///						Error code to identify error type.
	///						</para>
	///					</description>
	///					<term>Description</term>
	///					<description>
	///						<para><strong>Type:</strong><see cref="String"/></para>
	///						<para><strong>Value:</strong>
	///						Description of message.
	///						<note>
	///							Note that this value should not contain any sensitive information.
	///							Since detailed error information might be exploit for clackers,
	///							this value should not contain such information.
	///						</note>
	///						</para>
	///					</description>
	///					<term>DebugInformation</term>
	///					<description>
	///						<para><strong>Type:</strong><see cref="String"/></para>
	///						<para><strong>Value:</strong>
	///						Detailed information to debug.
	///						This value is optional.
	///						Server should send this information only when target end point (client) is certainly localhost 
	///						or server is explicitly configured as testing environment.
	///						</para>
	///					</description>
	///				</item>
	///			</list>
	///		</para>
	/// </remarks>
	[Serializable]
	public partial class RpcException : Exception
	{
		/// <summary>
		///		"ErrorCode" of utf-8.
		/// </summary>
		private static readonly MessagePackObject _errorCodeKeyUtf8 = MessagePackConvert.EncodeString( "ErrorCode" );

		private readonly RpcError _rpcError;

		/// <summary>
		///		Get metadata of error.
		/// </summary>
		/// <value>
		///		Metadata of error. This value will not be null.
		/// </value>
		public RpcError RpcError
		{
			get { return this._rpcError; }
		}

		private readonly string _debugInformation;

		/// <summary>
		///		Get debug information of error.
		/// </summary>
		/// <value>
		///		Debug information of error.
		///		This value may be null for security reason, and its contents are for developers, not end users.
		/// </value>
		public string DebugInformation
		{
			get { return this._debugInformation; }
		}

		/// <summary>
		///		Initialize new instance which represents specified error with specified message..
		/// </summary>
		/// <param name="rpcError">
		///		Metadata of error. If you specify null, <see cref="MsgPack.Rpc.RpcError.RemoteRuntimeError"/> is used.
		///	</param>
		/// <param name="message">
		///		Error message to desribe condition. Note that this message should not include security related information.
		///	</param>
		/// <param name="debugInformation">
		///		Debug information of error.
		///		This value can be null for security reason, and its contents are for developers, not end users.
		/// </param>
		/// <remarks>
		///		<para>
		///			For example, if some exception is occurred in server application,
		///			the value of <see cref="Exception.ToString()"/> should specify for <paramref name="debugInformation"/>.
		///			And then, user-friendly, safe message should be specified to <paramref name="message"/> like 'Internal Error."
		///		</para>
		///		<para>
		///			MessagePack-RPC for CLI runtime does not propagate <see cref="DebugInformation"/> for remote endpoint.
		///			So you should specify some error handler to instrument it (e.g. logging handler).
		///		</para>
		/// </remarks>
		public RpcException( RpcError rpcError, string message, string debugInformation ) : this( rpcError, message, debugInformation, null ) { }

		/// <summary>
		///		Initialize new instance which represents specified error with specified message and inner exception.
		/// </summary>
		/// <param name="rpcError">
		///		Metadata of error. If you specify null, <see cref="MsgPack.Rpc.RpcError.RemoteRuntimeError"/> is used.
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
		/// <remarks>
		///		<para>
		///			For example, if some exception is occurred in server application,
		///			the value of <see cref="Exception.ToString()"/> should specify for <paramref name="debugInformation"/>.
		///			And then, user-friendly, safe message should be specified to <paramref name="message"/> like 'Internal Error."
		///		</para>
		///		<para>
		///			MessagePack-RPC for CLI runtime does not propagate <see cref="DebugInformation"/> for remote endpoint.
		///			So you should specify some error handler to instrument it (e.g. logging handler).
		///		</para>
		/// </remarks>
		public RpcException( RpcError rpcError, string message, string debugInformation, Exception inner )
			: base( message, inner )
		{
			this._rpcError = rpcError ?? RpcError.RemoteRuntimeError;
			this._debugInformation = debugInformation;
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
		protected RpcException( SerializationInfo info, StreamingContext context ) : base( info, context ) { }

		/// <summary>
		///		Create <see cref="RpcException"/> or dervied instance which corresponds to sepcified error information.
		/// </summary>
		/// <param name="error">Basic error information. This information will propagate to client.</param>
		/// <param name="errorDetail">Detailed error information, which is usally debugging purpose only, so will not propagate to client.</param>
		/// <returns>
		///		<see cref="RpcException"/> or dervied instance which corresponds to sepcified error information.
		/// </returns>
		/// <exception cref="ArgumentException">
		///		<paramref name="error"/> is <see cref="MessagePackObject.IsNil">nil</see>.
		/// </exception>
		public static RpcException FromMessage( MessagePackObject error, MessagePackObject errorDetail )
		{
			// TODO: Application specific customization
			// TODO: Application specific exception class

			if ( error.IsNil )
			{
				throw new ArgumentException( "'error' must not be nil.", "error" );
			}

			// Recommeded path
			if ( error.IsTypeOf<byte[]>().GetValueOrDefault() )
			{
				string identifier = null;
				try
				{
					identifier = error.AsString();
				}
				catch ( InvalidOperationException ) { }

				int? errorCode = null;

				if ( errorDetail.IsTypeOf<IDictionary<MessagePackObject, MessagePackObject>>().GetValueOrDefault() )
				{
					var asDictionary = errorDetail.AsDictionary();
					MessagePackObject value;
					if ( asDictionary.TryGetValue( _errorCodeKeyUtf8, out value ) && value.IsTypeOf<int>().GetValueOrDefault() )
					{
						errorCode = value.AsInt32();
					}
				}

				if ( identifier != null || errorCode != null )
				{
					RpcError rpcError = RpcError.FromIdentifier( identifier, errorCode );
					return rpcError.ToException( errorDetail );
				}
			}

			// Other path.
			return new UnexpcetedRpcException( error, errorDetail );
		}

		/// <summary>
		///		Create <see cref="RpcException"/> for specified serialization error.
		/// </summary>
		/// <param name="serializationError">Serialization error.</param>
		/// <returns>
		///		<see cref="RpcException"/> for specified serialization error.
		/// </returns>
		internal static RpcException FromRpcError( RpcErrorMessage serializationError )
		{
			return serializationError.Error.ToException( serializationError.Detail );
		}
	}
}
