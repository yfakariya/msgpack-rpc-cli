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
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc.Server.Protocols
{
	/// <summary>
	///		Implements <see cref="ServerTransportManager{T}"/> for <see cref="InProcServerTransport"/>.
	/// </summary>
	public sealed class InProcServerTransportManager : ServerTransportManager<InProcServerTransport>
	{
		private readonly ConcurrentDictionary<long, BlockingCollection<byte[]>> _arrivedQueueTable;
		private readonly ConcurrentDictionary<long, BlockingCollection<byte[]>> _receivingQueueTable;


		/// <summary>
		///		<see cref="CancellationTokenSource"/> to process cancellation.
		/// </summary>
		private readonly CancellationTokenSource _cancellationTokenSource;

		/// <summary>
		///		Gets the cancellation token to subscribe cancellation.
		/// </summary>
		/// <value>
		///		The cancellation token to subscribe cancellation.
		/// </value>
		public CancellationToken CancellationToken
		{
			get { return this._cancellationTokenSource.Token; }
		}

		/// <summary>
		///		Occurs when the server send response.
		/// </summary>
		internal event EventHandler<InProcResponseEventArgs> Response;

		/// <summary>
		///		Raises the <see cref="E:Response"/> event.
		/// </summary>
		/// <param name="e">The <see cref="MsgPack.Rpc.InProcResponseEventArgs"/> instance containing the event data.</param>
		private void OnResponse( InProcResponseEventArgs e )
		{
			var handler = this.Response;
			if ( handler != null )
			{
				handler( this, e );
			}
		}

		public event EventHandler TransportReceiving;

		private void OnTransportReceiving()
		{
			var handler = this.TransportReceiving;
			if ( handler != null )
			{
				handler( this, EventArgs.Empty );
			}
		}

		public event EventHandler TransportReceived;

		private void OnTransportReceived()
		{
			var handler = this.TransportReceived;
			if ( handler != null )
			{
				handler( this, EventArgs.Empty );
			}
		}

		public event EventHandler<ShutdownCompletedEventArgs> TransportShutdownCompleted;

		private void OnTransportShutdownCompleted( ShutdownCompletedEventArgs e )
		{
			var handler = this.TransportShutdownCompleted;
			if ( handler != null )
			{
				handler( this, e );
			}
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="InProcServerTransportManager"/> class.
		/// </summary>
		/// <param name="server">The server which will host this instance.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="server"/> is <c>null</c>.
		/// </exception>
		public InProcServerTransportManager( RpcServer server, Func<InProcServerTransportManager, ObjectPool<InProcServerTransport>> transportPoolProvider )
			: base( server )
		{
			this._cancellationTokenSource = new CancellationTokenSource();
			this._arrivedQueueTable = new ConcurrentDictionary<long, BlockingCollection<byte[]>>();
			this._receivingQueueTable = new ConcurrentDictionary<long, BlockingCollection<byte[]>>();
			this.SetTransportPool( ( transportPoolProvider ?? ( manager => new OnTheFlyObjectPool<InProcServerTransport>( conf => new InProcServerTransport( manager ), null ) ) )( this ) );
		}

		protected sealed override void BeginShutdownCore()
		{
			this._cancellationTokenSource.Cancel();

			base.BeginShutdownCore();
		}

		protected sealed override void Dispose( bool disposing )
		{
			if ( !this._cancellationTokenSource.IsCancellationRequested )
			{
				this._cancellationTokenSource.Cancel();
			}

			this._cancellationTokenSource.Dispose();

			foreach ( var entry in this._arrivedQueueTable )
			{
				var queue = entry.Value;
				if ( queue != null )
				{
					queue.Dispose();
				}
			}

			foreach ( var entry in this._receivingQueueTable )
			{
				var queue = entry.Value;
				if ( queue != null )
				{
					queue.Dispose();
				}
			}

			base.Dispose( disposing );
		}

		protected override InProcServerTransport GetTransportCore( Socket bindingSocket )
		{
			var result = base.GetTransportCore( bindingSocket );
			result.Receiving += this.OnTransportReceiving;
			result.Received += this.OnTransportReceived;
			result.ShutdownCompleted += this.OnTransportShutdownCompleted;
			return result;
		}

		protected sealed override void ReturnTransportCore( InProcServerTransport transport )
		{
			transport.Receiving -= this.OnTransportReceiving;
			transport.Received -= this.OnTransportReceived;
		}

		private void OnTransportReceiving( object sender, EventArgs e )
		{
			this.OnTransportReceiving();
		}

		private void OnTransportReceived( object sender, EventArgs e )
		{
			this.OnTransportReceived();
		}

		private void OnTransportShutdownCompleted( object sender, ShutdownCompletedEventArgs e )
		{
			this.OnTransportShutdownCompleted( e );
		}

		/// <summary>
		///		Starts new session.
		/// </summary>
		/// <returns>
		///		<see cref="InProcServerTransport"/> to process in-proc communication from a client.
		/// </returns>
		public InProcServerTransport NewSession()
		{
			var result = this.GetTransport( null );
			result.StartReceive( this.GetRequestContext( result ) );
			return result;
		}

		/// <summary>
		///		Process in-proc communication sending response.
		/// </summary>
		/// <param name="context">The <see cref="ServerResponseContext"/>.</param>
		/// <returns>
		///		<see cref="Task"/> to be notified async operation result.
		/// </returns>
		internal Task SendAsync( ServerResponseContext context )
		{
			Contract.Assert( context != null );
			Contract.Assert( context.SocketContext.BufferList != null );

			var data = context.SocketContext.BufferList.SelectMany( b => b.Array.Skip( b.Offset ).Take( b.Count ) ).ToArray();
			context.SetBytesTransferred( data.Length );

			return
				Task.Factory.StartNew(
					state => this.SendResponseData( state as byte[] ),
					data
				);
		}

		/// <summary>
		///		Send specified data directly and synchronously as a response.
		/// </summary>
		/// <param name="data">Raw response data.</param>
		public void SendResponseData( byte[] data )
		{
			this.OnResponse( new InProcResponseEventArgs( data ) );
		}
	}
}
