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
	public class RpcArgumentExceptionTest : RpcExceptionTestBase<RpcArgumentException>
	{
		protected override RpcError DefaultError
		{
			get { return RpcError.ArgumentError; }
		}
		protected override RpcArgumentException NewRpcException( ConstructorKind kind, System.Collections.Generic.IDictionary<string, object> properties )
		{
			switch ( kind )
			{
				case ConstructorKind.Serialization:
				case ConstructorKind.WithInnerException:
				{
					return new RpcArgumentException( ( string )properties[ "MethodName" ], ( string )properties[ "ParameterName" ], GetMessage( properties ), GetDebugInformation( properties ), GetInnerException( properties ) );
				}
				default:
				{
					return new RpcArgumentException( ( string )properties[ "MethodName" ], ( string )properties[ "ParameterName" ], GetMessage( properties ), GetDebugInformation( properties ) );
				}
			}
		}

		protected override RpcArgumentException NewRpcException( RpcError rpcError, MessagePackObject unpackedException )
		{
			return new RpcArgumentException( unpackedException );
		}

		protected override System.Collections.Generic.IDictionary<string, object> GetTestArguments()
		{
			var result = base.GetTestArguments();
			result.Add( "MethodName", "TestMethod" );
			result.Add( "ParameterName", "testArgument" );
			return result;
		}

		protected override System.Collections.Generic.IDictionary<string, object> GetTestArgumentsWithAllNullValue()
		{
			var result = base.GetTestArgumentsWithAllNullValue();
			result.Add( "MethodName", "Dummy" );
			result.Add( "ParameterName", "Dummy" );
			return result;
		}

		protected override System.Collections.Generic.IDictionary<string, object> GetTestArgumentsWithDefaultValue()
		{
			var result = base.GetTestArgumentsWithDefaultValue();
			result.Add( "MethodName", "Dummy" );
			result.Add( "ParameterName", "Dummy" );
			return result;
		}

		[Test]
		public void TestConstructor_String_AsIs()
		{
			var methodName = "TestMethod";
			var parameterName = "testArgument";
			var target = new RpcArgumentException( methodName, parameterName );
			Assert.That( target.MethodName, Is.EqualTo( methodName ) );
			Assert.That( target.ParameterName, Is.EqualTo( parameterName ) );
			Assert.That( target.Message, Is.EqualTo( this.DefaultMessage ) );
			Assert.That( target.RpcError, Is.EqualTo( this.DefaultError ) );
			Assert.That( target.DebugInformation, Is.Empty );
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestConstructor_StringStringStringStringException_MethodNameIsNull()
		{
			new RpcArgumentException( null, "Dummy", null, null, null );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestConstructor_StringStringStringException_MethodNameIsEmpty()
		{
			new RpcArgumentException( String.Empty, "Dummy", null, null, null );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestConstructor_StringStringStringException_MethodNameIsWhitespaceOnly()
		{
			new RpcArgumentException( " ", "Dummy", null, null, null );
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestConstructor_StringStringStringStringException_ParameterNameIsNull()
		{
			new RpcArgumentException( "Dummy", null, null, null, null );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestConstructor_StringStringStringException_ParameterNameIsEmpty()
		{
			new RpcArgumentException( "Dummy", String.Empty, null, null, null );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestConstructor_StringStringStringException_ParameterNameIsWhitespaceOnly()
		{
			new RpcArgumentException( "Dummy", " ", null, null, null );
		}
	}
}