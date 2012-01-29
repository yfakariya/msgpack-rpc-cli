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
using NUnit.Framework;

namespace MsgPack.Rpc.Server.Dispatch.SvcFileInterop
{
	[TestFixture()]
	public class AttributeNameParsingStateTest
	{
		[Test]
		public void TestParse_OrdinalyNameTrailingEqualityAndDoubleQuotation_TransitToAttributeValueParsingStateWithDoubleQuotation()
		{
			var buffer = new StringBuilder();
			SvcDirectiveParserState target = new AttributeNameParsingState( GetPrevious(), buffer );
			var reader = new StringReader( "Service=\"" );
			target = target.Parse( reader );
			Assert.That( target, Is.TypeOf<AttributeValueParsingState>() );
			Assert.That( ( target as AttributeValueParsingState ).Quotation, Is.EqualTo( '"' ) );
		}

		[Test]
		public void TestParse_OrdinalyNameTrailingEqualityAndSingleQuotation_TransitToAttributeValueParsingStateWithSingleQuotation()
		{
			var buffer = new StringBuilder();
			SvcDirectiveParserState target = new AttributeNameParsingState( GetPrevious(), buffer );
			var reader = new StringReader( "Service='" );
			target = target.Parse( reader );
			Assert.That( target, Is.TypeOf<AttributeValueParsingState>() );
			Assert.That( ( target as AttributeValueParsingState ).Quotation, Is.EqualTo( '\'' ) );
		}

		[Test]
		public void TestParse_OrdinalyNameAndWhitespaceTrailingEqualityAndQuatation_TransitToAttributeValueParsingState()
		{
			var buffer = new StringBuilder();
			SvcDirectiveParserState target = new AttributeNameParsingState( GetPrevious(), buffer );
			var reader = new StringReader( " Service = \"" );
			target = target.Parse( reader );
			Assert.That( target, Is.TypeOf<AttributeNameParsingState>() );
			
			target = target.Parse( reader );
			Assert.That( target, Is.TypeOf<AttributeValueParsingState>() );
		}

		[Test]
		[ExpectedException( typeof( FormatException ), MatchType = MessageMatch.Contains, ExpectedMessage = "ends" )]
		public void TestParse_OrdinalyNameTrailingEqualityWithoutQuatation_TransitToAttributeValueParsingState()
		{
			var buffer = new StringBuilder();
			SvcDirectiveParserState target = new AttributeNameParsingState( GetPrevious(), buffer );
			var reader = new StringReader( "ServiceClass=" );
			target = target.Parse( reader );
		}

		[Test]
		[SetUICulture( "en-US" )]
		[ExpectedException( typeof( FormatException ), MatchType = MessageMatch.Contains, ExpectedMessage = "nexpected char" )]
		public void TestParse_NameContainsWhitespace_Fail()
		{
			var buffer = new StringBuilder();
			SvcDirectiveParserState target = new AttributeNameParsingState( GetPrevious(), buffer );
			var reader = new StringReader( "Ser vice=" );
			target = target.Parse( reader );
		}

		[Test]
		[SetUICulture( "en-US" )]
		[ExpectedException( typeof( FormatException ), MatchType = MessageMatch.Contains, ExpectedMessage = "nexpected char" )]
		public void TestParse_EqualityImmediately_Fail()
		{
			var buffer = new StringBuilder();
			SvcDirectiveParserState target = new AttributeNameParsingState( GetPrevious(), buffer );
			var reader = new StringReader( "=" );
			target = target.Parse( reader );
			Assert.That( target, Is.TypeOf<AttributeValueParsingState>() );
		}

		[Test]
		[SetUICulture( "en-US" )]
		[ExpectedException( typeof( FormatException ), MatchType = MessageMatch.Contains, ExpectedMessage = "nexpected char" )]
		public void TestParse_DoubleQuateImmediately_Fail()
		{
			var buffer = new StringBuilder();
			SvcDirectiveParserState target = new AttributeNameParsingState( GetPrevious(), buffer );
			var reader = new StringReader( "\"" );
			target = target.Parse( reader );
		}

		[Test]
		public void TestParse_PercentGreaterThanImmediately_TransitToFinishState()
		{
			var buffer = new StringBuilder();
			SvcDirectiveParserState target = new AttributeNameParsingState( GetPrevious(), buffer );
			var reader = new StringReader( "%>" );
			target = target.Parse( reader );
			Assert.That( target, Is.TypeOf<FinishedState>() );
		}

		[Test]
		public void TestParse_WhitespaceAndPercentGreaterThan_TransitToFinishState()
		{
			var buffer = new StringBuilder();
			SvcDirectiveParserState target = new AttributeNameParsingState( GetPrevious(), buffer );
			var reader = new StringReader( " %>" );
			target = target.Parse( reader );
			Assert.That( target, Is.TypeOf<AttributeNameParsingState>() );

			target = target.Parse( reader );
			Assert.That( target, Is.TypeOf<FinishedState>() );
		}

		[Test]
		[SetUICulture( "en-US" )]
		[ExpectedException( typeof( FormatException ), MatchType = MessageMatch.Contains, ExpectedMessage = "nexpected char" )]
		public void TestParse_LesserThanImmediately_Fail()
		{
			var buffer = new StringBuilder();
			SvcDirectiveParserState target = new AttributeNameParsingState( GetPrevious(), buffer );
			var reader = new StringReader( "<" );
			target = target.Parse( reader );
		}

		[Test]
		[SetUICulture( "en-US" )]
		[ExpectedException( typeof( FormatException ), MatchType = MessageMatch.Contains, ExpectedMessage = "nexpected char" )]
		public void TestParse_GreaterThanImmediately_Fail()
		{
			var buffer = new StringBuilder();
			SvcDirectiveParserState target = new AttributeNameParsingState( GetPrevious(), buffer );
			var reader = new StringReader( ">" );
			target = target.Parse( reader );
		}

		[Test]
		[SetUICulture( "en-US" )]
		[ExpectedException( typeof( FormatException ), MatchType = MessageMatch.Contains, ExpectedMessage = "ends" )]
		public void TestParse_PercentAndEnds_Fail()
		{
			var buffer = new StringBuilder();
			SvcDirectiveParserState target = new AttributeNameParsingState( GetPrevious(), buffer );
			var reader = new StringReader( "%" );
			target = target.Parse( reader );
		}

		private static RuntimeDirectiveIndicatorFoundState GetPrevious()
		{
			return new RuntimeDirectiveIndicatorFoundState( new ServerDirectiveIndicatorFoundState( new StartTagFoundState( new InitialState() ) ) );
		}
	}
}
