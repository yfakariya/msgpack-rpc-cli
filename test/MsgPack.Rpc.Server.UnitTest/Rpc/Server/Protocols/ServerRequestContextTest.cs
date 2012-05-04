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
using MsgPack.Rpc.Protocols;
using NUnit.Framework;

namespace MsgPack.Rpc.Server.Protocols
{
	[TestFixture()]
	public class ServerRequestContextTest
	{
		private static void TestCore( Action<ServerRequestContext, ServerTransport> test )
		{
			using ( var server = new RpcServer() )
			using ( var manager = new NullServerTransportManager( server ) )
			using ( var transport = new NullServerTransport( manager ) )
			using ( var target = new ServerRequestContext() )
			{
				test( target, transport );
			}
		}

		private static void InitializeBuffers( ServerRequestContext target )
		{
			target.ArgumentsBuffer.WriteByte( 1 );
			target.ArgumentsBufferPacker = Packer.Create( target.ArgumentsBuffer, false );
			target.ArgumentsBufferUnpacker = Unpacker.Create( target.ArgumentsBuffer, false );
			target.ArgumentsCount = 2;
			target.UnpackingBuffer = new ByteArraySegmentStream( target.ReceivedData );
			target.UnpackedArgumentsCount = 3;
			target.RootUnpacker = Unpacker.Create( target.UnpackingBuffer, false );
			target.HeaderUnpacker = Unpacker.Create( target.UnpackingBuffer, false );
			target.ArgumentsUnpacker = Unpacker.Create( target.UnpackingBuffer, false );
			target.SetCompletedSynchronously();
			target.MessageId = 123;
			target.MessageType = MessageType.Request;
			target.MethodName = "Method";
			target.NextProcess = _ => true;
		}

		[Test()]
		public void TestSetTransport_SetBoundTransportAndNextProcessBecomeUnpackRequestHeader()
		{
			TestCore( ( target, transport ) =>
				{
					target.SetTransport( transport );
					Assert.That( target.BoundTransport, Is.SameAs( transport ) );
					Assert.That( target.NextProcess, Is.EqualTo( new Func<ServerRequestContext, bool>( transport.UnpackRequestHeader ) ) );
				}
			);
		}

		[Test()]
		public void TestShiftCurrentReceivingBuffer_Intermediate_OffsetIsShift()
		{
			TestCore( ( target, _ ) =>
				{
					var oldBuffer = target.CurrentReceivingBuffer;
					target.SetBytesTransferred( 1 );
					target.ShiftCurrentReceivingBuffer();
					Assert.That( target.Offset, Is.EqualTo( 1 ) );
					Assert.That( target.CurrentReceivingBuffer, Is.SameAs( oldBuffer ) );
				}
			);
		}

		[Test()]
		public void TestShiftCurrentReceivingBuffer_Tail_OffsetIsZeroAndNewBuffer()
		{
			TestCore( ( target, _ ) =>
				{
					var oldBuffer = target.CurrentReceivingBuffer;
					target.SetBytesTransferred( oldBuffer.Length );
					target.ShiftCurrentReceivingBuffer();
					Assert.That( target.Offset, Is.EqualTo( 0 ) );
					Assert.That( target.CurrentReceivingBuffer, Is.Not.SameAs( oldBuffer ) );
				}
			);
		}

		[Test()]
		public void TestClear()
		{
			TestCore( ( target, _ ) =>
				{
					InitializeBuffers( target );
					target.SetReceivedData( new ArraySegment<byte>[] { new ArraySegment<byte>( new byte[] { 1 } ), new ArraySegment<byte>( new byte[] { 1, 2 } ) } );
					target.SetReceivingBuffer( new byte[] { 1 } );
					target.Clear();

					Assert.That( target.CurrentReceivingBuffer, Is.Not.Null.And.Not.Empty );
					Assert.That( target.ReceivedData, Is.Not.Null.And.Not.Empty );
					Assert.That( target.ArgumentsBuffer.Length, Is.EqualTo( 0 ) );
					Assert.That( target.ArgumentsBufferPacker, Is.Null );
					Assert.That( target.ArgumentsBufferUnpacker, Is.Null );
					Assert.That( target.ArgumentsCount, Is.EqualTo( 0 ) );
					Assert.That( target.ArgumentsUnpacker, Is.Null );
					Assert.That( target.CompletedSynchronously, Is.False );
					Assert.That( target.HeaderUnpacker, Is.Null );
					Assert.That( target.MessageId, Is.Null );
					Assert.That( target.MessageType, Is.EqualTo( MessageType.Response ) );
					Assert.That( target.MethodName, Is.Null );
					Assert.That( target.RootUnpacker, Is.Null );
					Assert.That( target.UnpackedArgumentsCount, Is.EqualTo( 0 ) );
					Assert.That( target.UnpackingBuffer, Is.Null );
				}
			);
		}

		[Test()]
		public void TestClearBuffers()
		{
			TestCore( ( target, _ ) =>
				{
					InitializeBuffers( target );
					target.SetReceivedData( new ArraySegment<byte>[] { new ArraySegment<byte>( new byte[] { 1 } ), new ArraySegment<byte>( new byte[] { 1, 2 } ) } );
					target.SetReceivingBuffer( new byte[] { 1 } );
					target.ClearBuffers();

					Assert.That( target.CurrentReceivingBuffer, Is.Not.Null.And.Not.Empty );
					Assert.That( target.ReceivedData, Is.Not.Null.And.Not.Empty );

					Assert.That( target.ArgumentsBuffer.Length, Is.Not.EqualTo( 0 ) );
					Assert.That( target.ArgumentsBufferPacker, Is.Null );
					Assert.That( target.ArgumentsBufferUnpacker, Is.Null );
					Assert.That( target.ArgumentsCount, Is.EqualTo( 0 ) );
					Assert.That( target.ArgumentsUnpacker, Is.Not.Null );
					Assert.That( target.HeaderUnpacker, Is.Null );
					Assert.That( target.MessageId, Is.Not.Null );
					Assert.That( target.MessageType, Is.EqualTo( MessageType.Request ) );
					Assert.That( target.MethodName, Is.Not.Null );
					Assert.That( target.RootUnpacker, Is.Null );
					Assert.That( target.UnpackedArgumentsCount, Is.EqualTo( 0 ) );
					Assert.That( target.UnpackingBuffer, Is.Not.Null );
				}
			);
		}

		[Test()]
		public void TestClearDispatchContext()
		{
			TestCore( ( target, _ ) =>
				{
					InitializeBuffers( target );
					target.SetReceivedData( new ArraySegment<byte>[] { new ArraySegment<byte>( new byte[] { 1 } ), new ArraySegment<byte>( new byte[] { 1, 2 } ) } );
					target.SetReceivingBuffer( new byte[] { 1 } );
					target.ClearDispatchContext();

					Assert.That( target.CurrentReceivingBuffer, Is.Not.Null.And.Not.Empty );
					Assert.That( target.ReceivedData, Is.Not.Null.And.Not.Empty );
					Assert.That( target.ArgumentsBuffer.Length, Is.EqualTo( 0 ) );
					Assert.That( target.ArgumentsBufferPacker, Is.Not.Null );
					Assert.That( target.ArgumentsBufferUnpacker, Is.Not.Null );
					Assert.That( target.ArgumentsCount, Is.Not.EqualTo( 0 ) );
					Assert.That( target.ArgumentsUnpacker, Is.Null );
					Assert.That( target.HeaderUnpacker, Is.Not.Null );
					Assert.That( target.MessageId, Is.Not.Null );
					Assert.That( target.MessageType, Is.EqualTo( MessageType.Response ) );
					Assert.That( target.MethodName, Is.Null );
					Assert.That( target.RootUnpacker, Is.Not.Null );
					Assert.That( target.UnpackedArgumentsCount, Is.Not.EqualTo( 0 ) );
					Assert.That( target.UnpackingBuffer, Is.Not.Null );
				}
			);
		}
	}
}
