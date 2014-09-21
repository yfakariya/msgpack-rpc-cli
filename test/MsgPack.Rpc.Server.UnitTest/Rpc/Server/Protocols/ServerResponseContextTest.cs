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
using System.Linq;
using MsgPack.Rpc.Protocols;
using MsgPack.Serialization;
using NUnit.Framework;

namespace MsgPack.Rpc.Server.Protocols
{
	[TestFixture()]
	public class ServerResponseContextTest
	{
		private static void TestCore( Action<ServerResponseContext, ServerTransport> test )
		{
			using ( var server = new RpcServer() )
			using ( var manager = new NullServerTransportManager( server ) )
			using ( var transport = new NullServerTransport( manager ) )
			using ( var target = new ServerResponseContext() )
			{
				target.SetTransport( transport );
				test( target, transport );
			}
		}

		[Test]
		public void TestSerialize_OnNullResult_ReturnValueDataIsNil_ErrorDataIsNil()
		{
			TestCore( ( target, transport ) =>
				{
					target.MessageId = 123;
					target.Serialize( default( object ), RpcErrorMessage.Success, null );

					Assert.That( Unpacking.UnpackObject( target.GetReturnValueData() ).Value.Equals( MessagePackObject.Nil ) );
					Assert.That( Unpacking.UnpackObject( target.GetErrorData() ).Value.Equals( MessagePackObject.Nil ) );
				}
			);
		}

		[Test()]
		public void TestSerialize_OnNotNullResult_ReturnValueDataIsSet_ErrorDataIsNil()
		{
			TestCore( ( target, transport ) =>
				{
					target.MessageId = 123;
					target.Serialize( "Test", RpcErrorMessage.Success, MessagePackSerializer.Get<string>() );

					Assert.That( Unpacking.UnpackObject( target.GetReturnValueData() ).Value.Equals( "Test" ) );
					Assert.That( Unpacking.UnpackObject( target.GetErrorData() ).Value.Equals( MessagePackObject.Nil ) );
				}
			);
		}

		[Test()]
		public void TestSerialize_OnError_ReturnValueDataIsSet_ErrorDataIsSet()
		{
			TestCore( ( target, transport ) =>
				{
					target.MessageId = 123;
					target.Serialize( default( object ), new RpcErrorMessage( RpcError.CallError, "Detail" ), null );

					Assert.That( Unpacking.UnpackObject( target.GetReturnValueData() ).Value.Equals( "Detail" ) );
					Assert.That( Unpacking.UnpackObject( target.GetErrorData() ).Value.Equals( RpcError.CallError.Identifier ) );
				}
			);
		}

		private static void AssertArraySegment( MessagePackObject expected, ArraySegment<byte> actual )
		{
			var result = Unpacking.UnpackObject( actual.AsEnumerable().ToArray() );
			Assert.That( result.ReadCount, Is.EqualTo( actual.Count ) );
			Assert.That( expected.Equals( result.Value ) );
		}

		[Test()]
		public void TestPrepare_SegmentsAreAllSet()
		{
			TestCore( ( target, transport ) =>
				{
					target.MessageId = 123;
					target.Serialize( default( object ), new RpcErrorMessage( RpcError.CallError, "Detail" ), null );
					target.Prepare( true );

					Assert.That( target.SendingBuffer.Length, Is.EqualTo( 4 ) );
					Assert.That( target.SendingBuffer[ 0 ].AsEnumerable().ToArray(), Is.EqualTo( new byte[] { 0x94, 0x1 } ) );
					AssertArraySegment( 123, target.SendingBuffer[ 1 ] );
					AssertArraySegment( RpcError.CallError.Identifier, target.SendingBuffer[ 2 ] );
					AssertArraySegment( "Detail", target.SendingBuffer[ 3 ] );
				}
			);
		}

		[Test()]
		public void TestClear()
		{
			TestCore( ( target, transport ) =>
				{
					target.MessageId = 123;
					target.Serialize( default( object ), new RpcErrorMessage( RpcError.CallError, "Detail" ), null );
					target.Prepare( true );
					target.SetCompletedSynchronously();

					target.Clear();

					Assert.That( target.ErrorDataPacker, Is.Not.Null );
					Assert.That( target.ReturnDataPacker, Is.Not.Null );
					Assert.That( target.GetReturnValueData(), Is.Not.Null.And.Empty );
					Assert.That( target.GetErrorData(), Is.Not.Null.And.Empty );
					Assert.That( target.MessageId, Is.Null );
					Assert.That( target.SendingBuffer[ 0 ].AsEnumerable().ToArray(), Is.EqualTo( new byte[] { 0x94, 0x1 } ) );
					Assert.That( target.SendingBuffer[ 1 ].AsEnumerable().ToArray(), Is.Empty );
					Assert.That( target.SendingBuffer[ 2 ].AsEnumerable().ToArray(), Is.Empty );
					Assert.That( target.SendingBuffer[ 3 ].AsEnumerable().ToArray(), Is.Empty );
					Assert.That( target.CompletedSynchronously, Is.False );
					Assert.That( target.BufferList, Is.Null );
				}
			);
		}
	}
}
