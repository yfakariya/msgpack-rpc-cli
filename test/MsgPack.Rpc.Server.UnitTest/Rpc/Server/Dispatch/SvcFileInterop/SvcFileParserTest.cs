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
	public class SvcFileParserTest
	{
		private static void TestParseCore( string content, string expected, bool writeBom )
		{
			using ( var buffer = new MemoryStream() )
			{
				if ( writeBom )
				{
					var bom = Encoding.UTF8.GetPreamble();
					buffer.Write( bom, 0, bom.Length );
				}

				var contentBytes = Encoding.UTF8.GetBytes( content );
				buffer.Write( contentBytes, 0, contentBytes.Length );

				buffer.Position = 0;

				SvcDirectiveParserState target = new InitialState();

				using ( var reader = new StreamReader( buffer ) )
				{
					do
					{
						target = target.Parse( reader );
					}
					while ( !target.IsFinished );
				}

				Assert.That( target.Directive.Service, Is.EqualTo( expected ) );
			}
		}

		[Test]
		public void TestParse_Normal_AsIs()
		{
			var value = "Example.Test.Service";
			TestParseCore( "<% @ ServiceHost  Service=\"" + value + "\" %>", value, false );
		}

		[Test]
		public void TestParse_WithBom_AsIs()
		{
			var value = "Example.Test.Service";
			TestParseCore( "<% @ ServiceHost  Service=\"" + value + "\" %>", value, true );
		}

		[Test]
		public void TestParse_WithExtra_Ignored()
		{
			var value = "Example.Test.Service";
			TestParseCore( "<% @ ServiceHost  Service=\"" + value + "\" Debug=\"true\" %>", value, false );
		}

		[Test]
		[ExpectedException( typeof( FormatException ) )]
		public void TestParse_InvalidName_Fail()
		{
			var value = "Example.Test.Service";
			TestParseCore( "<% @ ServiceHost  service=\"" + value + "\"", value, false );
		}

		[Test]
		[ExpectedException( typeof( FormatException ) )]
		public void TestParse_InvalidTagName_Fail()
		{
			var value = "Example.Test.Service";
			TestParseCore( "<% @ ServiceHosts  Service=\"" + value + "\"", value, false );
		}
	}
}
