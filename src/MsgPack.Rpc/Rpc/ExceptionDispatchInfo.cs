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
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace MsgPack.Rpc
{
#if !NET_4_5 && !SILVERLIGHT && !MONO
	internal sealed class ExceptionDispatchInfo
	{
		private static readonly Type[] _constructorParameterStringException = new[] { typeof( string ), typeof( Exception ) };
		private static readonly PropertyInfo _exceptionHResultProperty = typeof( Exception ).GetProperty( "HResult", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
		private static readonly MethodInfo _safeCreateMatroshikaMethod = typeof( ExceptionDispatchInfo ).GetMethod( "SafeCreateMatroshika", BindingFlags.Static | BindingFlags.NonPublic );
		private static readonly MethodInfo _safeCreateWrapperWin32ExceptionMethod = typeof( ExceptionDispatchInfo ).GetMethod( "SafeCreateWrapperWin32Exception", BindingFlags.Static | BindingFlags.NonPublic );

		private readonly Exception _source;

		private ExceptionDispatchInfo( Exception source )
		{
			if ( source == null )
			{
				throw new ArgumentNullException( "source" );
			}

			Contract.EndContractBlock();

			this._source = source;
			var preservable = source as IStackTracePreservable;
			if ( preservable != null )
			{
				preservable.PreserveStackTrace();
			}
		}

		[ContractInvariantMethod]
		[SuppressMessage( "Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "ContractInvariantMethod" )]
		private void ObjectInvariant()
		{
			Contract.Invariant( this._source != null );
		}

		public void Throw()
		{
			if ( this._source is IStackTracePreservable )
			{
				throw this._source;
			}
			else
			{
				throw CreateMatroshika( this._source );
			}
		}

		internal static Exception CreateMatroshika( Exception inner )
		{
			Contract.Requires( inner != null );
			Contract.Ensures( Contract.Result<Exception>() != null );

			Exception result = HandleKnownWin32Exception( inner );
			if ( result != null )
			{
				return result;
			}

			result = TryCreateMatroshikaWithExternalExceptionMatroshka( inner );
			if ( result != null )
			{
				return result;
			}

			result = HandleExternalExceptionInPartialTrust( inner );
			if ( result != null )
			{
				return result;
			}

			return GetMatroshika( inner ) ?? new TargetInvocationException( inner.Message, inner );
		}
		private static Exception HandleKnownWin32Exception( Exception inner )
		{
			// These do not have .ctor with innerException, so we must always create wrapper to preserve stack trace.
			SocketException asSocketException;
			HttpListenerException asHttpListenerException;
			NetworkInformationException asNetworkInformationException;
			Win32Exception asWin32Exception;

			if ( ( asSocketException = inner as SocketException ) != null )
			{
				var result = new WrapperSocketException( asSocketException );
				SetMatroshika( inner );
				return result;
			}

			if ( ( asHttpListenerException = inner as HttpListenerException ) != null )
			{
				var result = new WrapperHttpListenerException( asHttpListenerException );
				SetMatroshika( inner );
				return result;
			}

			if ( ( asNetworkInformationException = inner as NetworkInformationException ) != null )
			{
				var result = new WrapperNetworkInformationException( asNetworkInformationException );
				SetMatroshika( inner );
				return result;
			}

			if ( ( asWin32Exception = inner as Win32Exception ) != null )
			{
				if ( _safeCreateWrapperWin32ExceptionMethod.IsSecuritySafeCritical )
				{
					var result = SafeCreateWrapperWin32Exception( asWin32Exception );
					return result;
				}
				else
				{
					return new TargetInvocationException( asWin32Exception.Message, asWin32Exception );
				}
			}

			return null;
		}

		private static Exception TryCreateMatroshikaWithExternalExceptionMatroshka( Exception inner )
		{
			// Try matroshika with HResult setting(requires full trust).
			if ( _safeCreateMatroshikaMethod.IsSecuritySafeCritical )
			{
				ExternalException asExternalException;
				if ( ( asExternalException = inner as ExternalException ) != null )
				{
					var matroshika = SafeCreateMatroshika( asExternalException );
					if ( matroshika != null )
					{
						return matroshika;
					}
					else
					{
						// Fallback.
						return new TargetInvocationException( inner.Message, inner );
					}
				}
			}

			return null;
		}

		private static Exception HandleExternalExceptionInPartialTrust( Exception inner )
		{
			// Partial trust fallback.
			COMException asCOMException;
			SEHException asSEHException;
			ExternalException asExternalException;

			if ( ( asCOMException = inner as COMException ) != null )
			{
				var result = new WrapperCOMException( asCOMException.Message, asCOMException );
				SetMatroshika( inner );
				return result;
			}

			if ( ( asSEHException = inner as SEHException ) != null )
			{
				var result = new WrapperSEHException( asSEHException.Message, asSEHException );
				SetMatroshika( inner );
				return result;
			}

			if ( ( asExternalException = inner as ExternalException ) != null )
			{
				var result = new WrapperExternalException( asExternalException.Message, asExternalException );
				SetMatroshika( inner );
				return result;
			}

			return null;
		}

		[SecuritySafeCritical]
		private static Exception SafeCreateMatroshika( ExternalException inner )
		{
			var result = GetMatroshika( inner );
			if ( result != null )
			{
				_exceptionHResultProperty.SetValue( result, Marshal.GetHRForException( inner ), null );
			}

			return result;
		}

		[SecuritySafeCritical]
		private static WrapperWin32Exception SafeCreateWrapperWin32Exception( Win32Exception inner )
		{
			var result = new WrapperWin32Exception( inner.Message, inner );
			SetMatroshika( inner );
			return result;
		}

		private static Exception GetMatroshika( Exception inner )
		{
			var ctor = inner.GetType().GetConstructor( _constructorParameterStringException );
			if ( ctor == null )
			{
				return null;
			}
			var result = ctor.Invoke( new object[] { inner.Message, inner } ) as Exception;
			SetMatroshika( inner );
			return result;
		}
		private static void SetMatroshika( Exception exception )
		{
			exception.Data[ ExceptionModifiers.IsMatrioshkaInner ] = null;
		}

		public static ExceptionDispatchInfo Capture( Exception source )
		{
			// TODO: Capture Watson information.
			return new ExceptionDispatchInfo( source );
		}

		[Serializable]
		private sealed class WrapperExternalException : ExternalException
		{
			public WrapperExternalException( string message, ExternalException inner )
				: base( message, inner )
			{
				this.HResult = inner.ErrorCode;
			}

			private WrapperExternalException( SerializationInfo info, StreamingContext context ) : base( info, context ) { }
		}

		[Serializable]
		private sealed class WrapperCOMException : COMException
		{
			public WrapperCOMException( string message, COMException inner )
				: base( message, inner )
			{
				this.HResult = inner.ErrorCode;
			}

			private WrapperCOMException( SerializationInfo info, StreamingContext context ) : base( info, context ) { }
		}

		[Serializable]
		private sealed class WrapperSEHException : SEHException
		{
			public WrapperSEHException( string message, SEHException inner )
				: base( message, inner )
			{
				this.HResult = inner.ErrorCode;
			}

			private WrapperSEHException( SerializationInfo info, StreamingContext context ) : base( info, context ) { }
		}

		[Serializable]
		[SecuritySafeCritical]
		private sealed class WrapperWin32Exception : Win32Exception
		{
			public WrapperWin32Exception( string message, Win32Exception inner )
				: base( message, inner )
			{
				this.HResult = inner.ErrorCode;
			}

			private WrapperWin32Exception( SerializationInfo info, StreamingContext context ) : base( info, context ) { }
		}

		[Serializable]
		private sealed class WrapperHttpListenerException : HttpListenerException
		{
			private readonly string _innerStackTrace;

			public sealed override string StackTrace
			{
				get
				{
					return
						String.Join(
							this._innerStackTrace,
							"   --- End of preserved stack trace ---",
							Environment.NewLine,
							base.StackTrace
						);
				}
			}

			public WrapperHttpListenerException( HttpListenerException inner )
				: base( inner.ErrorCode )
			{
				this._innerStackTrace = inner.StackTrace;
			}

			private WrapperHttpListenerException( SerializationInfo info, StreamingContext context ) : base( info, context ) { }
		}

		[Serializable]
		private sealed class WrapperNetworkInformationException : NetworkInformationException
		{
			private readonly string _innerStackTrace;

			public sealed override string StackTrace
			{
				get
				{
					return
						String.Join(
							this._innerStackTrace,
							"   --- End of preserved stack trace ---",
							Environment.NewLine,
							base.StackTrace
						);
				}
			}

			public WrapperNetworkInformationException( NetworkInformationException inner )
				: base( inner.ErrorCode )
			{
				this._innerStackTrace = inner.StackTrace;
			}

			private WrapperNetworkInformationException( SerializationInfo info, StreamingContext context ) : base( info, context ) { }
		}

		[Serializable]
		private sealed class WrapperSocketException : SocketException
		{
			private readonly string _innerStackTrace;

			public sealed override string StackTrace
			{
				get
				{
					return
						String.Join(
							this._innerStackTrace,
							"   --- End of preserved stack trace ---",
							Environment.NewLine,
							base.StackTrace
						);
				}
			}

			public WrapperSocketException( SocketException inner )
				: base( inner.ErrorCode )
			{
				this._innerStackTrace = inner.StackTrace;
			}

			private WrapperSocketException( SerializationInfo info, StreamingContext context ) : base( info, context ) { }
		}
	}
#endif
}