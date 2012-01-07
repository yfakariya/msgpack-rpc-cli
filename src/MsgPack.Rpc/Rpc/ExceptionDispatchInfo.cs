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
using System.Reflection;
using System.Runtime.InteropServices;

namespace MsgPack.Rpc
{
#if !NET_4_5
	internal sealed class ExceptionDispatchInfo
	{
		private static readonly Type[] _constructorParameterStringException = new[] { typeof( string ), typeof( Exception ) };
		private static readonly PropertyInfo _exceptionHResultProperty = typeof( Exception ).GetProperty( "HResult", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );

		private readonly Exception _source;

		private ExceptionDispatchInfo( Exception source )
		{
			if ( source == null )
			{
				throw new ArgumentNullException( "source" );
			}

			this._source = source;
		}

		public void Throw()
		{
			throw CreateMatroshika( this._source );
		}

		internal static Exception CreateMatroshika( Exception inner )
		{
			ExternalException asExternalException;
#if !SILVERLIGHT
			if ( AppDomain.CurrentDomain.IsFullyTrusted )
			{
				if ( ( asExternalException = inner as ExternalException ) != null )
				{
					var result = GetMatroshika( inner );
					if ( result != null )
					{
						_exceptionHResultProperty.SetValue( result, Marshal.GetHRForException( inner ), null );
						return result;
					}
					else
					{
						// Fallback.
						return new TargetInvocationException( inner.Message, inner );
					}
				}
			}
#endif

			COMException asCOMException;
			SEHException asSEHException;
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

			return GetMatroshika( inner ) ?? new TargetInvocationException( inner.Message, inner );
		}

		private static Exception GetMatroshika( Exception inner )
		{
			var ctor = inner.GetType().GetConstructor( _constructorParameterStringException );
			if ( ctor == null )
			{
				return null;
			}
			var result = ctor.Invoke( new object[] { inner.Message, inner.InnerException } ) as Exception;
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
		}

		[Serializable]
		private sealed class WrapperCOMException : COMException
		{
			public WrapperCOMException( string message, COMException inner )
				: base( message, inner )
			{
				this.HResult = inner.ErrorCode;
			}
		}

		[Serializable]
		private sealed class WrapperSEHException : SEHException
		{
			public WrapperSEHException( string message, SEHException inner )
				: base( message, inner )
			{
				this.HResult = inner.ErrorCode;
			}
		}
	}
#endif
}