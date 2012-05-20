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
using NUnit.Framework;

namespace MsgPack.Rpc.Client.Protocols
{
	[TestFixture]
	public class ClientRequestContextTest
	{
		[Test]
		public void TestConstructor_AllPropertiesAreInitialized()
		{
			var target = new ClientRequestContext();

			Assert.That( target.ArgumentsPacker, Is.Not.Null );
			Assert.That( target.BoundTransport, Is.Null );
			Assert.That( target.MessageId, Is.Null );
			Assert.That( target.MessageType, Is.EqualTo( MessageType.Response ) );
			Assert.That( target.MethodName, Is.Null );
			Assert.That( target.NotificationCompletionCallback, Is.Null );
			Assert.That( target.RequestCompletionCallback, Is.Null );
			Assert.That( target.SendingBuffer, Is.Not.Null );
			Assert.That( target.SessionId, Is.EqualTo( 0 ) );
			Assert.That( target.SessionStartedAt, Is.EqualTo( default( DateTimeOffset ) ) );
		}
		
		[Test]
		public void TestSetTransport_NotNull_SetAsIs()
		{
			var target = new ClientRequestContext();
			using ( var manager = new NullClientTransportManager() )
			using ( var transport = new NullClientTransport( manager ) )
			{
				target.SetTransport( transport );
				Assert.That( target.BoundTransport, Is.SameAs( transport ) );
			}
		}

		[Test]
		public void TestSetRequest_NonNull_PropertiesAreSet()
		{
			int messageId = Environment.TickCount;
			string methodName = Guid.NewGuid().ToString();
			Action<ClientResponseContext, Exception, Boolean> completionCallback = ( _0, _1, _2 ) => { };

			var target = new ClientRequestContext();
			target.SetRequest( messageId, methodName, completionCallback );

			Assert.That( target.MessageType, Is.EqualTo( MessageType.Request ) );
			Assert.That( target.MessageId, Is.EqualTo( messageId ) );
			Assert.That( target.MethodName, Is.EqualTo( methodName ) );
			Assert.That( target.RequestCompletionCallback, Is.EqualTo( completionCallback ) );
			Assert.That( target.NotificationCompletionCallback, Is.Null );
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestSetRequest_MethodNameIsNull_Fail()
		{
			new ClientRequestContext().SetRequest( 0, null, ( _0, _1, _2 ) => { } );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestSetRequest_MethodNameIsEmpty_Fail()
		{
			new ClientRequestContext().SetRequest( 0, String.Empty, ( _0, _1, _2 ) => { } );
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestSetRequest_CompletionCallbackIsNull_Fail()
		{
			new ClientRequestContext().SetRequest( 0, "A", null );
		}

		[Test]
		public void TestSetNotification_NonNull_PropertiesAreSet()
		{
			string methodName = Guid.NewGuid().ToString();
			Action<Exception, Boolean> completionCallback = ( _0, _1 ) => { };

			var target = new ClientRequestContext();
			target.SetNotification( methodName, completionCallback );

			Assert.That( target.MessageType, Is.EqualTo( MessageType.Notification ) );
			Assert.That( target.MessageId, Is.Null );
			Assert.That( target.MethodName, Is.EqualTo( methodName ) );
			Assert.That( target.RequestCompletionCallback, Is.Null );
			Assert.That( target.NotificationCompletionCallback, Is.EqualTo( completionCallback ) );
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestSetNotification_MethodNameIsNull_Fail()
		{
			new ClientRequestContext().SetNotification( null, ( _0, _1 ) => { } );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestSetNotification_MethodNameIsEmpty_Fail()
		{
			new ClientRequestContext().SetNotification( String.Empty, ( _0, _1 ) => { } );
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestSetNotification_CompletionCallbackIsNull_Fail()
		{
			new ClientRequestContext().SetNotification( "A", null );
		}

		[Test]
		public void TestPrepare_SetAsRequest_SendingBufferAndSessionAreSet()
		{
			var target = new ClientRequestContext();
			target.SetRequest( 1, "A", ( _0, _1, _2 ) => { } );
			var oldSessionId = target.SessionId;

			target.Prepare( true );
			Assert.That( target.SendingBuffer.All( segment => segment.Array != null ), Is.Not.Null );
			Assert.That( target.BufferList, Is.SameAs( target.SendingBuffer ) );
		}

		[Test]
		public void TestPrepare_SetAsNotification_SendingBufferAndSessionAreSet()
		{
			var target = new ClientRequestContext();
			target.SetNotification( "A", ( _0, _1 ) => { } );
			var oldSessionId = target.SessionId;

			target.Prepare( true );
			Assert.That( target.SendingBuffer.All( segment => segment.Array != null ), Is.Not.Null );
			Assert.That( target.BufferList, Is.SameAs( target.SendingBuffer ) );
		}

		[Test]
		[ExpectedException( typeof( InvalidOperationException ) )]
		public void TestPrepare_SetXAreNotInvoked_Fail()
		{
			new ClientRequestContext().Prepare( true );
		}

		[Test]
		public void TestClearBuffer_BuffersAreClear()
		{
			int messageId = Environment.TickCount;
			string methodName = Guid.NewGuid().ToString();
			Action<ClientResponseContext, Exception, Boolean> completionCallback = ( _0, _1, _2 ) => { };

			var target = new ClientRequestContext();
			target.RenewSessionId();
			target.SetRequest( messageId, methodName, completionCallback );
			target.ArgumentsPacker.Pack( 1 );
			target.ClearBuffers();

			Assert.That( target.ArgumentsPacker, Is.Not.Null );
			Assert.That( target.ArgumentsPacker.Position, Is.EqualTo( 0 ) );
			Assert.That( target.BoundTransport, Is.Null );
			Assert.That( target.MessageId, Is.Not.Null );
			Assert.That( target.MessageType, Is.EqualTo( MessageType.Request ) );
			Assert.That( target.MethodName, Is.Not.Null );
			Assert.That( target.NotificationCompletionCallback, Is.Null );
			Assert.That( target.RequestCompletionCallback, Is.Not.Null );
			Assert.That( target.SendingBuffer.All( segment => segment.Array == null ) );
			Assert.That( target.SessionId, Is.Not.EqualTo( 0 ) );
			Assert.That( target.SessionStartedAt, Is.Not.EqualTo( default( DateTimeOffset ) ) );
		}

		[Test]
		public void TestClear_Initialized()
		{
			int messageId = Environment.TickCount;
			string methodName = Guid.NewGuid().ToString();
			Action<ClientResponseContext, Exception, Boolean> completionCallback = ( _0, _1, _2 ) => { };

			var target = new ClientRequestContext();
			target.RenewSessionId();
			target.SetRequest( messageId, methodName, completionCallback );
			target.ArgumentsPacker.Pack( 1 );
			target.Clear();

			Assert.That( target.ArgumentsPacker, Is.Not.Null );
			Assert.That( target.ArgumentsPacker.Position, Is.EqualTo( 0 ) );
			Assert.That( target.BoundTransport, Is.Null );
			Assert.That( target.MessageId, Is.Null );
			Assert.That( target.MessageType, Is.EqualTo( MessageType.Response ) );
			Assert.That( target.MethodName, Is.Null );
			Assert.That( target.NotificationCompletionCallback, Is.Null );
			Assert.That( target.RequestCompletionCallback, Is.Null );
			Assert.That( target.SendingBuffer.All( segment => segment.Array == null ) );
			Assert.That( target.SessionId, Is.EqualTo( 0 ) );
			Assert.That( target.SessionStartedAt, Is.EqualTo( default( DateTimeOffset ) ) );
		}
	}
}
