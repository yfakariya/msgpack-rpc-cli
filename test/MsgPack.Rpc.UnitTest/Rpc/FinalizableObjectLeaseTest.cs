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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Threading;
using System.Runtime.CompilerServices;

namespace MsgPack.Rpc
{
	// TODO: NLiblet

	[TestFixture]
	public class FinalizableObjectLeaseTest
	{
		[Test]
		public void TestDispose()
		{
			int isDisposed = 0;
			var holder = new ResourceHolder();
			var resource = new Resource( holder );
			int hashCode = RuntimeHelpers.GetHashCode( resource );
			var target =
				new FinalizableObjectLease<Resource>(
					resource,
					x =>
					{
						Assert.That( RuntimeHelpers.GetHashCode( resource ), Is.EqualTo( hashCode ) );
						Interlocked.Increment( ref isDisposed );
					}
				);
			target.Dispose();

			Assert.That( isDisposed, Is.Not.EqualTo( 0 ) );
			Assert.That( resource.IsDisposed, Is.EqualTo( 0 ) );
		}

		[Test]
		public void TestFinalize()
		{
			var holder = new ResourceHolder();
			var resource = new Resource( holder );
			var closure = new Closure( resource );
			var target =
				new FinalizableObjectLease<Resource>(
					resource,
					closure.OnFinalize
				);
			var targetReference = new WeakReference( target );
			target = null;
			GC.Collect();
			GC.WaitForPendingFinalizers();

			Assert.That( targetReference.IsAlive, Is.False, "Should not be finalized." );
			Assert.That( closure.IsFinalized, Is.Not.EqualTo( 0 ) );
			Assert.That( holder.Resource, Is.Null );
		}

		private sealed class ResourceHolder : IDisposable
		{
			private int _isDisposed;
			public Resource Resource;

			public void ResurrectMe( Resource resource )
			{
				if ( Interlocked.CompareExchange( ref this._isDisposed, 0, 0 ) != 0 )
				{
					return;
				}

				this.Resource = resource;
				GC.ReRegisterForFinalize( resource );
			}

			~ResourceHolder()
			{
				this.Dispose( false );
			}

			public void Dispose()
			{
				this.Dispose( true );
				GC.SuppressFinalize( this );
			}

			private void Dispose( bool disposing )
			{
				Interlocked.Exchange( ref this._isDisposed, 1 );
			}
		}

		private sealed class Resource : IDisposable
		{
			public int IsFinalized;
			public int IsDisposed;
			private readonly ResourceHolder _holder;

			public Resource( ResourceHolder holder )
			{
				this._holder = holder;
			}

			~Resource()
			{
				Interlocked.Increment( ref this.IsFinalized );
				this._holder.ResurrectMe( this );
			}

			public void Dispose()
			{
				Interlocked.Increment( ref this.IsDisposed );
				GC.SuppressFinalize( this );
			}
		}

		private sealed class Closure
		{
			public int IsFinalized;
			private readonly int _hashCode;

			public Closure( Resource resource )
			{
				this._hashCode = resource == null ? 0 : RuntimeHelpers.GetHashCode( resource );
			}

			public void OnFinalize( Resource resource )
			{
				Assert.That( resource == null ? 0 : RuntimeHelpers.GetHashCode( resource ), Is.EqualTo( this._hashCode ) );
				Interlocked.Increment( ref this.IsFinalized );
			}
		}
	}
}
