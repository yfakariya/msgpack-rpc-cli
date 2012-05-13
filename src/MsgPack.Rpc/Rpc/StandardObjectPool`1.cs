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
#if !SILVERLIGHT
using System.Collections.Concurrent;
#endif
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Threading;
#if SILVERLIGHT
using Mono.Collections.Concurrent;
using Mono.Threading;
#endif
using MsgPack.Rpc.StandardObjectPoolTracing;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Implements standard <see cref="ObjectPool{T}"/>.
	/// </summary>
	/// <typeparam name="T">
	///		The type of objects to be pooled.
	/// </typeparam>
	internal sealed class StandardObjectPool<T> : ObjectPool<T>
		where T : class
	{
		private static readonly bool _isDisposableTInternal = typeof( IDisposable ).IsAssignableFrom( typeof( T ) );

		// name for debugging purpose, explicitly specified, or automatically constructed.
		private readonly string _name;

		private readonly TraceSource _source;

		internal TraceSource TraceSource
		{
			get { return this._source; }
		}

		private readonly ObjectPoolConfiguration _configuration;

		private int _isCorrupted;
		private bool IsCorrupted
		{
			get { return Interlocked.CompareExchange( ref this._isCorrupted, 0, 0 ) != 0; }
		}

		private readonly Func<T> _factory;
		private readonly BlockingCollection<T> _pool;
		private readonly TimeSpan _borrowTimeout;

		// Debug
		internal int PooledCount
		{
			get { return this._pool.Count; }
		}

		private readonly BlockingCollection<WeakReference> _leases;
		private readonly ReaderWriterLockSlim _leasesLock;

		internal int LeasedCount
		{
			get { return this._leases.Count; }
		}


		// TODO: Timer might be too heavy.
		private readonly Timer _evictionTimer;
		private readonly int? _evictionIntervalMilliseconds;

		/// <summary>
		/// Initializes a new instance of the <see cref="StandardObjectPool&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="factory">
		///		The factory delegate to create <typeparamref name="T"/> type instance.
		///	</param>
		/// <param name="configuration">
		///		The <see cref="ObjectPoolConfiguration"/> which contains various settings of this object pool.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="factory"/> is <c>null</c>.
		/// </exception>
		public StandardObjectPool( Func<T> factory, ObjectPoolConfiguration configuration )
		{
			if ( factory == null )
			{
				throw new ArgumentNullException( "factory" );
			}

			Contract.EndContractBlock();

			var safeConfiguration = ( configuration ?? ObjectPoolConfiguration.Default ).AsFrozen();

			if ( String.IsNullOrWhiteSpace( safeConfiguration.Name ) )
			{
				this._source = new TraceSource( this.GetType().FullName );
				this._name = this.GetType().FullName + "@" + this.GetHashCode().ToString( "X", CultureInfo.InvariantCulture );
			}
			else
			{
				this._source = new TraceSource( safeConfiguration.Name );
				this._name = safeConfiguration.Name;
			}

#if !API_SIGNATURE_TEST
			if ( configuration == null && this._source.ShouldTrace( StandardObjectPoolTrace.InitializedWithDefaultConfiguration ) )
			{
				this._source.TraceEvent(
					StandardObjectPoolTrace.InitializedWithDefaultConfiguration,
					"Initialized with default configuration. { \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X} }",
					this._name,
					this.GetType(),
					this.GetHashCode()
				);
			}
			else if ( this._source.ShouldTrace( StandardObjectPoolTrace.InitializedWithConfiguration ) )
			{
				this._source.TraceEvent(
					StandardObjectPoolTrace.InitializedWithConfiguration,
					"Initialized with specified configuration. { \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X}, \"Configuration\" : {3} }",
					this._name,
					this.GetType(),
					this.GetHashCode(),
					configuration
				);
			}
#endif
			this._configuration = safeConfiguration;
			this._factory = factory;
			this._leasesLock = new ReaderWriterLockSlim( LockRecursionPolicy.NoRecursion );
			this._borrowTimeout = safeConfiguration.BorrowTimeout ?? TimeSpan.FromMilliseconds( Timeout.Infinite );
			this._pool = 
				new BlockingCollection<T>( 
#if !SILVERLIGHT
					new ConcurrentStack<T>() 
#else
					new ConcurrentStack<T>() 
#endif
				);

			if ( safeConfiguration.MaximumPooled == null )
			{
				this._leases = new BlockingCollection<WeakReference>( new ConcurrentQueue<WeakReference>() );
			}
			else
			{
				this._leases = new BlockingCollection<WeakReference>( new ConcurrentQueue<WeakReference>(), safeConfiguration.MaximumPooled.Value );
			}

			for ( int i = 0; i < safeConfiguration.MinimumReserved; i++ )
			{
				if ( !this.AddToPool( factory(), 0 ) )
				{
#if !API_SIGNATURE_TEST
					this._source.TraceEvent(
						StandardObjectPoolTrace.FailedToAddPoolInitially,
						"Failed to add item. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X} }}",
						this._name,
						this.GetType(),
						this.GetHashCode()
					);
#endif
				}
			}

			this._evictionIntervalMilliseconds = safeConfiguration.EvitionInterval == null ? default( int? ) : unchecked( ( int )safeConfiguration.EvitionInterval.Value.TotalMilliseconds );
			if ( safeConfiguration.MaximumPooled != null
				&& safeConfiguration.MinimumReserved != safeConfiguration.MaximumPooled.GetValueOrDefault()
				&& this._evictionIntervalMilliseconds != null )
			{
				this._evictionTimer = new Timer( this.OnEvictionTimerElapsed, null, this._evictionIntervalMilliseconds.Value, Timeout.Infinite );
			}
			else
			{
				this._evictionTimer = null;
			}
		}

		private bool AddToPool( T value, int millisecondsTimeout )
		{
			bool result = false;
			try { }
			finally
			{
				if ( this._pool.TryAdd( value, millisecondsTimeout ) )
				{
					if ( this._leases.TryAdd( new WeakReference( value ) ) )
					{
						result = true;
					}
				}
			}

			return result;
		}

		protected sealed override void Dispose( bool disposing )
		{
			if ( disposing )
			{
				this._pool.Dispose();
				if ( this._evictionTimer != null )
				{
					this._evictionTimer.Dispose();
				}
				this._leasesLock.Dispose();

#if !API_SIGNATURE_TEST
				if ( this._source.ShouldTrace( StandardObjectPoolTrace.Disposed ) )
				{
					this._source.TraceEvent(
						StandardObjectPoolTrace.Disposed,
						"Object pool is disposed. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X} }}",
						this._name,
						this.GetType(),
						this.GetHashCode()
					);
				}
#endif
			}
			else
			{
#if !API_SIGNATURE_TEST
				if ( this._source.ShouldTrace( StandardObjectPoolTrace.Finalized ) )
				{
					this._source.TraceEvent(
						StandardObjectPoolTrace.Finalized,
						"Object pool is finalized. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X} }}",
						this._name,
						this.GetType(),
						this.GetHashCode()
					);
				}
#endif
			}

			base.Dispose( disposing );
		}

		private void VerifyIsNotCorrupted()
		{
			if ( this.IsCorrupted )
			{
				throw new ObjectPoolCorruptedException();
			}
		}

		private void SetIsCorrupted()
		{
			Interlocked.Exchange( ref this._isCorrupted, 1 );
		}

		private void OnEvictionTimerElapsed( object state )
		{
			this.EvictExtraItemsCore( false );

			Contract.Assert( this._evictionIntervalMilliseconds.HasValue );

			if ( this.IsCorrupted )
			{
				return;
			}

			if ( !this._evictionTimer.Change( this._evictionIntervalMilliseconds.Value, Timeout.Infinite ) )
			{
#if !API_SIGNATURE_TEST
				this._source.TraceEvent(
					StandardObjectPoolTrace.FailedToRefreshEvictionTImer,
					"Failed to refresh evition timer. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X} }}",
					this._name,
					this.GetType(),
					this.GetHashCode()
				);
#endif
			}
		}

		/// <summary>
		///		Evicts the extra items from current pool.
		/// </summary>
		public sealed override void EvictExtraItems()
		{
			this.EvictExtraItemsCore( true );
		}

		private void EvictExtraItemsCore( bool isInduced )
		{
			int remains = this._pool.Count - this._configuration.MinimumReserved;
			int evicting = remains / 2 + remains % 2;
			if ( evicting > 0 )
			{
#if !API_SIGNATURE_TEST
				if ( isInduced && this._source.ShouldTrace( StandardObjectPoolTrace.EvictingExtraItemsInduced ) )
				{
					this._source.TraceEvent(
						StandardObjectPoolTrace.EvictingExtraItemsInduced,
						"Start induced eviction. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X}, \"Evicting\" : {3} }}",
						this._name,
						this.GetType(),
						this.GetHashCode(),
						evicting
					);
				}
				else if ( this._source.ShouldTrace( StandardObjectPoolTrace.EvictingExtraItemsPreiodic ) )
				{
					this._source.TraceEvent(
						StandardObjectPoolTrace.EvictingExtraItemsPreiodic,
						"Start periodic eviction. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X}, \"Evicting\" : {3} }}",
						this._name,
						this.GetType(),
						this.GetHashCode(),
						evicting
					);
				}
#endif

				var disposed = this.EvictItems( evicting );

#if !API_SIGNATURE_TEST
				if ( isInduced && this._source.ShouldTrace( StandardObjectPoolTrace.EvictedExtraItemsInduced ) )
				{
					this._source.TraceEvent(
						StandardObjectPoolTrace.EvictedExtraItemsInduced,
						"Finish induced eviction. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X}, \"Evicted\" : {3} }}",
						this._name,
						this.GetType(),
						this.GetHashCode(),
						disposed.Count
					);
				}
				else if ( this._source.ShouldTrace( StandardObjectPoolTrace.EvictedExtraItemsPreiodic ) )
				{
					this._source.TraceEvent(
						StandardObjectPoolTrace.EvictedExtraItemsPreiodic,
						"Finish periodic eviction. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X}, \"Evicted\" : {3} }}",
						this._name,
						this.GetType(),
						this.GetHashCode(),
						disposed.Count
					);
				}
#endif

				this.CollectLeases( disposed );
			}
			else
			{
				// Just GC
				this.CollectLeases( new List<T>( 0 ) );
			}
		}

		private List<T> EvictItems( int count )
		{
			List<T> disposed = new List<T>( count );
			for ( int i = 0; i < count; i++ )
			{
				T disposing;
				if ( !this._pool.TryTake( out disposing, 0 ) )
				{
					// Race, cancel eviction now.
					return disposed;
				}

				DisposeItem( disposing );
				disposed.Add( disposing );
			}

			return disposed;
		}

		private void CollectLeases( List<T> disposed )
		{
			bool isSuccess = false;
			try
			{
				this._leasesLock.EnterWriteLock();
				try
				{
					var buffer = new List<WeakReference>( this._leases.Count + Environment.ProcessorCount * 2 );
					WeakReference dequeud;
					while ( this._leases.TryTake( out dequeud ) )
					{
						buffer.Add( dequeud );
					}

					bool isFlushed = false;
					int freed = 0;
					foreach ( var item in buffer )
					{
						if ( !isFlushed && item.IsAlive && !disposed.Exists( x => Object.ReferenceEquals( x, SafeGetTarget( item ) ) ) )
						{
							if ( !this._leases.TryAdd( item ) )
							{
								// Just evict
								isFlushed = true;
								freed++;
							}
						}
						else
						{
							freed++;
						}
					}

#if !API_SIGNATURE_TEST
					if ( freed - disposed.Count > 0 && this._source.ShouldTrace( StandardObjectPoolTrace.GarbageCollectedWithLost ) )
					{
						this._source.TraceEvent(
							StandardObjectPoolTrace.GarbageCollectedWithLost,
							"Garbage items are collected, but there may be lost items. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X}, \"Collected\" : {3}, \"MayBeLost\" : {4} }}",
							this._name,
							this.GetType(),
							this.GetHashCode(),
							freed,
							freed - disposed.Count
						);
					}
					else if ( freed > 0 && this._source.ShouldTrace( StandardObjectPoolTrace.GarbageCollectedWithoutLost ) )
					{
						this._source.TraceEvent(
							StandardObjectPoolTrace.GarbageCollectedWithoutLost,
							"Garbage items are collected. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X}, \"Collected\" : {3} }}",
							this._name,
							this.GetType(),
							this.GetHashCode(),
							freed
						);
					}
#endif
				}
				finally
				{
					this._leasesLock.ExitWriteLock();
				}

				isSuccess = true;
			}
			finally
			{
				if ( !isSuccess )
				{
					this.SetIsCorrupted();
				}
			}
		}

		private static T SafeGetTarget( WeakReference item )
		{
			try
			{
				return item.Target as T;
			}
			catch ( InvalidOperationException )
			{
				return null;
			}
		}

		protected sealed override T BorrowCore()
		{
			this.VerifyIsNotCorrupted();

			T result;
			while ( true )
			{
				if ( this._pool.TryTake( out result, 0 ) )
				{
#if !API_SIGNATURE_TEST
					if ( this._source.ShouldTrace( StandardObjectPoolTrace.BorrowFromPool ) )
					{
						this.TraceBorrow( result );
					}
#endif

					return result;
				}

				this._leasesLock.EnterReadLock(); // TODO: Timeout
				try
				{
					if ( this._leases.Count < this._leases.BoundedCapacity )
					{
						var newObject = this._factory();
						Contract.Assume( newObject != null );

						if ( this._leases.TryAdd( new WeakReference( newObject ), 0 ) )
						{
#if !API_SIGNATURE_TEST
							if ( this._source.ShouldTrace( StandardObjectPoolTrace.ExpandPool ) )
							{
								this._source.TraceEvent(
									StandardObjectPoolTrace.ExpandPool,
									"Expand the pool. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X}, \"NewCount\" : {3} }}",
									this._name,
									this.GetType(),
									this.GetHashCode(),
									this._pool.Count
								);
							}

							this.TraceBorrow( newObject );
#endif
							return newObject;
						}
						else
						{
#if !API_SIGNATURE_TEST
							if ( this._source.ShouldTrace( StandardObjectPoolTrace.FailedToExpandPool ) )
							{
								this._source.TraceEvent(
									StandardObjectPoolTrace.FailedToExpandPool,
									"Failed to expand the pool. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X}, \"NewCount\" : {3} }}",
									this._name,
									this.GetType(),
									this.GetHashCode(),
									this._pool.Count
								);
							}
#endif

							DisposeItem( newObject );
						}
					}
				}
				finally
				{
					this._leasesLock.ExitReadLock();
				}

				// Wait or exception
				break;
			}

#if !API_SIGNATURE_TEST
			if ( this._source.ShouldTrace( StandardObjectPoolTrace.PoolIsEmpty ) )
			{
				this._source.TraceEvent(
					StandardObjectPoolTrace.PoolIsEmpty,
					"Pool is empty. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X} }}",
					this._name,
					this.GetType(),
					this.GetHashCode()
				);
			}
#endif

			if ( this._configuration.ExhausionPolicy == ExhausionPolicy.ThrowException )
			{
				throw new ObjectPoolEmptyException();
			}
			else
			{
				if ( !this._pool.TryTake( out result, this._borrowTimeout ) )
				{
					throw new TimeoutException( String.Format( CultureInfo.CurrentCulture, "The object borrowing is not completed in the time out {0}.", this._borrowTimeout ) );
				}

#if !API_SIGNATURE_TEST
				if ( this._source.ShouldTrace( StandardObjectPoolTrace.BorrowFromPool ) )
				{
					this.TraceBorrow( result );
				}
#endif

				return result;
			}
		}

#if !API_SIGNATURE_TEST
		private void TraceBorrow( T result )
		{
			this._source.TraceEvent(
				StandardObjectPoolTrace.BorrowFromPool,
				"Borrow the value from the pool. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X}, \"Evicted\" : 0x{2:X}, \"Resource\" : \"{3}\", \"HashCodeOfResource\" : 0x{4:X} }}",
				this._name,
				this.GetType(),
				this.GetHashCode(),
				result,
				result.GetHashCode()
			);
		}
#endif

		private static void DisposeItem( T item )
		{
			if ( _isDisposableTInternal )
			{
				( ( IDisposable )item ).Dispose();
			}
		}

		protected sealed override void ReturnCore( T value )
		{
			if ( !this._pool.TryAdd( value ) )
			{
#if !API_SIGNATURE_TEST
				this._source.TraceEvent(
					StandardObjectPoolTrace.FailedToReturnToPool,
					"Failed to return the value to the pool. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"Value\" : 0x{2:X}, \"Resource\" : \"{3}\", \"HashCodeOfResource\" : 0x{4:X} }}",
					this._name,
					this.GetType(),
					this.GetHashCode(),
					value,
					value.GetHashCode()
				);
#endif
				this.SetIsCorrupted();
				throw new ObjectPoolCorruptedException( "Failed to return the value to the pool." );
			}
			else
			{
#if !API_SIGNATURE_TEST
				if ( this._source.ShouldTrace( StandardObjectPoolTrace.ReturnToPool ) )
				{
					this._source.TraceEvent(
						StandardObjectPoolTrace.ReturnToPool,
						"Return the value to the pool. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"Value\" : 0x{2:X}, \"Resource\" : \"{3}\", \"HashCodeOfResource\" : 0x{4:X} }}",
						this._name,
						this.GetType(),
						this.GetHashCode(),
						value,
						value.GetHashCode()
					);
				}
#endif
			}
		}
	}
}
