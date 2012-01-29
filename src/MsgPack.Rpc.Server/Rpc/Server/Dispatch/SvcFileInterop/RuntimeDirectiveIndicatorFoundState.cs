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
using System.IO;
using System.Text;

namespace MsgPack.Rpc.Server.Dispatch.SvcFileInterop
{
	/// <summary>
	///		Represents parser state when the '&lt;%@' indicator sequence found.
	/// </summary>
	internal sealed class RuntimeDirectiveIndicatorFoundState : SvcDirectiveParserState
	{
		private const string _indicator = "ServiceHost";

		public RuntimeDirectiveIndicatorFoundState( ServerDirectiveIndicatorFoundState previous ) : base( previous ) { }

		protected sealed override SvcDirectiveParserState ParseCore( char currentChar, TextReader nextReader )
		{
			int c = currentChar;
			for ( int i = 0; i < _indicator.Length; i++, c = nextReader.Read() )
			{
				if ( c < 0 )
				{
					this.OnUnexpectedEof();
				}

				if ( _indicator[ i ] != c )
				{
					throw new NotSupportedException(
						String.Format(
							CultureInfo.CurrentCulture,
							"Unexpected char '{0}'(0x{1}) in line:{2}, position:{3}. Element name must be \"ServiceHost\"(case sensitive).",
							currentChar,
							( ushort )currentChar,
							this.LineNumber,
							this.Position
						)
					);
				}
			}

			return new AttributeNameParsingState( this, new StringBuilder() );
		}
	}
}
