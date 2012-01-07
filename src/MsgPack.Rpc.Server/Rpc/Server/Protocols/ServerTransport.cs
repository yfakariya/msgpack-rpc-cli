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
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc.Server.Protocols
{
	// FIXME: timeout -> close transport
	/// <summary>
	///		Encapselates underlying transport layer protocols and handle low level errors.
	/// </summary>
	public abstract partial class ServerTransport : IDisposable, IContextBoundableTransport
	{
		private Socket _boundSocket;

		public Socket BoundSocket
		{
			get { return this._boundSocket; }
			internal set { this._boundSocket = value; }
		}

		Socket IContextBoundableTransport.BoundSocket
		{
			get { return this.BoundSocket; }
		}

		private int _processing;

		private bool _isClientShutdowned;

		private bool _isDisposed;

		public bool IsDisposed
		{
			get
			{
				return this._isDisposed;
			}
		}

		private readonly ServerTransportManager _manager;

		protected internal ServerTransportManager Manager
		{
			get { return this._manager; }
		}

		private readonly Dispatcher _dispatcher;

		private bool _isInShutdown;

		public bool IsInShutdown
		{
			get { return this._isInShutdown; }
		}

		private EventHandler<EventArgs> _shutdownCompleted;

		internal event EventHandler<EventArgs> ShutdownCompleted
		{
			add
			{
				EventHandler<EventArgs> oldHandler;
				EventHandler<EventArgs> currentHandler = this._shutdownCompleted;
				do
				{
					oldHandler = currentHandler;
					var newHandler = Delegate.Combine( oldHandler, value ) as EventHandler<EventArgs>;
					currentHandler = Interlocked.CompareExchange( ref this._shutdownCompleted, newHandler, oldHandler );
				} while ( oldHandler != currentHandler );
			}
			remove
			{
				EventHandler<EventArgs> oldHandler;
				EventHandler<EventArgs> currentHandler = this._shutdownCompleted;
				do
				{
					oldHandler = currentHandler;
					var newHandler = Delegate.Remove( oldHandler, value ) as EventHandler<EventArgs>;
					currentHandler = Interlocked.CompareExchange( ref this._shutdownCompleted, newHandler, oldHandler );
				} while ( oldHandler != currentHandler );
			}
		}

		protected virtual void OnShutdownCompleted()
		{
			var handler = Interlocked.CompareExchange( ref this._shutdownCompleted, null, null );
			if ( handler != null )
			{
				handler( this, EventArgs.Empty );
			}
		}

		private void OnProcessFinished()
		{
			if ( Interlocked.Decrement( ref this._processing ) == 0 )
			{
				if ( this._isInShutdown || this._isClientShutdowned )
				{
					this._boundSocket.Shutdown( SocketShutdown.Send );
					this.OnShutdownCompleted();
				}
			}
		}

		protected ServerTransport( ServerTransportManager manager )
		{
			if ( manager == null )
			{
				throw new ArgumentNullException( "manager" );
			}

			this._manager = manager;
			this._dispatcher = manager.Server.Configuration.DispatcherProvider( manager.Server );
		}

		/// <summary>
		///		Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			this.Dispose( true );
			GC.SuppressFinalize( this );
		}

		/// <summary>
		///		Releases unmanaged and - optionally - managed resources.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected virtual void Dispose( bool disposing )
		{
			if ( disposing )
			{
				if ( !this.IsDisposed )
				{
					this.DisposeLease();
					this._isDisposed = true;
					Thread.MemoryBarrier();
				}
			}
		}

		public void BeginShutdown()
		{
			if ( !this._isInShutdown )
			{
				this._isInShutdown = true;
				Thread.MemoryBarrier();
				this._boundSocket.Shutdown( SocketShutdown.Receive );
			}
		}

		private void VerifyIsNotDisposed()
		{
			if ( this.IsDisposed )
			{
				throw new ObjectDisposedException( this.ToString() );
			}
		}

		private void HandleDeserializationError( ServerRequestContext context, string message, Func<byte[]> invalidRequestHeaderProvider )
		{
			if ( invalidRequestHeaderProvider != null && MsgPackRpcServerProtocolsTrace.ShouldTrace( MsgPackRpcServerProtocolsTrace.DumpInvalidRequestHeader ) )
			{
				var array = invalidRequestHeaderProvider();
				MsgPackRpcServerProtocolsTrace.TraceData( MsgPackRpcServerProtocolsTrace.DumpInvalidRequestHeader, BitConverter.ToString( array ), array );
			}

			this.HandleDeserializationError( context, RpcError.MessageRefusedError, "Invalid stream.", message, invalidRequestHeaderProvider );
		}

		private void HandleDeserializationError( ServerRequestContext context, RpcError error, string message, string debugInformation, Func<byte[]> invalidRequestHeaderProvider )
		{
			this.BeginShutdown();
			int? messageId = context.MessageType == MessageType.Request ? context.MessageId : default( int? );
			var rpcError = new RpcErrorMessage( error, message, debugInformation );

			MsgPackRpcServerProtocolsTrace.TraceRpcError( 
				error,
				"Deserialization error. [ \"Message ID\" : {0}, \"Error Code\" : {1}, \"Error ID\" : \"{2}\", \"Detail\" : \"{3}\" ]",
				messageId == null ? "(null)" : messageId.ToString(),
				error.ErrorCode,
				error.Identifier,
				rpcError.Detail.ToString()
			);

			this.Manager.RaiseClientError( context, rpcError );
			context.Clear();
			this.SendError( messageId, rpcError );
		}

		private bool HandleSocketError( Socket socket, SocketAsyncEventArgs context )
		{
			return this.Manager.HandleSocketError( socket, context );
		}

		/// <summary>
		///		Dispatches <see cref="E:SocketAsyncEventArgs.Completed"/> event and handles socket level error.
		/// </summary>
		/// <param name="sender"><see cref="Socket"/>.</param>
		/// <param name="e">Event data.</param>
		private void OnSocketOperationCompleted( object sender, SocketAsyncEventArgs e )
		{
			var socket = sender as Socket;
			var context = e as MessageContext;

			if ( !this.HandleSocketError( socket, e ) )
			{
				return;
			}

			switch ( context.LastOperation )
			{
				case SocketAsyncOperation.Receive:
				case SocketAsyncOperation.ReceiveFrom:
				case SocketAsyncOperation.ReceiveMessageFrom:
				{
					var requestContext = context as ServerRequestContext;
					Contract.Assert( requestContext != null );
					this.OnReceived( requestContext );
					break;
				}
				case SocketAsyncOperation.Send:
				case SocketAsyncOperation.SendTo:
				case SocketAsyncOperation.SendPackets:
				{
					var responseContext = context as ServerResponseContext;
					Contract.Assert( responseContext != null );
					this.OnSent( responseContext );
					break;
				}
				default:
				{
					MsgPackRpcServerProtocolsTrace.TraceEvent(
						MsgPackRpcServerProtocolsTrace.UnexpectedLastOperation,
						"Unexpected operation. [ \"sender.Handle\" : 0x{0}, \"remoteEndPoint\" : \"{1}\", \"lastOperation\" : \"{2}\" ]",
						socket.Handle,
						context.RemoteEndPoint,
						context.LastOperation
					);
					break;
				}
			}
		}

		void IContextBoundableTransport.OnSocketOperationCompleted( object sender, SocketAsyncEventArgs e )
		{
			this.OnSocketOperationCompleted( sender, e );
		}


		/// <summary>
		///		Receives byte stream from remote end point.
		/// </summary>
		/// <param name="context">Context information.</param>
		///	<exception cref="InvalidOperationException">
		///		This instance is not in 'Idle' state.
		///	</exception>
		///	<exception cref="ObjectDisposedException">
		///		This instance is disposed.
		///	</exception>
		public void Receive( ServerRequestContext context )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			if ( context.BoundTransport != this )
			{
				throw new ArgumentException( "Context is not bound to this object.", "context" );
			}

			this.VerifyIsNotDisposed();

			this.PrivateReceive( context );
		}

		private void PrivateReceive( ServerRequestContext context )
		{
			if ( this._isClientShutdowned )
			{
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.ReceiveCanceledDueToClientShutdown,
					"Cancel receive due to client shutdown. [ \"RemoteEndPoint\" : \"{0}\", \"LocalEndPoint\" : \"{1}\" ]",
					this._boundSocket == null ? null : this._boundSocket.RemoteEndPoint,
					this._boundSocket == null ? null : this._boundSocket.LocalEndPoint
				);
				return;
			}

			if ( this.IsInShutdown )
			{
				// Subsequent receival cannot be processed now.
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.ReceiveCanceledDueToServerShutdown,
					"Cancel receive due to server shutdown. [ \"RemoteEndPoint\" : \"{0}\", \"LocalEndPoint\" : \"{1}\" ]",
					this._boundSocket == null ? null : this._boundSocket.RemoteEndPoint,
					this._boundSocket == null ? null : this._boundSocket.LocalEndPoint
				);
				return;
			}

			// First, drain last received request.
			if ( context.ReceivedData.Any( segment => 0 < segment.Count ) )
			{
				this.DrainRemainingReceivedData( context );
			}
			else
			{
				// There might be dirty data due to client shutdown.
				context.ReceivedData.Clear();
				Array.Clear( context.CurrentReceivingBuffer, 0, context.CurrentReceivingBuffer.Length );

				context.SetBuffer( context.CurrentReceivingBuffer, 0, context.CurrentReceivingBuffer.Length );

				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.BeginReceive,
					"Receive inbound data. [ \"RemoteEndPoint\" : \"{0}\", \"LocalEndPoint\" : \"{1}\" ]",
					this._boundSocket == null ? null : this._boundSocket.RemoteEndPoint,
					this._boundSocket == null ? null : this._boundSocket.LocalEndPoint
				);
				this.ReceiveCore( context );
			}
		}

		private void DrainRemainingReceivedData( ServerRequestContext context )
		{
			// Process remaining binaries. This pipeline recursively call this method on other thread.
			if ( !context.NextProcess( context ) )
			{
				// Draining was not ended. Try to take next bytes.
				this.PrivateReceive( context );
			}

			// This method must be called on other thread on the above pipeline, so exit this thread.
		}

		/// <summary>
		///		Performs protocol specific asynchronous 'Receive' operation.
		/// </summary>
		/// <param name="context">Context information.</param>
		protected abstract void ReceiveCore( ServerRequestContext context );

		/// <summary>
		///		Called when asynchronous 'Receive' operation is completed.
		/// </summary>
		/// <param name="context">Context information.</param>
		///	<exception cref="InvalidOperationException">
		///		This instance is not in 'Idle' nor 'Receiving' state.
		///	</exception>
		///	<exception cref="ObjectDisposedException">
		///		This instance is disposed.
		///	</exception>
		protected virtual void OnReceived( ServerRequestContext context )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			if ( MsgPackRpcServerProtocolsTrace.ShouldTrace( MsgPackRpcServerProtocolsTrace.ReceiveInboundData ) )
			{
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.ReceiveInboundData,
					"Receive request. [ \"Socket\" : 0x{0:x8}, \"LocalEndPoint\" : \"{1}\", \"RemoteEndPoint\" : \"{2}\", \"BytesTransfered\" : {3} ]",
					context.SessionId,
					this._boundSocket.Handle,
					this._boundSocket.LocalEndPoint,
					this._boundSocket.RemoteEndPoint,
					context.BytesTransferred
				);
			}

			if ( context.BytesTransferred == 0 )
			{
				// recv() returns 0 when the client socket shutdown gracefully.
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.DetectClientShutdown,
					"Client shutdown current socket. [ \"RemoteEndPoint\" : \"{0}\" ]",
					context.RemoteEndPoint
				);
				this._isClientShutdowned = true;
				if ( !context.ReceivedData.Any( segment => 0 < segment.Count ) )
				{
					// There are not data to handle.
					context.Clear();
					return;
				}
			}
			else
			{
				context.ShiftCurrentReceivingBuffer();
			}

			// FIXME: Quota
			if ( MsgPackRpcServerProtocolsTrace.ShouldTrace( MsgPackRpcServerProtocolsTrace.DeserializeRequest ) )
			{
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.DeserializeRequest,
					"Deserialize request. [ \"SessionID\" : {0}, \"Length\" : {1} ]",
					context.SessionId,
					context.ReceivedData.Sum( item => ( long )item.Count )
				);
			}

			// Go deserialization pipeline.
			if ( !context.NextProcess( context ) )
			{
				if ( this._isClientShutdowned )
				{
					// Client no longer send any additional data, so reset state.
					// TODO: Tracing
					return;
				}

				if ( this.IsInShutdown )
				{
					// Server no longer process any subsequent retrieval.
					// TODO: Tracing
					return;
				}

				// Wait to arrive more data from client.
				this.ReceiveCore( context );
				return;
			}
		}

		// TODO: Move to other layer e.g. Server.
		/// <summary>
		///		Sends specified RPC error as response.
		/// </summary>
		/// <param name="rpcError">
		///		Error.
		///	</param>
		///	<exception cref="InvalidOperationException">
		///		This instance is not in 'Reserved for Response' state.
		///	</exception>
		///	<exception cref="ObjectDisposedException">
		///		This instance is disposed.
		///	</exception>
		private void SendError( int? messageId, RpcErrorMessage rpcError )
		{
			if ( messageId == null )
			{
				this.OnProcessFinished();
				return;
			}

			var context = this.Manager.ResponseContextPool.Borrow();
			context.MessageId = messageId.Value;

			context.Serialize<object>( null, rpcError, null );
			this.PrivateSend( context );
		}

		public void Send( ServerResponseContext context )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			if ( context.BoundTransport != this )
			{
				throw new ArgumentException( "Context is not bound to this object.", "context" );
			}

			this.VerifyIsNotDisposed();
			this.PrivateSend( context );
		}

		private void PrivateSend( ServerResponseContext context )
		{
			context.Prepare();

			if ( MsgPackRpcServerProtocolsTrace.ShouldTrace( MsgPackRpcServerProtocolsTrace.SendOutboundData ) )
			{
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.SendOutboundData,
					"Send response. [ \"SessionID\" : {0}, \"RemoteEndPoint\" : {1}, \"BytesTransferring\" : {2} ]",
					context.SessionId,
					context.RemoteEndPoint,
					context.SendingBuffer.Sum( segment => ( long )segment.Count )
				);
			}

			this.SendCore( context );
		}

		/// <summary>
		///		Performs protocol specific asynchronous 'Send' operation.
		/// </summary>
		/// <param name="context">Context information.</param>
		protected abstract void SendCore( ServerResponseContext context );

		/// <summary>
		///		Called when asynchronous 'Send' operation is completed.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the subsequent request is already received;
		///		<c>false</c>, otherwise.
		/// </returns>
		///	<exception cref="InvalidOperationException">
		///		This instance is not in 'Sending' state.
		///	</exception>
		///	<exception cref="ObjectDisposedException">
		///		This instance is disposed.
		///	</exception>
		protected virtual void OnSent( ServerResponseContext context )
		{
			if ( MsgPackRpcServerProtocolsTrace.ShouldTrace( MsgPackRpcServerProtocolsTrace.SentOutboundData ) )
			{
				MsgPackRpcServerProtocolsTrace.TraceEvent(
						MsgPackRpcServerProtocolsTrace.SentOutboundData,
						"Sent response. [ \"SessionID\" : {0}, \"RemoteEndPoint\" : \"{1}\", \"BytesTransferred\" : {2} ]",
						context.SessionId,
						context.RemoteEndPoint,
						context.BytesTransferred
					);
			}

			context.Clear();
			try
			{
				this.OnProcessFinished();
			}
			finally
			{
				this.Manager.ReturnTransport( this );
			}
		}
	}
}
