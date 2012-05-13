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
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MsgPack.Rpc.Protocols;
using MsgPack.Rpc.Server.Protocols;

namespace MsgPack.Rpc.Client.Protocols
{
	// TODO: Implement more reliable stack specifically for shutdown...
	/// <summary>
	///		Implements <see cref="ClientTransport"/> with in-proc method invocation.
	/// </summary>
	/// <remarks>
	///		This transport only support one session per manager.
	/// </remarks>
	public sealed class InProcClientTransport : ClientTransport
	{
		/// <summary>
		///		The queue for inbound data.
		/// </summary>
		private readonly BlockingCollection<byte[]> _inboundQueue;

		/// <summary>
		///		The queue to store pending data.
		/// </summary>
		private readonly ConcurrentQueue<InProcPacket> _pendingPackets;

		private InProcServerTransport _destination;

		protected override bool CanResumeReceiving
		{
			get { return true; }
		}

		/// <summary>
		///		Occurs when message sent.
		/// </summary>
		public event EventHandler<InProcMessageSentEventArgs> MessageSent;

		private void OnMessageSent( InProcMessageSentEventArgs e )
		{
			var handler = this.MessageSent;
			if ( handler != null )
			{
				handler( this, e );
			}
		}

		public event EventHandler<InProcDataSendingEventArgs> DataSending;

		private void OnDataSending( InProcDataSendingEventArgs e )
		{
			var handler = this.DataSending;
			if ( handler != null )
			{
				handler( this, e );
			}
		}

		private int _canSend;

		/// <summary>
		///		Occurs when response received.
		/// </summary>
		public event EventHandler<InProcResponseReceivedEventArgs> ResponseReceived;

		private readonly CancellationTokenSource _cancellationTokenSource;
		private readonly CancellationTokenSource _linkedCancellationTokenSource;

		/// <summary>
		///		Initializes a new instance of the <see cref="InProcClientTransport"/> class.
		/// </summary>
		/// <param name="manager">The manager which will manage this instance.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="manager"/> is <c>null</c>.
		/// </exception>
		public InProcClientTransport( InProcClientTransportManager manager )
			: base( manager )
		{
			this._inboundQueue = new BlockingCollection<byte[]>();
			this._pendingPackets = new ConcurrentQueue<InProcPacket>();
			this._cancellationTokenSource = new CancellationTokenSource();
			this._linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource( this._cancellationTokenSource.Token, manager.CancellationToken );
			Interlocked.Exchange( ref this._canSend, 1 );
		}

		protected override void Dispose( bool disposing )
		{
			if ( disposing )
			{
				this.Disconnect();
			}

			base.Dispose( disposing );
		}

		protected override void ResetConnection()
		{
			this.Disconnect();

			base.ResetConnection();
		}

		private void Disconnect()
		{
			var destination = Interlocked.Exchange( ref this._destination, null );
			if ( destination != null )
			{
				destination.Response -= this.OnDestinationResponse;
			}

			this._cancellationTokenSource.Cancel();
			Interlocked.Exchange( ref this._canSend, 0 );
		}

		internal void SetDestination( InProcServerTransport destination )
		{
			this._destination = destination;
			destination.Response += this.OnDestinationResponse;
		}

		private void OnDestinationResponse( object sender, InProcResponseEventArgs e )
		{
			var handler = this.ResponseReceived;

			if ( handler == null )
			{
				this._inboundQueue.Add( e.Data, this._linkedCancellationTokenSource.Token );
				return;
			}

			var eventArgs = new InProcResponseReceivedEventArgs( e.Data );
			handler( this, eventArgs );
			if ( eventArgs.ChunkedReceivedData == null )
			{
				this._inboundQueue.Add( e.Data, this._linkedCancellationTokenSource.Token );
			}
			else
			{
				foreach ( var data in eventArgs.ChunkedReceivedData )
				{
					this._inboundQueue.Add( data, this._linkedCancellationTokenSource.Token );
				}
			}
		}

		protected sealed override void ShutdownSending()
		{
			Interlocked.Exchange( ref this._canSend, 0 );
			this._destination.FeedData( new byte[ 0 ] );

			base.ShutdownSending();
		}

		protected sealed override void SendCore( ClientRequestContext context )
		{
			var destination = this._destination;
			if ( destination == null )
			{
				throw new ObjectDisposedException( this.ToString() );
			}

			var data = context.BufferList.SelectMany( segment => segment.Array.Skip( segment.Offset ).Take( segment.Count ) ).ToArray();
			var dataEventArgs = new InProcDataSendingEventArgs() { Data = data };
			this.OnDataSending( dataEventArgs );

			if ( Interlocked.CompareExchange( ref this._canSend, 0, 0 ) != 0 )
			{
				destination.FeedData( dataEventArgs.Data );
				this.OnMessageSent( new InProcMessageSentEventArgs( context ) );
			}
			else
			{
				context.SocketError = SocketError.OperationAborted;
			}

			using ( var dummySocket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp ) )
			{
				if ( !this.HandleSocketError( dummySocket, context ) )
				{
					return;
				}
			}

			this.OnSent( context );
		}

		protected sealed override void ReceiveCore( ClientResponseContext context )
		{
			Task.Factory.StartNew(
				() =>
				{
					try
					{
						InProcPacket.ProcessReceive( this._inboundQueue, this._pendingPackets, context, this._linkedCancellationTokenSource.Token );
					}
					catch ( OperationCanceledException )
					{
						return;
					}

					this.OnReceived( context );
				}
			);
		}
	}
}
