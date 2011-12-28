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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;

namespace MsgPack.Rpc
{
	// TODO: Move to NLiblet

	/// <summary>
	///		Defines exhausion policy of the object pool.
	/// </summary>
	public enum ExhausionPolicy
	{
		/// <summary>
		///		Blocks the caller threads until any objects will be available.
		/// </summary>
		BlockUntilAvailable,

		/// <summary>
		///		Throws the <see cref="ObjectPoolEmptyException"/> immediately.
		/// </summary>
		ThrowException
	}

	/// <summary>
	///		Occurs when the object pool with <see cref="ExhausionPolicy.ThrowException"/> is empty at borrowing.
	/// </summary>
	public sealed class ObjectPoolEmptyException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectPoolEmptyException"/> class.
		/// </summary>
		public ObjectPoolEmptyException() : base( "The object pool is empty." ) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectPoolEmptyException"/> class.
		/// </summary>
		/// <param name="message">The message.</param>
		public ObjectPoolEmptyException( string message ) : base( message ) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectPoolEmptyException"/> class.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="innerException">The inner exception.</param>
		public ObjectPoolEmptyException( string message, Exception innerException ) : base( message, innerException ) { }

#if !SILVERLIGHT
		private ObjectPoolEmptyException( SerializationInfo info, StreamingContext context ) : base( info, context ) { }
#endif
	}

	internal sealed class StandardObjectPool<T> : ObjectPool<T>
			where T : class, ILeaseable<T>
	{
		private readonly ObjectPoolConfiguration _configuration;
		private readonly Func<T> _factory;
		private readonly BlockingCollection<T> _pool;
		private readonly TimeSpan _borrowTimeout;

		// TODO: Timer might be too heavy.
		private readonly Timer _evictionTimer;
		private readonly int? _evictionIntervalMilliseconds;

		// FIXME: Configuration
		public StandardObjectPool( Func<T> factory, ObjectPoolConfiguration configuration )
		{
			if ( factory == null )
			{
				throw new ArgumentNullException( "factory" );
			}

			var safeConfiguration = ( configuration ?? ObjectPoolConfiguration.Default ).AsFrozen();

			this._configuration = safeConfiguration;
			this._factory = factory;
			this._borrowTimeout = safeConfiguration.BorrowTimeout ?? TimeSpan.FromMilliseconds( Timeout.Infinite );

			if ( safeConfiguration.MaximumPooled == null )
			{
				this._pool = new BlockingCollection<T>( new ConcurrentStack<T>() );
			}
			else
			{
				this._pool = new BlockingCollection<T>( new ConcurrentStack<T>(), safeConfiguration.MaximumPooled.Value );
			}

			for ( int i = 0; i < safeConfiguration.MinimumReserved; i++ )
			{
				this._pool.Add( this._factory() );
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

		protected sealed override void Dispose( bool disposing )
		{
			if ( disposing )
			{
				this._pool.Dispose();
				this._evictionTimer.Dispose();
			}

			base.Dispose( disposing );
		}

		private void OnEvictionTimerElapsed( object state )
		{
			this.EvictExtraItems();

			Contract.Assert( this._evictionIntervalMilliseconds.HasValue );

			if ( !this._evictionTimer.Change( this._evictionIntervalMilliseconds.Value, Timeout.Infinite ) )
			{
				Debug.WriteLine( "ObjectPool '{0}'(hash code:0x{1:x}) failed to refresh evition timer.", this.GetType(), RuntimeHelpers.GetHashCode( this ) );
			}
		}

		/// <summary>
		///		Evicts the extra items from current pool.
		/// </summary>
		public sealed override void EvictExtraItems()
		{
			int remains = this._pool.Count - this._configuration.MinimumReserved;
			this.EvictItems( remains / 2 + remains % 2 );
		}

		private void EvictItems( int count )
		{
			for ( int i = 0; i < count; i++ )
			{
				T disposing;
				if ( !this._pool.TryTake( out disposing, 0 ) )
				{
					// Race, cancel eviction now.
					return;
				}

				DisposeItem( disposing );
			}
		}

		protected sealed override T BorrowCore()
		{
			T result;
			if ( this._pool.TryTake( out result, 0 ) )
			{
				return result;
			}

			if ( this._pool.Count < this._pool.BoundedCapacity )
			{
				var newObject = this._factory();
				if ( this._pool.TryAdd( newObject, 0 ) )
				{
					return newObject;
				}

				DisposeItem( newObject );
			}

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

				return result;
			}
		}

		private static void DisposeItem( T item )
		{
			if ( typeof( T ) is IDisposable )
			{
				( ( IDisposable )item ).Dispose();
			}
		}

		protected sealed override ILease<T> Lease( T result )
		{
			return new FinalizableObjectLease<T>( result, this.Return );
		}

		protected sealed override void ReturnCore( T value )
		{
			if ( !this._pool.TryAdd( value ) )
			{
				// TODO: trace
			}
		}
	}
}
