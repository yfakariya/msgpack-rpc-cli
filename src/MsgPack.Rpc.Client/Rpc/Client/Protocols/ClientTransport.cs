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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MsgPack.Rpc.Protocols;
using MsgPack.Rpc.Protocols.Filters;

namespace MsgPack.Rpc.Client.Protocols
{
	// FIXME: timeout -> close transport
	/// <summary>
	///		Defines interface of client protocol binding.
	/// </summary>
	public abstract partial class ClientTransport : IDisposable, IContextBoundableTransport
	{
		private Socket _boundSocket;

		/// <summary>
		///		Gets the bound <see cref="Socket"/>.
		/// </summary>
		/// <value>
		///		The bound <see cref="Socket"/>.
		///		This value might be <c>null</c> when any sockets have not been bound, or underlying protocol does not rely socket.
		/// </value>
		public Socket BoundSocket
		{
			get { return Interlocked.CompareExchange( ref this._boundSocket, null, null ); }
			internal set { Interlocked.Exchange( ref this._boundSocket, value ); }
		}

		Socket IContextBoundableTransport.BoundSocket
		{
			get { return this._boundSocket; }
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

		private readonly ConcurrentDictionary<int, Action<ClientResponseContext, Exception, bool>> _pendingRequestTable;
		private readonly ConcurrentDictionary<long, Action<Exception, bool>> _pendingNotificationTable;

		private readonly ClientTransportManager _manager;

		/// <summary>
		///		Gets the <see cref="ClientTransportManager"/> which manages this instance.
		/// </summary>
		/// <value>
		///		The <see cref="ClientTransportManager"/> which manages this instance.
		///		This value will not be <c>null</c>.
		/// </value>
		protected internal ClientTransportManager Manager
		{
			get
			{
				Contract.Ensures( Contract.Result<ClientTransportManager>() != null );

				return this._manager;
			}
		}

		private int _shutdownSource;

		/// <summary>
		///		Gets a value indicating whether this instance is in shutdown.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is in shutdown; otherwise, <c>false</c>.
		/// </value>
		public bool IsClientShutdown
		{
			get { return Interlocked.CompareExchange( ref this._shutdownSource, 0, 0 ) == ( int )ShutdownSource.Client; }
		}

		/// <summary>
		///		Gets a value indicating whether this instance detectes server shutdown.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance detects server shutdown; otherwise, <c>false</c>.
		/// </value>
		public bool IsServerShutdown
		{
			get { return Interlocked.CompareExchange( ref this._shutdownSource, 0, 0 ) == ( int )ShutdownSource.Server; }
		}

		private bool IsInAnyShutdown
		{
			get { return Interlocked.CompareExchange( ref this._shutdownSource, 0, 0 ) != 0; }
		}

		private readonly IList<MessageFilter<ClientRequestContext>> _beforeSerializationFilters;

		internal IList<MessageFilter<ClientRequestContext>> BeforeSerializationFilters
		{
			get { return this._beforeSerializationFilters; }
		}

		private readonly IList<MessageFilter<ClientRequestContext>> _afterSerializationFilters;

		internal IList<MessageFilter<ClientRequestContext>> AfterSerializationFilters
		{
			get { return this._afterSerializationFilters; }
		}

		private readonly IList<MessageFilter<ClientResponseContext>> _beforeDeserializationFilters;

		internal IList<MessageFilter<ClientResponseContext>> BeforeDeserializationFilters
		{
			get { return this._beforeDeserializationFilters; }
		}

		private readonly IList<MessageFilter<ClientResponseContext>> _afterDeserializationFilters;

		internal IList<MessageFilter<ClientResponseContext>> AfterDeserializationFilters
		{
			get { return this._afterDeserializationFilters; }
		}

		private EventHandler<ShutdownCompletedEventArgs> _shutdownCompleted;

		/// <summary>
		///		Occurs when the initiated shutdown process is completed.
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
		///		Raises internal shutdown completion routine.
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

			var socket = Interlocked.Exchange( ref this._boundSocket, null );
			MsgPackRpcClientProtocolsTrace.TraceEvent(
				MsgPackRpcClientProtocolsTrace.TransportShutdownCompleted,
				"Transport shutdown is completed. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\" }}",
				socket == null ? IntPtr.Zero : socket.Handle,
				socket == null ? null : socket.RemoteEndPoint,
				socket == null ? null : socket.LocalEndPoint
			);

			if ( socket != null )
			{
				socket.Close();
			}

			// Notify shutdown to waiting clients.
			// Note that it is OK from concurrency point of view because additional modifications are guarded via shutdown flag.
			var errorMessage = new RpcErrorMessage( RpcError.TransportError, String.Format( CultureInfo.CurrentCulture, "Transport is shutdown. Shutdown source is: {0}", e.Source ), null );
			foreach ( var entry in this._pendingRequestTable )
			{
				entry.Value( null, errorMessage.ToException(), false );
			}

			foreach ( var entry in this._pendingNotificationTable )
			{
				entry.Value( errorMessage.ToException(), false );
			}

			this._pendingRequestTable.Clear();
			this._pendingNotificationTable.Clear();

			var handler = Interlocked.CompareExchange( ref this._shutdownCompleted, null, null );
			if ( handler != null )
			{
				handler( this, e );
			}
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="ClientTransport"/> class.
		/// </summary>
		/// <param name="manager">The manager which will manage this instance.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="manager"/> is <c>null</c>.
		/// </exception>
		protected ClientTransport( ClientTransportManager manager )
		{
			if ( manager == null )
			{
				throw new ArgumentNullException( "manager" );
			}

			Contract.EndContractBlock();

			this._manager = manager;
			this._pendingRequestTable = new ConcurrentDictionary<int, Action<ClientResponseContext, Exception, bool>>();
			this._pendingNotificationTable = new ConcurrentDictionary<long, Action<Exception, bool>>();

			this._beforeSerializationFilters =
				new ReadOnlyCollection<MessageFilter<ClientRequestContext>>(
					manager.Configuration.FilterProviders
					.OfType<MessageFilterProvider<ClientRequestContext>>()
					.Select( provider => provider.GetFilter( MessageFilteringLocation.BeforeSerialization ) )
					.Where( filter => filter != null )
					.ToArray()
				);
			this._afterSerializationFilters =
				new ReadOnlyCollection<MessageFilter<ClientRequestContext>>(
					manager.Configuration.FilterProviders
					.OfType<MessageFilterProvider<ClientRequestContext>>()
					.Select( provider => provider.GetFilter( MessageFilteringLocation.AfterSerialization ) )
					.Where( filter => filter != null )
					.ToArray()
				);
			this._beforeDeserializationFilters =
				new ReadOnlyCollection<MessageFilter<ClientResponseContext>>(
					manager.Configuration.FilterProviders
					.OfType<MessageFilterProvider<ClientResponseContext>>()
					.Select( provider => provider.GetFilter( MessageFilteringLocation.BeforeDeserialization ) )
					.Where( filter => filter != null )
					.Reverse()
					.ToArray()
				);
			this._afterDeserializationFilters =
				new ReadOnlyCollection<MessageFilter<ClientResponseContext>>(
					manager.Configuration.FilterProviders
					.OfType<MessageFilterProvider<ClientResponseContext>>()
					.Select( provider => provider.GetFilter( MessageFilteringLocation.AfterDeserialization ) )
					.Where( filter => filter != null )
					.Reverse()
					.ToArray()
				);
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

						MsgPackRpcClientProtocolsTrace.TraceEvent(
							MsgPackRpcClientProtocolsTrace.DisposeTransport,
							"Dispose transport. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\" }}",
							socket == null ? IntPtr.Zero : socket.Handle,
							socket == null ? null : socket.RemoteEndPoint,
							socket == null ? null : socket.LocalEndPoint
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
						MsgPackRpcClientProtocolsTrace.TraceEvent(
							MsgPackRpcClientProtocolsTrace.DisposeTransport,
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

		/// <summary>
		///		Initiates shutdown process.
		/// </summary>
		public void BeginShutdown()
		{
			if ( Interlocked.CompareExchange( ref this._shutdownSource, ( int )ShutdownSource.Client, 0 ) == 0 )
			{
				this.ShutdownSending();

				if ( this._pendingNotificationTable.Count == 0 && this._pendingRequestTable.Count == 0 )
				{
					this.ShutdownReceiving();
				}
			}
		}

		/// <summary>
		///		When overridden in the derived class, shutdowns the sending.
		/// </summary>
		protected virtual void ShutdownSending()
		{
			var socket = this.BoundSocket;
			MsgPackRpcClientProtocolsTrace.TraceEvent(
				MsgPackRpcClientProtocolsTrace.ShutdownSending,
				"Shutdown sending. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\" }}",
				socket == null ? IntPtr.Zero : socket.Handle,
				socket == null ? null : socket.RemoteEndPoint,
				socket == null ? null : socket.LocalEndPoint
			);
		}

		/// <summary>
		///		When overridden in the derived class, shutdowns the receiving.
		/// </summary>
		protected virtual void ShutdownReceiving()
		{
			var socket = this.BoundSocket;
			MsgPackRpcClientProtocolsTrace.TraceEvent(
				MsgPackRpcClientProtocolsTrace.ShutdownReceiving,
				"Shutdown receiving. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\" }}",
				socket == null ? IntPtr.Zero : socket.Handle,
				socket == null ? null : socket.RemoteEndPoint,
				socket == null ? null : socket.LocalEndPoint
			);

			this.OnShutdownCompleted( new ShutdownCompletedEventArgs( ( ShutdownSource )this._shutdownSource ) );
		}

		private void OnProcessFinished()
		{
			if ( this.IsInAnyShutdown )
			{
				if ( this._pendingNotificationTable.Count == 0 && this._pendingRequestTable.Count == 0 )
				{
					this.ShutdownReceiving();
				}
			}
		}

		private void OnSocketOperationCompleted( object sender, SocketAsyncEventArgs e )
		{
			var socket = sender as Socket;
			var context = e.GetContext();

			if ( !this.HandleSocketError( socket, context ) )
			{
				return;
			}

			switch ( context.LastOperation )
			{
				case SocketAsyncOperation.Send:
				case SocketAsyncOperation.SendTo:
				case SocketAsyncOperation.SendPackets:
				{
					var requestContext = context as ClientRequestContext;
					Contract.Assert( requestContext != null );
					this.OnSent( requestContext );
					break;
				}
				case SocketAsyncOperation.Receive:
				case SocketAsyncOperation.ReceiveFrom:
				case SocketAsyncOperation.ReceiveMessageFrom:
				{
					var responseContext = context as ClientResponseContext;
					Contract.Assert( responseContext != null );
					this.OnReceived( responseContext );
					break;
				}
				default:
				{
					MsgPackRpcClientProtocolsTrace.TraceEvent(
						MsgPackRpcClientProtocolsTrace.UnexpectedLastOperation,
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

		internal bool HandleSocketError( Socket socket, MessageContext context )
		{
			var rpcError = this.Manager.HandleSocketError( socket, context.SocketContext );
			if ( rpcError != null )
			{
				this.RaiseError( context.MessageId, context.SessionId, rpcError.Value, context.CompletedSynchronously );
			}

			return rpcError == null;
		}

		private void HandleDeserializationError( ClientResponseContext context, string message, Func<byte[]> invalidRequestHeaderProvider )
		{
			this.HandleDeserializationError( context, context.MessageId, new RpcErrorMessage( RpcError.RemoteRuntimeError, "Invalid stream.", message ), message, invalidRequestHeaderProvider );
		}

		private void HandleDeserializationError( ClientResponseContext context, int? messageId, RpcErrorMessage rpcError, string message, Func<byte[]> invalidRequestHeaderProvider )
		{
			if ( invalidRequestHeaderProvider != null && MsgPackRpcClientProtocolsTrace.ShouldTrace( MsgPackRpcClientProtocolsTrace.DumpInvalidResponseHeader ) )
			{
				var array = invalidRequestHeaderProvider();
				MsgPackRpcClientProtocolsTrace.TraceData( MsgPackRpcClientProtocolsTrace.DumpInvalidResponseHeader, BitConverter.ToString( array ), array );
			}

			this.RaiseError( messageId, context.SessionId, rpcError, context.CompletedSynchronously );
			// TODO: configurable
			context.NextProcess = this.DumpCorrupttedData;
		}

		private void RaiseError( int? messageId, long sessionId, RpcErrorMessage rpcError, bool completedSynchronously )
		{
			if ( messageId != null )
			{
				Action<ClientResponseContext, Exception, bool> handler = null;
				try
				{
					this._pendingRequestTable.TryRemove( messageId.Value, out handler );
				}
				finally
				{
					if ( handler == null )
					{
						this.HandleOrphan( messageId, sessionId, rpcError, null );
					}
					else
					{
						handler( null, rpcError.ToException(), completedSynchronously );
					}
				}
			}
			else
			{
				Action<Exception, bool> handler = null;
				try
				{
					this._pendingNotificationTable.TryRemove( sessionId, out handler );
				}
				finally
				{
					if ( handler == null )
					{
						this.HandleOrphan( messageId, sessionId, rpcError, null );
					}
					else
					{
						handler( rpcError.ToException(), completedSynchronously );
					}
				}
			}
		}

		private void HandleOrphan( ClientResponseContext context )
		{
			var error = ErrorInterpreter.UnpackError( context );
			MessagePackObject? returnValue = null;
			if ( error.IsSuccess )
			{
				returnValue = Unpacking.UnpackObject( context.ResultBuffer );
			}

			this.HandleOrphan( context.MessageId, context.SessionId, error, returnValue );
		}

		private void HandleOrphan( int? messageId, long sessionId, RpcErrorMessage rpcError, MessagePackObject? returnValue )
		{
			var socket = this.BoundSocket;
			MsgPackRpcClientProtocolsTrace.TraceEvent(
				MsgPackRpcClientProtocolsTrace.OrphanError,
				"There are no handlers to handle message which has MessageID:{0}, SessionID:{1}. This may indicate runtime problem or due to client recycling. {{ \"Socket\" : 0x{2:X}, \"RemoteEndPoint\" : \"{3}\", \"LocalEndPoint\" : \"{4}\", \"SessionID\" :{1}, \"MessageID\" : {0}, \"Error\" : {5}, \"ReturnValue\" : {6}, \"CallStack\" : \"{7}\" }}",
				messageId == null ? "(null)" : messageId.Value.ToString(),
				sessionId,
				socket == null ? IntPtr.Zero : socket.Handle,
				socket == null ? null : socket.RemoteEndPoint,
				socket == null ? null : socket.LocalEndPoint,
				rpcError,
				returnValue,
				new StackTrace( 0, true )
			);

			this._manager.HandleOrphan( messageId, sessionId, rpcError, returnValue );
		}

		private void DumpRequestData( DateTimeOffset sessionStartedAt, EndPoint destination, long sessionId, MessageType type, int? messageId, IList<ArraySegment<byte>> requestData )
		{
			if ( !this.Manager.Configuration.DumpCorruptResponse )
			{
				return;
			}

			using ( var stream = OpenDumpStream( sessionStartedAt, destination, sessionId, type, messageId ) )
			{
				foreach ( var segment in requestData )
				{
					stream.Write( segment.Array, segment.Offset, segment.Count );
				}

				stream.Flush();
			}
		}

		private static Stream OpenDumpStream( DateTimeOffset sessionStartedAt, EndPoint destination, long sessionId, MessageType type, int? messageId )
		{
			// TODO: configurable
			var filePath =
				Path.Combine(
					Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ),
					"MsgPack",
					"v" + typeof( ClientTransport ).Assembly.GetName().Version,
					"Client",
					"Dump",
					String.Format( CultureInfo.InvariantCulture, "{0:yyyy-MM-dd_HH-mm-ss}-{1}-{2}-{3}{4}.dat", sessionStartedAt, destination == null ? String.Empty : FileSystem.EscapeInvalidPathChars( destination.ToString(), "_" ), sessionId, type, messageId == null ? String.Empty : "-" + messageId )
				);

			var directoryPath = Path.GetDirectoryName( filePath );
			if ( !Directory.Exists( directoryPath ) )
			{
				Directory.CreateDirectory( directoryPath );
			}

#if !SILVERLIGHT
			return
				new FileStream(
					filePath,
					FileMode.Append,
					FileAccess.Write,
					FileShare.Read,
					64 * 1024,
					FileOptions.None
				);
#else
			return Stream.Null;
#endif
		}

		private static void ApplyFilters<T>( IList<MessageFilter<T>> filters, T context )
			where T : MessageContext
		{
			foreach ( var filter in filters )
			{
				filter.ProcessMessage( context );
			}
		}

		/// <summary>
		///		Gets the <see cref="ClientRequestContext"/> to store context information for request or notification.
		/// </summary>
		/// <returns>
		///		The <see cref="ClientRequestContext"/> to store context information for request or notification.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		///		This object is not ready to invoke this method.
		/// </exception>
		public virtual ClientRequestContext GetClientRequestContext()
		{
			var context = this.Manager.RequestContextPool.Borrow();
			context.SetTransport( this );
			context.RenewSessionId();
			return context;
		}

		/// <summary>
		///		Sends a request or notification message with the specified context.
		/// </summary>
		/// <param name="context">The context information.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="context"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="context"/> is not bound to this transport.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		This instance has been disposed.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///		This instance is in shutdown.
		///		Or the message ID or session ID is duplicated.
		/// </exception>
		/// <exception cref="RpcException">
		///		Failed to send request or notification to the server.
		/// </exception>
		public void Send( ClientRequestContext context )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			if ( !Object.ReferenceEquals( context.BoundTransport, this ) )
			{
				throw new ArgumentException( "Context is not bound to this object.", "context" );
			}

			this.VerifyIsNotDisposed();

			if ( this.IsClientShutdown )
			{
				throw new InvalidOperationException( "This transport is in shutdown." );
			}

			Contract.EndContractBlock();

			if ( this.IsServerShutdown )
			{
				throw new RpcErrorMessage( RpcError.TransportError, "Server did shutdown socket.", null ).ToException();
			}

			// Because exceptions here means client error, it should be handled like other client error.
			// Therefore, no catch clauses here.
			ApplyFilters( this._beforeSerializationFilters, context );

			context.Prepare();

			if ( context.MessageType == MessageType.Request )
			{
				if ( !this._pendingRequestTable.TryAdd( context.MessageId.Value, context.RequestCompletionCallback ) )
				{
					throw new InvalidOperationException( String.Format( CultureInfo.CurrentCulture, "Message ID '{0}' is already used.", context.MessageId ) );
				}
			}
			else
			{
				if ( !this._pendingNotificationTable.TryAdd( context.SessionId, context.NotificationCompletionCallback ) )
				{
					throw new InvalidOperationException( String.Format( CultureInfo.CurrentCulture, "Session ID '{0}' is already used.", context.MessageId ) );
				}
			}

			if ( MsgPackRpcClientProtocolsTrace.ShouldTrace( MsgPackRpcClientProtocolsTrace.SendOutboundData ) )
			{
				var socket = this.BoundSocket;
				MsgPackRpcClientProtocolsTrace.TraceEvent(
					MsgPackRpcClientProtocolsTrace.SendOutboundData,
					"Send request/notification. {{ \"SessionID\" : {0}, \"Socket\" : 0x{1:X}, \"RemoteEndPoint\" : \"{2}\", \"LocalEndPoint\" : \"{3}\", \"Type\" : \"{4}\", \"MessageID\" : {5}, \"Method\" : \"{6}\", \"BytesTransferring\" : {7} }}",
					context.SessionId,
					socket == null ? IntPtr.Zero : socket.Handle,
					socket == null ? null : socket.RemoteEndPoint,
					socket == null ? null : socket.LocalEndPoint,
					context.MessageType,
					context.MessageId,
					context.MethodName,
					context.SendingBuffer.Sum( segment => ( long )segment.Count )
				);
			}

			// Because exceptions here means client error, it should be handled like other client error.
			// Therefore, no catch clauses here.
			ApplyFilters( this._afterSerializationFilters, context );

			this.SendCore( context );
		}

		/// <summary>
		///		Performs protocol specific asynchronous 'Send' operation.
		/// </summary>
		/// <param name="context">Context information.</param>
		protected abstract void SendCore( ClientRequestContext context );

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
		protected virtual void OnSent( ClientRequestContext context )
		{
			if ( MsgPackRpcClientProtocolsTrace.ShouldTrace( MsgPackRpcClientProtocolsTrace.SentOutboundData ) )
			{
				var socket = this.BoundSocket;
				MsgPackRpcClientProtocolsTrace.TraceEvent(
					MsgPackRpcClientProtocolsTrace.SentOutboundData,
						"Sent request/notification. {{ \"SessionID\" : {0}, \"Socket\" : 0x{1:X}, \"RemoteEndPoint\" : \"{2}\", \"LocalEndPoint\" : \"{3}\", \"Type\" : \"{4}\", \"MessageID\" : {5}, \"Method\" : \"{6}\", \"BytesTransferred\" : {7} }}",
						context.SessionId,
						socket == null ? IntPtr.Zero : socket.Handle,
						socket == null ? null : socket.RemoteEndPoint,
						socket == null ? null : socket.LocalEndPoint,
						context.MessageType,
						context.MessageId,
						context.MethodName,
						context.BytesTransferred
					);
			}

			context.ClearBuffers();
			try
			{
				if ( context.MessageType == MessageType.Notification )
				{
					try
					{
						Action<Exception, bool> handler = null;
						try
						{
							this._pendingNotificationTable.TryRemove( context.SessionId, out handler );
						}
						finally
						{
							var rpcError = context.SocketError.ToClientRpcError();
							if ( handler != null )
							{
								handler( rpcError.IsSuccess ? null : rpcError.ToException(), context.CompletedSynchronously );
							}
						}
					}
					finally
					{
						this.OnProcessFinished();
					}
				}
				else
				{
					var responseContext = this.Manager.ResponseContextPool.Borrow();
					responseContext.SetTransport( this );
					responseContext.SessionId = context.SessionId;
					this.Receive( responseContext );
				}
			}
			finally
			{
				context.Clear();
				this.Manager.ReturnTransport( this );
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
		private void Receive( ClientResponseContext context )
		{
			Contract.Assert( context != null );
			Contract.Assert( context.BoundTransport == this, "Context is not bound to this object." );

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
				MsgPackRpcClientProtocolsTrace.TraceEvent(
					MsgPackRpcClientProtocolsTrace.BeginReceive,
					"Receive inbound data. {{  \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\" }}",
					socket == null ? IntPtr.Zero : socket.Handle,
					socket == null ? null : socket.RemoteEndPoint,
					socket == null ? null : socket.LocalEndPoint
				);
				this.ReceiveCore( context );
			}
		}

		private void DrainRemainingReceivedData( ClientResponseContext context )
		{
			// Process remaining binaries. This pipeline recursively call this method on other thread.
			if ( !context.NextProcess( context ) )
			{
				// Draining was not ended. Try to take next bytes.
				this.Receive( context );
			}

			// This method must be called on other thread on the above pipeline, so exit this thread.
		}

		/// <summary>
		///		Performs protocol specific asynchronous 'Receive' operation.
		/// </summary>
		/// <param name="context">Context information.</param>
		protected abstract void ReceiveCore( ClientResponseContext context );

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
		protected virtual void OnReceived( ClientResponseContext context )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			if ( MsgPackRpcClientProtocolsTrace.ShouldTrace( MsgPackRpcClientProtocolsTrace.ReceiveInboundData ) )
			{
				var socket = this.BoundSocket;
				MsgPackRpcClientProtocolsTrace.TraceEvent(
					MsgPackRpcClientProtocolsTrace.ReceiveInboundData,
					"Receive response. {{ \"SessionID\" : {0}, \"Socket\" : 0x{1:X}, \"RemoteEndPoint\" : \"{2}\", \"LocalEndPoint\" : \"{3}\", \"BytesTransfered\" : {4} }}",
					context.SessionId,
					socket == null ? IntPtr.Zero : socket.Handle,
					socket == null ? null : socket.RemoteEndPoint,
					socket == null ? null : socket.LocalEndPoint,
					context.BytesTransferred
				);
			}

			if ( context.BytesTransferred == 0 )
			{
				if ( Interlocked.CompareExchange( ref this._shutdownSource, ( int )ShutdownSource.Server, 0 ) == 0 )
				{
					this.ShutdownReceiving( context );

					// recv() returns 0 when the server socket shutdown gracefully.
					var socket = this.BoundSocket;
					MsgPackRpcClientProtocolsTrace.TraceEvent(
						MsgPackRpcClientProtocolsTrace.DetectServerShutdown,
						"Server shutdown current socket. {{ \"Socket\" : 0x{0:X}, \"RemoteEndPoint\" : \"{1}\", \"LocalEndPoint\" : \"{2}\" }}",
						socket == null ? IntPtr.Zero : socket.Handle,
						socket == null ? null : socket.RemoteEndPoint,
						socket == null ? null : socket.LocalEndPoint
					);
				}
				else if ( this.IsClientShutdown )
				{
					// Server sent shutdown response.
					this.ShutdownReceiving();
				}

				if ( !context.ReceivedData.Any( segment => 0 < segment.Count ) )
				{
					// There are no data to handle.
					return;
				}
			}
			else
			{
				context.ShiftCurrentReceivingBuffer();
			}

			// FIXME: Quota
			if ( MsgPackRpcClientProtocolsTrace.ShouldTrace( MsgPackRpcClientProtocolsTrace.DeserializeResponse ) )
			{
				MsgPackRpcClientProtocolsTrace.TraceEvent(
					MsgPackRpcClientProtocolsTrace.DeserializeResponse,
					"Deserialize response. {{ \"SessionID\" : {0}, \"Length\" : {1} }}",
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
				this.HandleDeserializationError( context, TryDetectMessageId( context ), new RpcErrorMessage( ex.RpcError, ex.Message, ex.DebugInformation ), "Filter rejects message.", () => context.ReceivedData.SelectMany( s => s.AsEnumerable() ).ToArray() );
				return;
			}

			if ( !context.NextProcess( context ) )
			{
				if ( this.IsServerShutdown )
				{
					this.ShutdownReceiving( context );
				}
				else
				{
					// Wait to arrive more data from server.
					this.ReceiveCore( context );
				}

				return;
			}
		}

		private static int? TryDetectMessageId( ClientResponseContext context )
		{
			if ( context.MessageId != null )
			{
				return context.MessageId;
			}

			using ( var stream = new ByteArraySegmentStream( context.ReceivedData ) )
			using ( var unpacker = Unpacker.Create( stream ) )
			{
				if ( !unpacker.Read() || !unpacker.IsArrayHeader || unpacker.Data.Value != 4 )
				{
					// Not a response message
					return null;
				}

				if ( !unpacker.Read() || !unpacker.Data.Value.IsTypeOf<Int32>().GetValueOrDefault() || unpacker.Data.Value != ( int )MessageType.Response )
				{
					// Not a response message or invalid message type
					return null;
				}

				if ( !unpacker.Read() || !unpacker.Data.Value.IsTypeOf<Int32>().GetValueOrDefault() )
				{
					// Invalid message ID.
					return null;
				}

				return unpacker.Data.Value.AsInt32();
			}
		}


		private void ShutdownReceiving( ClientResponseContext context )
		{
			context.Clear();
			this.ShutdownReceiving();
		}

		/// <summary>
		///		Returns the specified context to the <see cref="Manager"/>.
		/// </summary>
		/// <param name="context">The <see cref="ClientRequestContext"/> to be returned.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="context"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="context"/> is not bound to this transport.
		/// </exception>
		public void ReturnContext( ClientRequestContext context )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			if ( !Object.ReferenceEquals( context.BoundTransport, this ) )
			{
				throw new ArgumentException( "Context is not bound to this transport.", "context" );
			}

			Contract.EndContractBlock();

			context.UnboundTransport();
			this._manager.RequestContextPool.Return( context );
		}

		/// <summary>
		///		Returns the specified context to the <see cref="Manager"/>.
		/// </summary>
		/// <param name="context">The <see cref="ClientResponseContext"/> to be returned.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="context"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="context"/> is not bound to this transport.
		/// </exception>
		public void ReturnContext( ClientResponseContext context )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			if ( !Object.ReferenceEquals( context.BoundTransport, this ) )
			{
				throw new ArgumentException( "Context is not bound to this transport.", "context" );
			}

			Contract.EndContractBlock();

			this._manager.ResponseContextPool.Return( context );
		}
	}
}
