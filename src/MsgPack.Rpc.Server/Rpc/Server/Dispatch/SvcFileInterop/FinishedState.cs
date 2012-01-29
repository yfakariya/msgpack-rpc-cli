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
using System.Globalization;

namespace MsgPack.Rpc.Server.Dispatch.SvcFileInterop
{
	/// <summary>
	///		Reprsents finished state.
	/// </summary>
	internal sealed class FinishedState : SvcDirectiveParserState
	{
		/// <summary>
		///		Gets a value indicating whether parsing is finished.
		/// </summary>
		/// <value>
		///   <c>true</c>.
		/// </value>
		public sealed override bool IsFinished
		{
			get { return true; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FinishedState"/> class.
		/// </summary>
		/// <param name="previous">The previous state.</param>
		public FinishedState( AttributeNameParsingState previous ) : base( previous ) { }

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
			throw new FormatException( String.Format( CultureInfo.CurrentCulture, "Extra character '{0}'(U+{1:X4}) is found.", currentChar, ( int )currentChar ) );
		}
	}
}
