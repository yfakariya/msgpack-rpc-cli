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

namespace MsgPack.Rpc.Client
{
	// This class does NOT use Mono because they are too complicated to include here...

	/// <summary>
	///		Controls <see cref="Task"/> completion manually.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public sealed class TaskCompletionSource<T>
	{
		private readonly Task<T> _task;

		/// <summary>
		///		Gets the <see cref="Task{T}"/> to wait completion.
		/// </summary>
		/// <value>
		///		The <see cref="Task{T}"/> to wait completion.
		/// </value>
		public Task<T> Task
		{
			get { return this._task; }
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="TaskCompletionSource&lt;T&gt;"/> class.
		/// </summary>
		public TaskCompletionSource()
		{
			this._task = new Task<T>( null, null );
		}

		/// <summary>
		///		Sets the exceptional result of the <see cref="P:Task"/>.
		/// </summary>
		/// <param name="exception">The exception to be set.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="exception"/> is <c>null</c>.
		/// </exception>
		public void SetException( Exception exception )
		{
			if ( exception == null )
			{
				throw new ArgumentNullException( "exception" );
			}

			this.Task.SetException( exception );
		}

		/// <summary>
		///		Sets the result of the <see cref="P:Task"/>.
		/// </summary>
		/// <param name="result">The result to be set.</param>
		public void SetResult( T result )
		{
			this.Task.SetResult( result );
		}
	}
}
