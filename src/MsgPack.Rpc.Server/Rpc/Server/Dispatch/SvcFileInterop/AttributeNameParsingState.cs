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
			if ( !XmlValidation.IsStartNCNameChar( currentChar ) && currentChar != '%' )
			{
				return this.OnUnexpectedCharFound( currentChar );
			}

			this.Buffer.Append( currentChar );

			bool isNameEnd = false;
			bool isTransittingToFinish = currentChar == '%';

			for ( int read = nextReader.Read(); read >= 0; read = nextReader.Read() )
			{
				if ( isTransittingToFinish )
				{
					if ( ( char )read == '>' )
					{
						return new FinishedState( this );
					}
					else if ( Char.IsWhiteSpace( ( char )read ) )
					{
						continue;
					}

					return this.OnUnexpectedCharFound( ( char )read );
				}

				if ( ( char )read == '=' )
				{
					for ( int mayBeQuatation = nextReader.Read(); mayBeQuatation >= 0; mayBeQuatation = nextReader.Read() )
					{
						if ( mayBeQuatation == '\'' || mayBeQuatation == '"' )
						{
							return new AttributeValueParsingState( this, XmlConvert.DecodeName( this.Buffer.ToString() ), ( char )mayBeQuatation, this.Buffer );
						}
						else if ( Char.IsWhiteSpace((char) mayBeQuatation ))
						{
							continue;
						}
						else
						{
							return this.OnUnexpectedCharFound( ( char )mayBeQuatation );
						}
					}

					return this.OnUnexpectedEof();
				}
				else if ( ( char )read == '%' )
				{
					isTransittingToFinish = true;
				}
				else if ( Char.IsWhiteSpace( ( char )read ) )
				{
					isNameEnd = true;
				}
				else if ( XmlValidation.IsXmlChar( ( char )read ) )
				{
					if ( isNameEnd )
					{
						// 'NAME NAME' pattern
						this.OnUnexpectedCharFound( ( char )read );
					}

					this.Buffer.Append( ( char )read );
				}
				else
				{
					this.OnUnexpectedCharFound( ( char )read );
				}
			}

			return this.OnUnexpectedEof();
		}
	}
}
