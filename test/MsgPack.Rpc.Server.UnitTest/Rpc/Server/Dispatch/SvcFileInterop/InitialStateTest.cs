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
using NUnit.Framework;
using System.IO;

namespace MsgPack.Rpc.Server.Dispatch.SvcFileInterop
{
	[TestFixture()]
	public class InitialStateTest
	{
		[Test()]
		public void TestParse_LessorThanImmeditely_TransitToStartTagFoundState()
		{
			var target = new InitialState();
			var next = target.Parse( new StringReader( "<" ) );
			Assert.That( next, Is.TypeOf<StartTagFoundState>() );
		}

		[Test()]
		public void TestParse_TrailingWhitespace_ProceedsByOneAndFinallyTransitToStartTagFoundState()
		{
			SvcDirectiveParserState target = new InitialState();
			var reader = new StringReader( " \u3000\t<" );
			for ( int i = 0; i < 4; i++ )
			{
				Assert.That( target.IsFinished, Is.False );
				Assert.That( target, Is.TypeOf<InitialState>() );
				target = target.Parse( reader );
			}

			Assert.That( target, Is.TypeOf<StartTagFoundState>() );
		}

		[Test()]
		[ExpectedException( typeof( FormatException ) )]
		public void TestParse_NonWhitespace()
		{
			var target = new InitialState();
			var next = target.Parse( new StringReader( "a" ) );
		}
	}
}
