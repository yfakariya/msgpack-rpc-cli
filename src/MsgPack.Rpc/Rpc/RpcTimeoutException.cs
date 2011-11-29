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
using MsgPack.Collections;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Thrown when RPC invocation was time out.
	/// </summary>
	[Serializable]
	public sealed class RpcTimeoutException : RpcException
	{
		private const string _clientTimeoutKey = "ClientTimeout";
		private static readonly MessagePackObject _clientTimeoutKeyUtf8 = MessagePackConvert.EncodeString( _clientTimeoutKey );

		private readonly TimeSpan _clientTimeout;

		/// <summary>
		///		Get timeout value which was expired in client.
		/// </summary>
		/// <value>Timeout value in client.</value>
		public TimeSpan ClientTimeout
		{
			get { return this._clientTimeout; }
		}

		/// <summary>
		///		Initialize new instance.
		/// </summary>
		/// <param name="timeout">Timeout value in client.</param>
		public RpcTimeoutException( TimeSpan timeout ) : this( timeout, "Request has been timeout.", null ) { }

		/// <summary>
		///		Initialize new instance with specified message.
		/// </summary>
		/// <param name="timeout">Timeout value in client.</param>
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
		///			MessagePack-RPC for CLI runtime does not propagate <see cref="RpcException.DebugInformation"/> for remote endpoint.
		///			So you should specify some error handler to instrument it (e.g. logging handler).
		///		</para>
		/// </remarks>
		public RpcTimeoutException( TimeSpan timeout, string message, string debugInformation )
			: base( RpcError.TimeoutError, message, debugInformation )
		{
			this._clientTimeout = timeout;
		}

		/// <summary>
		///		Initialize new instance with specified message and inner exception.
		/// </summary>
		/// <param name="timeout">Timeout value in client.</param>
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
		///			MessagePack-RPC for CLI runtime does not propagate <see cref="RpcException.DebugInformation"/> for remote endpoint.
		///			So you should specify some error handler to instrument it (e.g. logging handler).
		///		</para>
		/// </remarks>
		public RpcTimeoutException( TimeSpan timeout, string message, string debugInformation, Exception inner )
			: base( RpcError.TimeoutError, message, debugInformation, inner )
		{
			this._clientTimeout = timeout;
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
		private RpcTimeoutException( SerializationInfo info, StreamingContext context )
			: base( info, context )
		{
			this._clientTimeout = new TimeSpan( info.GetInt64( _clientTimeoutKey ) );
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
		internal RpcTimeoutException( MessagePackObject unpackedException )
			: base( RpcError.TimeoutError, unpackedException )
		{
			MessagePackObjectDictionary.TryGetTimeSpan( unpackedException, _clientTimeoutKeyUtf8, RpcException.CreateSerializationException, out this._clientTimeout );
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
			info.AddValue( _clientTimeoutKey, this._clientTimeout.Ticks );
		}
	}
}
