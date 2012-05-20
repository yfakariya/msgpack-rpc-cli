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
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace MsgPack.Rpc
{
	partial class RpcException
	{
#if !SILVERLIGHT && !MONO
		private static readonly MethodInfo _safeGetHRFromExceptionMethod = typeof( RpcException ).GetMethod( "SafeGetHRFromException", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static );
#endif

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
		protected internal RpcException( RpcError rpcError, MessagePackObject unpackedException )
			: this( rpcError, unpackedException.GetString( MessageKeyUtf8 ), unpackedException.GetString( DebugInformationKeyUtf8 ) )
		{
			if ( unpackedException.IsDictionary )
			{
				MessagePackObject mayBeArray;
				if ( unpackedException.AsDictionary().TryGetValue( _remoteExceptionsUtf8, out mayBeArray ) && mayBeArray.IsArray )
				{
					var array = mayBeArray.AsList();
					this._remoteExceptions = new RemoteExceptionInformation[ array.Count ];
					for ( int i = 0; i < this._remoteExceptions.Length; i++ )
					{
						if ( array[ i ].IsList )
						{
							this._remoteExceptions[ i ] = new RemoteExceptionInformation( array[ i ].AsList() );
						}
						else
						{
							// Unexpected type.
							Debug.WriteLine( "Unexepcted ExceptionInformation at {0}, type: {1}, value: \"{2}\".", i, array[ i ].UnderlyingType, array[ i ] );
							this._remoteExceptions[ i ] = new RemoteExceptionInformation( new MessagePackObject[] { array[ i ] } );
						}
					}
				}
			}

#if !SILVERLIGHT && !MONO
			this.RegisterSerializeObjectStateEventHandler();
#endif
		}

		// NOT readonly for safe-deserialization
		private RemoteExceptionInformation[] _remoteExceptions;

#if !SILVERLIGHT
		[Serializable]
#endif
		private sealed class RemoteExceptionInformation
		{
			public readonly int Hop;
			public readonly string TypeName;
			public readonly int HResult;
			public readonly string Message;
			public readonly RemoteStackFrame[] StackTrace;
			public readonly MessagePackObjectDictionary Data;

			public RemoteExceptionInformation( IList<MessagePackObject> unpacked )
			{
				if ( unpacked.Count != 6 )
				{
					throw new SerializationException( "Count of remote exception information must be 6." );
				}

				this.Hop = unpacked[ 0 ].AsInt32();
				this.TypeName = unpacked[ 1 ].AsString();
				this.HResult = unpacked[ 2 ].AsInt32();
				this.Message = unpacked[ 3 ].AsString();
				this.StackTrace = unpacked[ 4 ].AsList().Select( item => new RemoteStackFrame( item.AsList() ) ).ToArray();
				this.Data = unpacked[ 5 ].AsDictionary();
			}
		}

#if !SILVERLIGHT
		[Serializable]
#endif
		private sealed class RemoteStackFrame
		{
			public readonly string MethodSignature;
			public readonly int ILOffset;
			public readonly int NativeOffset;
			public readonly string FileName;
			public readonly int FileLineNumber;
			public readonly int FileColumnNumber;

			public RemoteStackFrame( IList<MessagePackObject> unpacked )
			{
				switch ( unpacked.Count )
				{
					case 3:
					{
						this.MethodSignature = unpacked[ 0 ].AsString();
						this.ILOffset = unpacked[ 1 ].AsInt32();
						this.NativeOffset = unpacked[ 2 ].AsInt32();
						this.FileName = null;
						this.FileLineNumber = 0;
						this.FileColumnNumber = 0;
						break;
					}
					case 6:
					{
						this.MethodSignature = unpacked[ 0 ].AsString();
						this.ILOffset = unpacked[ 1 ].AsInt32();
						this.NativeOffset = unpacked[ 2 ].AsInt32();
						this.FileName = unpacked[ 3 ].AsString();
						this.FileLineNumber = unpacked[ 4 ].AsInt32();
						this.FileColumnNumber = unpacked[ 5 ].AsInt32();
						break;
					}
					default:
					{
						throw new SerializationException( "Count of remote stack frames must be 3 or 6." );
					}
				}
			}
		}

		internal static readonly MessagePackObject MessageKeyUtf8 = MessagePackConvert.EncodeString( "Message" );
		internal static readonly MessagePackObject DebugInformationKeyUtf8 = MessagePackConvert.EncodeString( "DebugInformation" );
		private static readonly MessagePackObject _errorCodeUtf8 = MessagePackConvert.EncodeString( "ErrorCode" );
		private static readonly MessagePackObject _remoteExceptionsUtf8 = MessagePackConvert.EncodeString( "RemoteExceptions" );

		/// <summary>
		///		Get <see cref="MessagePackObject"/> which contains data about this instance.
		/// </summary>
		/// <param name="isDebugMode">
		///		If this method should include debug information then true.
		/// </param>
		/// <returns>
		///		<see cref="MessagePackObject"/> which contains data about this instance.
		/// </returns>
		public MessagePackObject GetExceptionMessage( bool isDebugMode )
		{
			var store = new MessagePackObjectDictionary( 2 );
			store.Add( _errorCodeUtf8, this.RpcError.ErrorCode );
			store.Add( MessageKeyUtf8, isDebugMode ? this.Message : this.RpcError.DefaultMessageInvariant );
			this.GetExceptionMessage( store, isDebugMode );

			return new MessagePackObject( store );
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
		protected virtual void GetExceptionMessage( IDictionary<MessagePackObject, MessagePackObject> store, bool includesDebugInformation )
		{
			Contract.Requires( store != null );

			if ( !includesDebugInformation )
			{
				return;
			}

			if ( this.InnerException != null || this._remoteExceptions != null )
			{
				var innerList = new List<MessagePackObject>();
				if ( this._remoteExceptions != null )
				{
					foreach ( var remoteException in this._remoteExceptions )
					{
						MessagePackObject[] properties = new MessagePackObject[ 6 ];
						properties[ 0 ] = remoteException.Hop + 1;
						properties[ 1 ] = MessagePackConvert.EncodeString( remoteException.TypeName );
						// HResult is significant for some exception (e.g. IOException).
						properties[ 2 ] = remoteException.HResult;
						properties[ 3 ] = MessagePackConvert.EncodeString( remoteException.Message );
						properties[ 4 ] =
#if !SILVERLIGHT
							Array.ConvertAll(
#else
							ArrayExtensions.ConvertAll(
#endif
								remoteException.StackTrace,
								frame =>
									frame.FileName == null
									? new MessagePackObject( new MessagePackObject[] { frame.MethodSignature, frame.ILOffset, frame.NativeOffset } )
									: new MessagePackObject( new MessagePackObject[] { frame.MethodSignature, frame.ILOffset, frame.NativeOffset, frame.FileName, frame.FileLineNumber, frame.FileColumnNumber } )
							);
						properties[ 5 ] = new MessagePackObject( remoteException.Data );
						innerList.Add( properties );
					}
				}

				for ( var inner = this.InnerException; inner != null; inner = inner.InnerException )
				{
					MessagePackObject[] properties = new MessagePackObject[ 6 ];
					properties[ 0 ] = 0;
					properties[ 1 ] = MessagePackConvert.EncodeString( inner.GetType().FullName );
					// HResult is significant for some exception (e.g. IOException).
					properties[ 2 ] = SafeGetHRFromException( inner );
					properties[ 3 ] = MessagePackConvert.EncodeString( inner.Message );

					// stack trace
					var innerStackTrace = 
#if !SILVERLIGHT
						new StackTrace( inner, true );
#else
						new StackTrace( inner );
#endif
					var frames = new MessagePackObject[ innerStackTrace.FrameCount ];
					for ( int i = 0; i < frames.Length; i++ )
					{
						var frame = innerStackTrace.GetFrame( innerStackTrace.FrameCount - ( i + 1 ) );
#if !SILVERLIGHT
						if ( frame.GetFileName() == null )
						{
#endif
							frames[ i ] = new MessagePackObject[] { ToStackFrameMethodSignature( frame.GetMethod() ), frame.GetILOffset(), frame.GetNativeOffset() };
#if !SILVERLIGHT
						}
						else
						{
							frames[ i ] = new MessagePackObject[] { ToStackFrameMethodSignature( frame.GetMethod() ), frame.GetILOffset(), frame.GetNativeOffset(), frame.GetFileName(), frame.GetFileLineNumber(), frame.GetFileColumnNumber() };
						}
#endif
					}
					properties[ 4 ] = new MessagePackObject( frames );

					// data
					if ( inner.Data != null && inner.Data.Count > 0 )
					{
						var data = new MessagePackObjectDictionary( inner.Data.Count );
						foreach ( System.Collections.DictionaryEntry entry in inner.Data )
						{
							data.Add( MessagePackObject.FromObject( entry.Key ), MessagePackObject.FromObject( entry.Value ) );
						}

						properties[ 5 ] = new MessagePackObject( data );
					}

					innerList.Add( properties );
				}

				store.Add( _remoteExceptionsUtf8, new MessagePackObject( innerList ) );
			}

			store.Add( DebugInformationKeyUtf8, this.DebugInformation );

		}

		private static MessagePackObject ToStackFrameMethodSignature( MethodBase methodBase )
		{
			return String.Concat( methodBase.DeclaringType.FullName, ".", methodBase.Name, "(", String.Join( ", ", methodBase.GetParameters().Select( p => p.ParameterType.FullName ) ), ")" );
		}

		[SecuritySafeCritical]
		private static int SafeGetHRFromException( Exception exception )
		{
			var asExternalException = exception as ExternalException;
			if ( asExternalException != null )
			{
				// ExternalException.ErrorCode is SecuritySafeCritical and its assembly must be fully trusted.
				return asExternalException.ErrorCode;
			}
#if !SILVERLIGHT && !MONO
			else if ( _safeGetHRFromExceptionMethod.IsSecuritySafeCritical )
			{
				try
				{
					// Can invoke Marshal.GetHRForException because this assembly is fully trusted.
					return Marshal.GetHRForException( exception );
				}
				catch ( SecurityException ) { }
				catch ( MemberAccessException ) { }

				return 0;
			}
#endif
			else
			{
				// Cannot get HResult due to partial trust.
				return 0;
			}
		}
	}
}
