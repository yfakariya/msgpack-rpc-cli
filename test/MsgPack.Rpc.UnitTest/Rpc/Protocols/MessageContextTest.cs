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
	[TestFixture()]
	public class MessageContextTest
	{
		[Test()]
		public void TestConstructor_InitialValuesAreSet()
		{
			var target = new Target();
			AssertAreInitialzed( target );
		}

		private static void AssertAreInitialzed( Target target )
		{
			Assert.That( target.CompletedSynchronously, Is.False );
			Assert.That( target.MessageId, Is.Null );
			Assert.That( target.SessionId, Is.EqualTo( 0L ) );
			Assert.That( target.SessionStartedAt, Is.EqualTo( default( DateTimeOffset ) ) );
		}

		[Test()]
		public void TestSetCompletedSynchronously()
		{
			var target = new Target();
			target.SetCompletedSynchronously();

			Assert.That( target.CompletedSynchronously, Is.True );
		}
		
		[Test()]
		public void TestSetTransport()
		{
			var transport = new Transport();
			var target = new Target();
			target.SetTransport( transport );
			Assert.That( target.BoundTransport, Is.SameAs( transport ) );
		}

		[Test()]
		public void TestRenewSessionId()
		{
			var target = new Target();
			target.RenewSessionId();

			Assert.That( target.SessionId, Is.GreaterThan( 0L ) );
			Assert.That( target.SessionStartedAt, Is.GreaterThan( DateTimeOffset.UtcNow.Subtract( TimeSpan.FromDays( 1 ) ) ) );

			long old = target.SessionId;
			target.RenewSessionId();

			Assert.That( target.SessionId, Is.GreaterThan( old ) );
			Assert.That( target.SessionStartedAt, Is.GreaterThan( DateTimeOffset.UtcNow.Subtract( TimeSpan.FromDays( 1 ) ) ) );
		}

		[Test()]
		public void TestClear()
		{
			var target = new Target();
			target.RenewSessionId();
			target.SetTransport( new Transport() );
			target.MessageId = 123;
			target.SetCompletedSynchronously();

			target.Clear();

			AssertAreInitialzed( target );
		}

		private sealed class Target : MessageContext
		{
			public Target() : base() { }
		}

		private sealed class Transport : IContextBoundableTransport
		{
			public System.Net.Sockets.Socket BoundSocket
			{
				get { return null; }
			}

			public void OnSocketOperationCompleted( object sender, System.Net.Sockets.SocketAsyncEventArgs e ) { }
		}
	}
}
