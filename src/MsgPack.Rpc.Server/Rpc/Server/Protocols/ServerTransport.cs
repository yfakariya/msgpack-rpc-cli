#region -- License Terms --
//
// MessagePack for CLI
//
// Copyright (C) 2010-2013 FUJIWARA, Yusuke
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
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MsgPack.Rpc.Protocols;
using MsgPack.Rpc.Protocols.Filters;
using MsgPack.Rpc.Server.Dispatch;
using MsgPack.Serialization;

namespace MsgPack.Rpc.Server.Protocols
{
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
			get { return Interlocked.CompareExchange( ref this._boundSocket, null, null ); }
			internal set { Interlocked.Exchange( ref this._boundSocket, value ); }
		}

		Socket IContextBoundableTransport.BoundSocket
		{
			get { return this.BoundSocket; }
		}

		private int _processing;

		/// <summary>
		///		Gets a value indicating whether the protocol used by this class can resume receiving.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance can resume receiving; otherwise, <c>false</c>.
		/// </value>
		protected abstract bool CanResumeReceiving
		{
			get;
		}

		/// <summary>
		///		Gets a value indicating whether the underlying transport used by this instance can accept chunked buffer.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if the underlying transport can use chunked buffer; otherwise, <c>false</c>.
		/// 	This implementation returns <c>true</c>.
		/// </value>
		protected virtual bool CanUseChunkedBuffer
		{
			get { return true; }
		}


		#region ---- States ----

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

		private int _shutdownSource = ( int )ShutdownSource.Unknown;

		/// <summary>
		///		Gets a value indicating whether the counterpart client is shutdown.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if the counterpart client is shutdown; otherwise, <c>false</c>.
		/// </value>
		public bool IsClientShutdown
		{
			get { return Interlocked.CompareExchange( ref this._shutdownSource, 0, 0 ) == ( int )ShutdownSource.Client; }
		}

		/// <summary>
		///		Gets a value indicating whether this instance is in shutdown.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is in shutdown; otherwise, <c>false</c>.
		/// </value>
		public bool IsServerShutdown
		{
			get { return Interlocked.CompareExchange( ref this._shutdownSource, 0, 0 ) == ( int )ShutdownSource.Server; }
		}

		private bool IsInAnyShutdown
		{
			get { return Interlocked.CompareExchange( ref this._shutdownSource, 0, 0 ) != 0; }
		}

		private int _sendingShutdown;
		private int _receivingShutdown;

		private readonly ManualResetEventSlim _receivingShutdownEvent;

		#endregion

		#region ---- Filters ----

		private readonly IList<MessageFilter<ServerRequestContext>> _beforeDeserializationFilters;

		internal IList<MessageFilter<ServerRequestContext>> BeforeDeserializationFilters
		{
			get { return this._beforeDeserializationFilters; }
		}

		private readonly IList<MessageFilter<ServerResponseContext>> _afterSerializationFilters;

		internal IList<MessageFilter<ServerResponseContext>> AfterSerializationFilters
		{
			get { return this._afterSerializationFilters; }
		}

		#endregion

		#endregion

		#region -- Events --

		private EventHandler<ClientShutdownEventArgs> _clientShutdown;

		/// <summary>
		/// Occurs when detects client shutdown.
		/// </summary>
		internal event EventHandler<ClientShutdownEventArgs> ClientShutdown
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
		///		Raises internal client shutdown detection completion event to achieve graceful shutdown.
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

		private EventHandler<ShutdownCompletedEventArgs> _shutdownCompleted;

		/// <summary>
		///		Occurs when shutdown completed.
		/// </summary>
		internal event EventHandler<ShutdownCompletedEventArgs> ShutdownCompleted
		{
			add
			{
				EventHandler<ShutdownCompletedEventArgs> oldHandler;
				EventHandler<ShutdownCompletedEventArgs> currentHandler = this._shutdownCompleted;
				do
				{
					oldHandler = currentHandler;
					var newHandler = Delegate.Combine( oldHandler, value ) as EventHandler<ShutdownCompletedEventArgs>;
					currentHandler = Interlocked.CompareExchange( ref this._shutdownCompleted, newHandler, oldHandler );
				} while ( oldHandler != currentHandler );
			}
			remove
			{
				EventHandler<ShutdownCompletedEventArgs> oldHandler;
				EventHandler<ShutdownCompletedEventArgs> currentHandler = this._shutdownCompleted;
				do
				{
					oldHandler = currentHandler;
					var newHandler = Delegate.Remove( oldHandler, value ) as EventHandler<ShutdownCompletedEventArgs>;
					currentHandler = Interlocked.CompareExchange( ref this._shutdownCompleted, newHandler, oldHandler );
				} while ( oldHandler != currentHandler );
			}
		}

		/// <summary>
		///		Raises internal shutdown completion event to achieve graceful shutdown.
		/// </summary>
		/// <param name="e">The <see cref="MsgPack.Rpc.Protocols.ShutdownCompletedEventArgs"/> instance containing the event data.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="e"/> is <c>null</c>.
		/// </exception>
		protected virtual void OnShutdownCompleted( ShutdownCompletedEventArgs e )
		{
			if ( e == null )
			{
				throw new ArgumentNullException( "e" );
			}

			Contract.EndContractBlock();

			Contract.Assert( this._shutdownSource != 0 );
			var socket = Interlocked.Exchange( ref this._boundSocket, null );
			MsgPackRpcServerProtocolsTrace.TraceEvent(
				MsgPackRpcServerProtocolsTrace.TransportShutdownCompleted,
				"Transport shutdown is completed. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\" }}",
				GetHandle( socket ),
				GetRemoteEndPoint( socket, default( MessageContext ) ),
				GetLocalEndPoint( socket )
			);

			if ( socket != null )
			{
				socket.Close();
			}

			var handler = Interlocked.CompareExchange( ref this._shutdownCompleted, null, null );
			if ( handler != null )
			{
				handler( this, e );
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

			this._beforeDeserializationFilters =
				new ReadOnlyCollection<MessageFilter<ServerRequestContext>>(
					manager.Server.Configuration.FilterProviders
					.OfType<MessageFilterProvider<ServerRequestContext>>()
					.Select( provider => provider.GetFilter( MessageFilteringLocation.BeforeDeserialization ) )
					.Where( filter => filter != null )
					.ToArray()
				);
			this._afterSerializationFilters =
				new ReadOnlyCollection<MessageFilter<ServerResponseContext>>(
					manager.Server.Configuration.FilterProviders
					.OfType<MessageFilterProvider<ServerResponseContext>>()
					.Select( provider => provider.GetFilter( MessageFilteringLocation.AfterSerialization ) )
					.Where( filter => filter != null )
					.Reverse()
					.ToArray()
				);
			this._receivingShutdownEvent = new ManualResetEventSlim();
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
						var socket = this.BoundSocket;
						MsgPackRpcServerProtocolsTrace.TraceEvent(
							MsgPackRpcServerProtocolsTrace.DisposeTransport,
							"Dispose transport. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\" }}",
							GetHandle( socket ),
							GetRemoteEndPoint( socket, default( MessageContext ) ),
							GetLocalEndPoint( socket )
						);

						if ( Interlocked.CompareExchange( ref this._shutdownSource, ( int )ShutdownSource.Disposing, 0 ) == 0 )
						{
							var closingSocket = Interlocked.Exchange( ref this._boundSocket, null );
							if ( closingSocket != null )
							{
								closingSocket.Close();
							}
						}
					}
					catch ( ObjectDisposedException )
					{
						MsgPackRpcServerProtocolsTrace.TraceEvent(
							MsgPackRpcServerProtocolsTrace.DisposeTransport,
							"Dispose transport. {{ \"Socket\" : \"Disposed\", \"RemoteEndPoint\" : \"Disposed\", \"LocalEndPoint\" : \"Disposed\" }}"
						);
					}

					this._receivingShutdownEvent.Dispose();
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
		/// <returns>
		///		If shutdown process is initiated, then <c>true</c>.
		///		If shutdown is already initiated or completed, then <c>false</c>.
		/// </returns>
		internal bool BeginShutdown()
		{
			if ( Interlocked.CompareExchange( ref this._shutdownSource, ( int )ShutdownSource.Server, 0 ) == 0 )
			{
				var socket = this.BoundSocket;
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.BeginShutdownTransport,
					"Begin shutdown transport. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\" }}",
					GetHandle( socket ),
					GetRemoteEndPoint( socket, default( MessageContext ) ),
					GetLocalEndPoint( socket )
				);

				this.PrivateShutdownReceiving();
				this.TrySendShutdownSending();
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		///		Bookkeeps current session is finished.
		/// </summary>
		private void OnSessionFinished()
		{
			if ( Interlocked.Decrement( ref this._processing ) == 0 )
			{
				if ( this.IsInAnyShutdown )
				{
					this.PrivateShutdownSending();
				}
			}
		}

		/// <summary>
		///		Does shutdown sending if there are no active requests.
		/// </summary>
		private void TrySendShutdownSending()
		{
			if ( Interlocked.CompareExchange( ref this._processing, 0, 0 ) == 0 )
			{
				this.PrivateShutdownSending();
			}
		}

		private void PrivateShutdownSending()
		{
			if ( Interlocked.Exchange( ref this._sendingShutdown, 1 ) == 0 )
			{
				var socket = this.BoundSocket;
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.ShutdownSending,
					"Shutdown sending. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\" }}",
					GetHandle( socket ),
					GetRemoteEndPoint( socket, default( MessageContext ) ),
					GetLocalEndPoint( socket )
				);

				this.ShutdownSending();
			}
		}

		/// <summary>
		///		Shutdown sending on this transport.
		///		Do not call this method directly, or shutdown sequence may corrupt.
		/// </summary>
		/// <remarks>
		///		Usually, the derived class shutdown its <see cref="Socket"/> with <see cref="SocketShutdown.Send"/>.
		/// </remarks>
		protected virtual void ShutdownSending()
		{
			this._receivingShutdownEvent.Wait();
			this.OnShutdownCompleted( new ShutdownCompletedEventArgs( ( ShutdownSource )Interlocked.CompareExchange( ref this._shutdownSource, 0, 0 ) ) );
		}

		private void PrivateShutdownReceiving()
		{
			if ( Interlocked.Exchange( ref this._receivingShutdown, 1 ) == 0 )
			{
				var socket = this.BoundSocket;
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.ShutdownReceiving,
					"Shutdown receiving. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\" }}",
					GetHandle( socket ),
					GetRemoteEndPoint( socket, default( MessageContext ) ),
					GetLocalEndPoint( socket )
				);
				this.ShutdownReceiving();
			}
		}

		/// <summary>
		///		Shutdown receiving on this transport. 
		///		Do not call this method directly, or shutdown sequence may corrupt.
		/// </summary>
		/// <remarks>
		///		Usually, the derived class shutdown its <see cref="Socket"/> with <see cref="SocketShutdown.Receive"/>.
		/// </remarks>
		protected virtual void ShutdownReceiving()
		{
			this._receivingShutdownEvent.Set();
		}

		/// <summary>
		///		Resets the connection.
		/// </summary>
		protected virtual void ResetConnection()
		{
			var socket = this.BoundSocket;
			if ( socket != null )
			{
				// Reset immediately.
				socket.Close( 0 );
			}
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
			this.HandleDeserializationError( context, messageId, error, message, debugInformation, invalidRequestHeaderProvider );
		}

		/// <summary>
		///		Handle specified deserialization error.
		/// </summary>
		/// <param name="context">The <see cref="ServerRequestContext"/> which holds context information.</param>
		/// <param name="messageId">Detected Message ID.</param>
		/// <param name="error">The <see cref="RpcError"/> which represents the error.</param>
		/// <param name="message">The descriptive message which will be transfered to the client.</param>
		/// <param name="debugInformation">The debugging message.</param>
		/// <param name="invalidRequestHeaderProvider">A delegate which returns raw binary to be dumped.</param>
		private void HandleDeserializationError( ServerRequestContext context, int? messageId, RpcError error, string message, string debugInformation, Func<byte[]> invalidRequestHeaderProvider )
		{
			Contract.Assert( context != null );
			Contract.Assert( error != null );
			Contract.Assert( !String.IsNullOrEmpty( message ) );
			Contract.Assert( invalidRequestHeaderProvider != null );

			var rpcError = new RpcErrorMessage( error, message, debugInformation );

			MsgPackRpcServerProtocolsTrace.TraceRpcError(
				error,
				"Deserialization error. {{ \"Message ID\" : {0}, \"Error\" : {1} }}",
				messageId == null ? "(null)" : messageId.ToString(),
				rpcError
			);

			if ( invalidRequestHeaderProvider != null && MsgPackRpcServerProtocolsTrace.ShouldTrace( MsgPackRpcServerProtocolsTrace.DumpInvalidRequestHeader ) )
			{
				var array = invalidRequestHeaderProvider();
				MsgPackRpcServerProtocolsTrace.TraceData( MsgPackRpcServerProtocolsTrace.DumpInvalidRequestHeader, BitConverter.ToString( array ), array );
			}

			// Try send error response.
			this.SendError( context.RemoteEndPoint, context.SessionId, messageId, rpcError );
			// Delegates to the manager to raise error event.
			this.Manager.RaiseClientError( context, rpcError );
			context.Clear();

			this.BeginShutdown();
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

		#region -- Filter Application --

		private static void ApplyFilters<T>( IEnumerable<MessageFilter<T>> filters, T context )
			where T : MessageContext
		{
			foreach ( var filter in filters )
			{
				filter.ProcessMessage( context );
			}
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
			var context = e.GetContext();

			Contract.Assert( socket != null );
			Contract.Assert( context != null );

			if ( context.IsTimeout && context.SocketError == SocketError.OperationAborted )
			{
				return;
			}

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
						GetHandle( socket ),
						GetRemoteEndPoint( socket, e ),
						GetLocalEndPoint( socket ),
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

		private void OnSendTimeout( object sender, EventArgs e )
		{
			this.OnSendTimeout( sender as ServerResponseContext );
		}

		private void OnSendTimeout( OutboundMessageContext context )
		{
			Contract.Assert( context != null );

			var socket = this.BoundSocket;
			MsgPackRpcServerProtocolsTrace.TraceEvent(
				MsgPackRpcServerProtocolsTrace.SendTimeout,
					"Send timeout. {{  \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\", \"MessageId\" : {3}, \"BytesTransferred\" : {4}, \"Timeout\" : \"{5}\" }}",
					GetHandle( socket ),
					GetRemoteEndPoint( socket, context ),
					GetLocalEndPoint( socket ),
					context.MessageId,
					context.BytesTransferred,
					this._manager.Server.Configuration.SendTimeout
			);

			this.ResetConnection();
		}

		private void OnReceiveTimeout( object sender, EventArgs e )
		{
			this.OnReceiveTimeout( sender as ServerRequestContext );
		}

		private void OnReceiveTimeout( InboundMessageContext context )
		{
			Contract.Assert( context != null );

			var socket = this.BoundSocket;
			MsgPackRpcServerProtocolsTrace.TraceEvent(
				MsgPackRpcServerProtocolsTrace.ReceiveTimeout,
					"Receive timeout. {{  \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\", \"MessageId\" : {3}, \"BytesTransferred\" : {4}, \"Timeout\" : \"{5}\" }}",
					GetHandle( socket ),
					GetRemoteEndPoint( socket, context ),
					GetLocalEndPoint( socket ),
					context.MessageId,
					context.BytesTransferred,
					this._manager.Server.Configuration.ReceiveTimeout
			);

			if ( context.MessageId != null )
			{
				var rpcError = new RpcErrorMessage( RpcError.MessageRefusedError, "Receive timeout.", this._manager.Server.Configuration.ReceiveTimeout.ToString() );
				// Try send error response.
				this.SendError( context.RemoteEndPoint, context.SessionId, context.MessageId.Value, rpcError );
				// Delegates to the manager to raise error event.
				this.Manager.RaiseClientError( context as ServerRequestContext, rpcError );
				context.Clear();
			}

			this.ResetConnection();
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

			if ( this.IsServerShutdown )
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

				context.PrepareReceivingBuffer();

				var socket = this.BoundSocket;
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.BeginReceive,
					"Receive inbound data. {{  \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\" }}",
					GetHandle( socket ),
					GetRemoteEndPoint( socket, context ),
					GetLocalEndPoint( socket )
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
				this.FinishReceiving( context );

				return;
			}

			if ( this.IsServerShutdown )
			{
				// Server no longer process any subsequent retrieval.
				TraceCancelReceiveDueToServerShutdown( context );
				this.FinishReceiving( context );

				return;
			}

			if ( MsgPackRpcServerProtocolsTrace.ShouldTrace( MsgPackRpcServerProtocolsTrace.ReceiveInboundData ) )
			{
				var socket = this.BoundSocket;
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.ReceiveInboundData,
					"Receive request. {{ \"SessionID\" : {0}, \"Socket\" : 0x{1:X}, \"RemoteEndPoint\" : \"{2}\", \"LocalEndPoint\" : \"{3}\", \"BytesTransfered\" : {4} }}",
					context.SessionId,
					GetHandle( socket ),
					GetRemoteEndPoint( socket, context ),
					GetLocalEndPoint( socket ),
					context.BytesTransferred
				);
			}

			if ( context.BytesTransferred == 0 )
			{
				if ( Interlocked.CompareExchange( ref this._shutdownSource, ( int )ShutdownSource.Client, 0 ) == 0 )
				{
					this.PrivateShutdownReceiving();

					// recv() returns 0 when the client socket shutdown gracefully.
					var socket = this.BoundSocket;
					MsgPackRpcServerProtocolsTrace.TraceEvent(
						MsgPackRpcServerProtocolsTrace.DetectClientShutdown,
						"Client shutdown current socket. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\" }}",
						GetHandle( socket ),
						GetRemoteEndPoint( socket, context ),
						GetLocalEndPoint( socket )
					);

					this.OnClientShutdown( new ClientShutdownEventArgs( this, context.RemoteEndPoint ) );
				}

				if ( !context.ReceivedData.Any( segment => 0 < segment.Count ) )
				{
					this.FinishReceivingWithShutdown( context );
					return;
				}
			}
			else
			{
				context.ShiftCurrentReceivingBuffer();
			}

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

			// Exceptions here means message error.
			try
			{
				ApplyFilters( this._beforeDeserializationFilters, context );
			}
			catch ( RpcException ex )
			{
				this.HandleDeserializationError( context, TryDetectMessageId( context ), ex.RpcError, "Filter rejects request.", ex.Message, () => context.ReceivedData.SelectMany( s => s.AsEnumerable() ).ToArray() );
				this.FinishReceiving( context );
				return;
			}

			if ( !context.NextProcess( context ) )
			{
				if ( this.IsClientShutdown )
				{
					// Client no longer send any additional data, so reset state.
					TraceCancelReceiveDueToClientShutdown( context );
					this.FinishReceivingWithShutdown( context );
					return;
				}

				if ( this.IsServerShutdown )
				{
					// Server no longer process any subsequent retrieval.
					TraceCancelReceiveDueToServerShutdown( context );
					this.FinishReceiving( context );
					return;
				}

				if ( this.CanResumeReceiving )
				{
					// Wait to arrive more data from client.
					this.ReceiveCore( context );
					return;
				}
			}
			else if ( this.CanResumeReceiving )
			{
				// try next receive
				this.PrivateReceive( context );
				return;
			}

			this.FinishReceiving( context );
			return;
		}

		private void FinishReceivingWithShutdown( ServerRequestContext context )
		{
			// Shutdown sending
			if ( context.SessionId > 0 )
			{
				this.OnSessionFinished();
			}
			else
			{
				this.TrySendShutdownSending();
			}

			this.Manager.ReturnRequestContext( context );
		}

		private void FinishReceiving( ServerRequestContext context )
		{
			// Shutdown sending
			if ( context.SessionId > 0 )
			{
				this.OnSessionFinished();
			}

			this.Manager.ReturnRequestContext( context );
		}

		private static int? TryDetectMessageId( ServerRequestContext context )
		{
			if ( context.MessageId != null )
			{
				return context.MessageId;
			}

			using ( var stream = new ByteArraySegmentStream( context.ReceivedData ) )
			using ( var unpacker = Unpacker.Create( stream ) )
			{
				if ( !unpacker.Read() || !unpacker.IsArrayHeader || unpacker.LastReadData != 4 )
				{
					// Not a request message
					return null;
				}

				if ( !unpacker.Read() || !unpacker.LastReadData.IsTypeOf<Int32>().GetValueOrDefault() || unpacker.LastReadData != ( int )MessageType.Request )
				{
					// Not a request message or invalid message type
					return null;
				}

				if ( !unpacker.Read() || !unpacker.LastReadData.IsTypeOf<Int32>().GetValueOrDefault() )
				{
					// Invalid message ID.
					return null;
				}

				return unpacker.LastReadData.AsInt32();
			}
		}

		#region ---- Tracing ----

		private void TraceCancelReceiveDueToClientShutdown( ServerRequestContext context )
		{
			var socket = this.BoundSocket;
			MsgPackRpcServerProtocolsTrace.TraceEvent(
				MsgPackRpcServerProtocolsTrace.ReceiveCanceledDueToClientShutdown,
				"Cancel receive due to client shutdown. {{ \"Socket\" : 0x{0:X} \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\", \"SessionID\" : {3} }}",
				GetHandle( socket ),
				GetRemoteEndPoint( socket, context ),
				GetLocalEndPoint( socket ),
				context.SessionId
			);
		}

		private void TraceCancelReceiveDueToServerShutdown( ServerRequestContext context )
		{
			var socket = this.BoundSocket;
			MsgPackRpcServerProtocolsTrace.TraceEvent(
				MsgPackRpcServerProtocolsTrace.ReceiveCanceledDueToServerShutdown,
				"Cancel receive due to server shutdown. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\", \"SessionID\" : {3} }}",
				GetHandle( socket ),
				GetRemoteEndPoint( socket, context ),
				GetLocalEndPoint( socket ),
				context.SessionId
			);
		}

		#endregion

		#endregion

		#region -- Send --

		/// <summary>
		///		Sends specified RPC error as response.
		/// </summary>
		/// <param name="remoteEndPoint">The <see cref="EndPoint"/> of the destination.</param>
		/// <param name="sessionId">The session ID of cause session.</param>
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
		private void SendError( EndPoint remoteEndPoint, long sessionId, int? messageId, RpcErrorMessage rpcError )
		{
			if ( messageId == null )
			{
				// This session is notification, so cannot send response.
				this.OnSessionFinished();
				return;
			}

			var context = this.Manager.GetResponseContext( this, remoteEndPoint, sessionId, messageId.Value );

			context.Serialize<object>( null, rpcError, null );
			this.PrivateSend( context );
		}

		internal static void Serialize<T>( ServerResponseContext context, T returnValue, RpcErrorMessage error, MessagePackSerializer<T> returnValueSerializer )
		{
			if ( MsgPackRpcServerProtocolsTrace.ShouldTrace( MsgPackRpcServerProtocolsTrace.SerializeResponse ) )
			{
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.SerializeResponse,
					"Serialize response. {{ \"Error\" : {0}, \"ReturnValue\" : \"{1}\" }}",
					error,
					returnValue
				);
			}

			if ( error.IsSuccess )
			{
				context.ErrorDataPacker.PackNull();

				if ( returnValueSerializer == null )
				{
					// void
					context.ReturnDataPacker.PackNull();
				}
				else
				{
					returnValueSerializer.PackTo( context.ReturnDataPacker, returnValue );
				}
			}
			else
			{
				context.ErrorDataPacker.Pack( error.Error.Identifier );
				context.ReturnDataPacker.Pack( error.Detail );
			}
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

			context.Prepare( this.CanUseChunkedBuffer );

			if ( MsgPackRpcServerProtocolsTrace.ShouldTrace( MsgPackRpcServerProtocolsTrace.SendOutboundData ) )
			{
				var socket = this.BoundSocket;
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.SendOutboundData,
					"Send response. {{ \"SessionID\" : {0}, \"Socket\" : 0x{1:X}, \"RemoteEndPoint\" : \"{2}\", \"LocalEndPoint\" : \"{3}\", \"BytesTransferring\" : {4} }}",
					context.SessionId,
					GetHandle( socket ),
					GetRemoteEndPoint( socket, context ),
					GetLocalEndPoint( socket ),
					context.SendingBuffer.Sum( segment => ( long )segment.Count )
				);
			}

			// Because exceptions here means server error, it should be handled like other server error.
			// Therefore, no catch clauses here.
			ApplyFilters( this._afterSerializationFilters, context );

			if ( this._manager.Server.Configuration.SendTimeout != null )
			{
				context.Timeout += this.OnSendTimeout;
				context.StartWatchTimeout( this._manager.Server.Configuration.SendTimeout.Value );
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

			context.StopWatchTimeout();
			context.Timeout -= this.OnSendTimeout;

			if ( MsgPackRpcServerProtocolsTrace.ShouldTrace( MsgPackRpcServerProtocolsTrace.SentOutboundData ) )
			{
				var socket = this.BoundSocket;
				MsgPackRpcServerProtocolsTrace.TraceEvent(
						MsgPackRpcServerProtocolsTrace.SentOutboundData,
						"Sent response. {{ \"SessionID\" : {0}, \"Socket\" : 0x{1:X}, \"RemoteEndPoint\" : \"{2}\", \"LocalEndPoint\" : \"{3}\", \"BytesTransferred\" : {4} }}",
						context.SessionId,
						GetHandle( socket ),
						GetRemoteEndPoint( socket, context ),
						GetLocalEndPoint( socket ),
						context.BytesTransferred
					);
			}

			this.Manager.ReturnResponseContext( context );
			this.OnSessionFinished();
		}

		#endregion

		#region -- Tracing --

		internal static IntPtr GetHandle( Socket socket )
		{
			if ( socket != null )
			{
				try
				{
					return socket.Handle;
				}
				catch ( SocketException ) { }
				catch ( ObjectDisposedException ) { }
			}

			return IntPtr.Zero;
		}

		internal static EndPoint GetRemoteEndPoint( Socket socket, MessageContext context )
		{
			if ( context != null )
			{
				try
				{
					var result = context.RemoteEndPoint;
					if ( result != null )
					{
						return result;
					}
				}
				catch ( SocketException ) { }
				catch ( ObjectDisposedException ) { }
			}

			if ( socket != null )
			{
				try
				{
					return socket.RemoteEndPoint;
				}
				catch ( SocketException ) { }
				catch ( ObjectDisposedException ) { }
			}

			return null;
		}

		internal static EndPoint GetRemoteEndPoint( Socket socket, SocketAsyncEventArgs context )
		{
			if ( context != null )
			{
				try
				{
					var result = context.RemoteEndPoint;
					if ( result != null )
					{
						return result;
					}
				}
				catch ( SocketException ) { }
				catch ( ObjectDisposedException ) { }
			}

			if ( socket != null )
			{
				try
				{
					return socket.RemoteEndPoint;
				}
				catch ( SocketException ) { }
				catch ( ObjectDisposedException ) { }
			}

			return null;
		}

		internal static EndPoint GetLocalEndPoint( Socket socket )
		{
			if ( socket != null )
			{
				try
				{
					return socket.LocalEndPoint;
				}
				catch ( SocketException ) { }
				catch ( ObjectDisposedException ) { }
			}

			return null;
		}

		#endregion
	}
}
