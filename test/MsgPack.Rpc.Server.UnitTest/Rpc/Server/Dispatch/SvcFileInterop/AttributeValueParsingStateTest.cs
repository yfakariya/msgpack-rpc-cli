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
	public class AttributeValueParsingStateTest
	{
		private static void TestParseCore( char quotation, string name, string value )
		{
			TestParseCore(
				quotation,
				name,
				value,
				target => Assert.That( ( target as AttributeNameParsingState ).Directive.Service, Is.EqualTo( value ) )
			);
		}

		private static void TestParseCore( char quotation, string name, string value, Action<SvcDirectiveParserState> assertion )
		{
			SvcDirectiveParserState target = new AttributeValueParsingState( GetPrevious(), name, quotation, new StringBuilder() );
			var reader = new StringReader( value + quotation );
			target = target.Parse( reader );
			Assert.That( target, Is.TypeOf<AttributeNameParsingState>() );
			assertion( target );
		}

		[Test]
		public void TestParse_DoubleQuoteCharsAndDoubleQuote_NameWasService_TransitToAttributeNameParsingStateWithDirectiveSet()
		{
			TestParseCore( '"', "Service", "Example.Service" );
		}

		[Test]
		public void TestParse_SingleQuoteCharsAndSingleQuote_NameWasService_TransitToAttributeNameParsingStateWithDirectiveSet()
		{
			TestParseCore( '\'', "Service", "Example.Service" );
		}

		[Test]
		public void TestParse_QuotedChars_NameWasWrongCasingService_TransitToAttributeNameParsingStateWithoutDirectiveSet()
		{
			TestParseCore( '"', "service", "Example.Service", target => Assert.That( ( target as AttributeNameParsingState ).Directive.Service, Is.Null ) );
		}

		[Test]
		public void TestParse_QuotedChars_NameWasNotService_TransitToAttributeNameParsingStateWithoutDirectiveSet()
		{
			TestParseCore( '"', "Services", "Example.Service", target => Assert.That( ( target as AttributeNameParsingState ).Directive.Service, Is.Null ) );
		}

		[Test]
		public void TestParse_QuotedCharsWithWhitespace_NameWasService_TransitToAttributeNameParsingStateWithDirectiveSetAsIs()
		{
			TestParseCore( '"', "Service", " Example Service " );
		}

		[Test]
		public void TestParse_QuotedCharsWithEntityOrCharacterReference_NameWasService_TransitToAttributeNameParsingStateWithDirectiveSet()
		{
			TestParseCore( '"', "Service", "&amp;&quot;&lt;&gt;&apos;&#x123;", target => Assert.That( ( target as AttributeNameParsingState ).Directive.Service, Is.EqualTo( "&\"<>'" + ( char )0x123 ) ) );
		}

		[Test]
		public void TestParse_DoubleQuotedEmpty_NameWasService_TransitToAttributeNameParsingStateWithDirectiveSetEmpty()
		{
			TestParseCore( '"', "Service", String.Empty, target => Assert.That( ( target as AttributeNameParsingState ).Directive.Service, Is.Not.Null.And.Empty ) );
		}

		[Test]
		public void TestParse_SingleQuotedEmpty_NameWasService_TransitToAttributeNameParsingStateWithDirectiveSetEmpty()
		{
			TestParseCore( '\'', "Service", String.Empty, target => Assert.That( ( target as AttributeNameParsingState ).Directive.Service, Is.Not.Null.And.Empty ) );
		}

		[Test]
		[SetUICulture( "en-US" )]
		[ExpectedException( typeof( FormatException ), MatchType = MessageMatch.Contains, ExpectedMessage = "nexpected" )]
		public void TestParse_DoubleQuoteOneCharAndSingleQuote_NameWasService_Fail()
		{
			var quotation = '"';
			var name = "Service";
			var value = "Example.Service";
			SvcDirectiveParserState target = new AttributeValueParsingState( GetPrevious(), name, quotation, new StringBuilder() );
			var reader = new StringReader( value + '\'' );
			target = target.Parse( reader );
		}

		[Test]
		[SetUICulture( "en-US" )]
		[ExpectedException( typeof( FormatException ), MatchType = MessageMatch.Contains, ExpectedMessage = "nexpected" )]
		public void TestParse_SingleQuoteOneCharAndDoubleQuote_NameWasService_TransitToAttributeNameParsingStateWithDirectiveSet()
		{
			var quotation = '\'';
			var name = "Service";
			var value = "Example.Service";
			SvcDirectiveParserState target = new AttributeValueParsingState( GetPrevious(), name, quotation, new StringBuilder() );
			var reader = new StringReader( value + '"' );
			target = target.Parse( reader );
		}

		[Test]
		[SetUICulture( "en-US" )]
		[ExpectedException( typeof( FormatException ), MatchType = MessageMatch.Contains, ExpectedMessage = "ends" )]
		public void TestParse_NoEndQuot_Fail()
		{
			var quotation = '"';
			var name = "Service";
			var value = "Example.Service";
			SvcDirectiveParserState target = new AttributeValueParsingState( GetPrevious(), name, quotation, new StringBuilder() );
			var reader = new StringReader( value );
			target = target.Parse( reader );
		}

		[Test]
		[SetUICulture( "en-US" )]
		[ExpectedException( typeof( FormatException ), MatchType = MessageMatch.Contains, ExpectedMessage = "nexpected" )]
		public void TestParse_UnescapedLesserThan_Fail()
		{
			TestParseCore( '"', "Service", "<", _ => Assert.Fail() );
		}

		[Test]
		[SetUICulture( "en-US" )]
		[ExpectedException( typeof( FormatException ), MatchType = MessageMatch.Contains, ExpectedMessage = "nexpected" )]
		public void TestParse_UnescapedGreaterThan_Fail()
		{
			TestParseCore( '"', "Service", ">", _ => Assert.Fail() );
		}

		[Test]
		[SetUICulture( "en-US" )]
		[ExpectedException( typeof( FormatException ), MatchType = MessageMatch.Contains, ExpectedMessage = "reference is not end" )]
		public void TestParse_UnescapedAmpasand_Fail()
		{
			TestParseCore( '"', "Service", "&", _ => Assert.Fail() );
		}

		private static AttributeNameParsingState GetPrevious()
		{
			return new AttributeNameParsingState( new RuntimeDirectiveIndicatorFoundState( new ServerDirectiveIndicatorFoundState( new StartTagFoundState( new InitialState() ) ) ), new StringBuilder() );
		}
	}
}
