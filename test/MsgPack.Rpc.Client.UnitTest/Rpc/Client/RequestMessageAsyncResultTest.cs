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
using MsgPack.Rpc.Client.Protocols;
using NUnit.Framework;

namespace MsgPack.Rpc.Client
{
	[TestFixture]
	public class RequestMessageAsyncResultTest
	{
		private static ClientResponseContext CreateContext()
		{
			var context = new ClientResponseContext();
			context.ErrorBuffer = new ByteArraySegmentStream( new ArraySegment<byte>[] { new ArraySegment<byte>( new byte[] { 0xC0 } ) } );
			context.ResultBuffer = new ByteArraySegmentStream( new ArraySegment<byte>[] { new ArraySegment<byte>( new byte[] { 0xC0 } ) } );
			return context;
		}

		[Test]
		public void TestOnCompleted_CallbackCalled()
		{
			bool isCallbacked = false;
			object owner = new object();
			var target = new RequestMessageAsyncResult( owner, 1, _ => isCallbacked = true, null );
			target.OnCompleted( CreateContext(), null, false );
			Assert.That( isCallbacked, Is.True );
		}

		[Test]
		public void TestOnCompleted_ContextIsSet()
		{
			object owner = new object();
			var target = new RequestMessageAsyncResult( owner, 1, null, null );
			var context = CreateContext();
			target.OnCompleted( context, null, false );
			Assert.That( target.Result, Is.Not.Null );
		}

		[Test]
		public void TestOnCompleted_ErrorIsSet()
		{
			object owner = new object();
			var target = new RequestMessageAsyncResult( owner, 1, null, null );
			var error = new Exception();
			target.OnCompleted( CreateContext(), error, false );
			Assert.That( target.Error, Is.SameAs( error ) );
		}

		[Test]
		public void TestOnCompleted_CompletedSynchronouly_False_SetFalse()
		{
			object owner = new object();
			var target = new RequestMessageAsyncResult( owner, 1, null, null );
			target.OnCompleted( CreateContext(), null, false );
			Assert.That( ( target as IAsyncResult ).CompletedSynchronously, Is.False );
		}

		[Test]
		public void TestOnCompleted_CompletedSynchronouly_True_SetTrue()
		{
			object owner = new object();
			var target = new RequestMessageAsyncResult( owner, 1, null, null );
			target.OnCompleted( CreateContext(), null, true );
			Assert.That( ( target as IAsyncResult ).CompletedSynchronously, Is.True );
		}
	}
}
