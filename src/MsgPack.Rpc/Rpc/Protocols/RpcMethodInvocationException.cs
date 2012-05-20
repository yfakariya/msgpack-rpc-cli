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
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Thrown if something wrong during remote method invocation.
	/// </summary>
#if !SILVERLIGHT
	[Serializable]
#endif
	[SuppressMessage( "Microsoft.Usage", "CA2240:ImplementISerializableCorrectly", Justification = "Using ISafeSerializationData." )]
	[SuppressMessage( "Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Justification = "Using ISafeSerializationData." )]
	public class RpcMethodInvocationException : RpcException
	{
		private const string _methodNameKey = "MethodName";
		internal static readonly MessagePackObject MethodNameKeyUtf8 = MessagePackConvert.EncodeString( _methodNameKey );

		// NOT readonly for safe deserialization
		private string _methodName;

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
		public RpcMethodInvocationException( RpcError rpcError, string methodName ) : this( rpcError, methodName, null, null, null ) { }

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
		public RpcMethodInvocationException( RpcError rpcError, string methodName, string message, string debugInformation )
			: this( rpcError, methodName, message, debugInformation, null ) { }

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
			: base( rpcError ?? RpcError.CallError, message, debugInformation, inner )
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
		///		Initialize new instance with unpacked data.
		/// </summary>
		/// <param name="rpcError">
		///		Metadata of error. If you specify null, <see cref="MsgPack.Rpc.RpcError.RemoteRuntimeError"/> is used.
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
			this._methodName = unpackedException.GetString( MethodNameKeyUtf8 );
			Contract.Assume( this._methodName != null, "Unpacked data does not have MethodName." );
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
		///		Or <see cref="MethodName"/> is <c>null</c> or blank.
		/// </exception>
		/// <permission cref="System.Security.Permissions.SecurityPermission"><c>LinkDemand</c>, <c>Flags=SerializationFormatter</c></permission>
		[SecurityPermission( SecurityAction.LinkDemand, SerializationFormatter = true )]
		protected RpcMethodInvocationException( SerializationInfo info, StreamingContext context )
			: base( info, context )
		{
			this._methodName = info.GetString( _methodNameKey );

			if ( String.IsNullOrWhiteSpace( this._methodName ) )
			{
				throw new SerializationException( "'MethodName' is required" );
			}
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

			info.AddValue( _methodNameKey, this._methodName );
		}
#endif

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
			store.Add( MethodNameKeyUtf8, MessagePackConvert.EncodeString( this._methodName ) );
		}

#if !SILVERLIGHT && !MONO
		/// <summary>
		///		When overridden on the derived class, handles <see cref="E:Exception.SerializeObjectState"/> event to add type-specified serialization state.
		/// </summary>
		/// <param name="sender">The <see cref="Exception"/> instance itself.</param>
		/// <param name="e">
		///		The <see cref="System.Runtime.Serialization.SafeSerializationEventArgs"/> instance containing the event data.
		///		The overriding method adds its internal state to this object via <see cref="M:SafeSerializationEventArgs.AddSerializedState"/>.
		///	</param>
		/// <seealso cref="ISafeSerializationData"/>
		protected override void OnSerializeObjectState( object sender, SafeSerializationEventArgs e )
		{
			base.OnSerializeObjectState( sender, e );
			e.AddSerializedState(
				new SerializedState()
				{
					MethodName = this._methodName
				}
			);
		}

		[Serializable]
		private sealed class SerializedState : ISafeSerializationData
		{
			public string MethodName;

			public void CompleteDeserialization( object deserialized )
			{
				var enclosing = deserialized as RpcMethodInvocationException;
				enclosing._methodName = this.MethodName;
			}
		}
#endif
	}
}
