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
using System.Net.Sockets;
using System.Threading;
using MsgPack.Collections;
using MsgPack.Rpc.Serialization;
using MsgPack.Rpc.Services;

namespace MsgPack.Rpc.Protocols
{
	public interface ITransportReceiveHandler
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="context"></param>
		/// <returns>If success to derialize from buffer then true. If buffer does not have enough data to deserialize then false.</returns>
		void OnReceive( ReceivingContext context );

		ChunkBuffer GetBufferForReceive( SendingContext context );

		ChunkBuffer ReallocateReceivingBuffer( ChunkBuffer oldBuffer, long requestedLength, ReceivingContext context );
	}

	/// <summary>
	///		Encapselates low-level transportation context which uses async socket.
	/// </summary>
	public sealed class ClientSessionContext : IDisposable
	{
		private readonly RpcSocketAsyncEventArgs _socketContext;

		public RpcSocketAsyncEventArgs SocketContext
		{
			get { return this._socketContext; }
		}

		private readonly RpcClientOptions _options;

		public RpcClientOptions Options
		{
			get { return this._options; }
		} 

		private readonly ITransportReceiveHandler _transportReceiveHandler;

		public ITransportReceiveHandler TransportReceiveHandler
		{
			get { return this._transportReceiveHandler; }
		}


		public object UserToken { get; set; }

		public bool IsInContinuousRetrieval { get; set; }

		private readonly Lazy<ManualResetEventSlim> _retrievalWaitHandle;

		public ManualResetEventSlim RetrievalWaitHandle
		{
			get { return _retrievalWaitHandle.Value; }
		}

		public ClientSessionContext(
			ITransportReceiveHandler transportReceiveHandler,
			RpcClientOptions options,
			RpcSocketAsyncEventArgs socketContext
		)
		{
			if ( transportReceiveHandler == null )
			{
				throw new ArgumentNullException( "transportReceiveHandler" );
			}

			if ( options == null )
			{
				throw new ArgumentNullException( "options" );
			}

			if ( socketContext == null )
			{
				throw new ArgumentNullException( "socketContext" );
			}

			Contract.EndContractBlock();

			this._transportReceiveHandler = transportReceiveHandler;
			this._options = options;
			this._socketContext = socketContext;
		}

		public void Dispose()
		{
			if ( this._retrievalWaitHandle.IsValueCreated )
			{
				this._retrievalWaitHandle.Value.Dispose();
			}
		}

		public int ReceivedBytes { get; set; }
	}
}
