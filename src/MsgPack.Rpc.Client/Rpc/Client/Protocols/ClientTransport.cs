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
using System.Net.Sockets;
using System.Diagnostics.Contracts;
using System.Collections.Concurrent;
using System.Threading;
using System.Globalization;
using MsgPack.Rpc.Serialization;
using MsgPack.Collections;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Define interface of client protocol binding.
	/// </summary>
	public abstract class ClientTransport : IDisposable, ITransportReceiveHandler
	{
		private const int _defaultSegmentSize = 32 * 1024;
		private const int _defaultSegmentCount = 1;

		protected int InitialSegmentSize
		{
			get
			{
				return this.Options == null ? _defaultSegmentSize : ( this.Options.BufferSegmentSize ?? _defaultSegmentSize );
			}
		}

		protected int InitialSegmentCount
		{
			get
			{
				return this.Options == null ? _defaultSegmentCount : ( this.Options.BufferSegmentCount ?? _defaultSegmentCount );
			}
		}

		private readonly RequestMessageSerializer _requestSerializer;
		private readonly ResponseMessageSerializer _responseSerializer;

		private readonly ClientEventLoop _eventLoop;

		protected ClientEventLoop EventLoop
		{
			get { return this._eventLoop; }
		}

		private readonly RpcClientOptions _options;

		public RpcClientOptions Options
		{
			get { return this._options; }
		}

		private readonly CountdownEvent _sessionTableLatch = new CountdownEvent( 1 );
		private readonly TimeSpan _drainTimeout;

		private readonly ConcurrentDictionary<int, IResponseHandler> _sessionTable = new ConcurrentDictionary<int, IResponseHandler>();

		private bool _disposed;

		protected ClientTransport( RpcTransportProtocol protocol, ClientEventLoop eventLoop, RpcClientOptions options )
		{
			if ( eventLoop == null )
			{
				throw new ArgumentNullException( "eventLoop" );
			}

			Contract.EndContractBlock();

			this._eventLoop = eventLoop;
			this._requestSerializer = ClientServices.RequestSerializerFactory.Create( protocol, options );
			this._responseSerializer = ClientServices.ResponseDeserializerFactory.Create( protocol, options );
			this._drainTimeout = options == null ? TimeSpan.FromSeconds( 3 ) : options.DrainTimeout ?? TimeSpan.FromSeconds( 3 );
			this._options = options ?? new RpcClientOptions();
			this._options.Freeze();
		}

		public void Dispose()
		{
			this.Dispose( true );
			GC.SuppressFinalize( this );
		}

		protected virtual void Dispose( bool disposing )
		{
			this.Drain();
			this._disposed = true;
		}

		protected void Drain()
		{
			this._sessionTableLatch.Signal();
			this._sessionTableLatch.Wait( this._drainTimeout, this.EventLoop.CancellationToken );
		}

		public void Send( MessageType type, int? messageId, String method, IList<object> arguments, Action<SendingContext, Exception, bool> onMessageSent, IResponseHandler responseHandler )
		{
			switch ( type )
			{
				case MessageType.Request:
				case MessageType.Notification:
				{
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException( "type", type, "'type' must be 'Request' or 'Notificatiion'." );
				}
			}

			if ( method == null )
			{
				throw new ArgumentNullException( "method" );
			}

			if ( String.IsNullOrWhiteSpace( method ) )
			{
				throw new ArgumentException( "'method' cannot be empty.", "method" );
			}

			if ( arguments == null )
			{
				throw new ArgumentNullException( "arguments" );
			}

			if ( this._disposed )
			{
				throw new ObjectDisposedException( this.ToString() );
			}

			Contract.EndContractBlock();

			var sendingContext = this.CreateNewSendingContext( messageId, onMessageSent );
			RpcErrorMessage serializationError = this._requestSerializer.Serialize( messageId, method, arguments, sendingContext.SendingBuffer );
			if ( !serializationError.IsSuccess )
			{
				throw new RpcTransportException( serializationError.Error, serializationError.Detail );
			}

			if ( messageId.HasValue )
			{
				try { }
				finally
				{
					this._sessionTableLatch.AddCount();
					if ( !this._sessionTable.TryAdd( messageId.Value, responseHandler ) )
					{
						throw new InvalidOperationException(
							String.Format( CultureInfo.CurrentCulture, "Message ID:{0} is already used.", messageId.Value )
						);
					}
				}
			}

			// Must set BufferList here.
			sendingContext.SocketContext.BufferList = sendingContext.SendingBuffer.Chunks;
			this.SendCore( sendingContext );
		}

		protected abstract SendingContext CreateNewSendingContext( int? messageId, Action<SendingContext, Exception, bool> onMessageSent );

		protected abstract void SendCore( SendingContext context );

		void ITransportReceiveHandler.OnReceive( ReceivingContext context )
		{
			ResponseMessage result;
			// FIXME: Feeding deserliaztion.
			//	If data is not enough, Deserialize return null.
			//  So this method return false, caller(EventLoop) retrieve more data.
			//  Feeding callback is NOT straight forward.
			var error = this._responseSerializer.Deserialize( context.ReceivingBuffer, out result );
			this.OnReceiveCore( context, result, error );
		}

		ChunkBuffer ITransportReceiveHandler.GetBufferForReceive( SendingContext context )
		{
			return this.GetBufferForReceiveCore( context );
		}

		protected virtual ChunkBuffer GetBufferForReceiveCore( SendingContext context )
		{
			// Reuse sending buffer.
			return context.SendingBuffer.Chunks;
		}

		ChunkBuffer ITransportReceiveHandler.ReallocateReceivingBuffer( ChunkBuffer oldBuffer, long requestedLength, ReceivingContext context )
		{
			return this.ReallocateReceivingBufferCore( oldBuffer, requestedLength, context );
		}

		protected virtual ChunkBuffer ReallocateReceivingBufferCore( ChunkBuffer oldBuffer, long requestedLength, ReceivingContext context )
		{
			return ChunkBuffer.CreateDefault( context.SessionContext.Options.BufferSegmentCount ?? 1, context.SessionContext.Options.BufferSegmentSize ?? ChunkBuffer.DefaultSegmentSize );
		}

		protected virtual void OnReceiveCore( ReceivingContext context, ResponseMessage response, RpcErrorMessage error )
		{
			IResponseHandler handler;
			bool removed;
			try { }
			finally
			{
				removed = this._sessionTable.TryRemove( response.MessageId, out handler );
				this._sessionTableLatch.Signal();
			}

			if ( removed )
			{
				if ( error.IsSuccess )
				{
					handler.HandleResponse( response, false );
				}
				else
				{
					handler.HandleError( error, false );
				}
			}
			else
			{
				// TODO: trace unrecognized receive message.
			}
		}
	}
}
