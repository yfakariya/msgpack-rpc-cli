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
	public class RpcTimeoutExceptionTest : RpcExceptionTestBase<RpcTimeoutException>
	{
		protected override RpcError DefaultError
		{
			get { return RpcError.TimeoutError; }
		}

		protected override RpcTimeoutException NewRpcException( ConstructorKind kind, System.Collections.Generic.IDictionary<string, object> properties )
		{
			switch ( kind )
			{
				case ConstructorKind.Serialization:
				case ConstructorKind.WithInnerException:
				{
					return new RpcTimeoutException( ( TimeSpan )( properties[ "ClientTimeout" ] ?? TimeSpan.Zero ), GetMessage( properties ), GetDebugInformation( properties ), GetInnerException( properties ) );
				}
				default:
				{
					return new RpcTimeoutException( ( TimeSpan )( properties[ "ClientTimeout" ] ?? TimeSpan.Zero ), GetMessage( properties ), GetDebugInformation( properties ) );
				}
			}
		}

		protected override RpcTimeoutException NewRpcException( RpcError rpcError, MessagePackObject unpackedException )
		{
			return new RpcTimeoutException( unpackedException );
		}

		protected override System.Collections.Generic.IDictionary<string, object> GetTestArguments()
		{
			var result = base.GetTestArguments();
			result.Add( "ClientTimeout", TimeSpan.FromSeconds( 123 ) );
			return result;
		}

		protected override System.Collections.Generic.IDictionary<string, object> GetTestArgumentsWithAllNullValue()
		{
			var result = base.GetTestArgumentsWithAllNullValue();
			result.Add( "ClientTimeout", null );
			return result;
		}

		protected override System.Collections.Generic.IDictionary<string, object> GetTestArgumentsWithDefaultValue()
		{
			var result = base.GetTestArgumentsWithDefaultValue();
			result.Add( "ClientTimeout", TimeSpan.Zero );
			return result;
		}

		[Test]
		public void TestConstructor_TimeSpan_AsIs()
		{
			var timeout = TimeSpan.FromSeconds( 123 );
			var target = new RpcTimeoutException( timeout );
			Assert.That( target.ClientTimeout, Is.EqualTo( timeout ) );
			Assert.That( target.Message, Is.EqualTo( this.DefaultMessage ) );
			Assert.That( target.RpcError, Is.EqualTo( this.DefaultError ) );
			Assert.That( target.DebugInformation, Is.Empty );
		}
	}
}
