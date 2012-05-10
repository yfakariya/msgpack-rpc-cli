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
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using MsgPack.Rpc.Protocols;
using System.Threading;
using System.Reflection;
using System.Net.Sockets;

namespace MsgPack.Rpc.Server.Protocols
{
	/// <summary>
	///		Implements <see cref="ServerTransport"/> with in-proc method invocation.
	/// </summary>
	/// <remarks>
	///		This transport only support one session per manager.
	/// </remarks>
	public sealed class InProcServerTransport : ServerTransport
	{
		protected override bool CanResumeReceiving
		{
			get { return true; }
		}

		private readonly InProcServerTransportManager _manager;

		/// <summary>
		///		The queue for inbound data.
		/// </summary>
		private readonly BlockingCollection<byte[]> _inboundQueue;

		/// <summary>
		///		The queue to store pending data.
		/// </summary>
		private readonly ConcurrentQueue<InProcPacket> _pendingPackets;

		private Task _receivingTask;
		private readonly CancellationTokenSource _receivingCancellationTokenSource;
		private readonly CancellationTokenSource _cancellationTokenSource;
		private int _isDisposed;

		private EventHandler _receiving;

		public event EventHandler Receiving
		{
			add
			{
				EventHandler oldHandler;
				EventHandler currentHandler = this._receiving;
				do
				{
					oldHandler = currentHandler;
					var newHandler = Delegate.Combine( oldHandler, value ) as EventHandler;
					currentHandler = Interlocked.CompareExchange( ref this._receiving, newHandler, oldHandler );
				} while ( oldHandler != currentHandler );
			}
			remove
			{
				EventHandler oldHandler;
				EventHandler currentHandler = this._receiving;
				do
				{
					oldHandler = currentHandler;
					var newHandler = Delegate.Remove( oldHandler, value ) as EventHandler;
					currentHandler = Interlocked.CompareExchange( ref this._receiving, newHandler, oldHandler );
				} while ( oldHandler != currentHandler );
			}
		}

		private void OnReceiving()
		{
			var handler = Interlocked.CompareExchange( ref this._receiving, null, null );
			if ( handler != null )
			{
				handler( this, EventArgs.Empty );
			}
		}

		private EventHandler _received;

		public event EventHandler Received
		{
			add
			{
				EventHandler oldHandler;
				EventHandler currentHandler = this._received;
				do
				{
					oldHandler = currentHandler;
					var newHandler = Delegate.Combine( oldHandler, value ) as EventHandler;
					currentHandler = Interlocked.CompareExchange( ref this._received, newHandler, oldHandler );
				} while ( oldHandler != currentHandler );
			}
			remove
			{
				EventHandler oldHandler;
				EventHandler currentHandler = this._received;
				do
				{
					oldHandler = currentHandler;
					var newHandler = Delegate.Remove( oldHandler, value ) as EventHandler;
					currentHandler = Interlocked.CompareExchange( ref this._received, newHandler, oldHandler );
				} while ( oldHandler != currentHandler );
			}
		}

		private void OnReceived()
		{
			var handler = Interlocked.CompareExchange( ref this._received, null, null );
			if ( handler != null )
			{
				handler( this, EventArgs.Empty );
			}
		}

		public event EventHandler<InProcResponseEventArgs> Response;

		private void OnResponse( InProcResponseEventArgs e )
		{
			var handler = this.Response;
			if ( handler != null )
			{
				handler( this, e );
			}
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="InProcServerTransport"/> class.
		/// </summary>
		/// <param name="manager">The manager which will manage this instance.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="manager"/> is <c>null</c>.
		/// </exception>
		public InProcServerTransport( InProcServerTransportManager manager )
			: base( manager )
		{
			this._manager = manager;
			this._inboundQueue = new BlockingCollection<byte[]>();
			this._pendingPackets = new ConcurrentQueue<InProcPacket>();
			manager.Response += this.OnManagerResponse;
			this._cancellationTokenSource = new CancellationTokenSource();
			this._receivingCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource( manager.CancellationToken, this._cancellationTokenSource.Token );
		}

		protected override void Dispose( bool disposing )
		{
			if ( disposing )
			{
				if ( Interlocked.Exchange( ref this._isDisposed, 1 ) == 0 )
				{
					this._cancellationTokenSource.Cancel();
					var receivingTask = Interlocked.CompareExchange( ref this._receivingTask, null, null );
					if ( receivingTask != null )
					{
						receivingTask.Wait( TimeSpan.FromSeconds( 1 ) );
					}

					this._receivingCancellationTokenSource.Dispose();
					this._inboundQueue.Dispose();
					this._manager.Response -= this.OnManagerResponse;
				}
			}

			base.Dispose( disposing );
		}

		private void OnManagerResponse( object sender, InProcResponseEventArgs e )
		{
			this.OnResponse( e );
		}

		protected sealed override void ShutdownSending()
		{
			this._manager.SendResponseData( new byte[ 0 ] );

			base.ShutdownSending();
		}

		protected override void ResetConnection()
		{
			this._manager.SendResponseData( new byte[ 0 ] );

			base.ResetConnection();
		}

		/// <summary>
		///		Feed specified data in current session.
		/// </summary>
		/// <param name="data">Data to be feeded.</param>
		/// <remarks>
		///		This method is thread safe.
		/// </remarks>
		public void FeedData( byte[] data )
		{
			if ( data == null )
			{
				throw new ArgumentNullException( "data" );
			}

			Contract.EndContractBlock();

			this._inboundQueue.Add( data, this._manager.CancellationToken );
		}

		internal void StartReceive( ServerRequestContext context )
		{
			if ( this._receivingTask == null )
			{
				Contract.Assert( context.BoundTransport == this );
				this._receivingTask =
					Task.Factory.StartNew(
						this.DoReceiveLoop,
						context,
						this._receivingCancellationTokenSource.Token,
						TaskCreationOptions.LongRunning,
						TaskScheduler.Default
					);
			}
		}

		private void DoReceiveLoop( object state )
		{
			var context = state as ServerRequestContext;
			try
			{
				while ( !this.IsDisposed && !this.IsServerShutdown && !this.IsClientShutdown && !this._receivingCancellationTokenSource.IsCancellationRequested )
				{
					this.Receive( context );
				}
			}
			catch ( OperationCanceledException ) { }
			catch ( AggregateException ex )
			{
				ex.Handle( inner => inner is OperationCanceledException );
				if ( ex.InnerExceptions.Count > 0 )
				{
					throw new TargetInvocationException( ex );
				}
			}
		}

		protected override void ReceiveCore( ServerRequestContext context )
		{
			this.OnReceiving();
			try
			{
				InProcPacket.ProcessReceive( this._inboundQueue, this._pendingPackets, context, this._receivingCancellationTokenSource.Token );
			}
			catch ( OperationCanceledException )
			{
				context.SocketError = SocketError.OperationAborted;
			}

			this.OnReceived( context );
			this.OnReceived();
		}

		protected override void SendCore( ServerResponseContext context )
		{
			this._manager.SendAsync( context )
				.ContinueWith(
					previous => this.OnSent( context )
				).Wait( this._receivingCancellationTokenSource.Token );
		}
	}
}