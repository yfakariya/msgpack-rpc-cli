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
using System.Diagnostics;
using System.IO;
using System.Diagnostics.Contracts;

namespace MsgPack.Rpc.Server.Dispatch.SvcFileInterop
{
	/// <summary>
	///		Parser for WCF .svc file content.
	/// </summary>
	internal static class SvcFileParser
	{
		/// <summary>
		///		Parses content as <see cref="ServiceHostDirective"/>.
		/// </summary>
		/// <param name="input"><see cref="TextReader"/> to read the content.</param>
		/// <returns>Parsed <see cref="ServiceHostDirective"/>.</returns>
		public static ServiceHostDirective Parse( TextReader input )
		{
			Contract.Assert( input != null );

			SvcDirectiveParserState state = new InitialState();

			while ( !state.IsFinished )
			{
				state = state.Parse( input );
			}

			return state.Directive;
		}
	}
}