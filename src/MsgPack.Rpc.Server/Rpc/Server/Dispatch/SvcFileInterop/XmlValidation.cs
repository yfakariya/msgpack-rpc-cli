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

namespace MsgPack
{
	internal static class XmlValidation
	{
		public static bool IsStartNCNameChar( char c )
		{
			if( c == '_' )
			{
				return true;
			}
			
			switch( CharUnicodeInfo.GetUnicodeCategory ( c ) )
			{
				case UnicodeCategory.LowercaseLetter:
				case UnicodeCategory.ModifierLetter:
				case UnicodeCategory.OtherLetter:
				case UnicodeCategory.TitlecaseLetter:
				case UnicodeCategory.UppercaseLetter:
				{
					return true;
				}
				default:
				{
					return false;
				}
			}
		}
		
		public static bool IsXmlChar(char c )
		{
			switch( c )
			{
				case '\u0009':
				case '\u000A':
				case '\u000D':
				{
					return true;
				}
			}
			
			if( c < '\u0020' || Char.IsSurrogate( c ) || '\uFFFD' < c )
			{
				return false;
			}
			
			return true;
		}
	}
}

