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
using System.Text;
using System.Xml;

namespace MsgPack.Rpc.Server.Dispatch.SvcFileInterop
{
	/// <summary>
	///		Represents "attribute name (and following quality sign and quote) parsing" state.
	/// </summary>
	internal sealed class AttributeNameParsingState : ServiceHostDirectiveParsingState
	{
		public AttributeNameParsingState( RuntimeDirectiveIndicatorFoundState previous, StringBuilder buffer ) : base( previous, buffer ) { }

		public AttributeNameParsingState( AttributeValueParsingState previous, StringBuilder buffer ) : base( previous, buffer ) { }

		/// <summary>
		/// Parses .svc file content from the specified reader.
		/// </summary>
		/// <param name="currentChar">The current char.</param>
		/// <param name="nextReader">The reader to fetch next chars.</param>
		/// <returns>
		/// Next state.
		/// </returns>
		protected sealed override SvcDirectiveParserState ParseCore( char currentChar, TextReader nextReader )
		{
			if ( !XmlConvert.IsStartNCNameChar( currentChar ) )
			{
				return this.OnUnexpectedCharFound( currentChar );
			}

			this.Buffer.Append( currentChar );

			for ( int read = nextReader.Read(); read >= 0; read = nextReader.Read() )
			{
				if ( ( char )read == '=' )
				{
					int mayBeQuatation = nextReader.Read();
					if ( mayBeQuatation == '\'' || mayBeQuatation == '"' )
					{
						return new AttributeValueParsingState( this, XmlConvert.DecodeName( this.Buffer.ToString() ), ( char )mayBeQuatation, this.Buffer );
					}
					else if ( mayBeQuatation == -1 )
					{
						return this.OnUnepxctedEof();
					}
					else
					{
						return this.OnUnexpectedCharFound( ( char )mayBeQuatation );
					}
				}
				else if ( ( char )read == '%' )
				{
					var nextChar = nextReader.Read();
					if ( nextChar == -1 )
					{
						return this.OnUnepxctedEof();
					}
					else if ( ( char )nextChar == '>' )
					{
						return new FinishedState( this );
					}
					else
					{
						return this.OnUnexpectedCharFound( ( char )read );
					}
				}
				else if ( XmlConvert.IsXmlChar( ( char )read ) )
				{
					this.Buffer.Append( ( char )read );
				}
				else
				{
					this.OnUnexpectedCharFound( ( char )read );
				}
			}

			return this.OnUnepxctedEof();
		}
	}
}
