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
using System.Threading;

namespace MsgPack.Rpc.Client
{
	// This class does NOT use Mono because they are too complicated to include here...
	/// <summary>
	///		Represents the task which will be completed in the future.
	///		Note that this class only support the usage which together with <see cref="TaskCompletionSource{T}"/>.
	/// </summary>
	public sealed class Task<T> : Task
	{
		private Box _result;

		/// <summary>
		///		Gets the result of the async task.
		/// </summary>
		/// <value>
		///		The result of the async task.
		/// </value>
		public T Result
		{
			get
			{
				this.Wait();

				return Interlocked.CompareExchange( ref this._result, null, null ).Value;
			}
		}

		private Func<object, T> _func;
		private object _state;

		internal Task( Func<object, T> func, object state )
		{
			Interlocked.Exchange( ref this._func, func );
			Interlocked.Exchange( ref this._state, state );
		}

		internal override void RunSynchronously( TaskScheduler schedular )
		{
			try
			{
				this.SetResult( this._func( this._state ) );
			}
			catch ( Exception ex )
			{
				this.SetException( ex );
			}
		}

		internal void SetResult( T result )
		{
			Interlocked.Exchange( ref this._result, new Box( result ) );
			base.SetCompletion();
		}

		private sealed class Box
		{
			public readonly T Value;

			public Box( T value )
			{
				this.Value = value;
				Thread.MemoryBarrier();
			}
		}
	}
}