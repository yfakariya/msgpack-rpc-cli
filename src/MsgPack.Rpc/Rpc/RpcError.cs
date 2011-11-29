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
using System.Reflection;
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc
{
	// FIXME: Refactor when server finished.
	/// <summary>
	///		Represents pre-defined MsgPack-RPC error metadata.
	/// </summary>
	/// <remarks>
	///		See https://gist.github.com/470667/d33136f74584381bdb58b6444abfcb4a8bbe8abc for details.
	/// </remarks>
	public sealed class RpcError
	{
		#region -- Built-in Errors --

		/// <summary>
		///		Cannot get response from server.
		///		Details are unknown at all, for instance, message might reach server.
		///		It might be success when you retry.
		/// </summary>
		public static readonly RpcError TimeoutError =
			new RpcError(
				"RPCError.TimeoutError",
				-60,
				"Request has been timeout.",
				typeof( RpcTimeoutException ),
				( error, data ) => new RpcTimeoutException( data )
			);

		/// <summary>
		///		Cannot initiate transferring message.
		///		It may was network failure, was configuration issue, or failed to handshake.
		/// </summary>
		public static readonly RpcError TransportError =
			new RpcError(
				"RPCError.ClientError.TransportError",
				-50,
				"Cannot initiate transferring message.",
				typeof( RpcTransportException ),
				( error, data ) => new RpcTransportException( error, data )
			);

		/// <summary>
		///		Cannot reach specified remote end point.
		///		This error is transport protocol specific.
		/// </summary>
		public static readonly RpcError NetworkUnreacheableError =
			new RpcError(
				"RPCError.ClientError.TranportError.NetworkUnreacheableError",
				-51,
				"Cannot reach specified remote end point.",
				typeof( RpcTransportException ),
				( error, data ) => new RpcTransportException( error, data )
			);

		/// <summary>
		///		Connection was refused explicitly by remote end point.
		///		It should fail when you retry.
		///		This error is connection oriented transport protocol specific.
		/// </summary>
		public static readonly RpcError ConnectionRefusedError =
			new RpcError(
				"RPCError.ClientError.TranportError.ConnectionRefusedError",
				-52,
				"Connection was refused explicitly by remote end point.",
				typeof( RpcTransportException ),
				( error, data ) => new RpcTransportException( error, data )
			);

		/// <summary>
		///		Connection timout was occurred.
		///		It might be success when you retry.
		///		This error is connection oriented transport protocol specific.
		/// </summary>
		public static readonly RpcError ConnectionTimeoutError =
			new RpcError(
				"RPCError.ClientError.TranportError.ConnectionTimeoutError",
				-53,
				"Connection timout was occurred.",
				typeof( RpcTransportException ),
				( error, data ) => new RpcTransportException( error, data )
			);

		/// <summary>
		///		Message was refused explicitly by remote end point.
		/// </summary>
		/// <remarks>
		///		<para>
		///			End point issues this error when:
		///			<list type="bullet">
		///				<item>Couild not deserialize the message.</item>
		///				<item>Message structure of deserialized message was wrong as MessagePack-RPC protocol.</item>
		///				<item>Any value of message was wrong as the protocol.</item>
		///			</list>
		///		</para>
		///		<para>
		///			It may be caused when:
		///			<list type="bullet">
		///				<item>Some deserializing issues were occurred.</item>
		///				<item>Unexpected item type was found as the protocol (e.g. arguments field was not array).</item>
		///				<item>Unexpected item value was found as the protocol (e.g. undefined message type field).</item>
		///			</list>
		///		</para>
		///		<para>
		///			The root cause of this issue might be:
		///			<list type="bullet">
		///				<item>There are some bugs on used library in client or server.</item>
		///				<item>Versions of MessagePack library in client and server were not compatible.</item>
		///				<item>Versions of MessagePack-RPC library in client and server were not compatible.</item>
		///				<item>Packet was changed unexpectedly.</item>
		///			</list>
		///		</para>
		/// </remarks>
		public static readonly RpcError MessageRefusedError =
			new RpcError(
				"RPCError.ClientError.MessageRefusedError",
				-40,
				"Message was refused explicitly by remote end point.",
				typeof( RpcProtocolException ),
				( error, data ) => new RpcProtocolException( error, data )
			);

		/// <summary>
		///		Message was refused explicitly by remote end point due to it was too large.
		///		Structure may be right, but message was simply too large or some portions might be corruptted.
		/// </summary>
		/// <remarks>
		///		<para>
		///			It may be caused when:
		///			<list type="bullet">
		///				<item>Message is too large to be expected by remote end point.</item>
		///			</list>
		///		</para>
		///		<para>
		///			The root cause of this issue might be:
		///			<list type="bullet">
		///				<item>Versions of MessagePack library in client and server were not compatible.</item>
		///				<item>Versions of MessagePack-RPC library in client and server were not compatible.</item>
		///				<item>Packet was changed unexpectedly.</item>
		///				<item>Malicious issuer tried to send invalid message.</item>
		///				<item>Expected value by remote end point was simply too small.</item>
		///			</list>
		///		</para>
		/// </remarks>
		public static readonly RpcError MessageTooLargeError =
			new RpcError(
				"RPCError.ClientError.MessageRefusedError.MessageTooLargeError",
				-41, "Message is too large.",
				typeof( RpcMessageTooLongException ),
				( error, data ) => new RpcMessageTooLongException( data )
			);

		/// <summary>
		///		Failed to call specified method.
		///		Message was certainly reached and the structure was right, but failed to call method.
		/// </summary>
		public static readonly RpcError CallError =
			new RpcError(
				"RPCError.ClientError.CallError",
				-20,
				"Failed to call specified method.",
				typeof( RpcMethodInvocationException ),
				( error, data ) => new RpcMethodInvocationException( error, data )
			);

		/// <summary>
		///		Specified method was not found.
		/// </summary>
		public static readonly RpcError NoMethodError =
			new RpcError(
				"RPCError.ClientError.CallError.NoMethodError",
				-21,
				"Specified method was not found.",
				typeof( RpcMissingMethodException ),
				( error, data ) => new RpcMissingMethodException( data )
			);

		/// <summary>
		///		Some argument(s) were wrong.
		/// </summary>
		public static readonly RpcError ArgumentError =
			new RpcError(
				"RPCError.ClientError.CallError.ArgumentError",
				-22,
				"Some argument(s) were wrong.",
				typeof( RpcArgumentException ),
				( error, data ) => new RpcArgumentException( data )
			);

		/// <summary>
		///		Server cannot process received message.
		///		Other server might process your request.
		/// </summary>
		public static readonly RpcError ServerError =
			new RpcError(
				"RPCError.ServerError",
				-30,
				"Server cannot process received message.",
				typeof( RpcServerUnavailableException ),
				( error, data ) => new RpcServerUnavailableException( error, data )
			);

		/// <summary>
		///		Server is busy.
		///		Other server may process your request.
		/// </summary>
		public static readonly RpcError ServerBusyError =
			new RpcError(
				"RPCError.ServerError.ServerBusyError",
				-31,
				"Server is busy.",
				typeof( RpcServerUnavailableException ),
				( error, data ) => new RpcServerUnavailableException( error, data )
			);

		/// <summary>
		///		Internal runtime error in remote end point.
		/// </summary>
		public static readonly RpcError RemoteRuntimeError =
			new RpcError(
				"RPCError.RemoteRuntimeError",
				-10,
				"Remote end point failed to process request.",
				typeof( RpcException ),
				( error, data ) => new RpcException( error, data )
			);

		#endregion -- Built-in Errors --

		private const string _unexpectedErrorIdentifier = "RPCError.RemoteError.UnexpectedError";
		private const int _unexpectedErrorCode = Int32.MaxValue;

		internal static readonly RpcError Unexpected =
			new RpcError(
				_unexpectedErrorIdentifier,
				_unexpectedErrorCode,
				"Unexpected RPC error is occurred.",
				typeof( UnexpcetedRpcException ),
				null
			);

		private static readonly Dictionary<string, RpcError> _identifierDictionary = new Dictionary<string, RpcError>();
		private static readonly Dictionary<int, RpcError> _errorCodeDictionary = new Dictionary<int, RpcError>();

		static RpcError()
		{
			foreach ( FieldInfo field in
				typeof( RpcError ).FindMembers(
					MemberTypes.Field,
					BindingFlags.Static | BindingFlags.Public,
					( member, criteria ) => ( member as FieldInfo ).FieldType.Equals( criteria ),
					typeof( RpcError )
				)
			)
			{
				var builtInError = field.GetValue( null ) as RpcError;
				_identifierDictionary.Add( builtInError.Identifier, builtInError );
				_errorCodeDictionary.Add( builtInError.ErrorCode, builtInError );
			}
		}

		private readonly string _identifier;

		/// <summary>
		///		Get iedntifier of this error.
		/// </summary>
		/// <value>
		///		Iedntifier of this error.
		/// </value>
		public string Identifier
		{
			get { return this._identifier; }
		}

		private readonly int _errorCode;

		/// <summary>
		///		Get error code of this error.
		/// </summary>
		/// <value>
		///		Error code of this error.
		/// </value>
		public int ErrorCode
		{
			get { return this._errorCode; }
		}

		private readonly string _defaultMessageInvariant;

		/// <summary>
		///		Get default message in invariant culture.
		/// </summary>
		/// <value>
		///		Default message in invariant culture.
		/// </value>
		/// <remarks>
		///		You can use this property to build custom exception.
		/// </remarks>
		public string DefaultMessageInvariant
		{
			get { return _defaultMessageInvariant; }
		}

		/// <summary>
		///		Get default message in current UI culture.
		/// </summary>
		/// <value>
		///		Default message in current UI culture.
		/// </value>
		/// <remarks>
		///		You can use this property to build custom exception.
		/// </remarks>
		public string DefaultMessage
		{
			get
			{
				// TODO: localiation key: Idnentifier ".DefaultMessage"
				return this.DefaultMessageInvariant;
			}
		}

		private readonly Type _exceptionType;
		private readonly Func<RpcError, MessagePackObject, RpcException> _exceptionUnmarshaler;

		private RpcError( string identifier, int errorCode, string defaultMessageInvariant, Type exceptionType, Func<RpcError, MessagePackObject, RpcException> exceptionUnmarshaler )
		{
			this._identifier = identifier;
			this._errorCode = errorCode;
			this._defaultMessageInvariant = defaultMessageInvariant;
			this._exceptionType = exceptionType;
			this._exceptionUnmarshaler = exceptionUnmarshaler;
		}

		/// <summary>
		///		Create <see cref="RpcException"/> which corresponds to this error with specified detailed information.
		/// </summary>
		/// <param name="detail">
		///		Detailed error information.
		/// </param>
		/// <returns>
		///		<see cref="RpcException"/> which corresponds to this error with specified detailed information.
		/// </returns>
		internal RpcException ToException( MessagePackObject detail )
		{
			Contract.Assume( this._exceptionUnmarshaler != null );

			return this._exceptionUnmarshaler( this, detail );
		}

		/// <summary>
		///		Create custom error with specified identifier and error code.
		/// </summary>
		/// <param name="identifier">
		///		Identifier of custom error. This should be "RPCError.&lt;ApplicationName&gt;.&lt;ErrorType&gt;[.&lt;ErrorSubType&gt;]."
		/// </param>
		/// <param name="errorCode">
		///		Error code of custom error. This must be positive or zero.
		/// </param>
		/// <returns>
		///		Custom <see cref="RpcError"/> with specified <paramref name="identifier"/> and <paramref name="errorCode"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="identifier"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="identifier"/> is empty or blank.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///		<paramref name="errorCode"/> is negative.
		/// </exception>
		public static RpcError CustomError( string identifier, int errorCode )
		{
			if ( identifier == null )
			{
				throw new ArgumentNullException( "identifier" );
			}

			if ( String.IsNullOrWhiteSpace( identifier ) )
			{
				throw new ArgumentException( "'identifier' cannot be empty.", "identifier" );
			}

			if ( errorCode < 0 )
			{
				throw new ArgumentOutOfRangeException( "errorCode", errorCode, "Application error code must be grator than or equal to 0." );
			}

			Contract.EndContractBlock();

			return
				new RpcError(
					identifier.StartsWith( "RPCError.", StringComparison.Ordinal ) ? identifier : "RPCError." + identifier,
					errorCode,
					"Application throw exception.",
					typeof( RpcFaultException ),
					( error, data  ) => new RpcFaultException( error, data )
				);
		}

		/// <summary>
		///		Get built-in error with specified identifier and error code, or create custom error when specified identifier and error code is not built-in error.
		/// </summary>
		/// <param name="identifier">
		///		Identifier of error.
		/// </param>
		/// <param name="errorCode">
		///		Error code of error.
		/// </param>
		/// <returns>
		///		Built-in or custom <see cref="RpcError"/> corresponds to <paramref name="identifier"/> or <paramref name="errorCode"/>.
		/// </returns>
		public static RpcError FromIdentifier( string identifier, int? errorCode )
		{
			RpcError result;
			if ( errorCode != null && _errorCodeDictionary.TryGetValue( errorCode.Value, out result ) )
			{
				return result;
			}

			if ( identifier != null && _identifierDictionary.TryGetValue( identifier, out result ) )
			{
				return result;
			}

			return CustomError( String.IsNullOrWhiteSpace( identifier ) ? _unexpectedErrorIdentifier : identifier, errorCode ?? _unexpectedErrorCode );
		}
	}
}
