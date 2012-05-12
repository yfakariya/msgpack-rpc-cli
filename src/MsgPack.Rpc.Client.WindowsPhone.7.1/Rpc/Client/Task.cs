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
using System.Net;
using Mono.Threading;
using System.Threading;

namespace MsgPack.Rpc.Client
{
	// This class does NOT use Mono because they are too complicated to include here...
	// This class is TOO naive but it is enough to support API compatibility.

	/// <summary>
	///		Represents the task which will be completed in the future.
	/// </summary>
	public abstract class Task : IDisposable
	{
		private readonly ManualResetEventSlim _event;
		private Exception _exception;

		internal Task()
		{
			this._event = new ManualResetEventSlim();
		}

		/// <summary>
		///		Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			this.Dispose( true );
			GC.SuppressFinalize( this );
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected virtual void Dispose( bool disposing )
		{
			if ( disposing )
			{
				this._event.Dispose();
			}
		}

		/// <summary>
		///		Waits for the <see cref="Task"/> to complete execution. 
		/// </summary>
		/// <exception cref="ObjectDisposedException">
		///		This instance is disposed.
		/// </exception>
		/// <exception cref="Exception">
		///		Any exception occured in async task.
		/// </exception>
		public void Wait()
		{
			this._event.Wait();
			var exception = Interlocked.CompareExchange( ref this._exception, null, null );
			if ( exception != null )
			{
				throw new Mono.AggregateException( exception );
			}
		}

		internal abstract void RunSynchronously( TaskScheduler notUsed );

		internal void SetException( Exception exception )
		{
			Interlocked.Exchange( ref this._exception, exception );
			this.SetCompletion();
		}

		internal void SetCompletion()
		{
			this._event.Set();
		}
	}
}
