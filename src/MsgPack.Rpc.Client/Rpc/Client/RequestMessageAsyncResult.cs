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
using System.Threading;
using MsgPack.Rpc.Protocols;
using MsgPack.Rpc.Client.Protocols;

namespace MsgPack.Rpc.Client
{
	/// <summary>
	///		<see cref="IAsyncResult"/> implementation for async RPC.
	/// </summary>
	internal sealed class RequestMessageAsyncResult : MessageAsyncResult
	{
		private ClientResponseContext _responseContext;

		public ClientResponseContext ResponseContext
		{
			get { return this._responseContext; }
		}

		public void OnCompleted( ClientResponseContext context, Exception exception, bool completedSynchronously )
		{
			if ( exception != null )
			{
				base.OnError( exception, completedSynchronously );
				return;
			}

			var error = ErrorInterpreter.UnpackError( context );
			if ( !error.IsSuccess )
			{
				base.OnError( error.ToException(), completedSynchronously );
				return;
			}

			Interlocked.Exchange( ref this._responseContext, context );
			base.Complete( completedSynchronously );
		}

		public RequestMessageAsyncResult( Object owner, int messageId, AsyncCallback asyncCallback, object asyncState )
			: base( owner, messageId, asyncCallback, asyncState ) { }
	}
}
