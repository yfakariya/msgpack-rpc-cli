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
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MsgPack.Rpc.Client
{
	[TestFixture]
	public class AsyncResultTest
	{
		private static void TestCompletionCore( bool expectedIsCompletedSynchronously, Action<AsyncResult> test, Action<AsyncResult> assertion )
		{
			using ( var waitHandle = new ManualResetEventSlim() )
			{
				object owner = new object();
				var target =
					new Target(
						owner,
						null,
						null
					);
				using ( var task =
					Task.Factory.StartNew( () =>
						{
							test( target );
						}
					) )
				{
					target.WaitForCompletion();
					Assert.That( target.IsCompleted, Is.True );
					Assert.That( target.IsFinished, Is.False );
					Assert.That( ( target as IAsyncResult ).CompletedSynchronously, Is.EqualTo( expectedIsCompletedSynchronously ) );

					// NOTE: Invocation of AsyncCallback is derived class responsibility.

					if ( assertion != null )
					{
						assertion( target );
					}

					task.Wait( 10 );
				}
			}
		}

		[Test]
		public void TestConstructor_OwnerAndAsyncCallbackAndAsyncStateAreSet()
		{
			object owner = new object();
			object state = new object();
			AsyncCallback callback = _ => { };
			var target =
				new Target(
					owner,
					callback,
					state
				);
			Assert.That( target.Owner, Is.SameAs( owner ) );
			Assert.That( target.AsyncCallback, Is.EqualTo( callback ) );
			Assert.That( target.AsyncState, Is.SameAs( state ) );
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestConstructor_OwnerIsNull_Fail()
		{
			var target =
				new Target(
					null,
					_ => { },
					new object()
				);
		}

		[Test]
		public void TestConstructor_AsyncCallbackIsNull_AsIs()
		{
			object owner = new object();
			object state = new object();
			AsyncCallback callback = null;
			var target =
				new Target(
					owner,
					callback,
					state
				);
			Assert.That( target.Owner, Is.SameAs( owner ) );
			Assert.That( target.AsyncCallback, Is.EqualTo( callback ) );
			Assert.That( target.AsyncState, Is.SameAs( state ) );
		}

		[Test]
		public void TestConstructor_AsyncStateIsNull_AsIs()
		{
			object owner = new object();
			object state = null;
			AsyncCallback callback = _ => { };
			var target =
				new Target(
					owner,
					callback,
					state
				);
			Assert.That( target.Owner, Is.SameAs( owner ) );
			Assert.That( target.AsyncCallback, Is.EqualTo( callback ) );
			Assert.That( target.AsyncState, Is.SameAs( state ) );
		}

		[Test]
		public void TestComplete_IsCompletedSynchronously_CompleteWithCompletedSynchronouslyIsTrue()
		{
			TestCompletionCore( true, target => target.Complete( true ), _ => { } );
		}

		[Test]
		public void TestComplete_IsNotCompletedSynchronously_CompleteWithCompletedSynchronouslyIsFalse()
		{
			TestCompletionCore( false, target => target.Complete( false ), _ => { } );
		}

		[Test]
		public void TestOnError_IsCompletedSynchronously_ErrorWithCompletedSynchronouslyIsTrue()
		{
			var error = new Exception();
			TestCompletionCore( true, target => target.OnError( error, true ), target => Assert.That( target.Error, Is.SameAs( error ) ) );
		}

		[Test]
		public void TestOnError_IsNotCompletedSynchronously_CompleteWithCompletedSynchronouslyIsFalse()
		{
			var error = new Exception();
			TestCompletionCore( false, target => target.OnError( error, false ), target => Assert.That( target.Error, Is.SameAs( error ) ) );
		}

		[Test]
		public void TestFinish_Disposed()
		{
			var target = new Target( new object(), null, null );
			// NOTE: Finish might reset property value itself, so cache to local to test.
			var waitHandle = target.AsyncWaitHandle;
			waitHandle.WaitOne( 0 );
			target.Complete( false );
			target.Finish();
			Assert.That( waitHandle.SafeWaitHandle.IsClosed );
		}

		private sealed class Target : AsyncResult
		{
			public Target( object owner, AsyncCallback callback, object state ) : base( owner, callback, state ) { }
		}
	}
}
