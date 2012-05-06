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
	[Timeout( 3000 )]
	public class TimeoutWatcherTest
	{
		// Tweak this value for your machine.
		private static readonly TimeSpan _timeout = TimeSpan.FromMilliseconds( 30 );

		[Test]
		public void TestConstructor_IsTimeoutFalse()
		{
			using ( var target = new TimeoutWatcher() )
			{
				Assert.That( target.IsTimeout, Is.False );
			}
		}

		[Test]
		public void TestStart_Timeout_TimeoutEventOccurred_IsTimeoutToBeTrue()
		{
			using ( var target = new TimeoutWatcher() )
			using ( var waitHandle = new ManualResetEventSlim() )
			{
				target.Timeout += ( sender, e ) => waitHandle.Set();
				target.Start( _timeout );
				Assert.That( waitHandle.Wait( TimeSpan.FromSeconds( 1 ) ) );
				Assert.That( target.IsTimeout );
			}
		}

		[Test]
		public void TestStart_StoppedTimeout_TimeoutEventNotOccurred_IsTimeoutToBeFalse()
		{
			using ( var target = new TimeoutWatcher() )
			using ( var waitHandle = new ManualResetEventSlim() )
			{
				target.Timeout += ( sender, e ) => waitHandle.Set();
				target.Start( _timeout );
				target.Stop();
				Assert.That( waitHandle.Wait( ( int )( _timeout.TotalMilliseconds * 2 ) ), Is.False );
				Assert.That( target.IsTimeout, Is.False );
			}
		}

		[Test]
		public void TestStop_Twise_Harmless()
		{
			using ( var target = new TimeoutWatcher() )
			using ( var waitHandle = new ManualResetEventSlim() )
			{
				target.Timeout += ( sender, e ) => waitHandle.Set();
				target.Start( _timeout );
				target.Stop();
				Assert.That( waitHandle.Wait( ( int )( _timeout.TotalMilliseconds * 2 ) ), Is.False );
				target.Stop();
				Assert.That( target.IsTimeout, Is.False );
			}
		}

		[Test]
		public void TestStop_AfterTimeout_Harmless()
		{
			using ( var target = new TimeoutWatcher() )
			using ( var waitHandle = new ManualResetEventSlim() )
			{
				target.Timeout += ( sender, e ) => waitHandle.Set();
				target.Start( _timeout );
				Assert.That( waitHandle.Wait( TimeSpan.FromSeconds( 1 ) ) );
				target.Stop();
				Assert.That( target.IsTimeout, Is.True );
			}
		}

		[Test]
		public void TestReset_NotTimeout_Harmless()
		{
			using ( var target = new TimeoutWatcher() )
			using ( var waitHandle = new ManualResetEventSlim() )
			{
				target.Timeout += ( sender, e ) => waitHandle.Set();
				target.Start( _timeout );
				target.Stop();
				Assert.That( waitHandle.Wait( ( int )( _timeout.TotalMilliseconds * 2 ) ), Is.False );
				target.Reset();
				Assert.That( target.IsTimeout, Is.False );
			}
		}

		[Test]
		public void TestReset_Timeout_IsTimeoutToBeFalse()
		{
			using ( var target = new TimeoutWatcher() )
			using ( var waitHandle = new ManualResetEventSlim() )
			{
				target.Timeout += ( sender, e ) => waitHandle.Set();
				target.Start( _timeout );
				Assert.That( waitHandle.Wait( TimeSpan.FromSeconds( 1 ) ) );
				Assert.That( target.IsTimeout, Is.True );
				target.Reset();
				Assert.That( target.IsTimeout, Is.False );
			}
		}

		[Test]
		public void TestReset_CanStartAgain()
		{
			using ( var target = new TimeoutWatcher() )
			using ( var waitHandle = new ManualResetEventSlim() )
			{
				target.Timeout += ( sender, e ) => waitHandle.Set();
				for ( int i = 0; i < 2; i++ )
				{
					Assert.That( target.IsTimeout, Is.False, "Attempt: {0}", i );
					target.Start( _timeout );
					Assert.That( waitHandle.Wait( TimeSpan.FromSeconds( 1 ) ), "Attempt: {0}", i );
					Assert.That( target.IsTimeout, Is.True, "Attempt: {0}", i );
					target.Reset();
					waitHandle.Reset();
				}
			}
		}
	}
}
