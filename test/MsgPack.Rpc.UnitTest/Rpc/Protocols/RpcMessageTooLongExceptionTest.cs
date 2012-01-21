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
	public class RpcMessageTooLongExceptionTest : RpcExceptionTestBase<RpcMessageTooLongException>
	{
		protected override RpcError DefaultError
		{
			get { return RpcError.MessageTooLargeError; }
		}

		protected override RpcMessageTooLongException NewRpcException( ConstructorKind kind, System.Collections.Generic.IDictionary<string, object> properties )
		{
			switch ( kind )
			{
				case ConstructorKind.Serialization:
				case ConstructorKind.WithInnerException:
				{
					return new RpcMessageTooLongException( GetMessage( properties ), GetDebugInformation( properties ), GetInnerException( properties ) );
				}
				case ConstructorKind.Default:
				default:
				{
					return new RpcMessageTooLongException( GetMessage( properties ), GetDebugInformation( properties ) );
				}
			}
		}

		protected override RpcMessageTooLongException NewRpcException( RpcError rpcError, MessagePackObject unpackedException )
		{
			return new RpcMessageTooLongException( unpackedException );
		}

		[Test]
		public void TestDefaultConstructor_DefaultPropertyValuesSet()
		{
			var target = new RpcMessageTooLongException();
			Assert.That( target.RpcError, Is.EqualTo( RpcError.MessageTooLargeError ) );
			Assert.That( target.Message, Is.Not.Null.And.Not.Empty );
		}
	}
}
