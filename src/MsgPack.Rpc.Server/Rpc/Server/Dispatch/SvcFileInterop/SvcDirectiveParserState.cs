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
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;

namespace MsgPack.Rpc.Server.Dispatch.SvcFileInterop
{
	/// <summary>
	///		Defines basic features for .svc file parser state machines.
	/// </summary>
	internal abstract class SvcDirectiveParserState
	{
		/// <summary>
		///		Gets the current position.
		/// </summary>
		/// <value>
		///		The current position, starts with 0.
		/// </value>
		public int Position { get; private set; }

		/// <summary>
		///		Gets the current line number.
		/// </summary>
		/// <value>
		///		The current line number, starts with 1.
		/// </value>
		public int LineNumber { get; private set; }

		/// <summary>
		///		Gets a value indicating whether parsing is finished.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if parsing is finished; otherwise, <c>false</c>.
		/// </value>
		public virtual bool IsFinished
		{
			get { return false; }
		}

		private readonly ServiceHostDirective _directive;

		/// <summary>
		///		Gets the parsed directive entity.
		/// </summary>
		/// <value>
		///		The parsed directive entity.
		/// </value>
		public ServiceHostDirective Directive { get { return this._directive; } }

		/// <summary>
		///		Gets a value indicating whether this instance can skip whitespace.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance can skip whitespace; otherwise, <c>false</c>.
		/// </value>
		protected virtual bool CanSkipWhitespace
		{
			get { return true; }
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="SvcDirectiveParserState"/> class.
		/// </summary>
		/// <param name="previousState">The previous state.</param>
		protected SvcDirectiveParserState( SvcDirectiveParserState previousState )
		{
			if ( previousState != null )
			{
				this.LineNumber = previousState.LineNumber;
				this.Position = previousState.Position;
				this._directive = previousState.Directive;
			}
			else
			{
				this.LineNumber = 1;
				this.Position = 0;
				this._directive = new ServiceHostDirective();
			}
		}

		/// <summary>
		///		Parses .svc file content from the specified reader.
		/// </summary>
		/// <param name="reader">The reader to read file content.</param>
		/// <returns>Next state.</returns>
		public SvcDirectiveParserState Parse( TextReader reader )
		{
			Contract.Requires( reader != null );

			using ( var wrappedReader = new LineCountingTextReader( this, reader ) )
			{
				int c = wrappedReader.Read();
				if ( c == -1 )
				{
					this.OnUnexpectedEof();
				}

				if ( this.CanSkipWhitespace && Char.IsWhiteSpace( ( char )c ) )
				{
					return this;
				}

				switch ( CharUnicodeInfo.GetUnicodeCategory( ( char )c ) )
				{
					case UnicodeCategory.Control:
					case UnicodeCategory.OtherNotAssigned:
					{
						throw new InvalidOperationException(
							String.Format(
								CultureInfo.CurrentCulture,
								"Unexpectedly char 'u+{0:x4}' at line:{1}, position:{2}",
								c,
								this.LineNumber,
								this.Position
							)
						);
					}
				}

				return this.ParseCore( ( char )c, wrappedReader );
			}
		}

		/// <summary>
		///		Parses .svc file content from the specified reader.
		/// </summary>
		/// <param name="currentChar">The current char.</param>
		/// <param name="nextReader">The reader to fetch next chars.</param>
		/// <returns>Next state.</returns>
		protected abstract SvcDirectiveParserState ParseCore( char currentChar, TextReader nextReader );

		/// <summary>
		///		Transit to eof state.
		/// </summary>
		/// <returns>N/A.</returns>
		/// <exception cref="InvalidOperationException">
		///		Always thrown to indicate unexpected EOF.
		/// </exception>
		protected SvcDirectiveParserState OnUnexpectedEof()
		{
			throw new FormatException(
				String.Format(
					CultureInfo.CurrentCulture,
					"Unexpectedly ends at line:{0}, position:{1}",
					this.LineNumber,
					this.Position
				)
			);
		}

		/// <summary>
		///		Transit to unexpected char found error state.
		/// </summary>
		/// <returns>N/A.</returns>
		/// <exception cref="InvalidOperationException">
		///		Always thrown to indicate unexpected character is found.
		/// </exception>
		protected SvcDirectiveParserState OnUnexpectedCharFound( char currentChar )
		{
			throw new FormatException(
				String.Format(
					CultureInfo.CurrentCulture,
					"Unexpected char '{0}'(0x{1}) in line:{2}, position:{3}.",
					Escape( currentChar ),
					( ushort )currentChar,
					this.LineNumber,
					this.Position
				)
			);
		}

		private static char Escape( char c )
		{
			switch ( CharUnicodeInfo.GetUnicodeCategory( c ) )
			{
				case UnicodeCategory.Control:
				case UnicodeCategory.EnclosingMark:
				case UnicodeCategory.Format:
				case UnicodeCategory.LineSeparator:
				case UnicodeCategory.NonSpacingMark:
				case UnicodeCategory.OtherNotAssigned:
				case UnicodeCategory.ParagraphSeparator:
				case UnicodeCategory.PrivateUse:
				case UnicodeCategory.Surrogate:
				{
					return '\uFFFD';
				}
				default:
				{
					return c;
				}
			}
		}

		/// <summary>
		///		Wraps <see cref="TextReader"/> to handle line/position update.
		/// </summary>
		private sealed class LineCountingTextReader : TextReader
		{
			private readonly SvcDirectiveParserState _enclosing;
			// DO NOT dispose.
			private readonly TextReader _underlying;

			public LineCountingTextReader( SvcDirectiveParserState enclosing, TextReader underlying )
			{
				GC.SuppressFinalize( this );
				this._enclosing = enclosing;
				this._underlying = underlying;
			}

			protected sealed override void Dispose( bool disposing )
			{
				base.Dispose( disposing );
			}

			public sealed override int Read()
			{
				int result = this._underlying.Read();
				if ( result == -1 )
				{
					return result;
				}

				if ( result == '\u000a' )
				{
					this._enclosing.Position = 0;
					this._enclosing.LineNumber++;
				}
				else
				{
					this._enclosing.Position++;
				}

				return result;
			}
		}
	}
}
