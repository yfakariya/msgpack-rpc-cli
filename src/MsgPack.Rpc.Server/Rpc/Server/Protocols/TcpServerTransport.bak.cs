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
using MsgPack.Rpc.Serialization;
using MsgPack.Collections;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		<see cref="ServerTranport"/> for TCP/IP.
	/// </summary>
	internal sealed class TcpServerTransport : ServerTransport
	{
		private readonly RequestMessageSerializer _requestSerializer;
		private readonly ResponseMessageSerializer _responseSerializer;
		private readonly ServerEventLoop _eventLoop;
		private readonly RpcServerOptions _options;

		public int InitialBufferLength
		{
			get
			{
				return
					this._options == null
					? ServerEventLoop.DefaultInitialSendBufferSize
					: ( this._options.InitialSendBufferSize ?? ServerEventLoop.DefaultInitialSendBufferSize );
			}
		}

		public TcpServerTransport(
			ServerEventLoop eventLoop,
			RpcServerOptions options,
			RequestMessageSerializer requestMessageSerializer,
			ResponseMessageSerializer responseMessageSerializer
		)
		{
			if ( eventLoop == null )
			{
				throw new ArgumentNullException( "eventLoop" );
			}

			if ( requestMessageSerializer == null )
			{
				throw new ArgumentNullException( "requestMessageSerializer" );
			}

			if ( responseMessageSerializer == null )
			{
				throw new ArgumentNullException( "responseMessageSerializer" );
			}

			Contract.EndContractBlock();

			this._eventLoop = eventLoop;
			this._options = options;
			this._requestSerializer = requestMessageSerializer;
			this._responseSerializer = responseMessageSerializer;
		}

		protected sealed override void Dispose( bool disposing )
		{
			base.Dispose( disposing );
		}

		protected sealed override void OnReceivedCore( RpcServerSession session )
		{
			// Deserialize
			RequestMessage request;
#warning TODO: specify appropriate buffer.
			var error = this._requestSerializer.Deserialize( null, out request );
			if ( !error.IsSuccess )
			{
				this._eventLoop.HandleError( new RpcTransportErrorEventArgs( RpcTransportOperation.Deserialize, error ) );
				return;
			}

			// Fire Dispatch
			session.ProcessRequest( request );
		}

		protected sealed override void SendCore( RpcServerSession session, MessageType messageType, int messageId, object returnValue, bool isVoid, Exception exception )
		{
			RpcException rpcException = exception as RpcException;
			if ( rpcException == null && exception != null )
			{
				rpcException = new RpcException( RpcError.CallError, "Remote method throws exception.", exception.ToString() );
			}

			// FIXME: Buffer strategy
			RpcOutputBuffer buffer = new RpcOutputBuffer( ChunkBuffer.CreateDefault() );
			var error = this._responseSerializer.Serialize( messageId, returnValue, isVoid, rpcException, buffer );
			if ( !error.IsSuccess )
			{
				this._eventLoop.HandleError( new RpcTransportErrorEventArgs( RpcTransportOperation.Deserialize, messageId, error ) );
				return;
			}

			this._eventLoop.SendAsync( session.Context, buffer.ReadBytes() );
		}
	}
}
