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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MsgPack.Rpc.Protocols;
using System.Diagnostics.Contracts;
using MsgPack.Rpc.Dispatch;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Represents RPC session to specific client.
	/// </summary>
	public sealed class RpcServerSession : IDisposable
	{
		private readonly ServerTransport _transport;

		public ServerTransport Transport
		{
			get { return this._transport; }
		}

		private readonly ServerSessionContext _sessionContext;

		public ServerSessionContext Context
		{
			get { return this._sessionContext; }
		}

		private readonly Dispatcher _dispatcher;

		internal RpcServerSession( Dispatcher dispatcher, ServerTransport transport, ServerSessionContext context )
		{
			if ( dispatcher == null )
			{
				throw new ArgumentNullException( "dispatcher" );
			}

			if ( transport == null )
			{
				throw new ArgumentNullException( "transport" );
			}

			if ( !context.IsValid )
			{
				throw new ArgumentException( "'context' is invalid.", "context" );
			}

			Contract.EndContractBlock();

			this._dispatcher = dispatcher;
			this._transport = transport;
			this._sessionContext = context;
		}

		public void Dispose()
		{
			this._transport.Dispose();
		}

		internal void ProcessRequest( RequestMessage request )
		{
			var result = this._dispatcher.Dispatch( this, request.MessageId, request.Method, request.Arguments );
			this.Transport.Send( this, MessageType.Response, request.MessageId, result.ReturnValue, result.IsVoid, result.Exception );
		}
	}
}
