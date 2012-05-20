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
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Represents MessagePack-RPC related exception.
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
#if !SILVERLIGHT
	[Serializable]
#endif
	[SuppressMessage( "Microsoft.Usage", "CA2240:ImplementISerializableCorrectly", Justification = "Using ISafeSerializationData." )]
	[SuppressMessage( "Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Justification = "Using ISafeSerializationData." )]
	public partial class RpcException : Exception
	{
		private const string _debugInformationKey = "DebugInformation";
		private const string _remoteExceptionsKey = "RemoteExceptions";
		private const string _rpcErrorIdentifierKey = "RpcError";
		private const string _rpcErrorCodeKey = "RpcErrorCode";
		private const string _preservedStackTraceKey = "PreservedStackTrace";

		// NOT readonly for safe-deserialization
		private RpcError _rpcError;

		/// <summary>
		///		Gets the metadata of the error.
		/// </summary>
		/// <value>
		///		The metadata of the error. This value will not be <c>null</c>.
		/// </value>
		public RpcError RpcError
		{
			get
			{
				Contract.Ensures( Contract.Result<RpcError>() != null );
				Contract.Assert( this._rpcError != null );
				return this._rpcError;
			}
		}

		// NOT readonly for safe-deserialization
		private string _debugInformation;

		/// <summary>
		///		Gets the debug information of the error.
		/// </summary>
		/// <value>
		///		The debug information of the error.
		///		This value will not be <c>null</c> but may be empty for security reason, and its contents are for developers, not end users.
		/// </value>
		public string DebugInformation
		{
			get { return this._debugInformation ?? String.Empty; }
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="RpcException"/> class with a specified error message.
		/// </summary>
		/// <param name="rpcError">
		///		The metadata of the error. If <c>null</c> is specified, the <see cref="MsgPack.Rpc.RpcError.RemoteRuntimeError"/> is used.
		///	</param>
		/// <param name="message">
		///		The error message to desribe condition. Note that this message should not include security related information.
		///	</param>
		/// <param name="debugInformation">
		///		The debug information of the error.
		///		This value can be <c>null</c> for security reason, and its contents are for developers, not end users.
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
		///		Initializes a new instance of the <see cref="RpcException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception. 
		/// </summary>
		/// <param name="rpcError">
		///		The metadata of the error. If <c>null</c> is specified, the <see cref="MsgPack.Rpc.RpcError.RemoteRuntimeError"/> is used.
		///	</param>
		/// <param name="message">
		///		The error message to desribe condition. Note that this message should not include security related information.
		///	</param>
		/// <param name="debugInformation">
		///		The debug information of the error.
		///		This value can be <c>null</c> for security reason, and its contents are for developers, not end users.
		/// </param>
		/// <param name="inner">
		///		The exception which caused this error.
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
			: base( message ?? ( rpcError ?? RpcError.RemoteRuntimeError ).DefaultMessage, inner )
		{
			this._rpcError = rpcError ?? RpcError.RemoteRuntimeError;
			this._debugInformation = debugInformation;
#if !SILVERLIGHT && !MONO
			this.RegisterSerializeObjectStateEventHandler();
#endif
		}

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
		protected RpcException( SerializationInfo info, StreamingContext context )
			: base( info, context )
		{
			this._debugInformation = info.GetString( _debugInformationKey );
			this._remoteExceptions = info.GetValue( _remoteExceptionsKey, typeof( RemoteExceptionInformation[] ) ) as RemoteExceptionInformation[];
			this._rpcError = Rpc.RpcError.FromIdentifier( info.GetString( _rpcErrorIdentifierKey ), info.GetInt32( _rpcErrorCodeKey ) );
			this._preservedStackTrace = info.GetValue( _preservedStackTraceKey, typeof( List<string> ) ) as List<string>;
		}

		/// <summary>
		///		When overridden in a derived class, sets the <see cref="SerializationInfo"/> with information about the exception.
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
		/// <permission cref="System.Security.Permissions.SecurityPermission"><c>LinkDemand</c>, <c>Flags=SerializationFormatter</c></permission>
		[SecurityPermission( SecurityAction.LinkDemand, SerializationFormatter = true )]
		public override void GetObjectData( SerializationInfo info, StreamingContext context )
		{
			base.GetObjectData( info, context );

			info.AddValue( _debugInformationKey, this._debugInformation );
			info.AddValue( _remoteExceptionsKey, this._remoteExceptions );
			info.AddValue( _rpcErrorIdentifierKey, this._rpcError.Identifier );
			info.AddValue( _rpcErrorCodeKey, this._rpcError.ErrorCode );
			info.AddValue( _preservedStackTraceKey, this._preservedStackTrace );
		}
#endif

#if !SILVERLIGHT && !MONO
		internal static T Get<T>( SerializationEntry entry, string name, Func<SerializationEntry, T> getter )
		{
			Contract.Assert( name != null );
			Contract.Assert( getter != null );

			try
			{
				return getter( entry );
			}
			catch ( InvalidCastException ex )
			{
				throw new SerializationException( String.Format( CultureInfo.CurrentCulture, "Invalid '{0}' value.", name ), ex );
			}
		}

		/// <summary>
		///		When overridden on the derived class, handles <see cref="E:Exception.SerializeObjectState"/> event to add type-specified serialization state.
		/// </summary>
		/// <param name="sender">The <see cref="Exception"/> instance itself.</param>
		/// <param name="e">
		///		The <see cref="System.Runtime.Serialization.SafeSerializationEventArgs"/> instance containing the event data.
		///		The overriding method adds its internal state to this object via <see cref="M:SafeSerializationEventArgs.AddSerializedState"/>.
		///	</param>
		///	<remarks>
		///		The overriding method MUST invoke base implementation, or the serialization should fail.
		///	</remarks>
		/// <seealso cref="ISafeSerializationData"/>
		protected virtual void OnSerializeObjectState( object sender, SafeSerializationEventArgs e )
		{
			e.AddSerializedState(
				new SerializedState()
				{
					DebugInformation = this._debugInformation,
					RemoteExceptions = this._remoteExceptions,
					RpcErrorIdentifier = this._rpcError.Identifier,
					RpcErrorCode = this._rpcError.ErrorCode,
					PreservedStackTrace = this._preservedStackTrace
				}
			);
		}

		private void RegisterSerializeObjectStateEventHandler()
		{
			this.SerializeObjectState += this.OnSerializeObjectState;
		}

		[Serializable]
		private sealed class SerializedState : ISafeSerializationData
		{
			public string DebugInformation;
			public RemoteExceptionInformation[] RemoteExceptions;
			public string RpcErrorIdentifier;
			public int? RpcErrorCode;
			public List<string> PreservedStackTrace;

			public void CompleteDeserialization( object deserialized )
			{
				var enclosing = deserialized as RpcException;
				enclosing._debugInformation = this.DebugInformation;
				enclosing._remoteExceptions = this.RemoteExceptions;
				enclosing._rpcError = MsgPack.Rpc.RpcError.FromIdentifier( this.RpcErrorIdentifier, this.RpcErrorCode );
				enclosing._preservedStackTrace = this.PreservedStackTrace;
				enclosing.RegisterSerializeObjectStateEventHandler();
			}
		}
#endif
	}
}
