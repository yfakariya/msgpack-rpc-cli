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
	///		Minimal implementation of <see cref="IAsyncResult"/>.
	/// </summary>
	internal class AsyncResult : IAsyncResult
	{
		// State flags
		private const int _initialized = 0;
		private const int _completed = 0x100;
		private const int _completedSynchronously = 0x101;
		private const int _finished = 0x2;

		private readonly object _owner;

		/// <summary>
		///		Get owner of asynchrnous invocation.
		/// </summary>
		/// <value>
		///		Owner of asynchrnous invocation. This value will not be null.
		/// </value>
		internal object Owner
		{
			get { return _owner; }
		}

		private readonly AsyncCallback _asyncCallback;

		/// <summary>
		///		Get callback of asynchrnous invocation which should be called in completion.
		/// </summary>
		/// <value>
		///		Callback of asynchrnous invocation which should be called in completion.
		///		This value could be null.
		/// </value>
		public AsyncCallback AsyncCallback
		{
			get { return this._asyncCallback; }
		}

		private readonly object _asyncState;

		/// <summary>
		///		Get state object of asynchrnous invocation which will be passed to <see cref="AsyncCallback"/>.
		/// </summary>
		/// <value>
		///		State object of asynchrnous invocation which will be passed to <see cref="AsyncCallback"/>.
		///		This value could be null.
		/// </value>
		public object AsyncState
		{
			get { return this._asyncState; }
		}

		private ManualResetEvent _asyncWaitHandle;

		/// <summary>
		///		Get <see cref="WaitHandle"/> to be used coordinate multiple asynchronous invocation.
		/// </summary>
		/// <value>
		///		<see cref="WaitHandle"/> to be used coordinate multiple asynchronous invocation.
		/// </value>
		public WaitHandle AsyncWaitHandle
		{
			get { return LazyInitializer.EnsureInitialized( ref this._asyncWaitHandle, () => new ManualResetEvent( false ) ); }
		}

		// manipulated via Interlocked methods.
		private int _state;

		bool IAsyncResult.CompletedSynchronously
		{
			get { return ( this._state & _completedSynchronously ) != 0; }
		}

		/// <summary>
		///		Get value asynchronous invocation is completed.
		/// </summary>
		/// <value>
		///		If asynchronous invocation is completed, that is, BeginInvoke is finished then true.
		/// </value>
		public bool IsCompleted
		{
			get { return ( this._state & _completed ) != 0; }
		}

		/// <summary>
		///		Get value asynchronous invocation is finished.
		/// </summary>
		/// <value>
		///		If asynchronous invocation is finished, that is, EncInvoke is finished then true.
		/// </value>
		public bool IsFinished
		{
			get { return ( this._state & _finished ) != 0; }
		}

		private Exception _error;

		/// <summary>
		///		Get error corresponds to this message.
		/// </summary>
		/// <value>
		///		Error corresponds to this message.
		/// </value>
		public Exception Error
		{
			get { return this._error; }
		}

		/// <summary>
		///		Initialize new instance.
		/// </summary>
		/// <param name="owner">
		///		Owner of asynchrnous invocation. This value will not be null.
		/// </param>
		/// <param name="asyncCallback">
		///		Callback of asynchrnous invocation which should be called in completion.
		///		This value can be null.
		/// </param>
		/// <param name="asyncState">
		///		State object of asynchrnous invocation which will be passed to <see cref="AsyncCallback"/>.
		///		This value can be null.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="owner"/> is null.
		/// </exception>
		protected AsyncResult( object owner, AsyncCallback asyncCallback, object asyncState )
		{
			if ( owner == null )
			{
				throw new ArgumentNullException( "owner" );
			}

			this._owner = owner;
			this._asyncCallback = asyncCallback;
			this._asyncState = asyncState;
		}

		/// <summary>
		///		Record asynchronous invocation result and set completion.
		/// </summary>
		/// <param name="completedSynchronously">
		///		When operation is completed same thread as initiater then true.
		/// </param>
		internal void Complete( bool completedSynchronously )
		{
			int state = _completed | ( completedSynchronously ? _completedSynchronously : 0 );
			if ( Interlocked.CompareExchange( ref this._state, state, _initialized ) == _initialized )
			{
				var waitHandle = this._asyncWaitHandle;
				if ( waitHandle != null )
				{
					waitHandle.Set();
				}
			}
		}

		/// <summary>
		///		Complete this invocation as error.
		/// </summary>
		/// <param name="error">
		///		Occurred exception.
		///	</param>
		/// <param name="completedSynchronously">
		///		When operation is completed same thread as initiater then true.
		/// </param>
		public void OnError( Exception error, bool completedSynchronously )
		{
			try { }
			finally
			{
				Interlocked.Exchange( ref this._error, error );
				this.Complete( completedSynchronously );
			}
		}

		/// <summary>
		///		Record all operation is finished.
		/// </summary>
		public void Finish()
		{
			Contract.Assert( this._state != _initialized );
			int oldValue = this._state;
			int newValue = this._state | _finished;
			while ( Interlocked.CompareExchange( ref this._state, newValue, oldValue ) != oldValue )
			{
				oldValue = this._state;
				newValue = oldValue | _finished;
			}

			if ( this._error != null )
			{
				throw this._error;
			}
		}

		/// <summary>
		///		Verify ownership and return typed instance.
		/// </summary>
		/// <typeparam name="TAsyncResult">Type of returning <paramref name="asyncResult"/>.</typeparam>
		/// <param name="asyncResult"><see cref="IAsyncResult"/> passed to EndInvoke.</param>
		/// <param name="owner">'this' reference of EndInvoke to be verified.</param>
		/// <returns>Verified <paramref name="asyncResult"/>.</returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="asyncResult"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="asyncResult"/> is not <typeparamref name="TAsyncResult"/>.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///		<paramref name="owner"/> is not same as <see cref="Owner"/>.
		///		Or <see cref="IsFinished"/> is true.
		/// </exception>
		internal static TAsyncResult Verify<TAsyncResult>( IAsyncResult asyncResult, object owner )
			where TAsyncResult : AsyncResult
		{
			Contract.Assert( owner != null );
			if ( asyncResult == null )
			{
				throw new ArgumentNullException( "asyncResult" );
			}

			TAsyncResult result = asyncResult as TAsyncResult;
			if ( result == null )
			{
				throw new ArgumentException( "Unknown asyncResult.", "asyncResult" );
			}

			if ( !Object.ReferenceEquals( result.Owner, owner ) )
			{
				throw new InvalidOperationException( "Async operation was not started on this instance." );
			}

			if ( result.IsFinished )
			{
				throw new InvalidOperationException( "Async operation has already been finished." );
			}

			return result;
		}
	}
}
