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

namespace MsgPack.Rpc.Server
{
	/// <summary>
	///		Represents event data of the <see cref="RpcServer.ServerError"/> event.
	/// </summary>
	public sealed class RpcServerErrorEventArgs : EventArgs
	{
		private readonly Exception _exception;

		/// <summary>
		///		Gets the exception occurred in the server stack.
		/// </summary>
		/// <value>
		///		The exception occurred in the server stack.
		///		This value will not be <c>null</c>.
		/// </value>
		public Exception Exception
		{
			get
			{
				Contract.Ensures( Contract.Result<Exception>() != null );

				return this._exception;
			}
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="RpcServerErrorEventArgs"/> class.
		/// </summary>
		/// <param name="exception">The exception occurred in the server stack.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="exception"/> is <c>null</c>.
		/// </exception>
		public RpcServerErrorEventArgs( Exception exception )
		{
			if ( exception == null )
			{
				throw new ArgumentNullException( "exception" );
			}

			Contract.EndContractBlock();

			this._exception = exception;
		}
	}
}
