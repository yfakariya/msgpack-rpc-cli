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
	// TODO: NLiblet
	[TestFixture]
	public class LeaseableTest
	{
		[Test]
		public void TestSetLease_First_Success()
		{
			var target = new TestTarget();
			target.SetLease( new ForgettableObjectLease<TestTarget>( target ) );
			Assert.That( target.IsLeased );
		}

		[Test]
		public void TestSetLease_Duplicate_Fail()
		{
			var target = new TestTarget();
			target.SetLease( new ForgettableObjectLease<TestTarget>( target ) );
			Assert.That( target.IsLeased );
		}

		[Test]
		public void TestDispose_UnsetLease()
		{
			var closure = new Closure();
			var target = new TestTarget();
			target.SetLease(
				new CallbackLease<TestTarget>(
					target,
					closure.OnDispose
				)
			);
			target.Dispose();
			Assert.That( target.IsLeased, Is.False );
			Assert.That( closure.IsDisposed, Is.Not.EqualTo( 0 ) );
			Assert.That( closure.IsFinalized, Is.EqualTo( 0 ) );
		}

		[Test]
		public void TestFinalize_UnsetLease()
		{
			var closure = new Closure();
			var target = new TestTarget();
			var lease =
				new CallbackLease<TestTarget>(
					target,
					closure.OnDispose
				);
			target.SetLease( lease );
			var targetReference = new WeakReference( target );
			var leaseReference = new WeakReference( lease );
			target = null;
			lease = null;
			GC.Collect();
			GC.WaitForPendingFinalizers();

			Assert.That( targetReference.IsAlive, Is.False, "Finalizer should not be invoked." );
			Assert.That( leaseReference.IsAlive, Is.False, "Finalizer should not be invoked." );
			Assert.That( closure.IsDisposed, Is.Not.EqualTo( 0 ) );
			Assert.That( closure.IsFinalized, Is.Not.EqualTo( 0 ) );
		}

		[Test]
		public void TestSetLeaseAndDispose_Twise_Success()
		{
			var target = new TestTarget();
			target.SetLease( new ForgettableObjectLease<TestTarget>( target ) );
			Assert.That( target.IsLeased, Is.True );
			target.Dispose();
			Assert.That( target.IsLeased, Is.False );
			target.SetLease( new ForgettableObjectLease<TestTarget>( target ) );
			Assert.That( target.IsLeased, Is.True );
		}

		private sealed class TestTarget : Leaseable, ILeaseable<TestTarget>
		{
			public TestTarget() { }

			public void SetLease( ILease<TestTarget> lease )
			{
				base.SetLease( lease );
			}
		}

		private sealed class Closure
		{
			public int IsDisposed;
			public int IsFinalized;

			public void OnDispose( bool disposing )
			{
				Interlocked.Increment( ref this.IsDisposed );
				if ( !disposing )
				{
					Interlocked.Increment( ref this.IsFinalized );
				}
			}
		}
	}
}