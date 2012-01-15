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
using System.Threading;
using NUnit.Framework;

namespace MsgPack.Rpc
{
	[TestFixture]
	public class StandardObjectPoolTest
	{
		[Test]
		public void TestBorrow_CanUpToMaximum()
		{
			const int maximum = 3;
			using ( var target =
				new StandardObjectPool<PooledObject>(
					() => new PooledObject(),
					new ObjectPoolConfiguration()
					{
						BorrowTimeout = TimeSpan.FromSeconds( 3 ),
						EvitionInterval = null,
						ExhausionPolicy = ExhausionPolicy.ThrowException,
						MaximumPooled = maximum,
						MinimumReserved = 1
					}
				) )
			{
				var result = new List<PooledObject>();
				try
				{
					for ( int i = 0; i < maximum; i++ )
					{
						result.Add( target.Borrow() );
					}
				}
				finally
				{
					result.DisposeAll();
				}
			}
		}

		[Test]
		[ExpectedException( typeof( TimeoutException ) )]
		public void TestBorrow_BlockUntilAvailable_Exceeds_Timeout()
		{
			const int maximum = 1;
			using ( var target =
				new StandardObjectPool<PooledObject>(
					() => new PooledObject(),
					new ObjectPoolConfiguration()
					{
						BorrowTimeout = TimeSpan.FromTicks( 1 ),
						EvitionInterval = null,
						ExhausionPolicy = ExhausionPolicy.BlockUntilAvailable,
						MaximumPooled = maximum,
						MinimumReserved = 1
					}
				) )
			{
				var result = new List<PooledObject>();
				try
				{
					for ( int i = 0; i < maximum + 1; i++ )
					{
						result.Add( target.Borrow() );
					}
				}
				finally
				{
					result.DisposeAll();
				}
			}
		}

		[Test]
		[ExpectedException( typeof( ObjectPoolEmptyException ) )]
		[Timeout( 500 )]
		public void TestBorrow_ThrowException_Exceeds_ThrowException()
		{
			const int maximum = 9;
			using ( var target =
				new StandardObjectPool<PooledObject>(
					() => new PooledObject(),
					new ObjectPoolConfiguration()
					{
						BorrowTimeout = null,
						EvitionInterval = null,
						ExhausionPolicy = ExhausionPolicy.ThrowException,
						MaximumPooled = maximum,
						MinimumReserved = 1
					}
				) )
			{
				var result = new List<PooledObject>();
				try
				{
					for ( int i = 0; i < maximum + 1; i++ )
					{
						result.Add( target.Borrow() );
					}
				}
				finally
				{
					result.DisposeAll();
				}
			}
		}

		[Test]
		public void TestEviction_Induced_ReducedToHalf()
		{
			const int maximum = 8;
			using ( var target =
				new StandardObjectPool<PooledObject>(
					() => new PooledObject(),
					new ObjectPoolConfiguration()
					{
						Name = "Pool",
						BorrowTimeout = null,
						EvitionInterval = TimeSpan.FromHours( 1 ),
						ExhausionPolicy = ExhausionPolicy.ThrowException,
						MaximumPooled = maximum,
						MinimumReserved = 1
					}
				) )
			{
				var items = new List<PooledObject>();
				try
				{
					for ( int i = 0; i < maximum; i++ )
					{
						items.Add( target.Borrow() );
					}

					foreach ( var item in items )
					{
						target.Return( item );
					}

					Assert.That( target.LeasedCount, Is.EqualTo( maximum ) );
					target.EvictExtraItems();
					Assert.That( target.LeasedCount, Is.EqualTo( maximum / 2 ) );
					target.EvictExtraItems();
					Assert.That( target.LeasedCount, Is.EqualTo( maximum / 4 ) );
					target.EvictExtraItems();
					Assert.That( target.LeasedCount, Is.EqualTo( maximum / 8 ) );
					target.EvictExtraItems();
					Assert.That( target.LeasedCount, Is.EqualTo( maximum / 8 ) );
				}
				finally
				{
					items.DisposeAll();
				}
			}
		}

		[Test]
		public void TestEviction_Timer_ReducedAtLeast()
		{
			const int maximum = 2;
			using ( var target =
				new StandardObjectPool<PooledObject>(
					() => new PooledObject(),
					new ObjectPoolConfiguration()
					{
						Name = "Pool",
						BorrowTimeout = null,
						EvitionInterval = TimeSpan.FromMilliseconds( 500 ),
						ExhausionPolicy = ExhausionPolicy.ThrowException,
						MaximumPooled = maximum,
						MinimumReserved = 1
					}
				) )
			{
				var items = new List<PooledObject>();
				try
				{
					for ( int i = 0; i < maximum; i++ )
					{
						items.Add( target.Borrow() );
					}

					foreach ( var item in items )
					{
						target.Return( item );
					}

					Assert.That( target.LeasedCount, Is.EqualTo( maximum ) );

					Thread.Sleep( TimeSpan.FromMilliseconds( 1000 ) );

					Assert.That( target.LeasedCount, Is.LessThan( maximum ) );
				}
				finally
				{
					items.DisposeAll();
				}
			}
		}

		[Test]
		public void TestEviction_MinimumIsZero_PooledIsZero_NoActions()
		{
			const int minimum = 0;
			const int maximum = 2;
			using ( var target =
				new StandardObjectPool<PooledObject>(
					() => new PooledObject(),
					new ObjectPoolConfiguration()
					{
						BorrowTimeout = null,
						EvitionInterval = TimeSpan.FromMilliseconds( 500 ),
						ExhausionPolicy = ExhausionPolicy.ThrowException,
						MaximumPooled = maximum,
						MinimumReserved = minimum
					}
				) )
			{
				var items = new List<PooledObject>();
				try
				{
					for ( int i = 0; i < maximum; i++ )
					{
						items.Add( target.Borrow() );
					}

					Assert.That( target.PooledCount, Is.EqualTo( 0 ) );
					target.EvictExtraItems();
					Assert.That( target.PooledCount, Is.EqualTo( 0 ) );
				}
				finally
				{
					items.DisposeAll();
				} 
			}
		}

		[Test]
		public void TestEviction_MinimumIsNotZero_PooledIsZero_Refilled()
		{
			const int minimum = 1;
			const int maximum = 2;
			using ( var target =
				new StandardObjectPool<PooledObject>(
					() => new PooledObject(),
					new ObjectPoolConfiguration()
					{
						BorrowTimeout = null,
						EvitionInterval = TimeSpan.FromMilliseconds( 500 ),
						ExhausionPolicy = ExhausionPolicy.ThrowException,
						MaximumPooled = maximum,
						MinimumReserved = minimum
					}
				) )
			{
				var items = new List<PooledObject>();
				try
				{
					for ( int i = 0; i < maximum; i++ )
					{
						items.Add( target.Borrow() );
					}

					Assert.That( target.PooledCount, Is.EqualTo( 0 ) );
					target.EvictExtraItems();
					Assert.That( target.PooledCount, Is.EqualTo( 0 ) );
				}
				finally
				{
					items.DisposeAll();
				}
			}
		}

		private sealed class ExternalResource : IDisposable
		{
			private InternalResource _internalResource;
			private readonly ObjectPool<InternalResource> _pool;

			public ExternalResource( ObjectPool<InternalResource> pool )
			{
				this._pool = pool;
				this._internalResource = pool.Borrow();
			}

			~ExternalResource()
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
				if ( disposing )
				{
					var resource = Interlocked.Exchange( ref this._internalResource, null );
					if ( resource != null )
					{
						this._pool.Return( resource );
					}
				}
			}
		}

		private sealed class InternalResource : IDisposable
		{
			private int _isDisposed;

			public bool IsDisposed
			{
				get { return Interlocked.CompareExchange( ref this._isDisposed, 0, 0 ) != 0; }
			}
			public readonly Guid Id;

			public InternalResource()
			{
				this.Id = Guid.NewGuid();
			}

			public void Dispose()
			{
				Interlocked.Exchange( ref this._isDisposed, 1 );
			}
		}
	}
}