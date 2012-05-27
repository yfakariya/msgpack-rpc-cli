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
using System.Security;
using System.Threading;
#if WINDOWS_PHONE
using Mono;
using Mono.Threading;
#endif

namespace MsgPack.Rpc
{
	/// <summary>
	///		Provides access to RPC server application context information.
	/// </summary>
	public sealed class RpcApplicationContext : IDisposable
	{
		internal static readonly object HardTimeoutToken = new object();
		private const int StateActive = 0;
		private const int StateSoftTimeout = 1;
		private const int StateHardTimeout = 2;
		private const int StateDisposed = 3;

		[ThreadStatic]
		private static RpcApplicationContext _current;

		/// <summary>
		///		Gets the current context.
		/// </summary>
		/// <value>
		///		The current context.
		///		If this thread is initiated by the dispatcher, then <c>null</c>.
		/// </value>
		public static RpcApplicationContext Current
		{
			get { return _current; }
		}

		/// <summary>
		///		Sets the current context for this thread.
		/// </summary>
		/// <param name="context">The context.</param>
		internal static void SetCurrent( RpcApplicationContext context )
		{
			_current = context;
			context._boundThread = new WeakReference( Thread.CurrentThread );
#if !SILVERLIGHT
			context._hardTimeoutWatcher.Reset();
#endif
			context._softTimeoutWatcher.Reset();
		}

		/// <summary>
		///		Clears current instance.
		/// </summary>
		internal static void Clear()
		{
			var current = _current;
			if ( current != null )
			{
				try
				{
					current.StopTimeoutWatch();
				}
				finally
				{
					current._boundThread = null;
					Interlocked.Exchange( ref current._state, StateActive );
					_current = null;
				}
			}
		}

		/// <summary>
		///		Gets a value indicating whether this application thread is canceled.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this pplication thread is canceled; otherwise, <c>false</c>.
		/// 	Note that if <see cref="Current"/> returns <c>null</c>, then this property returns <c>false</c>.
		/// </value>
		public static bool IsCanceled
		{
			get
			{
				var current = Current;
				return current != null && current.CancellationToken.IsCancellationRequested;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the execution timeout is enabled on this application thread.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if the execution timeout is enabled on this application thread; otherwise, <c>false</c>.
		/// 	Note that if <see cref="Current"/> returns <c>null</c>, then this property returns <c>false</c>.
		/// </value>
		public static bool IsExecutionTimeoutEnabled
		{
			get
			{
				var current = Current;
				return current != null && current._softTimeout != null;
			}
		}

		internal bool IsSoftTimeout
		{
			get { return this._softTimeoutWatcher.IsTimeout; }
		}

		// Set from SetCurrent
		private WeakReference _boundThread;
		private AggregateException _exceptionInCancellationCallback;
		private readonly TimeoutWatcher _softTimeoutWatcher;
#if !SILVERLIGHT
		private readonly TimeoutWatcher _hardTimeoutWatcher;
#endif
		private readonly CancellationTokenSource _cancellationTokenSource;

#if DEBUG
		[Obsolete("DO NOT use this member except testing purposes.")]
		internal event EventHandler DebugSoftTimeout;

		private void OnDebugSoftTimeout()
		{
			var handler = this.DebugSoftTimeout;
			if ( handler != null )
			{
				handler( this, EventArgs.Empty );
			}
		}
#endif

		/// <summary>
		///		Gets the <see cref="CancellationToken"/> associated with this context.
		/// </summary>
		/// <value>
		///		The <see cref="CancellationToken"/> associated with this context.
		/// </value>
#if WINDOWS_PHONE
		internal CancellationToken CancellationToken
#else
		public CancellationToken CancellationToken
#endif
		{
			get { return this._cancellationTokenSource.Token; }
		}

		private TimeSpan? _softTimeout;

#if !SILVERLIGHT
		private TimeSpan? _hardTimeout;
#endif

		private int _state;

		internal bool IsDisposed
		{
			get { return Interlocked.CompareExchange( ref this._state, 0, 0 ) == StateDisposed; }
		}

		// called from SetCurrent
		internal RpcApplicationContext( TimeSpan? softTimeout, TimeSpan? hardTimeout )
		{
			this._softTimeout = softTimeout;
#if !SILVERLIGHT
			this._hardTimeout = hardTimeout;
#endif
			this._softTimeoutWatcher = new TimeoutWatcher();
			this._softTimeoutWatcher.Timeout += ( sender, e ) => this.OnSoftTimeout();
#if !SILVERLIGHT
			this._hardTimeoutWatcher = new TimeoutWatcher();
			this._hardTimeoutWatcher.Timeout += ( sender, e ) => this.OnHardTimeout();
#endif
			this._cancellationTokenSource = new CancellationTokenSource();
		}

		/// <summary>
		///		Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			if ( Interlocked.Exchange( ref this._state, StateDisposed ) != StateDisposed )
			{
#if !SILVERLIGHT
				this._hardTimeoutWatcher.Dispose();
#endif
				this._softTimeoutWatcher.Dispose();
				this._cancellationTokenSource.Dispose();
			}
		}

		private void OnSoftTimeout()
		{
			if ( Interlocked.CompareExchange( ref this._state, StateSoftTimeout, StateActive ) != StateActive )
			{
				return;
			}

			try
			{
				this._cancellationTokenSource.Cancel();
			}
			catch ( AggregateException ex )
			{
				Interlocked.Exchange( ref this._exceptionInCancellationCallback, ex );
			}

#if !SILVERLIGHT
			if ( this._hardTimeout != null )
			{
				this._hardTimeoutWatcher.Start( this._hardTimeout.Value );
			}
#endif
#if DEBUG
			this.OnDebugSoftTimeout();
#endif
		}

#if !SILVERLIGHT
		private void OnHardTimeout()
		{
			if ( Interlocked.CompareExchange( ref this._state, StateHardTimeout, StateSoftTimeout ) != StateSoftTimeout )
			{
				return;
			}

			try
			{
				DoHardTimeout();
			}
			catch ( SecurityException ) { }
			catch ( MemberAccessException ) { }
		}

		[SecuritySafeCritical]
		private void DoHardTimeout()
		{
			var thread = this._boundThread.Target as Thread;
			if ( thread != null )
			{
				try
				{
					thread.Abort( HardTimeoutToken );
				}
				catch ( ThreadStateException ) { }
			}
		}
#endif

		internal void StartTimeoutWatch()
		{
			if ( this._softTimeout == null )
			{
				return;
			}

			this._softTimeoutWatcher.Start( this._softTimeout.Value );
		}

		internal void StopTimeoutWatch()
		{
#if !SILVERLIGHT
			this._hardTimeoutWatcher.Stop();
#endif
			this._softTimeoutWatcher.Stop();

			var exceptionInCancellationCallback = Interlocked.Exchange( ref _exceptionInCancellationCallback, null );
			if ( exceptionInCancellationCallback != null )
			{
				throw exceptionInCancellationCallback;
			}
		}
	}
}
