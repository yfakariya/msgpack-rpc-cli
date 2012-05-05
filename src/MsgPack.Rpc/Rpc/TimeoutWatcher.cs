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
using System.Diagnostics.Contracts;
using System.Threading;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Watches timeout.
	/// </summary>
	internal class TimeoutWatcher : IDisposable
	{
		private const int StateIdle = 0;
		private const int StateWatching = 1;
		private const int StateTimeout = 2;
		private const int StateDisposed = 3;

		private readonly object _resourceLock = new object();
		private ManualResetEvent _waitHandle;
		private RegisteredWaitHandle _registeredWaitHandle;
		private int _state;

		/// <summary>
		///		Gets a value indicating whether timeout is occurred.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if timeout is occurred; otherwise, <c>false</c>.
		/// </value>
		public bool IsTimeout
		{
			get
			{
				this.VerifyIsNotDisposed();

				return Interlocked.CompareExchange( ref this._state, 0, 0 ) == StateTimeout;
			}
		}

		private EventHandler _timeout;

		/// <summary>
		///		Occurs when operation timeout.
		/// </summary>
		public event EventHandler Timeout
		{
			add
			{
				this.VerifyIsNotDisposed();

				EventHandler oldHandler;
				EventHandler currentHandler = this._timeout;
				do
				{
					oldHandler = currentHandler;
					var newHandler = Delegate.Combine( oldHandler, value ) as EventHandler;
					currentHandler = Interlocked.CompareExchange( ref this._timeout, newHandler, oldHandler );
				} while ( oldHandler != currentHandler );
			}
			remove
			{
				this.VerifyIsNotDisposed();

				EventHandler oldHandler;
				EventHandler currentHandler = this._timeout;
				do
				{
					oldHandler = currentHandler;
					var newHandler = Delegate.Remove( oldHandler, value ) as EventHandler;
					currentHandler = Interlocked.CompareExchange( ref this._timeout, newHandler, oldHandler );
				} while ( oldHandler != currentHandler );
			}
		}

		private void OnTimeout()
		{
			var handler = Interlocked.CompareExchange( ref this._timeout, null, null );
			if ( handler != null )
			{
				handler( this, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TimeoutWatcher"/> class.
		/// </summary>
		public TimeoutWatcher()
		{
		}

		/// <summary>
		///		Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			if ( Interlocked.Exchange( ref this._state, StateDisposed ) != StateDisposed )
			{
				lock ( this._resourceLock )
				{
					if ( this._waitHandle != null )
					{
						if ( this._registeredWaitHandle != null )
						{
							this._registeredWaitHandle.Unregister( this._waitHandle );
							this._registeredWaitHandle = null;
						}

						this._waitHandle.Close();
					}
				}
			}
		}

		private void VerifyIsNotDisposed()
		{
			if ( Interlocked.CompareExchange( ref this._state, 0, 0 ) == StateDisposed )
			{
				throw new ObjectDisposedException( this.ToString() );
			}
		}

		/// <summary>
		///		Resets this instance.
		/// </summary>
		public void Reset()
		{
			lock ( this._resourceLock )
			{
				this.VerifyIsNotDisposed();

				if ( this._waitHandle != null )
				{
					if ( this._registeredWaitHandle != null )
					{
						this._registeredWaitHandle.Unregister( this._waitHandle );
						this._registeredWaitHandle = null;
					}

					// It is unstable to reuse wait handles...
					this._waitHandle.Close();
					this._waitHandle = null;
				}

				Interlocked.Exchange( ref this._state, StateIdle );
			}
		}

		/// <summary>
		///		Starts timeout watch.
		/// </summary>
		/// <param name="timeout">The timeout.</param>
		/// <exception cref="InvalidOperationException">
		///		This instance already start wathing.
		/// </exception>
		public void Start( TimeSpan timeout )
		{
			lock ( this._resourceLock )
			{
				this.VerifyIsNotDisposed();

				if ( this._registeredWaitHandle != null )
				{
					throw new InvalidOperationException( "Already started." );
				}

				if ( this._waitHandle == null )
				{
					this._waitHandle = new ManualResetEvent( false );
				}

				this._registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject( this._waitHandle, OnPulse, null, timeout, true );
				Interlocked.Exchange( ref this._state, StateWatching );
			}
		}

		private void OnPulse( object state, bool isTimeout )
		{
			if ( isTimeout && Interlocked.CompareExchange( ref this._state, StateTimeout, StateWatching ) == StateWatching )
			{
				this.OnTimeout();
			}
		}

		/// <summary>
		///		Stops timeout watch.
		/// </summary>
		public void Stop()
		{
			lock ( this._resourceLock )
			{
				this.VerifyIsNotDisposed();

				// Do not override Disposed/Timeout
				Interlocked.CompareExchange( ref this._state, StateIdle, StateWatching );

				if ( this._waitHandle != null )
				{
					this._waitHandle.Set();
				}
			}
		}
	}
}
