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
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Reflection;
using MsgPack.Rpc.Protocols;

[module: SuppressMessage( "Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Scope = "member", Target = "MsgPack.Rpc.RpcError.#.cctor()", Justification = "Type initializer is required." )]

namespace MsgPack.Rpc
{
	/// <summary>
	///		Represents pre-defined MsgPack-RPC error metadata.
	/// </summary>
	/// <remarks>
	///		See https://gist.github.com/470667/d33136f74584381bdb58b6444abfcb4a8bbe8abc for details.
	/// </remarks>
	public sealed class RpcError
	{
		#region -- Built-in Errors --

		private static readonly RpcError _timeoutError =
			new RpcError(
				"RPCError.TimeoutError",
				-60,
				"Request has been timeout.",
				( error, data ) => new RpcTimeoutException( data )
			);

		/// <summary>
		///		Gets the <see cref="RpcError"/> for when the client cannot get response from server.
		///		Details are unknown at all, for instance, message might reach server.
		///		It might be success when the client retry.
		/// </summary>
		/// <value>
		///		The <see cref="RpcError"/> for timeout error.
		/// </value>
		public static RpcError TimeoutError
		{
			get
			{
				Contract.Ensures( Contract.Result<RpcError>() != null );
				return RpcError._timeoutError;
			}
		}

		private static readonly RpcError _transportError =
			new RpcError(
				"RPCError.ClientError.TransportError",
				-50,
				"Cannot initiate transferring message.",
				( error, data ) => new RpcTransportException( error, data )
			);

		/// <summary>
		///		Gets the <see cref="RpcError"/> for when the client cannot initiate transferring message.
		///		It might be network failure, be configuration issue, or handshake failure.
		/// </summary>
		/// <value>
		///		The <see cref="RpcError"/> for general tranport error.
		/// </value>
		public static RpcError TransportError
		{
			get
			{
				Contract.Ensures( Contract.Result<RpcError>() != null );
				return RpcError._transportError;
			}
		}


		private static readonly RpcError _networkUnreacheableError =
			new RpcError(
				"RPCError.ClientError.TranportError.NetworkUnreacheableError",
				-51,
				"Cannot reach specified remote end point.",
				( error, data ) => new RpcTransportException( error, data )
			);

		/// <summary>
		///		Gets the <see cref="RpcError"/> for when the client cannot reach specified remote end point.
		///		This error is transport protocol specific.
		/// </summary>
		/// <value>
		///		The <see cref="RpcError"/> for network unreacheable error.
		/// </value>
		public static RpcError NetworkUnreacheableError
		{
			get
			{
				Contract.Ensures( Contract.Result<RpcError>() != null );
				return RpcError._networkUnreacheableError;
			}
		}


		private static readonly RpcError _connectionRefusedError =
			new RpcError(
				"RPCError.ClientError.TranportError.ConnectionRefusedError",
				-52,
				"Connection was refused explicitly by remote end point.",
				( error, data ) => new RpcTransportException( error, data )
			);

		/// <summary>
		///		Gets the <see cref="RpcError"/> for when the client connection was explicitly refused by the remote end point.
		///		It should fail when you retry.
		///		This error is connection oriented transport protocol specific.
		/// </summary>
		/// <value>
		///		The <see cref="RpcError"/> for connection refused error.
		/// </value>
		public static RpcError ConnectionRefusedError
		{
			get
			{
				Contract.Ensures( Contract.Result<RpcError>() != null );
				return RpcError._connectionRefusedError;
			}
		}


		private static readonly RpcError _connectionTimeoutError =
			new RpcError(
				"RPCError.ClientError.TranportError.ConnectionTimeoutError",
				-53,
				"Connection timout was occurred.",
				( error, data ) => new RpcTransportException( error, data )
			);

		/// <summary>
		///		Gets the <see cref="RpcError"/> for when a connection timout was occurred.
		///		It might be success when you retry.
		///		This error is connection oriented transport protocol specific.
		/// </summary>
		/// <value>
		///		The <see cref="RpcError"/> for connection timeout error.
		/// </value>
		public static RpcError ConnectionTimeoutError
		{
			get
			{
				Contract.Ensures( Contract.Result<RpcError>() != null );
				return RpcError._connectionTimeoutError;
			}
		}


		private static readonly RpcError _messageRefusedError =
			new RpcError(
				"RPCError.ClientError.MessageRefusedError",
				-40,
				"Message was refused explicitly by remote end point.",
				( error, data ) => new RpcProtocolException( error, data )
			);

		/// <summary>
		///		Gets the <see cref="RpcError"/> for when the message was explicitly refused by remote end point.
		/// </summary>
		/// <value>
		///		The <see cref="RpcError"/> for message refused error.
		/// </value>
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
		public static RpcError MessageRefusedError
		{
			get
			{
				Contract.Ensures( Contract.Result<RpcError>() != null );
				return RpcError._messageRefusedError;
			}
		}


		private static readonly RpcError _messageTooLargeError =
			new RpcError(
				"RPCError.ClientError.MessageRefusedError.MessageTooLargeError",
				-41, "Message is too large.",
				( error, data ) => new RpcMessageTooLongException( data )
			);

		/// <summary>
		///		Gets the <see cref="RpcError"/> for when the message was refused explicitly by remote end point due to it was too large.
		///		The structure may be right, but message was simply too large or some portions might be corruptted.
		/// </summary>
		/// <value>
		///		The <see cref="RpcError"/> for message too large error.
		/// </value>
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
		public static RpcError MessageTooLargeError
		{
			get
			{
				Contract.Ensures( Contract.Result<RpcError>() != null );
				return RpcError._messageTooLargeError;
			}
		}


		private static readonly RpcError _callError =
			new RpcError(
				"RPCError.ClientError.CallError",
				-20,
				"Failed to call specified method.",
				( error, data ) => new RpcMethodInvocationException( error, data )
			);

		/// <summary>
		///		Gets the <see cref="RpcError"/> for when the RPC runtime failed to call specified method.
		///		The message was certainly reached and the structure was right, but failed to call method.
		/// </summary>
		/// <value>
		///		The <see cref="RpcError"/> for call error.
		/// </value>
		public static RpcError CallError
		{
			get
			{
				Contract.Ensures( Contract.Result<RpcError>() != null );
				return RpcError._callError;
			}
		}


		private static readonly RpcError _noMethodError =
			new RpcError(
				"RPCError.ClientError.CallError.NoMethodError",
				-21,
				"Specified method was not found.",
				( error, data ) => new RpcMissingMethodException( data )
			);

		/// <summary>
		///		Gets the <see cref="RpcError"/> for when the specified method was not found.
		///		The message was certainly reached and the structure was right, but failed to call method.
		/// </summary>
		/// <value>
		///		The <see cref="RpcError"/> for no method error.
		/// </value>
		public static RpcError NoMethodError
		{
			get
			{
				Contract.Ensures( Contract.Result<RpcError>() != null );
				return RpcError._noMethodError;
			}
		}


		private static readonly RpcError _argumentError =
			new RpcError(
				"RPCError.ClientError.CallError.ArgumentError",
				-22,
				"Some argument(s) were wrong.",
				( error, data ) => new RpcArgumentException( data )
			);

		/// <summary>
		///		Gets the <see cref="RpcError"/> for when the some argument(s) are wrong.
		///		The serialized value might be ill formed or the value is not valid semantically.
		/// </summary>
		/// <value>
		///		The <see cref="RpcError"/> for argument error.
		/// </value>
		public static RpcError ArgumentError
		{
			get
			{
				Contract.Ensures( Contract.Result<RpcError>() != null );
				return RpcError._argumentError;
			}
		}


		private static readonly RpcError _serverError =
			new RpcError(
				"RPCError.ServerError",
				-30,
				"Server cannot process received message.",
				( error, data ) => new RpcServerUnavailableException( error, data )
			);

		/// <summary>
		///		Gets the <see cref="RpcError"/> for when the server cannot process received message.
		///		Other server might process your request.
		/// </summary>
		/// <value>
		///		The <see cref="RpcError"/> for server error.
		/// </value>
		public static RpcError ServerError
		{
			get
			{
				Contract.Ensures( Contract.Result<RpcError>() != null );
				return RpcError._serverError;
			}
		}


		private static readonly RpcError _serverBusyError =
			new RpcError(
				"RPCError.ServerError.ServerBusyError",
				-31,
				"Server is busy.",
				( error, data ) => new RpcServerUnavailableException( error, data )
			);

		/// <summary>
		///		Gets the <see cref="RpcError"/> for when the server is busy.
		///		Other server may process your request.
		/// </summary>
		/// <value>
		///		The <see cref="RpcError"/> for server busy error.
		/// </value>
		public static RpcError ServerBusyError
		{
			get
			{
				Contract.Ensures( Contract.Result<RpcError>() != null );
				return RpcError._serverBusyError;
			}
		}


		private static readonly RpcError _remoteRuntimeError =
			new RpcError(
				"RPCError.RemoteRuntimeError",
				-10,
				"Remote end point failed to process request.",
				( error, data ) => new RpcException( error, data )
			);

		/// <summary>
		///		Gets the <see cref="RpcError"/> for when an internal runtime error is occurred in the remote end point.
		/// </summary>
		/// <value>
		///		The <see cref="RpcError"/> for remote runtime error.
		/// </value>
		public static RpcError RemoteRuntimeError
		{
			get
			{
				Contract.Ensures( Contract.Result<RpcError>() != null );
				return RpcError._remoteRuntimeError;
			}
		}


		#endregion -- Built-in Errors --

		private const string _unexpectedErrorIdentifier = "RPCError.RemoteError.UnexpectedError";
		private const int _unexpectedErrorCode = Int32.MaxValue;

		private static readonly RpcError _unexpected =
			new RpcError(
				_unexpectedErrorIdentifier,
				_unexpectedErrorCode,
				"Unexpected RPC error is occurred.",
				( error, data ) => new RpcException( error, data )
			);

		/// <summary>
		///		Gets the <see cref="RpcError"/> for unexpected error.
		/// </summary>
		///	<value>
		///		The <see cref="RpcError"/> for unexpected error.
		///	</value>
		/// <remarks>
		///		The <see cref="RemoteRuntimeError"/> should be used for caught 'unexpected' exception.
		///		This value is for unexpected situation on exception marshaling.
		/// </remarks>
		internal static RpcError Unexpected
		{
			get
			{
				Contract.Ensures( Contract.Result<RpcError>() != null );
				return RpcError._unexpected;
			}
		}


		private static readonly Dictionary<string, RpcError> _identifierDictionary = new Dictionary<string, RpcError>();
		private static readonly Dictionary<int, RpcError> _errorCodeDictionary = new Dictionary<int, RpcError>();

		static RpcError()
		{
			foreach ( FieldInfo field in
				typeof( RpcError ).FindMembers(
					MemberTypes.Field,
					BindingFlags.Static | BindingFlags.NonPublic,
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
				// TODO: localization key: Idnentifier ".DefaultMessage"
				return this.DefaultMessageInvariant;
			}
		}

		private readonly Func<RpcError, MessagePackObject, RpcException> _exceptionUnmarshaler;

		private RpcError( string identifier, int errorCode, string defaultMessageInvariant, Func<RpcError, MessagePackObject, RpcException> exceptionUnmarshaler )
		{
			this._identifier = identifier;
			this._errorCode = errorCode;
			this._defaultMessageInvariant = defaultMessageInvariant;
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
		///		Determines whether the specified <see cref="System.Object"/> is equal to this instance.
		/// </summary>
		/// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
		/// <returns>
		///		<c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
		/// </returns>
		public sealed override bool Equals( object obj )
		{
			if ( Object.ReferenceEquals( this, obj ) )
			{
				return true;
			}

			var other = obj as RpcError;
			if ( Object.ReferenceEquals( other, null ) )
			{
				return false;
			}

			return this._errorCode == other._errorCode && this._identifier == other._identifier;
		}

		/// <summary>
		///		Returns a hash code for this instance.
		/// </summary>
		/// <returns>
		///		A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
		/// </returns>
		public sealed override int GetHashCode()
		{
			return this._errorCode.GetHashCode();
		}

		/// <summary>
		///		Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		///		A <see cref="System.String"/> that represents this instance.
		/// </returns>
		public sealed override string ToString()
		{
			return String.Format( CultureInfo.CurrentCulture, "{0}({1}): {2}", this._identifier, this._errorCode, this.DefaultMessage );
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
#if !SILVERLIGHT
				throw new ArgumentOutOfRangeException( "errorCode", errorCode, "Application error code must be grator than or equal to 0." );
#else
				throw new ArgumentOutOfRangeException( "errorCode", "Application error code must be grator than or equal to 0." );
#endif
			}

			Contract.EndContractBlock();

			return
				new RpcError(
					identifier,
					errorCode,
					"Application throws exception.",
					( error, data ) => new RpcFaultException( error, data )
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

		/// <summary>
		///		Determines whether two <see cref="RpcError"/> instances have the same value. 
		/// </summary>
		/// <param name="left">A <see cref="RpcError"/> instance to compare with <paramref name="right"/>.</param>
		/// <param name="right">A <see cref="RpcError"/> instance to compare with <paramref name="left"/>.</param>
		/// <returns>
		///		<c>true</c> if the <see cref="RpcError"/> instances are equivalent; otherwise, <c>false</c>.
		/// </returns>
		public static bool operator ==( RpcError left, RpcError right )
		{
			if ( Object.ReferenceEquals( left, null ) )
			{
				return Object.ReferenceEquals( right, null );
			}
			else
			{
				return left.Equals( right );
			}
		}

		/// <summary>
		///		Determines whether two <see cref="RpcError"/> instances do not have the same value. 
		/// </summary>
		/// <param name="left">A <see cref="RpcError"/> instance to compare with <paramref name="right"/>.</param>
		/// <param name="right">A <see cref="RpcError"/> instance to compare with <paramref name="left"/>.</param>
		/// <returns>
		///		<c>true</c> if the <see cref="RpcError"/> instances are not equivalent; otherwise, <c>false</c>.
		/// </returns>
		public static bool operator !=( RpcError left, RpcError right )
		{
			return !( left == right );
		}
	}
}
