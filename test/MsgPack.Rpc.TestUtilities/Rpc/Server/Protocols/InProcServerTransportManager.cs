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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MsgPack.Rpc.Server.Protocols
{
	/// <summary>
	///		Implements <see cref="ServerTransportManager{T}"/> for <see cref="InProcServerTransport"/>.
	/// </summary>
	public sealed class InProcServerTransportManager : ServerTransportManager<InProcServerTransport>
	{
		/// <summary>
		///		The queue to emulate client to server communication.
		/// </summary>
		private readonly BlockingCollection<byte[]> _inboundQueue = new BlockingCollection<byte[]>();

		/// <summary>
		///		The dictionary to emulate continuous receiving.
		/// </summary>
		private readonly ConcurrentDictionary<ServerRequestContext, MemoryStream> _pendingRequestTable = new ConcurrentDictionary<ServerRequestContext, MemoryStream>();

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
		public event EventHandler<InProcResponseEventArgs> Response;

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

		/// <summary>
		///		Initializes a new instance of the <see cref="InProcServerTransportManager"/> class.
		/// </summary>
		/// <param name="server">The server which will host this instance.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="server"/> is <c>null</c>.
		/// </exception>
		public InProcServerTransportManager( RpcServer server )
			: base( server )
		{
			this._cancellationTokenSource = new CancellationTokenSource();
		}

		protected sealed override void BeginShutdownCore()
		{
			this.RaiseResponse( new byte[ 0 ] );
			this._cancellationTokenSource.Cancel();
			base.BeginShutdownCore();
		}

		protected sealed override void DisposeCore( bool disposing )
		{
			if ( !this._cancellationTokenSource.IsCancellationRequested )
			{
				this._cancellationTokenSource.Cancel( true );
			}

			this._cancellationTokenSource.Dispose();
			this._inboundQueue.Dispose();
			base.DisposeCore( disposing );
		}

		protected sealed override InProcServerTransport GetTransportCore()
		{
			return new InProcServerTransport( this );
		}

		protected sealed override void ReturnTransportCore( InProcServerTransport transport )
		{
			// nop
		}

		/// <summary>
		///		Sends specified data to the server.
		/// </summary>
		/// <param name="data">The data to be sent.</param>
		internal void SendToServer( byte[] data )
		{
			Contract.Assert( data != null );
			this._inboundQueue.Add( data );
		}

		/// <summary>
		///		Process in-proc communication receiving request/notification.
		/// </summary>
		/// <param name="context">The <see cref="ServerRequestContext"/>.</param>
		internal void Receive( ServerRequestContext context )
		{
			Contract.Assert( context != null );

			MemoryStream buffer;
			if ( !this._pendingRequestTable.TryGetValue( context, out buffer ) )
			{
				buffer = new MemoryStream( this._inboundQueue.Take( this._cancellationTokenSource.Token ) );
				this._pendingRequestTable.TryAdd( context, buffer );
			}

			buffer.Read( context.Buffer, context.Offset, context.Buffer.Length - context.Offset );

			if ( buffer.Position == buffer.Length )
			{
				this._pendingRequestTable.TryRemove( context, out buffer );
			}
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

			return
				Task.Factory.StartNew(
					this.RaiseResponse,
					context.BufferList.SelectMany( b => b.Array.Skip( b.Offset ).Take( b.Count ) ).ToArray()
				);
		}

		/// <summary>
		///		Raises the <see cref="E:Response"/> event.
		/// </summary>
		/// <param name="state">The array of <see cref="Byte"/>.</param>
		private void RaiseResponse( object state )
		{
			this.OnResponse( new InProcResponseEventArgs( state as byte[] ) );
		}
	}
}
