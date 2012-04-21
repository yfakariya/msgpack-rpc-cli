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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MsgPack.Rpc.Client
{
	internal static class FileSystem
	{
		private static readonly Regex _invalidPathChars =
			new Regex(
				"[" + Regex.Escape( String.Join( String.Empty, Path.GetInvalidPathChars().Concat( Path.GetInvalidFileNameChars() ).Distinct() ) ) + "]",
#if !SILVERLIGHT
				 RegexOptions.Compiled |
#endif
				 RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Singleline
			);

		public static string EscapeInvalidPathChars( string value, string replacement )
		{
			if ( value == null )
			{
				throw new ArgumentNullException( "value" );
			}

#if !SILVERIGHT
			return _invalidPathChars.Replace( value, replacement ?? String.Empty );
#else
			return "." + Path.DirectorySepartorChar + _invalidPathChars.Replace( value, replacement ?? String.Empty );
#endif
		}
	}
}
