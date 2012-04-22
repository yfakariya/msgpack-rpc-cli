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
using MsgPack.Rpc.Protocols;
using MsgPack.Rpc.Server.Dispatch;

namespace MsgPack.Rpc.Server.Protocols
{
	// FIXME: timeout -> close transport (in send/receive/execute)
	/// <summary>
	///		Encapselates underlying transport layer protocols and handle low level errors.
	/// </summary>
	public abstract partial class ServerTransport : IDisposable, IContextBoundableTransport
	{
		#region -- Properties --

		private Socket _boundSocket;

		/// <summary>
		///		Gets the bound <see cref="Socket"/> to the this transport.
		/// </summary>
		/// <value>
		///		The bound <see cref="Socket"/> to the this transport.
		///		This value might be <c>null</c>.
		/// </value>
		/// <remarks>
		///		This value can be set via <see cref="ServerTransportManager{T}.BindSocket"/> utility method.
		/// </remarks>
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

		private int _isClientShutdown;

		/// <summary>
		///		Gets a value indicating whether the counterpart client is shutdown.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if the counterpart client is shutdown; otherwise, <c>false</c>.
		/// </value>
		public bool IsClientShutdown
		{
			get { return Interlocked.CompareExchange( ref this._isClientShutdown, 0, 0 ) != 0; }
		}

		private int _isDisposed;

		/// <summary>
		///		Gets a value indicating whether this instance is disposed.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is disposed; otherwise, <c>false</c>.
		/// </value>
		public bool IsDisposed
		{
			get { return Interlocked.CompareExchange( ref this._isDisposed, 0, 0 ) != 0; }
		}

		private readonly ServerTransportManager _manager;

		/// <summary>
		///		Gets the <see cref="ServerTransportManager"/> which manages this instance.
		/// </summary>
		/// <value>
		///		The <see cref="ServerTransportManager"/> which manages this instance.
		///		This value will not be <c>null</c>.
		/// </value>
		protected internal ServerTransportManager Manager
		{
			get
			{
				Contract.Ensures( Contract.Result<ServerTransportManager>() != null );

				return this._manager;
			}
		}

		private readonly Dispatcher _dispatcher;

		private int _isInShutdown;

		/// <summary>
		///		Gets a value indicating whether this instance is in shutdown.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is in shutdown; otherwise, <c>false</c>.
		/// </value>
		public bool IsInShutdown
		{
			get { return Interlocked.CompareExchange( ref this._isInShutdown, 0, 0 ) != 0; }
		}

		#endregion

		#region -- Events --

		private EventHandler<ClientShutdownEventArgs> _clientShutdown;

		/// <summary>
		/// Occurs when detects client shutdown.
		/// </summary>
		public event EventHandler<ClientShutdownEventArgs> ClientShutdown
		{
			add
			{
				EventHandler<ClientShutdownEventArgs> oldHandler;
				EventHandler<ClientShutdownEventArgs> currentHandler = this._clientShutdown;
				do
				{
					oldHandler = currentHandler;
					var newHandler = Delegate.Combine( oldHandler, value ) as EventHandler<ClientShutdownEventArgs>;
					currentHandler = Interlocked.CompareExchange( ref this._clientShutdown, newHandler, oldHandler );
				} while ( oldHandler != currentHandler );
			}
			remove
			{
				EventHandler<ClientShutdownEventArgs> oldHandler;
				EventHandler<ClientShutdownEventArgs> currentHandler = this._clientShutdown;
				do
				{
					oldHandler = currentHandler;
					var newHandler = Delegate.Remove( oldHandler, value ) as EventHandler<ClientShutdownEventArgs>;
					currentHandler = Interlocked.CompareExchange( ref this._clientShutdown, newHandler, oldHandler );
				} while ( oldHandler != currentHandler );
			}
		}

		/// <summary>
		///		Raises the <see cref="E:ClientShutdown"/> event.
		/// </summary>
		/// <param name="e">The <see cref="MsgPack.Rpc.Server.Protocols.ClientShutdownEventArgs"/> instance containing the event data.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="e"/> is <c>null</c>.
		/// </exception>
		protected virtual void OnClientShutdown( ClientShutdownEventArgs e )
		{
			if ( e == null )
			{
				throw new ArgumentNullException( "e" );
			}

			var handler = Interlocked.CompareExchange( ref this._clientShutdown, null, null );
			if ( handler != null )
			{
				handler( this, e );
			}
		}

		private EventHandler<EventArgs> _shutdownCompleted;

		/// <summary>
		///		Occurs when shutdown completed.
		/// </summary>
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

		/// <summary>
		///		Raises internal shutdown completion event to achieve graceful shutdown.
		/// </summary>
		protected virtual void OnShutdownCompleted()
		{
			MsgPackRpcServerProtocolsTrace.TraceEvent(
				MsgPackRpcServerProtocolsTrace.TransportShutdownCompleted,
				"Transport shutdown is completed. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\" }}",
				this._boundSocket == null ? IntPtr.Zero : this._boundSocket.Handle,
				this._boundSocket == null ? null : this._boundSocket.RemoteEndPoint,
				this._boundSocket == null ? null : this._boundSocket.LocalEndPoint
			);

			this.Manager.ReturnTransport( this );

			var handler = Interlocked.CompareExchange( ref this._shutdownCompleted, null, null );
			if ( handler != null )
			{
				handler( this, EventArgs.Empty );
			}
		}

		#endregion

		#region -- Initialization / Disposal --

		/// <summary>
		///		Initializes a new instance of the <see cref="ServerTransport"/> class.
		/// </summary>
		/// <param name="manager">The manager which will manage this instance.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="manager"/> is <c>null</c>.
		/// </exception>
		protected ServerTransport( ServerTransportManager manager )
		{
			if ( manager == null )
			{
				throw new ArgumentNullException( "manager" );
			}

			Contract.EndContractBlock();

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
				if ( Interlocked.Exchange( ref this._isDisposed, 1 ) == 0 )
				{
					try
					{
						MsgPackRpcServerProtocolsTrace.TraceEvent(
							MsgPackRpcServerProtocolsTrace.DisposeTransport,
							"Dispose transport. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\" }}",
							this._boundSocket == null ? IntPtr.Zero : this._boundSocket.Handle,
							this._boundSocket == null ? null : this._boundSocket.RemoteEndPoint,
							this._boundSocket == null ? null : this._boundSocket.LocalEndPoint
						);
					}
					catch ( ObjectDisposedException )
					{
						MsgPackRpcServerProtocolsTrace.TraceEvent(
							MsgPackRpcServerProtocolsTrace.DisposeTransport,
							"Dispose transport. {{ \"Socket\" : \"Disposed\", \"RemoteEndPoint\" : \"Disposed\", \"LocalEndPoint\" : \"Disposed\" }}"
						);
					}
				}
			}
		}

		private void VerifyIsNotDisposed()
		{
			if ( this.IsDisposed )
			{
				throw new ObjectDisposedException( this.ToString() );
			}
		}

		#endregion

		#region -- Shutdown --

		/// <summary>
		///		Called from the manager, begins graceful shutdown on this transport.
		/// </summary>
		internal void BeginShutdown()
		{
			if ( Interlocked.CompareExchange( ref this._isInShutdown, 1, 0 ) == 0 )
			{
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.BeginShutdownTransport,
					"Begin shutdown transport. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\" }}",
					this._boundSocket == null ? IntPtr.Zero : this._boundSocket.Handle,
					this._boundSocket == null ? null : this._boundSocket.RemoteEndPoint,
					this._boundSocket == null ? null : this._boundSocket.LocalEndPoint
				);

				this.ShutdownReceiving();

				if ( Interlocked.CompareExchange( ref this._processing, 0, 0 ) == 0 )
				{
					this.ShutdownSending();
					this.OnShutdownCompleted();
				}
			}
		}

		/// <summary>
		///		Bookkeeps current session is finished.
		/// </summary>
		private void OnSessionFinished()
		{
			if ( Interlocked.Decrement( ref this._processing ) == 0 )
			{
				if ( this.IsInShutdown || this.IsClientShutdown )
				{
					this.ShutdownSending();
					this.OnShutdownCompleted();
				}
			}
		}

		/// <summary>
		///		Shutdown sending on this transport.
		/// </summary>
		/// <remarks>
		///		Usually, the derived class shutdown its <see cref="Socket"/> with <see cref="SocketShutdown.Send"/>.
		/// </remarks>
		protected virtual void ShutdownSending()
		{
			MsgPackRpcServerProtocolsTrace.TraceEvent(
				MsgPackRpcServerProtocolsTrace.ShutdownSending,
				"Shutdown sending. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\" }}",
				this._boundSocket == null ? IntPtr.Zero : this._boundSocket.Handle,
				this._boundSocket == null ? null : this._boundSocket.RemoteEndPoint,
				this._boundSocket == null ? null : this._boundSocket.LocalEndPoint
			);
		}

		/// <summary>
		///		Shutdown receiving on this transport.
		/// </summary>
		/// <remarks>
		///		Usually, the derived class shutdown its <see cref="Socket"/> with <see cref="SocketShutdown.Receive"/>.
		/// </remarks>
		protected virtual void ShutdownReceiving()
		{
			MsgPackRpcServerProtocolsTrace.TraceEvent(
				MsgPackRpcServerProtocolsTrace.ShutdownReceiving,
				"Shutdown receiving. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\" }}",
				this._boundSocket == null ? IntPtr.Zero : this._boundSocket.Handle,
				this._boundSocket == null ? null : this._boundSocket.RemoteEndPoint,
				this._boundSocket == null ? null : this._boundSocket.LocalEndPoint
			);
		}

		#endregion

		#region -- Error Handling --

		/// <summary>
		///		Handle general deserialization error.
		/// </summary>
		/// <param name="context">The <see cref="ServerRequestContext"/> which holds context information.</param>
		/// <param name="message">The debugging message.</param>
		/// <param name="invalidRequestHeaderProvider">A delegate which returns raw binary to be dumped.</param>
		private void HandleDeserializationError( ServerRequestContext context, string message, Func<byte[]> invalidRequestHeaderProvider )
		{
			Contract.Assert( context != null );
			Contract.Assert( invalidRequestHeaderProvider != null );

			if ( invalidRequestHeaderProvider != null && MsgPackRpcServerProtocolsTrace.ShouldTrace( MsgPackRpcServerProtocolsTrace.DumpInvalidRequestHeader ) )
			{
				var array = invalidRequestHeaderProvider();
				MsgPackRpcServerProtocolsTrace.TraceData( MsgPackRpcServerProtocolsTrace.DumpInvalidRequestHeader, BitConverter.ToString( array ), array );
			}

			this.HandleDeserializationError( context, RpcError.MessageRefusedError, "Invalid stream.", message, invalidRequestHeaderProvider );
		}

		/// <summary>
		///		Handle specified deserialization error.
		/// </summary>
		/// <param name="context">The <see cref="ServerRequestContext"/> which holds context information.</param>
		/// <param name="error">The <see cref="RpcError"/> which represents the error.</param>
		/// <param name="message">The descriptive message which will be transfered to the client.</param>
		/// <param name="debugInformation">The debugging message.</param>
		/// <param name="invalidRequestHeaderProvider">A delegate which returns raw binary to be dumped.</param>
		private void HandleDeserializationError( ServerRequestContext context, RpcError error, string message, string debugInformation, Func<byte[]> invalidRequestHeaderProvider )
		{
			Contract.Assert( context != null );
			Contract.Assert( error != null );
			Contract.Assert( !String.IsNullOrEmpty( message ) );
			Contract.Assert( invalidRequestHeaderProvider != null );

			int? messageId = context.MessageType == MessageType.Request ? context.MessageId : default( int? );
			var rpcError = new RpcErrorMessage( error, message, debugInformation );

			MsgPackRpcServerProtocolsTrace.TraceRpcError(
				error,
				"Deserialization error. {{ \"Message ID\" : {0}, \"Error\" : {1} }}",
				messageId == null ? "(null)" : messageId.ToString(),
				rpcError
			);

			this.BeginShutdown();
			// Try send error response.
			this.SendError( messageId, rpcError );
			// Delegates to the manager to raise error event.
			this.Manager.RaiseClientError( context, rpcError );
			context.Clear();
		}

		/// <summary>
		///		Handles the socket level error.
		/// </summary>
		/// <param name="socket">The <see cref="Socket"/>.</param>
		/// <param name="context">The <see cref="System.Net.Sockets.SocketAsyncEventArgs"/> instance containing the event data.</param>
		/// <returns>
		///		<c>true</c>, if the error can be ignore, it is in shutdown which is initiated by another thread, for example; otherwise, <c>false</c>.
		/// </returns>
		private bool HandleSocketError( Socket socket, SocketAsyncEventArgs context )
		{
			// Delegates to the manager to raise error event.
			return this.Manager.HandleSocketError( socket, context );
		}

		#endregion

		#region -- Async Socket Event Handler --

		/// <summary>
		///		Dispatches <see cref="E:SocketAsyncEventArgs.Completed"/> event and handles socket level error.
		/// </summary>
		/// <param name="sender"><see cref="Socket"/>.</param>
		/// <param name="e">Event data.</param>
		private void OnSocketOperationCompleted( object sender, SocketAsyncEventArgs e )
		{
			var socket = sender as Socket;
			var context = e as MessageContext;

			Contract.Assert( socket != null );
			Contract.Assert( context != null );

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
						"Unexpected operation. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\", \"LastOperation\" : \"{3}\" }}",
						socket.Handle,
						socket.RemoteEndPoint,
						socket.LocalEndPoint,
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

		#endregion

		#region -- Receive --

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

			Contract.EndContractBlock();

			this.VerifyIsNotDisposed();

			this.PrivateReceive( context );
		}

		private void PrivateReceive( ServerRequestContext context )
		{
			Contract.Assert( context != null );

			if ( this.IsClientShutdown )
			{
				TraceCancelReceiveDueToClientShutdown( context );
				return;
			}

			if ( this.IsInShutdown )
			{
				// Subsequent receival cannot be processed now.
				TraceCancelReceiveDueToServerShutdown( context );
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
					"Receive inbound data. {{  \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\" }}",
					this._boundSocket == null ? IntPtr.Zero : this._boundSocket.Handle,
					this._boundSocket == null ? null : this._boundSocket.RemoteEndPoint,
					this._boundSocket == null ? null : this._boundSocket.LocalEndPoint
				);
				this.ReceiveCore( context );
			}
		}

		private void DrainRemainingReceivedData( ServerRequestContext context )
		{
			Contract.Assert( context != null );

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

			Contract.EndContractBlock();

			if ( this.IsClientShutdown )
			{
				// Client no longer send any additional data, so reset state.
				TraceCancelReceiveDueToClientShutdown( context );
				return;
			}

			if ( this.IsInShutdown )
			{
				// Server no longer process any subsequent retrieval.
				TraceCancelReceiveDueToServerShutdown( context );
				// Shutdown sending
				this.OnSessionFinished();
				return;
			}

			if ( MsgPackRpcServerProtocolsTrace.ShouldTrace( MsgPackRpcServerProtocolsTrace.ReceiveInboundData ) )
			{
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.ReceiveInboundData,
					"Receive request. {{ \"SessionID\" : {0}, \"Socket\" : 0x{1:X}, \"RemoteEndPoint\" : \"{2}\", \"LocalEndPoint\" : \"{3}\", \"BytesTransfered\" : {4} }}",
					context.SessionId,
					this._boundSocket == null ? IntPtr.Zero : this._boundSocket.Handle,
					this._boundSocket == null ? null : this._boundSocket.RemoteEndPoint,
					this._boundSocket == null ? null : this._boundSocket.LocalEndPoint,
					context.BytesTransferred
				);
			}

			if ( context.BytesTransferred == 0 )
			{
				// recv() returns 0 when the client socket shutdown gracefully.
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.DetectClientShutdown,
					"Client shutdown current socket. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\" }}",
					this._boundSocket == null ? IntPtr.Zero : this._boundSocket.Handle,
					this._boundSocket == null ? null : this._boundSocket.RemoteEndPoint,
					this._boundSocket == null ? null : this._boundSocket.LocalEndPoint
				);
				Interlocked.Exchange( ref this._isClientShutdown, 1 );
				this.OnClientShutdown( new ClientShutdownEventArgs( this, context.RemoteEndPoint ) );

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
					"Deserialize request. {{ \"SessionID\" : {0}, \"Length\" : {1} }}",
					context.SessionId,
					context.ReceivedData.Sum( item => ( long )item.Count )
				);
			}

			// Go deserialization pipeline.
			if ( !context.NextProcess( context ) )
			{
				if ( this.IsClientShutdown )
				{
					// Client no longer send any additional data, so reset state.
					TraceCancelReceiveDueToClientShutdown( context );
					return;
				}

				if ( this.IsInShutdown )
				{
					// Server no longer process any subsequent retrieval.
					TraceCancelReceiveDueToServerShutdown( context );
					// Shutdown sending
					this.OnSessionFinished();
					return;
				}

				// Wait to arrive more data from client.
				this.ReceiveCore( context );
			}
			else
			{
				// try next receive
				this.PrivateReceive( context );
			}
		}

		#region ---- Tracing ----

		private void TraceCancelReceiveDueToClientShutdown( ServerRequestContext context )
		{
			MsgPackRpcServerProtocolsTrace.TraceEvent(
				MsgPackRpcServerProtocolsTrace.ReceiveCanceledDueToClientShutdown,
				"Cancel receive due to client shutdown. {{ \"Socket\" : 0x{0:X} \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\", \"SessionID\" : {3} }}",
				this._boundSocket == null ? IntPtr.Zero : this._boundSocket.Handle,
				this._boundSocket == null ? null : this._boundSocket.RemoteEndPoint,
				this._boundSocket == null ? null : this._boundSocket.LocalEndPoint,
				context.SessionId
			);
		}

		private void TraceCancelReceiveDueToServerShutdown( ServerRequestContext context )
		{
			MsgPackRpcServerProtocolsTrace.TraceEvent(
				MsgPackRpcServerProtocolsTrace.ReceiveCanceledDueToServerShutdown,
				"Cancel receive due to server shutdown. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\", \"SessionID\" : {3} }}",
				this._boundSocket == null ? IntPtr.Zero : this._boundSocket.Handle,
				this._boundSocket == null ? null : this._boundSocket.RemoteEndPoint,
				this._boundSocket == null ? null : this._boundSocket.LocalEndPoint,
				context.SessionId
			);
		}

		#endregion

		#endregion

		#region -- Send --

		// TODO: Move to other layer e.g. Server.
		/// <summary>
		///		Sends specified RPC error as response.
		/// </summary>
		/// <param name="messageId">The message ID of the inbound message.</param>
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
				// This session is notification, so cannot send response.
				this.OnSessionFinished();
				return;
			}

			var context = this.Manager.ResponseContextPool.Borrow();
			context.MessageId = messageId.Value;

			context.Serialize<object>( null, rpcError, null );
			this.PrivateSend( context );
		}

		/// <summary>
		///		Sends response to the client of the current session.
		/// </summary>
		/// <param name="context">
		///		The <see cref="ServerResponseContext"/> holds context information of the reponse.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="context"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="context"/> is not bound to this transport.
		/// </exception>
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

			Contract.EndContractBlock();

			this.VerifyIsNotDisposed();
			this.PrivateSend( context );
		}

		private void PrivateSend( ServerResponseContext context )
		{
			Contract.Assert( context != null );

			context.Prepare();

			if ( MsgPackRpcServerProtocolsTrace.ShouldTrace( MsgPackRpcServerProtocolsTrace.SendOutboundData ) )
			{
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.SendOutboundData,
					"Send response. {{ \"SessionID\" : {0}, \"Socket\" : 0x{1:X}, \"RemoteEndPoint\" : \"{2}\", \"LocalEndPoint\" : \"{3}\", \"BytesTransferring\" : {4} }}",
					context.SessionId,
					this._boundSocket == null ? IntPtr.Zero : this._boundSocket.Handle,
					this._boundSocket == null ? null : this._boundSocket.RemoteEndPoint,
					this._boundSocket == null ? null : this._boundSocket.LocalEndPoint,
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
		/// <exception cref="ArgumentNullException">
		///		<paramref name="context"/> is <c>null</c>.
		/// </exception>
		///	<exception cref="InvalidOperationException">
		///		This instance is not in 'Sending' state.
		///	</exception>
		///	<exception cref="ObjectDisposedException">
		///		This instance is disposed.
		///	</exception>
		protected virtual void OnSent( ServerResponseContext context )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			Contract.EndContractBlock();

			if ( MsgPackRpcServerProtocolsTrace.ShouldTrace( MsgPackRpcServerProtocolsTrace.SentOutboundData ) )
			{
				MsgPackRpcServerProtocolsTrace.TraceEvent(
						MsgPackRpcServerProtocolsTrace.SentOutboundData,
						"Sent response. {{ \"SessionID\" : {0}, \"Socket\" : 0x{1:X}, \"RemoteEndPoint\" : \"{2}\", \"LocalEndPoint\" : \"{3}\", \"BytesTransferred\" : {4} }}",
						context.SessionId,
						this._boundSocket == null ? IntPtr.Zero : this._boundSocket.Handle,
						this._boundSocket == null ? null : this._boundSocket.RemoteEndPoint,
						this._boundSocket == null ? null : this._boundSocket.LocalEndPoint,
						context.BytesTransferred
					);
			}

			context.Clear();
			this.OnSessionFinished();
		}

		#endregion
	}
}
