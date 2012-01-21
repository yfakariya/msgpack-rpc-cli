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
	public class RpcTransportExceptionTest : RpcExceptionTestBase<RpcTransportException>
	{
		protected override RpcError DefaultError
		{
			get { return RpcError.TransportError; }
		}

		protected override RpcTransportException NewRpcException( ConstructorKind kind, System.Collections.Generic.IDictionary<string, object> properties )
		{
			switch ( kind )
			{
				case ConstructorKind.Serialization:
				case ConstructorKind.WithInnerException:
				{
					return new RpcTransportException( GetRpcError( properties ), GetMessage( properties ), GetDebugInformation( properties ), GetInnerException( properties ) );
				}
				case ConstructorKind.Default:
				default:
				{
					return new RpcTransportException( GetRpcError( properties ), GetMessage( properties ), GetDebugInformation( properties ) );
				}
			}
		}

		protected override RpcTransportException NewRpcException( RpcError rpcError, MessagePackObject unpackedException )
		{
			return new RpcTransportException( rpcError, unpackedException );
		}

		[Test]
		public void TestConstructor_RpcError_NotNull_DefaultValuesSet()
		{
			var error = RpcError.ConnectionRefusedError;
			var target = new RpcTransportException( error );

			Assert.That( target.RpcError, Is.SameAs( error ) );
			Assert.That( target.Message, Is.Not.Null.And.Not.Empty );
		}

		[Test]
		public void TestConstructor_RpcError_Null_DefaultValuesSet()
		{
			var target = new RpcTransportException( null );

			Assert.That( target.RpcError, Is.EqualTo( RpcError.TransportError ) );
			Assert.That( target.Message, Is.Not.Null.And.Not.Empty );
		}
	}
}
