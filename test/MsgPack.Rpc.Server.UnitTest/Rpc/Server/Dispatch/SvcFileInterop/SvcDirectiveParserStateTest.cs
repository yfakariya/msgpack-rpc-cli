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
using NUnit.Framework;

namespace MsgPack.Rpc.Server.Dispatch.SvcFileInterop
{
	[TestFixture]
	public class SvcDirectiveParserStateTest
	{
		[Test]
		[ExpectedException( typeof( FormatException ) )]
		public void TestParse_Empty()
		{
			var target = new Target();
			target.Parse( new StringReader( String.Empty ) );
		}

		[Test]
		public void TestParse_LineNumberAndPosition()
		{
			SvcDirectiveParserState target = new InitialState();
			var reader = new StringReader( "  \r\n <" );

			Assert.That( target.Position, Is.EqualTo( 0 ) );
			Assert.That( target.LineNumber, Is.EqualTo( 1 ) );

			target = target.Parse( reader );

			Assert.That( target.Position, Is.EqualTo( 1 ) );
			Assert.That( target.LineNumber, Is.EqualTo( 1 ) );

			target = target.Parse( reader );

			Assert.That( target.Position, Is.EqualTo( 2 ) );
			Assert.That( target.LineNumber, Is.EqualTo( 1 ) );

			target = target.Parse( reader );

			Assert.That( target.Position, Is.EqualTo( 3 ) );
			Assert.That( target.LineNumber, Is.EqualTo( 1 ) );

			target = target.Parse( reader );

			Assert.That( target.Position, Is.EqualTo( 0 ) );
			Assert.That( target.LineNumber, Is.EqualTo( 2 ) );

			target = target.Parse( reader );

			Assert.That( target.Position, Is.EqualTo( 1 ) );
			Assert.That( target.LineNumber, Is.EqualTo( 2 ) );

			target = target.Parse( reader );

			Assert.That( target.Position, Is.EqualTo( 2 ) );
			Assert.That( target.LineNumber, Is.EqualTo( 2 ) );
			Assert.That( target, Is.TypeOf<StartTagFoundState>() );
		}

		private sealed class Target : SvcDirectiveParserState
		{
			public Target() : base( null ) { }

			protected override SvcDirectiveParserState ParseCore( char currentChar, TextReader nextReader )
			{
				throw new NotImplementedException();
			}
		}
	}
}
