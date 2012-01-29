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
	[TestFixture()]
	public class RuntimeDirectiveIndicatorFoundStateTest
	{
		[Test()]
		public void TestParse_tImmediatelyValie_TransitToAttributeNameParsingState()
		{
			SvcDirectiveParserState target = new RuntimeDirectiveIndicatorFoundState( GetPrevious() );
			var reader = new StringReader( "ServiceHost" );
			target = target.Parse( reader );
			Assert.That( target, Is.TypeOf<AttributeNameParsingState>() );
		}

		[Test()]
		public void TestParse_LeadingWhitespaceAndValid_StayAndFinallyTransitToAttributeNameParsingState()
		{
			SvcDirectiveParserState target = new RuntimeDirectiveIndicatorFoundState( GetPrevious() );
			var reader = new StringReader( " ServiceHost" );

			target = target.Parse( reader );

			Assert.That( target, Is.TypeOf<RuntimeDirectiveIndicatorFoundState>() );
	
			target = target.Parse( reader );

			Assert.That( target, Is.TypeOf<AttributeNameParsingState>() );
		}

		[Test()]
		[ExpectedException( typeof( NotSupportedException ) )]
		public void TestParse_IncludesWhiteSpace_StayAndFinallyTransitToAttributeNameParsingState()
		{
			SvcDirectiveParserState target = new RuntimeDirectiveIndicatorFoundState( GetPrevious() );
			var reader = new StringReader( "Service Host" );

			target = target.Parse( reader );
		}

		[Test()]
		[ExpectedException( typeof( NotSupportedException ) )]
		public void TestParse_Invalid()
		{
			SvcDirectiveParserState target = new RuntimeDirectiveIndicatorFoundState( GetPrevious() );
			var reader = new StringReader( "e" );

			target = target.Parse( reader );
		}

		private static ServerDirectiveIndicatorFoundState GetPrevious()
		{
			return new ServerDirectiveIndicatorFoundState( new StartTagFoundState( new InitialState() ) );
		}
	}
}
