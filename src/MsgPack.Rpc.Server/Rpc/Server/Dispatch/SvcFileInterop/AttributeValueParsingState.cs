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
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MsgPack.Rpc.Server.Dispatch.SvcFileInterop
{
	/// <summary>
	///		Represents "attribute value parsing" state.
	/// </summary>
	internal sealed class AttributeValueParsingState : ServiceHostDirectiveParsingState
	{
		// may single or double, the value started with this char.
		private readonly char _quotation;

		// debugging purposes
		internal char Quotation
		{
			get { return this._quotation; }
		}

		protected sealed override bool CanSkipWhitespace
		{
			get { return false; }
		}

		private readonly string _attributeName;

		public AttributeValueParsingState( AttributeNameParsingState previous, string attributeName, char quotation, StringBuilder buffer )
			: base( previous, buffer )
		{
			this._attributeName = attributeName;
			this._quotation = quotation;
			buffer.Clear();
		}

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
			for ( int read = currentChar; read != this._quotation; read = nextReader.Read() )
			{
				switch ( read )
				{
					case -1:
					{
						return this.OnUnexpectedEof();
					}
					case ( int )'\'':
					case ( int )'"':
					case ( int )'<':
					case ( int )'>':
					{
						return this.OnUnexpectedCharFound( ( char )read );
					}
				}

				this.Buffer.Append( ( char )read );
			}

			if ( this._attributeName == "Service" )
			{
				this.Directive.Service = this.NormalizeAttributeValue();
			}

			return new AttributeNameParsingState( this, this.Buffer );
		}

		/// <summary>
		///		Normalizes attribute value for XML-spec compliance.
		/// </summary>
		/// <returns>Normalized attribute value.</returns>
		private string NormalizeAttributeValue()
		{
			return String.Join( String.Empty, NormalizeWhiteSpaceChars( Dereference( this.NormalizeLineBreaks() ) ) );
		}

		/// <summary>
		///		Normalizes the line breaks to #xA;.
		/// </summary>
		/// <returns>
		///		Iterator to get normalized characters.
		/// </returns>
		/// <remarks>
		///		To avoid duplicated process, this method normalizes line breaks to \u0020.
		/// </remarks>
		private IEnumerable<char> NormalizeLineBreaks()
		{
			bool wasCarriageReturn = false;
			for ( int i = 0; i < this.Buffer.Length; i++ )
			{
				if ( this.Buffer[ i ] == '\u000a' )
				{
					yield return ' ';
					wasCarriageReturn = false;

				}
				else if ( this.Buffer[ i ] == '\u000d' )
				{
					wasCarriageReturn = true;
				}
				else if ( wasCarriageReturn )
				{
					yield return ' ';
					yield return this.Buffer[ i ];
					wasCarriageReturn = false;
				}
				else
				{
					yield return this.Buffer[ i ];
					wasCarriageReturn = false;
				}
			}
		}

		private static readonly Regex _entityPattern =
			new Regex( "^&(#(?<dec>[0-9]+)|#x(?<hex>[0-9a-fA-F]+)|(?<known>amp)|(?<known>lt)|(?<known>gt)|(?<known>quot)|(?<known>apos));$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture );

		/// <summary>
		///		Dereferences the character or known entity references.
		/// </summary>
		/// <param name="chars">The iterator.</param>
		/// <returns>The iterator.</returns>
		private IEnumerable<char> Dereference( IEnumerable<char> chars )
		{
			var entityBuffer = new StringBuilder( 10 );
			foreach ( char c in chars )
			{
				if ( entityBuffer.Length > 0 )
				{
					entityBuffer.Append( c );
					if ( c == ';' )
					{
						foreach ( char entityChar in GetEntityValue( entityBuffer ) )
						{
							yield return entityChar;
						}

						entityBuffer.Clear();
					}
				}
				else if ( c == '&' )
				{
					entityBuffer.Append( c );
				}
				else
				{
					yield return c;
				}
			}

			if ( entityBuffer.Length > 0 )
			{
				throw new FormatException( String.Format( CultureInfo.CurrentCulture, "Entity or character reference is not end in line {0}, position {1}.", this.LineNumber, this.Position ) );
			}
		}

		/// <summary>
		///		Gets the entity value.
		/// </summary>
		/// <param name="entityBuffer">The entity buffer.</param>
		/// <returns>The referenced entity value.</returns>
		private static string GetEntityValue( StringBuilder entityBuffer )
		{
			var match = _entityPattern.Match( entityBuffer.ToString() );
			string entityValue = null;
			if ( match.Success )
			{
				if ( match.Groups[ "dec" ].Success )
				{
					entityValue = Char.ConvertFromUtf32( Int32.Parse( match.Groups[ "dec" ].Value, CultureInfo.InvariantCulture ) );
				}
				else if ( match.Groups[ "hex" ].Success )
				{
					entityValue = Char.ConvertFromUtf32( Int32.Parse( match.Groups[ "hex" ].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture ) );
				}
				else if ( match.Groups[ "known" ].Success )
				{
					switch ( match.Groups[ "known" ].Value )
					{
						case "amp":
						{
							entityValue = "&";
							break;
						}
						case "lt":
						{
							entityValue = "<";
							break;
						}
						case "gt":
						{
							entityValue = ">";
							break;
						}
						case "quot":
						{
							entityValue = "\"";
							break;
						}
						case "apos":
						{
							entityValue = "'";
							break;
						}
					}
				}
			}

			if ( entityValue == null )
			{
				throw new NotSupportedException(
					String.Format(
						CultureInfo.CurrentCulture,
						"Entity reference '{0}' is not supported.",
						entityBuffer.ToString()
					)
				);
			}

			return entityValue;
		}

		/// <summary>
		///		Normalizes the white space chars according to XML spec.
		/// </summary>
		/// <param name="chars">The iterator.</param>
		/// <returns>The iterator.</returns>
		private static IEnumerable<char> NormalizeWhiteSpaceChars( IEnumerable<char> chars )
		{
			foreach ( var c in chars )
			{
				switch ( c )
				{
					case '\t':
					case '\r':
					case '\n':
					{
						yield return ' ';
						break;
					}
					default:
					{
						yield return c;
						break;
					}
				}
			}
		}
	}
}
