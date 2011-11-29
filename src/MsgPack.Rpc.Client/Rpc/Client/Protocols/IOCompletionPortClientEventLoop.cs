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
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Collections.Concurrent;
using MsgPack.Rpc.Serialization;
using MsgPack.Collections;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		<see cref="ClientEventLoop"/> implementation taking into account of I/O Completion Port of Windows NT Kernel.
	/// </summary>
	public sealed class IOCompletionPortClientEventLoop : ClientEventLoop
	{
		public IOCompletionPortClientEventLoop( RpcClientOptions options, EventHandler<RpcTransportErrorEventArgs> errorHandler, CancellationTokenSource cancellationTokenSource )
			: base( options, errorHandler, cancellationTokenSource )
		{
		}

		protected override void ConnectCore( ConnectingContext context )
		{
			if ( !context.Client.Socket.ConnectAsync( context.Client.SocketContext ) )
			{
				this.OnConnected( context, true );
			}
		}

		protected sealed override void OnConnected( ConnectingContext context, bool completedSynchronously )
		{
			if ( context.Client.SocketContext.ConnectByNameError != null )
			{
				// error 
				context.Client.OnConnectError(
					RpcError.NetworkUnreacheableError,
					context.Client.SocketContext.ConnectByNameError,
					completedSynchronously,
					context.UserAsyncState
				);
				return;
			}

			if ( context.Client.SocketContext.SocketError != SocketError.Success )
			{
				// error 
				context.Client.OnConnectError(
					RpcError.NetworkUnreacheableError,
					new SocketException( ( int )context.Client.SocketContext.SocketError ),
					completedSynchronously,
					context.UserAsyncState
				);
				return;
			}

			base.OnConnected( context, completedSynchronously );
		}

		protected sealed override void SendCore( SendingContext context )
		{
			if ( context.CancellationToken.IsCancellationRequested )
			{
				// TODO : trace
				return;
			}

			if ( !context.SocketContext.SendAsync() )
			{
				context.SocketContext.OnSent( true );
			}
		}

		protected sealed override void OnSent( SendingContext context, bool completedSynchronously )
		{
			base.OnSent( context, completedSynchronously );
		}

		protected sealed override void SendToCore( SendingContext context )
		{
			if ( context.CancellationToken.IsCancellationRequested )
			{
				// TODO : trace
				return;
			}

			if ( !context.SocketContext.SendToAsync() )
			{
				context.SocketContext.OnSent( true );
			}
		}

		protected sealed override void ReceiveCore( ReceivingContext context )
		{
			if ( !context.SocketContext.ReceiveAsync() )
			{
				context.SocketContext.OnReceived( true );
			}
		}

		protected sealed override void ReceiveFromCore( ReceivingContext context )
		{
			if ( !context.SocketContext.ReceiveFromAsync() )
			{
				context.SocketContext.OnReceived( true );
			}
		}

		protected sealed override void OnReceived( ReceivingContext context, bool completedSynchronously )
		{
			if ( context.SessionContext.IsInContinuousRetrieval )
			{
				// Eventually continue blocked unpacking process.
				context.SessionContext.RetrievalWaitHandle.Set();
				return;
			}
			else
			{
				// Eventually invoke callback from ITransportReceiveHandler
				base.OnReceived( context, completedSynchronously );
			}
		}

		protected sealed override BufferFeeding FeedMore( int? requestedLength, RpcSocketAsyncEventArgs context )
		{
			Contract.Assume( context != null );
			Contract.Assume( context.UserToken is ReceivingContext );

			var receivingContext = ( ReceivingContext )context.UserToken;

			Contract.Assert( receivingContext.SocketContext != null );
			Contract.Assert( receivingContext.SocketContext.ConnectSocket != null );
			Contract.Assert( receivingContext.SocketContext.ConnectSocket.RemoteEndPoint != null );

			// TODO: Buffer recycling
			var newBuffer =
				receivingContext.ReceivingBuffer.Reallocate(
					requestedLength
					??
					( receivingContext.SessionContext.Options.BufferSegmentCount ?? 1 ) *
					( receivingContext.SessionContext.Options.BufferSegmentSize ?? GCChunkBuffer.DefaultSegmentSize ),
					receivingContext
				);

			Contract.Assert( newBuffer.Count > 0 );
			Contract.Assert( newBuffer.All( item => item.Count > 0 ) );

			receivingContext.SocketContext.BufferList = newBuffer;

			if ( receivingContext.SocketContext.ConnectSocket.Available > 0 )
			{
				return new BufferFeeding( context.ConnectSocket.Receive( context ), newBuffer );
			}
			else
			{
				receivingContext.SessionContext.IsInContinuousRetrieval = true;
				receivingContext.SessionContext.RetrievalWaitHandle.Reset();

				// TODO: timeout
				receivingContext.SessionContext.RetrievalWaitHandle.Wait( receivingContext.CancellationToken );

				return
					new BufferFeeding(
						( receivingContext.SocketContext.BytesTransferred > receivingContext.ReceivingBuffer.Chunks.TotalLength )
						? receivingContext.ReceivingBuffer.Chunks.TotalLength
						: receivingContext.SocketContext.BytesTransferred,
						newBuffer
					);
			}
		}
	}
}