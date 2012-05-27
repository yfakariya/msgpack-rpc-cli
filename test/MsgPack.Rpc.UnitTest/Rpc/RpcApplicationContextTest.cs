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
using NUnit.Framework;

namespace MsgPack.Rpc
{
	[TestFixture]
	public class RpcApplicationContextTest
	{
		[Test]
		public void TestSoftTimeout_TokenCanceledAndIsCanceledToBeTrue()
		{
			var timeout = TimeSpan.FromMilliseconds( 50 );
			using ( var target = new RpcApplicationContext( timeout, null ) )
			{
				RpcApplicationContext.SetCurrent( target );
				target.StartTimeoutWatch();
				Assert.That( target.CancellationToken.WaitHandle.WaitOne( TimeSpan.FromSeconds( 1 ) ) );
				Assert.That( RpcApplicationContext.IsCanceled );
			}
		}

		[Test]
		public void TestSoftTimeout_ExceptionThrownOnStop()
		{
			var timeout = TimeSpan.FromMilliseconds( 50 );
			using ( var target = new RpcApplicationContext( timeout, null ) )
			using ( var cancellationEvent = new ManualResetEventSlim() )
			{
				var message = Guid.NewGuid().ToString();
				RpcApplicationContext.SetCurrent( target );
#pragma warning disable 0618
				target.DebugSoftTimeout += ( sender, e ) => cancellationEvent.Set();
#pragma warning restore 0618
				target.CancellationToken.Register( () =>
					{
						throw new Exception( message );
					}
				);
				target.StartTimeoutWatch();
				Assert.That( target.CancellationToken.WaitHandle.WaitOne( TimeSpan.FromSeconds( 1 ) ) );
				// Mono sets WaitHandle eagerly (CLR might set after callback)
				Assert.That( cancellationEvent.Wait( TimeSpan.FromSeconds( 1 ) ) );
				Assert.That( RpcApplicationContext.IsCanceled );
				var exception = Assert.Throws<AggregateException>( () => target.StopTimeoutWatch() );
				Assert.That( exception.InnerException.Message, Is.EqualTo( message ) );
			}
		}

		[Test]
		public void TestSoftTimeout_Stoped_Never()
		{
			var timeout = TimeSpan.FromMilliseconds( 50 );
			using ( var target = new RpcApplicationContext( timeout, null ) )
			{
				RpcApplicationContext.SetCurrent( target );
				target.StartTimeoutWatch();
				target.StopTimeoutWatch();
				Assert.That( target.CancellationToken.WaitHandle.WaitOne( TimeSpan.FromMilliseconds( timeout.TotalMilliseconds * 2 ) ), Is.False );
				Assert.That( RpcApplicationContext.IsCanceled, Is.False );
			}
		}

		[Test]
		public void TestHardTimeout_TokenCanceledAndIsCanceledToBeTrue()
		{
			var timeout = TimeSpan.FromMilliseconds( 20 );
			using ( var target = new RpcApplicationContext( timeout, timeout ) )
			{
				RpcApplicationContext.SetCurrent( target );
				target.StartTimeoutWatch();
				try
				{
					Thread.Sleep( TimeSpan.FromMilliseconds( timeout.TotalMilliseconds * 3 ) );
					Assert.Fail();
				}
				catch ( ThreadAbortException ex )
				{
					Assert.That( ex.ExceptionState, Is.EqualTo( RpcApplicationContext.HardTimeoutToken ) );
					Thread.ResetAbort();
				}

				Assert.That( RpcApplicationContext.IsCanceled );
			}
		}

		[Test]
		public void TestHardTimeout_Stopped_Never()
		{
			var timeout = TimeSpan.FromMilliseconds( 20 );
			using ( var waitHandle = new ManualResetEventSlim() )
			using ( var target = new RpcApplicationContext( timeout, timeout ) )
			{
				RpcApplicationContext.SetCurrent( target );
				target.StartTimeoutWatch();
				target.CancellationToken.Register( () => waitHandle.Set() );

				Assert.That( waitHandle.Wait( TimeSpan.FromSeconds( 1 ) ) );
				target.StopTimeoutWatch();
				Thread.Sleep( TimeSpan.FromMilliseconds( timeout.TotalMilliseconds * 2 ) );

				Assert.That( RpcApplicationContext.IsCanceled );
			}
		}

		[Test]
		public void TestTimeout_SoftTimeoutIsNull_NeverTimeout()
		{
			var timeout = TimeSpan.FromMilliseconds( 20 );
			using ( var waitHandle = new ManualResetEventSlim() )
			using ( var target = new RpcApplicationContext( null, timeout ) )
			{
				RpcApplicationContext.SetCurrent( target );
				target.StartTimeoutWatch();
				Assert.That( waitHandle.Wait( TimeSpan.FromMilliseconds( timeout.TotalMilliseconds * 2 ) ), Is.False );
				Assert.That( RpcApplicationContext.IsCanceled, Is.False );
			}
		}

		[Test]
		public void TestClear_CanReuse()
		{
			var timeout = TimeSpan.FromMilliseconds( 50 );
			using ( var target = new RpcApplicationContext( timeout, timeout ) )
			{
				for ( int i = 0; i < 2; i++ )
				{
					RpcApplicationContext.Clear();
					RpcApplicationContext.SetCurrent( target );
					target.StartTimeoutWatch();
					try
					{
						Thread.Sleep( TimeSpan.FromMilliseconds( timeout.TotalMilliseconds * 3 ) );
						Assert.Fail( "Attempt:{0}", i );
					}
					catch ( ThreadAbortException ex )
					{
						Assert.That( ex.ExceptionState, Is.EqualTo( RpcApplicationContext.HardTimeoutToken ) );
						Thread.ResetAbort();
					}

					Assert.That( RpcApplicationContext.IsCanceled );
				}
			}
		}
	}
}
