#region -- License Terms --
//
// MessagePack for CLI
//
// Copyright (C) 2010-2013 FUJIWARA, Yusuke
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
	public class UnexpcetedRpcExceptionTest
	{
		[Test]
		public void TestConstructor_AsIs()
		{
			var error = new MessagePackObject( Guid.NewGuid().ToString() );
			var errorDetail = new MessagePackObject( Guid.NewGuid().ToString() );

			var target = new UnexpcetedRpcException( error, errorDetail );

			Assert.That( target.Message, Is.EqualTo( RpcError.Unexpected.DefaultMessage ) );
			Assert.That( target.RpcError, Is.EqualTo( RpcError.Unexpected ) );
			Assert.That( target.Error, Is.EqualTo( error ) );
			Assert.That( target.ErrorDetail, Is.EqualTo( errorDetail ) );
		}

		[Test]
		public void TestSerialization_PropertiesSupplied_DeserializedNormally()
		{
			new GeneralUnexpcetedRpcExceptionTest().TestSerialization_PropertiesSupplied_DeserializedNormally();
		}

		[Test]
		public void TestSerialization_PropertiesAreNotSupplied_DeserializedNormally()
		{
			new GeneralUnexpcetedRpcExceptionTest().TestSerialization_PropertiesAreNotSupplied_DeserializedNormally();
		}

		[Explicit]
		private sealed class GeneralUnexpcetedRpcExceptionTest : RpcExceptionTestBase<UnexpcetedRpcException>
		{
			protected override RpcError DefaultError
			{
				get { return RpcError.Unexpected; }
			}

			protected override System.Collections.Generic.IDictionary<string, object> GetTestArguments()
			{
				var result = base.GetTestArguments();
				result[ "Message" ] = this.DefaultMessage;
				result[ "DebugInformation" ] = String.Empty;
				result[ "InnerException" ] = null;
				result.Add( "Error", new MessagePackObject( Guid.NewGuid().ToString() ) );
				result.Add( "ErrorDetail", new MessagePackObject( Guid.NewGuid().ToString() ) );
				return result;
			}

			protected override System.Collections.Generic.IDictionary<string, object> GetTestArgumentsWithAllNullValue()
			{
				var result = base.GetTestArgumentsWithAllNullValue();
				result.Add( "Error", MessagePackObject.Nil );
				result.Add( "ErrorDetail", MessagePackObject.Nil );
				return result;
			}

			protected override System.Collections.Generic.IDictionary<string, object> GetTestArgumentsWithDefaultValue()
			{
				var result = base.GetTestArgumentsWithDefaultValue();
				result.Add( "Error", MessagePackObject.Nil );
				result.Add( "ErrorDetail", MessagePackObject.Nil );
				return result;
			}

			protected override UnexpcetedRpcException NewRpcException( ConstructorKind kind, System.Collections.Generic.IDictionary<string, object> properties )
			{
				return new UnexpcetedRpcException( ( MessagePackObject )properties[ "Error" ], ( MessagePackObject )properties[ "ErrorDetail" ] );
			}

			protected override UnexpcetedRpcException NewRpcException( RpcError rpcError, MessagePackObject unpackedException )
			{
				Assert.Ignore( "UnexpcetedRpcException does not handle this behavior in the first place." );
				return null;
			}

			protected override void AssertProperties( UnexpcetedRpcException target, ConstructorKind kind, System.Collections.Generic.IDictionary<string, object> properties )
			{
				// Not call base.AssertProperties bevause UnexpcetedRpcException does not use general properties.
				Assert.That( target.Error, Is.EqualTo( ( MessagePackObject )properties[ "Error" ] ) );
				Assert.That( target.ErrorDetail, Is.EqualTo( ( MessagePackObject )properties[ "ErrorDetail" ] ) );
			}
		}
	}
}
