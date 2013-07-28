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
using System.Collections.Generic;
using System.IO;

namespace MsgPack.Rpc.Client.Protocols
{
	[TestFixture]
	public class ClientResponseContextTest
	{
		[Test]
		public void TestConstructor_AllPropertiesAreInitialized()
		{
			var target = new ClientResponseContext();

			Assert.That( target.BoundTransport, Is.Null );
			Assert.That( target.CurrentReceivingBuffer, Is.Not.Null );
			Assert.That( target.CurrentReceivingBufferOffset, Is.EqualTo( 0 ) );
			Assert.That( target.ErrorBuffer, Is.Null );
			Assert.That( target.ErrorStartAt, Is.EqualTo( -1 ) );
			Assert.That( target.HeaderUnpacker, Is.Null );
			Assert.That( target.MessageId, Is.Null );
			Assert.That( target.NextProcess, Is.Null );
			Assert.That( target.ReceivedData, Is.Not.Null );
			Assert.That( target.ResultBuffer, Is.Null );
			Assert.That( target.ResultStartAt, Is.EqualTo( -1 ) );
			Assert.That( target.RootUnpacker, Is.Null );
			Assert.That( target.SessionId, Is.EqualTo( 0 ) );
			Assert.That( target.SessionStartedAt, Is.EqualTo( default( DateTimeOffset ) ) );
			Assert.That( target.UnpackingBuffer, Is.Null );
		}

		[Test]
		public void TestSetTransport_NotNull_SetAsIs()
		{
			var target = new ClientResponseContext();
			using ( var manager = new NullClientTransportManager() )
			using ( var transport = new NullClientTransport( manager ) )
			{
				target.SetTransport( transport );
				Assert.That( target.BoundTransport, Is.SameAs( transport ) );
				Assert.That( target.NextProcess, Is.EqualTo( new Func<ClientResponseContext, bool>( transport.UnpackResponseHeader ) ) );
			}
		}

		[Test]
		public void TestShiftCurrentReceivingBuffer_LessThanRemains_CurrentReceivingBufferOffsetIsShifttedAndReceivedDataIsAppended()
		{
			int bytesTransferred = 13;
			var target = new ClientResponseContext();
			target.SetBytesTransferred( bytesTransferred );
			target.ShiftCurrentReceivingBuffer();
			Assert.That( target.CurrentReceivingBufferOffset, Is.EqualTo( bytesTransferred ) );
			Assert.That( target.ReceivedData.Count, Is.EqualTo( 1 ) );
			Assert.That( target.ReceivedData[ 0 ].Array, Is.EqualTo( target.CurrentReceivingBuffer ) );
			Assert.That( target.ReceivedData[ 0 ].Offset, Is.EqualTo( 0 ) );
			Assert.That( target.ReceivedData[ 0 ].Count, Is.EqualTo( bytesTransferred ) );
		}

		[Test]
		public void TestShiftCurrentReceivingBuffer_EqualToRemains_CurrentReceivingBufferOffsetIsInitializedAndBufferIsSwappedAndReceivedDataIsAppended()
		{
			int bytesTransferred = 13;
			var target = new ClientResponseContext();
			var oldBuffer = target.CurrentReceivingBuffer;

			target.SetBytesTransferred( bytesTransferred );
			target.ShiftCurrentReceivingBuffer();

			target.SetBytesTransferred( oldBuffer.Length - bytesTransferred );
			target.ShiftCurrentReceivingBuffer();
			Assert.That( target.CurrentReceivingBuffer, Is.Not.SameAs( oldBuffer ) );
			Assert.That( target.CurrentReceivingBufferOffset, Is.EqualTo( 0 ) );
			Assert.That( target.ReceivedData.Count, Is.EqualTo( 2 ) );
			Assert.That( target.ReceivedData[ 1 ].Array, Is.EqualTo( oldBuffer ) );
			Assert.That( target.ReceivedData[ 1 ].Offset, Is.EqualTo( bytesTransferred ) );
			Assert.That( target.ReceivedData[ 1 ].Count, Is.EqualTo( oldBuffer.Length - bytesTransferred ) );
		}

		[Test]
		public void TestShiftCurrentReceivingBuffer_GratorThanRemains_CurrentReceivingBufferOffsetIsRecountedAndBufferIsSwappedAndReceivedDataIsAppended()
		{
			int bytesTransferred = 13;
			var target = new ClientResponseContext();
			var oldBuffer = target.CurrentReceivingBuffer;

			target.SetBytesTransferred( bytesTransferred );
			target.ShiftCurrentReceivingBuffer();

			target.SetBytesTransferred( oldBuffer.Length - bytesTransferred );
			target.ShiftCurrentReceivingBuffer();

			Assert.That( target.CurrentReceivingBuffer, Is.Not.SameAs( oldBuffer ) );
			Assert.That( target.CurrentReceivingBufferOffset, Is.EqualTo( 0 ) );
			Assert.That( target.ReceivedData.Count, Is.EqualTo( 2 ) );
			Assert.That( target.ReceivedData[ 1 ].Array, Is.EqualTo( oldBuffer ) );
			Assert.That( target.ReceivedData[ 1 ].Offset, Is.EqualTo( bytesTransferred ) );
			Assert.That( target.ReceivedData[ 1 ].Count, Is.EqualTo( oldBuffer.Length - bytesTransferred ) );
		}

		private static void MakeBufferDirty( ClientResponseContext target )
		{
			target.SetTransport( new DummyClientTransport() );
			target.SetReceivingBuffer( new byte[] { 1, 2, 3, 4 } );
			target.SetBytesTransferred( 1 );
			target.ShiftCurrentReceivingBuffer();
			target.ErrorBuffer = new ByteArraySegmentStream( CreateDirtyBytes() );
			target.ErrorStartAt = 1;
			target.HeaderUnpacker = Unpacker.Create( new MemoryStream() );
			target.MessageId = 1;
			target.NextProcess = _ => true;
			target.ReceivedData.Add( new ArraySegment<byte>( new byte[] { 1, 2, 3, 4 } ) );
			target.ResultBuffer = new ByteArraySegmentStream( CreateDirtyBytes() );
			target.ResultStartAt = 2;
			target.RenewSessionId();
			target.UnpackingBuffer = new ByteArraySegmentStream( CreateDirtyBytes() );
		}

		private static IList<ArraySegment<byte>> CreateDirtyBytes()
		{
			return new ArraySegment<byte>[] { new ArraySegment<byte>( new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 } ) };
		}

		[Test()]
		public void TestClear()
		{
			var target = new ClientResponseContext();
			MakeBufferDirty( target );
			target.Clear();

			Assert.That( target.BoundTransport, Is.Not.Null );
			Assert.That( target.CurrentReceivingBuffer, Is.Not.Null );
			Assert.That( target.CurrentReceivingBufferOffset, Is.Not.EqualTo( 0 ) );
			Assert.That( target.ErrorBuffer, Is.Null );
			Assert.That( target.ErrorStartAt, Is.EqualTo( -1 ) );
			Assert.That( target.HeaderUnpacker, Is.Null );
			Assert.That( target.MessageId, Is.Null );
			Assert.That( target.NextProcess, Is.Not.Null );
			Assert.That( target.ReceivedData, Is.Not.Null );
			Assert.That( target.ResultBuffer, Is.Null );
			Assert.That( target.ResultStartAt, Is.EqualTo( -1 ) );
			Assert.That( target.RootUnpacker, Is.Null );
			Assert.That( target.SessionId, Is.EqualTo( 0 ) );
			Assert.That( target.SessionStartedAt, Is.EqualTo( default( DateTimeOffset ) ) );
			Assert.That( target.UnpackingBuffer, Is.Null );
		}

		[Test()]
		public void TestClearBuffers()
		{
			var target = new ClientResponseContext();
			MakeBufferDirty( target );

			var length = target.UnpackingBuffer.Length;
			target.UnpackingBuffer.Position++;
			target.ClearBuffers();

			Assert.That( target.BoundTransport, Is.Not.Null );
			Assert.That( target.CurrentReceivingBuffer, Is.Not.Null );
			Assert.That( target.CurrentReceivingBufferOffset, Is.Not.EqualTo( 0 ) );
			Assert.That( target.ErrorBuffer, Is.Null );
			Assert.That( target.ErrorStartAt, Is.EqualTo( -1 ) );
			Assert.That( target.HeaderUnpacker, Is.Null );
			Assert.That( target.MessageId, Is.Not.Null );
			Assert.That( target.NextProcess, Is.Not.Null );
			Assert.That( target.ReceivedData, Is.Not.Null );
			Assert.That( target.ResultBuffer, Is.Null );
			Assert.That( target.ResultStartAt, Is.EqualTo( -1 ) );
			Assert.That( target.RootUnpacker, Is.Null );
			Assert.That( target.SessionId, Is.Not.EqualTo( 0 ) );
			Assert.That( target.SessionStartedAt, Is.Not.EqualTo( default( DateTimeOffset ) ) );
			Assert.That( target.UnpackingBuffer, Is.Not.Null );
			Assert.That( target.UnpackingBuffer.Length, Is.LessThan( length ) );
		}

		private sealed class DummyClientTransport : ClientTransport
		{
			protected override bool CanResumeReceiving
			{
				get { return true; }
			}

			public DummyClientTransport() : base( new DummyClientTransportManager() ) { }
			protected override void SendCore( ClientRequestContext context )
			{
				throw new NotImplementedException();
			}

			protected override void ReceiveCore( ClientResponseContext context )
			{
				throw new NotImplementedException();
			}

			private sealed class DummyClientTransportManager : ClientTransportManager<DummyClientTransport>
			{
				public DummyClientTransportManager() : base( new RpcClientConfiguration() ) { }

				protected override System.Threading.Tasks.Task<ClientTransport> ConnectAsyncCore( System.Net.EndPoint targetEndPoint )
				{
					throw new NotImplementedException();
				}
			}
		}
	}
}
