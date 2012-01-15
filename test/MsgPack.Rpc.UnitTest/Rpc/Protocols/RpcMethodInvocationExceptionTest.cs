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

namespace MsgPack.Rpc.Protocols
{
	[TestFixture]
	public class RpcMethodInvocationExceptionTest : RpcExceptionTestBase<RpcMethodInvocationException>
	{
		protected override RpcError DefaultError
		{
			get { return RpcError.CallError; }
		}

		protected override RpcMethodInvocationException NewRpcException( ConstructorKind kind, System.Collections.Generic.IDictionary<string, object> properties )
		{
			switch ( kind )
			{
				case ConstructorKind.Serialization:
				case ConstructorKind.WithInnerException:
				{
					return new RpcMethodInvocationException( GetRpcError( properties ), ( string )properties[ "MethodName" ], GetMessage( properties ), GetDebugInformation( properties ), GetInnerException( properties ) );
				}
				default:
				{
					return new RpcMethodInvocationException( GetRpcError( properties ), ( string )properties[ "MethodName" ], GetMessage( properties ), GetDebugInformation( properties ) );
				}
			}
		}

		protected override RpcMethodInvocationException NewRpcException( RpcError rpcError, MessagePackObject unpackedException )
		{
			return new RpcMethodInvocationException( rpcError, unpackedException );
		}

		protected override System.Collections.Generic.IDictionary<string, object> GetTestArguments()
		{
			var result = base.GetTestArguments();
			result.Add( "MethodName", "TestMethod" );
			return result;
		}

		protected override System.Collections.Generic.IDictionary<string, object> GetTestArgumentsWithAllNullValue()
		{
			var result = base.GetTestArgumentsWithAllNullValue();
			result.Add( "MethodName", "Dummy" );
			return result;
		}

		protected override System.Collections.Generic.IDictionary<string, object> GetTestArgumentsWithDefaultValue()
		{
			var result = base.GetTestArgumentsWithDefaultValue();
			result.Add( "MethodName", "Dummy" );
			return result;
		}

		[Test]
		public void TestConstructor_String_AsIs()
		{
			var methodName = "TestMethod";
			var target = new RpcMethodInvocationException( this.DefaultError, methodName );
			Assert.That( target.MethodName, Is.EqualTo( methodName ) );
			Assert.That( target.Message, Is.EqualTo( this.DefaultMessage ) );
			Assert.That( target.RpcError, Is.EqualTo( this.DefaultError ) );
			Assert.That( target.DebugInformation, Is.Empty );
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestConstructor_StringStringStringException_MethodNameIsNull()
		{
			new RpcMethodInvocationException( this.DefaultError, null, null, null, null );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestConstructor_StringStringStringException_MethodNameIsEmpty()
		{
			new RpcMethodInvocationException( this.DefaultError, String.Empty, null, null, null );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestConstructor_StringStringStringException_MethodNameIsWhitespaceOnly()
		{
			new RpcMethodInvocationException( this.DefaultError, " ", null, null, null );
		}
	}
}
