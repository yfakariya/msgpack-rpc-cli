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

namespace MsgPack.Rpc
{
	/// <summary>
	///Tests the Exception Extensions 
	/// </summary>
	[TestFixture()]
	public class ExceptionExtensionsTest
	{
		[Test()]
		public void TestGetInnerException_InnerIsNull_Null()
		{
			var source = new Exception();
			var result = source.GetInnerException();
			Assert.That( result, Is.Null );
		}

		[Test()]
		public void TestGetInnerException_NotMarked_AsIs()
		{
			var nestedInner = new Exception( "Nested inner" );
			var inner = new Exception( "Inner", nestedInner );
			var source = new Exception( "Outer", inner );
	
			var result = source.GetInnerException();
			
			Assert.That( result, Is.SameAs( inner ) );
		}

		[Test()]
		public void TestGetInnerException_Marked_HasInner_NestedInner()
		{
			var nestedInner = new Exception( "Nested inner" );
			var inner = new Exception( "Inner", nestedInner );
			inner.Data[ ExceptionModifiers.IsMatrioshkaInner ] = null;
			var source = new Exception( "Outer", inner );

			var result = source.GetInnerException();

			Assert.That( result, Is.SameAs( nestedInner ) );
		}

		[Test()]
		public void TestGetInnerException_Marked_DoesNotHaveInner_NestedInner()
		{
			var inner = new Exception( "Inner", null );
			inner.Data[ ExceptionModifiers.IsMatrioshkaInner ] = null;
			var source = new Exception( "Outer", inner );

			var result = source.GetInnerException();

			Assert.That( result, Is.Null );
		}

		[Test()]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestGetInnerException_Null()
		{
			ExceptionExtensions.GetInnerException( null );
		}
	}
}
