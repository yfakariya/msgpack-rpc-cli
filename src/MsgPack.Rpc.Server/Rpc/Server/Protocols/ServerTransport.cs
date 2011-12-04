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
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using MsgPack.Rpc.Protocols;
using System.Collections.Generic;

namespace MsgPack.Rpc.Server.Protocols
{
	/// <summary>
	///		Encapselates underlying transport layer protocols and handle low level errors.
	/// </summary>
	public abstract partial class ServerTransport : IDisposable
	{
		/// <summary>
		///		State of this transport.
		/// </summary>
		private TransportState _state;

		/// <summary>
		///		Context information including socket state, session state, and buffers.
		/// </summary>
		private readonly ServerSocketAsyncEventArgs _context;

		private readonly DeserializationState _deserializationState;


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
			Tracer.Protocols.TraceEvent(
				Tracer.EventType.DispatchRequest,
				Tracer.EventId.DispatchRequest,
				"Dispatch request. [ \"type\" : \"{0}\", \"id\" : {1}, \"method\" : \"{2}\" ]",
				e.MessageType,
				e.Id,
				e.MethodName
			);

			var handler = this.MessageReceived;
			if ( handler != null )
			{
				handler( this, e );
			}
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="ServerTransport"/> class.
		/// </summary>
		/// <param name="context">The context information.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="context"/> is <c>null</c>.
		/// </exception>
		protected ServerTransport( ServerSocketAsyncEventArgs context )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			this._context = context;
			this._deserializationState = new DeserializationState( this.UnpackRequestHeader );
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
				this._context.Dispose();
				this._state = TransportState.Disposed;
			}
		}

		/// <summary>
		///		Verify internal state transition.
		/// </summary>
		/// <param name="desiredState">Desired next state.</param>
		private void VerifyState( TransportState desiredState )
		{
			if ( this._state == TransportState.Disposed )
			{
				throw new ObjectDisposedException( this.ToString() );
			}

			if ( this._state != desiredState )
			{
				throw new InvalidOperationException( String.Format( CultureInfo.CurrentCulture, "Tranport must be '{0}' but actual '{1}'.", desiredState, this._state ) );
			}
		}
		/// <summary>
		///		Initializes this transport for specified <see cref="EndPoint"/>.
		/// </summary>
		/// <param name="bindingEndPoint">The binding local end point.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="bindingEndPoint"/> is <c>null</c>.
		/// </exception>
		///	<exception cref="InvalidOperationException">
		///		This instance is not in 'Uninitialized' state.
		///	</exception>
		///	<exception cref="ObjectDisposedException">
		///		This instance is disposed.
		///	</exception>
		public void Initialize( EndPoint bindingEndPoint )
		{
			if ( bindingEndPoint == null )
			{
				throw new ArgumentNullException( "bindingEndPoint" );
			}

			this.VerifyState( TransportState.Uninitialized );
			this._context.Completed += OnSocketOperationCompleted;
			this.InitializeCore( this._context, bindingEndPoint );
			this._state = TransportState.Idle;
		}

		/// <summary>
		///		Performs derived class specific initialization for specified <see cref="EndPoint"/>.
		/// </summary>
		/// <param name="context">The context information. This value will not be <c>null</c>.</param>
		/// <param name="bindingEndPoint">The binding local end point. This value will not be <c>null</c>.</param>
		protected abstract void InitializeCore( ServerSocketAsyncEventArgs context, EndPoint bindingEndPoint );

		/// <summary>
		///		Dispatches <see cref="E:SocketAsyncEventArgs.Completed"/> event and handles socket level error.
		/// </summary>
		/// <param name="sender"><see cref="Socket"/>.</param>
		/// <param name="e">Event data.</param>
		private void OnSocketOperationCompleted( object sender, SocketAsyncEventArgs e )
		{
			var context = ( ServerSocketAsyncEventArgs )e;

			// FIXME: Error handling

			switch ( context.LastOperation )
			{
				case SocketAsyncOperation.Accept:
				{
					this.OnAcceptted( context );
					break;
				}
				case SocketAsyncOperation.Receive:
				case SocketAsyncOperation.ReceiveFrom:
				case SocketAsyncOperation.ReceiveMessageFrom:
				{
					this.OnReceived( context );
					break;
				}
				case SocketAsyncOperation.Send:
				case SocketAsyncOperation.SendTo:
				case SocketAsyncOperation.SendPackets:
				{
					this.OnSent( context );
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
		///		Called when asynchronous 'Accept' operation is completed.
		/// </summary>
		/// <param name="context">Context information.</param>
		///	<exception cref="InvalidOperationException">
		///		This instance is not in 'Idle' state.
		///	</exception>
		///	<exception cref="ObjectDisposedException">
		///		This instance is disposed.
		///	</exception>
		protected virtual void OnAcceptted( ServerSocketAsyncEventArgs context )
		{
			this.VerifyState( TransportState.Idle );
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
		protected void Receive( ServerSocketAsyncEventArgs context )
		{
			this.VerifyState( TransportState.Idle );
			context.SetBuffer( context.ReceivingBuffer, 0, context.ReceivingBuffer.Length );
			this.ReceiveCore( context );
		}

		/// <summary>
		///		Performs protocol specific asynchronous 'Receive' operation.
		/// </summary>
		/// <param name="context">Context information.</param>
		protected abstract void ReceiveCore( ServerSocketAsyncEventArgs context );

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
		protected virtual void OnReceived( ServerSocketAsyncEventArgs context )
		{
			if ( this._state == TransportState.Idle )
			{
				this._state = TransportState.Receiving;
				// TODO: Increment concurrency counter
			}
			else
			{
				this.VerifyState( TransportState.Receiving );
			}

			Tracer.Protocols.TraceEvent(
				Tracer.EventType.AcceptInboundTcp,
				Tracer.EventId.AcceptInboundTcp,
				"Receive request. [ \"socket\" : 0x{0:x8}, \"localEndPoint\" : \"{1}\", \"remoteEndPoint\" : \"{2}\", \"bytesTransfered\" : {3}, \"available\" : {4} ]",
				context.AcceptSocket.Handle,
				context.AcceptSocket.LocalEndPoint,
				context.AcceptSocket.RemoteEndPoint,
				context.BytesTransferred,
				context.AcceptSocket.ReceiveBufferSize
			);

			context.ReceivedData.Add( new ArraySegment<byte>( context.ReceivingBuffer, context.Offset, context.BytesTransferred ) );
			context.SetReceivingBufferOffset( context.BytesTransferred );

			// FIXME: Quota
			Tracer.Protocols.TraceEvent(
				Tracer.EventType.DeserializeRequest,
				Tracer.EventId.DeserializeRequest,
				"Deserialize request. [\"length\" : 0x{0:x}]",
				context.ReceivedData.Sum( item => ( long )item.Count )
			);

			if ( !this._deserializationState.NextProcess( context ) )
			{
				this.ReceiveCore( context );
				return;
			}
		}

		/// <summary>
		///		Free this context to enable receive subsequent request/notification. 
		/// </summary>
		///	<exception cref="InvalidOperationException">
		///		This instance is not in 'Reserved for Response' state.
		///	</exception>
		///	<exception cref="ObjectDisposedException">
		///		This instance is disposed.
		///	</exception>
		private void Free()
		{
			this.VerifyState( TransportState.Reserved );
			this._state = TransportState.Idle;
		}

		// TODO: Move to other layer e.g. Server.
		/// <summary>
		///		Sends specified return value as response.
		/// </summary>
		/// <param name="returnValue">
		///		Return value. 
		///		This value must be serializable via <see cref="T:MsgPack.Serialization.MessagePackSerializer{T}"/>. 
		///		This value can be <c>null</c>, but remote client might not understand nor permit it.
		///	</param>
		///	<exception cref="InvalidOperationException">
		///		This instance is not in 'Reserved for Response' state.
		///	</exception>
		///	<exception cref="ObjectDisposedException">
		///		This instance is disposed.
		///	</exception>
		public void Send( object returnValue )
		{
			this.VerifyState( TransportState.Reserved );
			this._state = TransportState.Sending;

			this.SendCore( null, returnValue );
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
		public void SendError( RpcErrorMessage rpcError )
		{
			this.VerifyState( TransportState.Reserved );
			this._state = TransportState.Sending;

			this.SendCore( rpcError.Error.Identifier, rpcError.Detail );
		}

		// TODO: Move to other layer e.g. Server.
		/// <summary>
		///		Sends specified data as response.
		/// </summary>
		/// <param name="errorData">
		///		Error identifier.
		/// </param>
		/// <param name="returnData">
		///		Return value or detailed error information.
		/// </param>
		private void SendCore( object errorData, object returnData )
		{
			Tracer.Protocols.TraceEvent(
				Tracer.EventType.SerializeResponse,
				Tracer.EventId.SerializeResponse,
				"Serialize response. [ \"errorData\" : \"{0}\", \"returnData\" : \"{1}\" ]",
				errorData,
				returnData
			);

			ArraySegment<byte> errorDataBytes;
			if ( errorData == null )
			{
				errorDataBytes = Constants.Nil;
			}
			else
			{
				using ( var packer = Packer.Create( this._context.ErrorDataBuffer, false ) )
				{
					this._context.SerializationContext.GetSerializer( errorData.GetType() ).PackTo( packer, errorData );
					errorDataBytes = new ArraySegment<byte>( this._context.ErrorDataBuffer.GetBuffer(), 0, unchecked( ( int )this._context.ErrorDataBuffer.Length ) );
				}
			}

			ArraySegment<byte> returnDataBytes;
			if ( returnData == null )
			{
				returnDataBytes = Constants.Nil;
			}
			else
			{
				using ( var packer = Packer.Create( this._context.ReturnDataBuffer, false ) )
				{
					this._context.SerializationContext.GetSerializer( returnData.GetType() ).PackTo( packer, returnData );
					returnDataBytes = new ArraySegment<byte>( this._context.ReturnDataBuffer.GetBuffer(), 0, unchecked( ( int )this._context.ReturnDataBuffer.Length ) );
				}
			}

			this.SendCore( errorDataBytes, returnDataBytes );
		}

		// TODO: Move to other layer e.g. Server.
		/// <summary>
		///		Sends specified data as response.
		/// </summary>
		/// <param name="errorData">
		///		Serialized error identifier.
		/// </param>
		/// <param name="returnData">
		///		Serialized return value or detailed error information.
		/// </param>
		/// <remarks>
		///		Dispatcher uses this method to avoid boxing.
		/// </remarks>
		internal void SendCore( ArraySegment<byte> errorData, ArraySegment<byte> returnData )
		{
			SerializeResponse( errorData, returnData );

			Tracer.Protocols.TraceEvent(
				Tracer.EventType.SendOutboundData,
				Tracer.EventId.SendOutboundData,
				"Send response. [ \"bytesTransferring\" : {0} ]",
				this._context.SendingBuffer.Sum( segment => ( long )segment.Count )
			);

			this.SendCore( this._context );
		}

		// TODO: Move to other layer e.g. Server.
		private void SerializeResponse( ArraySegment<byte> errorData, ArraySegment<byte> returnData )
		{
			using ( var packer = Packer.Create( this._context.IdBuffer, false ) )
			{
				packer.Pack( this._context.Id );
				this._context.SendingBuffer[ 1 ] = new ArraySegment<byte>( this._context.IdBuffer.GetBuffer(), 0, unchecked( ( int )this._context.IdBuffer.Position ) );
			}

			this._context.SendingBuffer[ 2 ] = errorData;
			this._context.SendingBuffer[ 3 ] = returnData;
			this._context.SetBuffer( null, 0, 0 );
			this._context.BufferList = this._context.SendingBuffer;
		}

		/// <summary>
		///		Performs protocol specific asynchronous 'Send' operation.
		/// </summary>
		/// <param name="context">Context information.</param>
		protected abstract void SendCore( ServerSocketAsyncEventArgs context );

		/// <summary>
		///		Called when asynchronous 'Send' operation is completed.
		/// </summary>
		/// <param name="context">Context information.</param>
		///	<exception cref="InvalidOperationException">
		///		This instance is not in 'Sending' state.
		///	</exception>
		///	<exception cref="ObjectDisposedException">
		///		This instance is disposed.
		///	</exception>
		protected virtual void OnSent( ServerSocketAsyncEventArgs context )
		{
			Tracer.Protocols.TraceEvent(
				Tracer.EventType.SentOutboundData,
				Tracer.EventId.SentOutboundData,
				"Sent response. [ \"remoteEndPoint\" : \"{0}\", \"bytesTransferred\" : {1} ]",
				context.RemoteEndPoint,
				context.BytesTransferred
			);
			this.VerifyState( TransportState.Sending );
			this._state = TransportState.Idle;
			context.BufferList = null;
			context.IdBuffer.SetLength( 0 );
			context.ErrorDataBuffer.SetLength( 0 );
			context.ReturnDataBuffer.SetLength( 0 );
			// TODO: Decrement concurrency counter
		}
	}
}
