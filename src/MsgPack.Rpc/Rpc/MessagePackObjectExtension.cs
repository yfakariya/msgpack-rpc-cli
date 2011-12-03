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
using System.Globalization;

namespace MsgPack.Rpc
{
	internal static class MessagePackObjectExtension
	{
		public static bool TryGetString( this MessagePackObject source, MessagePackObject key, Func<string, Exception> exceptionGenerator, out string result )
		{
			if ( source.IsDictionary )
			{
				MessagePackObject value;
				if ( source.AsDictionary().TryGetValue( key, out value ) && value.IsTypeOf<string>().GetValueOrDefault() )
				{
					result = value.AsString();
					return true;
				}
			}

			HandleError( source, exceptionGenerator );

			result = null;
			return false;
		}

		public static bool TryGetTimeSpan( this MessagePackObject source, MessagePackObject key, Func<string, Exception> exceptionGenerator, out TimeSpan result )
		{
			if ( source.IsDictionary )
			{
				MessagePackObject value;
				if ( source.AsDictionary().TryGetValue( key, out value ) && value.IsTypeOf<Int64>().GetValueOrDefault() )
				{
					result = new TimeSpan( value.AsInt64() );
					return true;
				}
			}

			HandleError( source, exceptionGenerator );

			result = default( TimeSpan );
			return false;
		}

		private static void HandleError( MessagePackObject source, Func<string, Exception> exceptionGenerator )
		{
			if ( exceptionGenerator != null )
			{
				// FIXME: Configurable
				string message = String.Format( CultureInfo.CurrentCulture, "'{0}' is not valid error information.", source );
				throw exceptionGenerator( message );
			}
		}
	}
}
