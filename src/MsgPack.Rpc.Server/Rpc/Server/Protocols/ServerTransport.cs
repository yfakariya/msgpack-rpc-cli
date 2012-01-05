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
	/// <summary>
	///		Encapselates underlying transport layer protocols and handle low level errors.
	/// </summary>
	public abstract partial class ServerTransport : IDisposable
	{
		private Socket _boundSocket;

		protected internal Socket BoundSocket
		{
			get { return this._boundSocket; }
			internal set { this._boundSocket = value; }
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

		private readonly WeakReference _managerReference;

		protected internal ServerTransportManager Manager
		{
			get
			{
				if ( this._managerReference.IsAlive )
				{
					try
					{
						return this._managerReference.Target as ServerTransportManager;
					}
					catch ( InvalidOperationException ) { }
				}

				return null;
			}
		}

		private readonly Dispatcher _dispatcher;

		private bool _isInShutdown;

		public bool IsInShutdown
		{
			get { return this._isInShutdown; }
		}

		// TODO: Move to other layer e.g. Server.
		/// <summary>
		///		Occurs when request or notifiction mesage is received.
		/// </summary>
		public event EventHandler<RpcMessageReceivedEventArgs> MessageReceived;

		/// <summary>
		///		Raises the <see cref="E:MessageReceived"/> event.
		/// </summary>
		/// <param name="e">The <see cref="MsgPack.Rpc.Server.Protocols.RpcMessageReceivedEventArgs"/> instance containing the event data.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="e"/> is <c>null</c>.
		/// </exception>
		protected virtual void OnMessageReceived( RpcMessageReceivedEventArgs e )
		{
			if ( e == null )
			{
				throw new ArgumentNullException( "e" );
			}

			this.OnMessageReceivedCore( e );
		}

		private void OnMessageReceivedCore( RpcMessageReceivedEventArgs e )
		{
			if ( Tracer.Protocols.Switch.ShouldTrace( Tracer.EventType.DispatchRequest ) )
			{
				Tracer.Protocols.TraceEvent(
					Tracer.EventType.DispatchRequest,
					Tracer.EventId.DispatchRequest,
					"Dispatch request. [ \"type\" : \"{0}\", \"id\" : {1}, \"method\" : \"{2}\" ]",
					e.MessageType,
					e.Id,
					e.MethodName
				);
			}

			var handler = this.MessageReceived;
			if ( handler != null )
			{
				handler( this, e );
			}
		}

		internal event EventHandler AllResponseSent;

		protected virtual void OnAllResponseSent()
		{
			var handler = this.AllResponseSent;
			if ( handler != null )
			{
				handler( this, EventArgs.Empty );
			}
		}

		private void OnProcessFinished()
		{
			if ( Interlocked.Decrement( ref this._processing ) == 0 )
			{
				try
				{
					this.OnAllResponseSent();
				}
				finally
				{
					this._boundSocket.Shutdown( SocketShutdown.Send );
				}
			}
		}

		protected ServerTransport( ServerTransportManager manager )
		{
			if ( manager == null )
			{
				throw new ArgumentNullException( "manager" );
			}

			var server = manager.Server;
			if ( server == null )
			{
				throw new InvalidOperationException( "Cannot get RpcServer from the transport manager." );
			}

			this._managerReference = new WeakReference( manager );
			this._dispatcher = server.Configuration.DispatcherProvider( server );
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
			if ( invalidRequestHeaderProvider != null && Tracer.Protocols.Switch.ShouldTrace( Tracer.EventType.DumpInvalidRequestHeader ) )
			{
				var array = invalidRequestHeaderProvider();
				Tracer.Protocols.TraceData( Tracer.EventType.DumpInvalidRequestHeader, Tracer.EventId.DumpInvalidRequestHeader, BitConverter.ToString( array ), array );
			}

			this.HandleDeserializationError( context, RpcError.MessageRefusedError, "Invalid stream.", message, invalidRequestHeaderProvider );
		}

		private void HandleDeserializationError( ServerRequestContext context, RpcError error, string message, string debugInformation, Func<byte[]> invalidRequestHeaderProvider )
		{
			this.BeginShutdown();
			int? messageId = context.MessageType == MessageType.Request ? context.MessageId : default( int? );
			var rpcError = new RpcErrorMessage( error, message, debugInformation );
			this.HandleError( RpcTransportOperation.Deserialize, rpcError );
			context.Clear();

			this.SendError( messageId, rpcError );
		}

		internal void HandleError( RpcTransportOperation operation, RpcErrorMessage error )
		{
			this.HandleError( new RpcTransportErrorEventArgs( operation, error ) );
		}

		private void HandleError( RpcTransportErrorEventArgs e )
		{
			var rpcError =
				e.RpcError
				?? ( e.SocketErrorCode == null
				? new RpcErrorMessage( RpcError.RemoteRuntimeError, RpcError.RemoteRuntimeError.DefaultMessage )
				: e.SocketErrorCode.Value.ToServerRpcError() );
			Tracer.Protocols.TraceEvent(
				Tracer.EventType.ForRpcError( e.RpcError.Value.Error ),
				Tracer.EventId.ForRpcError( e.RpcError.Value.Error ),
				"Error. [ \"Message ID\" : {0}, \"Operation\" : \"{1}\", \"Error Code\" : {2}, \"Error ID\" : \"{3}\", \"Detail\" : \"{4}\" ]",
				e.MessageId == null ? "null" : e.MessageId.Value.ToString(),
				e.Operation,
				rpcError.Error.ErrorCode,
				rpcError.Error.Identifier,
				rpcError.Detail.ToString()
			);

			this.Manager.OnErrorCore( e );
		}

		/// <summary>
		///		Dispatches <see cref="E:SocketAsyncEventArgs.Completed"/> event and handles socket level error.
		/// </summary>
		/// <param name="sender"><see cref="Socket"/>.</param>
		/// <param name="e">Event data.</param>
		internal void OnSocketOperationCompleted( object sender, SocketAsyncEventArgs e )
		{
			var context = ( ServerContext )e;

			if ( !this.Manager.HandleError( sender, e ) )
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
					Tracer.Protocols.TraceEvent(
						Tracer.EventType.UnexpectedLastOperation,
						Tracer.EventId.UnexpectedLastOperation,
						"Unexpected operation. [ \"sender.Handle\" : 0x{0}, \"remoteEndPoint\" : \"{1}\", \"lastOperation\" : \"{2}\" ]",
						( ( Socket )sender ).Handle,
						context.RemoteEndPoint,
						context.LastOperation
					);
					break;
				}
			}
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
				Tracer.Protocols.TraceEvent(
					Tracer.EventType.ReceiveCanceledDueToClientShutdown,
					Tracer.EventId.ReceiveCanceledDueToClientShutdown,
					"Cancel receive due to client shutdown. [ \"RemoteEndPoint\" : \"{0}\", \"LocalEndPoint\" : \"{1}\" ]",
					context.AcceptSocket == null ? null : context.AcceptSocket.RemoteEndPoint,
					context.AcceptSocket == null ? null : context.AcceptSocket.LocalEndPoint
				);
				return;
			}

			if ( this.IsInShutdown )
			{
				// Subsequent receival cannot be processed now.
				Tracer.Protocols.TraceEvent(
					Tracer.EventType.ReceiveCanceledDueToServerShutdown,
					Tracer.EventId.ReceiveCanceledDueToServerShutdown,
					"Cancel receive due to server shutdown. [ \"RemoteEndPoint\" : \"{0}\", \"LocalEndPoint\" : \"{1}\" ]",
					context.AcceptSocket == null ? null : context.AcceptSocket.RemoteEndPoint,
					context.AcceptSocket == null ? null : context.AcceptSocket.LocalEndPoint
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
				Array.Clear( context.ReceivingBuffer, 0, context.ReceivingBuffer.Length );

				context.SetBuffer( context.ReceivingBuffer, 0, context.ReceivingBuffer.Length );

				Tracer.Protocols.TraceEvent(
					Tracer.EventType.BeginReceive,
					Tracer.EventId.BeginReceive,
					"Receive inbound data. [ \"RemoteEndPoint\" : \"{0}\", \"LocalEndPoint\" : \"{1}\" ]",
					context.AcceptSocket == null ? null : context.AcceptSocket.RemoteEndPoint,
					context.AcceptSocket == null ? null : context.AcceptSocket.LocalEndPoint
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

			if ( Tracer.Protocols.Switch.ShouldTrace( Tracer.EventType.ReceiveInboundData ) )
			{
				Tracer.Protocols.TraceEvent(
					Tracer.EventType.ReceiveInboundData,
					Tracer.EventId.ReceiveInboundData,
					"Receive request. [ \"Socket\" : 0x{0:x8}, \"LocalEndPoint\" : \"{1}\", \"RemoteEndPoint\" : \"{2}\", \"BytesTransfered\" : {3}, \"Available\" : {4} ]",
					context.SessionId,
					context.AcceptSocket.Handle,
					context.AcceptSocket.LocalEndPoint,
					context.AcceptSocket.RemoteEndPoint,
					context.BytesTransferred,
					context.AcceptSocket.ReceiveBufferSize
				);
			}

			if ( context.BytesTransferred == 0 )
			{
				// recv() returns 0 when the client socket shutdown gracefully.
				Tracer.Protocols.TraceEvent(
					Tracer.EventType.DetectClientShutdown,
					Tracer.EventId.DetectClientShutdown,
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
				context.ReceivedData.Add( new ArraySegment<byte>( context.ReceivingBuffer, context.Offset, context.BytesTransferred ) );
				context.SetReceivingBufferOffset( context.BytesTransferred );
			}

			// FIXME: Quota
			if ( Tracer.Protocols.Switch.ShouldTrace( Tracer.EventType.DeserializeRequest ) )
			{
				Tracer.Protocols.TraceEvent(
					Tracer.EventType.DeserializeRequest,
					Tracer.EventId.DeserializeRequest,
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

			var manager = this.Manager;
			if ( manager == null )
			{
				// TODO: Logging
				this.OnProcessFinished();
				return;
			}

			var context = manager.ResponseContextPool.Borrow();
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

			if ( Tracer.Protocols.Switch.ShouldTrace( Tracer.EventType.SendOutboundData ) )
			{
				Tracer.Protocols.TraceEvent(
					Tracer.EventType.SendOutboundData,
					Tracer.EventId.SendOutboundData,
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
			if ( Tracer.Protocols.Switch.ShouldTrace( Tracer.EventType.SentOutboundData ) )
			{
				Tracer.Protocols.TraceEvent(
						Tracer.EventType.SentOutboundData,
						Tracer.EventId.SentOutboundData,
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
				var manager = this.Manager;
				if ( manager != null )
				{
					manager.ReturnTransport( this );
				}
			}
		}
	}
}
