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
	[TestFixture]
	public class RpcErrorMessageTest
	{
		[Test]
		public void TestConstructor_RpcError_MesssagePackObject_NotNull_AsIs()
		{
			var error = RpcError.MessageRefusedError;
			var detail = "ABC";
			var target = new RpcErrorMessage( error, detail );
			Assert.That( target.Error, Is.EqualTo( error ) );
			Assert.That( target.Detail.Equals( detail ) );
			Assert.That( target.IsSuccess, Is.False );
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestConstructor_RpcError_MesssagePackObject_RpcErrorIsNull()
		{
			var target = new RpcErrorMessage( null, "ABC" );
		}

		[Test]
		public void TestConstructor_RpcError_MesssagePackObject_DetailIsNil_AsIs()
		{
			var error = RpcError.MessageRefusedError;
			var detail = MessagePackObject.Nil;
			var target = new RpcErrorMessage( error, detail );
			Assert.That( target.Error, Is.EqualTo( error ) );
			Assert.That( target.Detail, Is.EqualTo( detail ) );
			Assert.That( target.IsSuccess, Is.False );
		}

		[Test]
		public void TestConstructor_RpcErrorStringString_NotNull_AsIs()
		{
			var error = RpcError.MessageRefusedError;
			var message = Guid.NewGuid().ToString();
			var debugInformation = Guid.NewGuid().ToString();
			var target = new RpcErrorMessage( error, message, debugInformation );
			Assert.That( target.Error, Is.EqualTo( error ) );
			Assert.That( target.Detail.AsDictionary()[ RpcException.MessageKeyUtf8 ].Equals( message ) );
			Assert.That( target.Detail.AsDictionary()[ RpcException.DebugInformationKeyUtf8 ].Equals( debugInformation ) );
			Assert.That( target.IsSuccess, Is.False );
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestConstructor_RpcErrorStringString_ErrorIsNull()
		{
			var target = new RpcErrorMessage( null, "A", "B" );
		}

		[Test]
		public void TestConstructor_RpcErrorStringString_MessageIsNull_NotContainedInDictionary()
		{
			var error = RpcError.MessageRefusedError;
			var message = default( string );
			var debugInformation = Guid.NewGuid().ToString();
			var target = new RpcErrorMessage( error, message, debugInformation );
			Assert.That( target.Error, Is.EqualTo( error ) );
			Assert.That( target.Detail.AsDictionary().ContainsKey( RpcException.MessageKeyUtf8 ), Is.False );
			Assert.That( target.Detail.AsDictionary()[ RpcException.DebugInformationKeyUtf8 ].Equals( debugInformation ) );
			Assert.That( target.IsSuccess, Is.False );
		}

		[Test]
		public void TestConstructor_RpcErrorStringString_DebugIsNull_NotContainedInDictionary()
		{
			var error = RpcError.MessageRefusedError;
			var message = Guid.NewGuid().ToString();
			var debugInformation = default( string );
			var target = new RpcErrorMessage( error, message, debugInformation );
			Assert.That( target.Error, Is.EqualTo( error ) );
			Assert.That( target.Detail.AsDictionary()[ RpcException.MessageKeyUtf8 ].Equals( message ) );
			Assert.That( target.Detail.AsDictionary().ContainsKey( RpcException.DebugInformationKeyUtf8 ), Is.False );
			Assert.That( target.IsSuccess, Is.False );
		}


		[Test]
		public void TestToException_RpcErrorMessagePackObject_NonDictionaryMesssagePackObject_Ignored()
		{
			var error = RpcError.MessageRefusedError;
			var detail = "ABC";

			var target = new RpcErrorMessage( error, detail );
			var result = target.ToException();

			Assert.That( result.RpcError, Is.EqualTo( error ) );
			Assert.That( result.Message, Is.EqualTo( result.RpcError.DefaultMessageInvariant ) );
			Assert.That( result.DebugInformation, Is.Not.Null.And.Empty );
		}

		[Test]
		public void TestToException_RpcErrorMessagePackObject_DictionaryMesssagePackObject_AsIs()
		{
			var error = RpcError.MessageRefusedError;
			var message = Guid.NewGuid().ToString();
			var debugInformation = Guid.NewGuid().ToString();
			var detail =
				new MessagePackObjectDictionary()
				{
					{ RpcException.MessageKeyUtf8, message },
					{ RpcException.DebugInformationKeyUtf8, debugInformation }
				};

			var target = new RpcErrorMessage( error, new MessagePackObject( detail ) );
			var result = target.ToException();

			Assert.That( result.RpcError, Is.EqualTo( error ) );
			Assert.That( result.Message, Is.EqualTo( message ) );
			Assert.That( result.DebugInformation, Is.EqualTo( debugInformation ) );

		}

		[Test]
		public void TestToException_RpcErrorStringString_NotNull_AsIs()
		{
			var error = RpcError.MessageRefusedError;
			var message = Guid.NewGuid().ToString();
			var debugInformation = Guid.NewGuid().ToString();

			var target = new RpcErrorMessage( error, message, debugInformation );
			var result = target.ToException();

			Assert.That( target.Error, Is.EqualTo( error ) );
			Assert.That( target.Detail.AsDictionary()[ RpcException.MessageKeyUtf8 ].Equals( message ) );
			Assert.That( target.Detail.AsDictionary()[ RpcException.DebugInformationKeyUtf8 ].Equals( debugInformation ) );
			Assert.That( target.IsSuccess, Is.False );
		}

		[Test]
		public void TestSuccess_ExceptErrorAndDetailAndToException_Default()
		{
			Assert.That( RpcErrorMessage.Success.IsSuccess, Is.True );
			Assert.That( RpcErrorMessage.Success.Equals( new RpcErrorMessage( RpcError.MessageRefusedError, "A", "B" ) ), Is.False );
			Assert.That( RpcErrorMessage.Success.ToString(), Is.Not.Null.And.Empty );
			var dummy = default( RpcErrorMessage ).GetHashCode();
		}

		[Test]
		[ExpectedException( typeof( InvalidOperationException ) )]
		public void TestSuccess_Error()
		{
			var dummy = RpcErrorMessage.Success.Error;
		}

		[Test]
		[ExpectedException( typeof( InvalidOperationException ) )]
		public void TestSuccess_Detail()
		{
			var dummy = RpcErrorMessage.Success.Detail;
		}

		[Test]
		[ExpectedException( typeof( InvalidOperationException ) )]
		public void TestSuccess_ToException()
		{
			RpcErrorMessage.Success.ToException();
		}

		[Test]
		public void TestDefault_ExceptErrorAndDetailAndToException_Default()
		{
			Assert.That( default( RpcErrorMessage ).IsSuccess, Is.True );
			Assert.That( default( RpcErrorMessage ).Equals( new RpcErrorMessage( RpcError.MessageRefusedError, "A", "B" ) ), Is.False );
			Assert.That( default( RpcErrorMessage ).ToString(), Is.Not.Null.And.Empty );
			var dummy = default( RpcErrorMessage ).GetHashCode();
		}

		[Test]
		[ExpectedException( typeof( InvalidOperationException ) )]
		public void TestDefault_Error()
		{
			var dummy = default( RpcErrorMessage ).Error;
		}

		[Test]
		[ExpectedException( typeof( InvalidOperationException ) )]
		public void TestDefault_Detail()
		{
			var dummy = default( RpcErrorMessage ).Detail;
		}

		[Test]
		[ExpectedException( typeof( InvalidOperationException ) )]
		public void TestDefault_ToException()
		{
			default( RpcErrorMessage ).ToException();
		}

		[Test]
		public void TestEquals_SameError_SameDetail_True()
		{
			var left = new RpcErrorMessage( RpcError.MessageRefusedError, CreateDetail( "Message", "Debug" ) );
			var right = new RpcErrorMessage( RpcError.MessageRefusedError, CreateDetail( "Message", "Debug" ) );
			Assert.That( left.Equals( right ), Is.True );
			Assert.That( right.Equals( left ), Is.True );
			Assert.That( left == right, Is.True );
			Assert.That( right == left, Is.True );
			Assert.That( left != right, Is.False );
			Assert.That( right != left, Is.False );
		}

		[Test]
		public void TestEquals_SameError_DifferenceDetail_False()
		{
			var left = new RpcErrorMessage( RpcError.MessageRefusedError, CreateDetail( "Message", "Debug" ) );
			var right = new RpcErrorMessage( RpcError.MessageRefusedError, CreateDetail( "Messag", "Debu" ) );
			Assert.That( left.Equals( right ), Is.False );
			Assert.That( right.Equals( left ), Is.False );
			Assert.That( left == right, Is.False );
			Assert.That( right == left, Is.False );
			Assert.That( left != right, Is.True );
			Assert.That( right != left, Is.True );
		}

		[Test]
		public void TestEquals_DifferenceError_SameDetail_False()
		{
			var left = new RpcErrorMessage( RpcError.MessageTooLargeError, CreateDetail( "Message", "Debug" ) );
			var right = new RpcErrorMessage( RpcError.MessageRefusedError, CreateDetail( "Message", "Debug" ) );
			Assert.That( left.Equals( right ), Is.False );
			Assert.That( right.Equals( left ), Is.False );
			Assert.That( left == right, Is.False );
			Assert.That( right == left, Is.False );
			Assert.That( left != right, Is.True );
			Assert.That( right != left, Is.True );
		}

		[Test]
		public void TestEquals_SameError_NilDetail_False()
		{
			var left = new RpcErrorMessage( RpcError.MessageRefusedError, CreateDetail( "Message", "Debug" ) );
			var right = new RpcErrorMessage( RpcError.MessageRefusedError, MessagePackObject.Nil );
			Assert.That( left.Equals( right ), Is.False );
			Assert.That( right.Equals( left ), Is.False );
			Assert.That( left == right, Is.False );
			Assert.That( right == left, Is.False );
			Assert.That( left != right, Is.True );
			Assert.That( right != left, Is.True );
		}

		[Test]
		public void TestEquals_Null_False()
		{
			var left = new RpcErrorMessage( RpcError.MessageRefusedError, CreateDetail( "Message", "Debug" ) );
			Assert.That( left.Equals( null ), Is.False );
		}

		[Test]
		public void TestEquals_OtherType_False()
		{
			var left = new RpcErrorMessage( RpcError.MessageRefusedError, CreateDetail( "Message", "Debug" ) );
			Assert.That( left.Equals( "ABC" ), Is.False );
		}

		[Test]
		public void TestGetHashCode_AtLeastSuccess()
		{
			var target = new RpcErrorMessage( RpcError.MessageRefusedError, CreateDetail( "Message", "Debug" ) );
			target.GetHashCode();
		}

		[Test]
		public void TestToString_ContainsErrorAndMessageAndDebug()
		{
			var message = Guid.NewGuid().ToString();
			var debugInformation = Guid.NewGuid().ToString();

			var target = new RpcErrorMessage( RpcError.MessageRefusedError, CreateDetail( message, debugInformation ) );
			var result = target.ToString();
			Assert.That( result, Is.StringContaining( message ).And.StringContaining( debugInformation ).And.StringContaining( target.Error.Identifier ) );
		}

		private MessagePackObject CreateDetail( string message, string debugInformation )
		{
			return
				new MessagePackObject(
					new MessagePackObjectDictionary()
					{
						{ RpcException.MessageKeyUtf8, message },
						{ RpcException.DebugInformationKeyUtf8, debugInformation }
					}
				);
		}

		// Equals, ToString, GetHashCode
	}
}
