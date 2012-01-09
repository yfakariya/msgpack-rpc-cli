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

namespace MsgPack.Rpc.Server
{
	/// <summary>
	///		Holds event data for <see cref="CallbackServer.Error"/> event.
	/// </summary>
	public sealed class CallbackServerErrorEventArgs : EventArgs
	{
		private readonly Exception _exception;

		/// <summary>
		///		Gets the exception thrown.
		/// </summary>
		/// <value>
		///		The exception thrown.
		/// </value>
		public Exception Exception
		{
			get { return this._exception; }
		}

		private readonly bool _isClientError;

		/// <summary>
		///		Gets a value indicating whether this instance is client error.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is client error; otherwise, <c>false</c>.
		/// </value>
		public bool IsClientError
		{
			get { return this._isClientError; }
		}

		/// <summary>
		///		Gets a value indicating whether this instance is server error.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is server error; otherwise, <c>false</c>.
		/// </value>
		public bool IsServerError
		{
			get { return !this._isClientError; }
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="CallbackServerErrorEventArgs"/> class.
		/// </summary>
		/// <param name="exception">The exception thrown.</param>
		/// <param name="isClientError"><c>true</c> if this instance is client error; otherwise, <c>false</c>.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="exception"/> is <c>null</c>.
		/// </exception>
		public CallbackServerErrorEventArgs( Exception exception, bool isClientError )
		{
			if ( exception == null )
			{
				throw new ArgumentNullException( "exception" );
			}

			this._exception = exception;
			this._isClientError = isClientError;
		}
	}
}
