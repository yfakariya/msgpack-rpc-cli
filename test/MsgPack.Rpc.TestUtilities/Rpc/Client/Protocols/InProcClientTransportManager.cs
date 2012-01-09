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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MsgPack.Rpc.Server.Protocols;
using System.Net;
using System.Net.Sockets;

namespace MsgPack.Rpc.Client.Protocols
{
	/// <summary>
	///		Implements <see cref="ClientTransportManager{T}"/> for <see cref="InProcClientTransport"/>.
	/// </summary>
	public sealed class InProcClientTransportManager : ClientTransportManager<InProcClientTransport>
	{
		/// <summary>
		///		The queue to emulate server to client communication.
		/// </summary>
		private readonly BlockingCollection<byte[]> _inboundQueue;

		/// <summary>
		///		The dictionary to emulate continuous receiving.
		/// </summary>
		private readonly ConcurrentDictionary<ClientResponseContext, MemoryStream> _pendingResponseTable;

		/// <summary>
		///		The target server to be invoked.
		/// </summary>
		private readonly InProcServerTransportManager _target;

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
		/// Initializes a new instance of the <see cref="InProcClientTransportManager"/> class.
		/// </summary>
		/// <param name="configuration">
		///		The <see cref="RpcClientConfiguration"/> which contains various configuration information.
		/// </param>
		/// <param name="target">
		///		The target server to be invoked.
		///	</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="configuration"/> is <c>null</c>.
		///		Or, <paramref name="target"/> is <c>null</c>.
		/// </exception>
		public InProcClientTransportManager( RpcClientConfiguration configuration, InProcServerTransportManager target )
			: base( configuration )
		{
			if ( target == null )
			{
				throw new ArgumentNullException( "target" );
			}

			this._inboundQueue = new BlockingCollection<byte[]>();
			this._pendingResponseTable = new ConcurrentDictionary<ClientResponseContext, MemoryStream>();
			this._target = target;
			target.Response += this.OnReceiving;
			this._cancellationTokenSource = new CancellationTokenSource();
		}

		protected sealed override void DisposeCore( bool disposing )
		{
			this._target.Dispose();
			if ( !this._cancellationTokenSource.IsCancellationRequested )
			{
				this._cancellationTokenSource.Cancel( true );
			}

			this._cancellationTokenSource.Dispose();
			this._inboundQueue.Dispose();
			base.DisposeCore( disposing );
		}

		protected sealed override void BeginShutdownCore()
		{
			this._target.SendToServer( new byte[ 0 ] );
			this._cancellationTokenSource.Cancel();
			base.BeginShutdownCore();
		}

		protected override Task<ClientTransport> ConnectAsyncCore( EndPoint targetEndPoint )
		{
			return Task.Factory.StartNew( () => this.GetTransport( new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp ) ) as ClientTransport );
		}

		protected sealed override InProcClientTransport GetTransportCore()
		{
			return new InProcClientTransport( this );
		}

		protected sealed override void ReturnTransportCore( InProcClientTransport transport )
		{
			// nop
		}

		/// <summary>
		///		Process in-proc communication sending request/notification.
		/// </summary>
		/// <param name="context">The <see cref="ClientRequestContext"/>.</param>
		internal void Send( ClientRequestContext context )
		{
			this._target.SendToServer( context.BufferList.SelectMany( segment => segment.Array.Skip( segment.Offset ).Take( segment.Count ) ).ToArray() );
		}

		/// <summary>
		///		Handles <see cref="E:InProcServerTransportManager.Response"/> event.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="MsgPack.Rpc.InProcResponseEventArgs"/> instance containing the event data.</param>
		private void OnReceiving( object sender, InProcResponseEventArgs e )
		{
			this._inboundQueue.Add( e.Data );
		}

		/// <summary>
		///		Process in-proc communication receiving response.
		/// </summary>
		/// <param name="context">The <see cref="ClientResponseContext"/>.</param>
		/// <returns>
		///		<see cref="Task"/> to be notified async operation result.
		/// </returns>
		internal Task ReceiveAsync( ClientResponseContext context )
		{
			return Task.Factory.StartNew( this.ReceiveCore, context );
		}

		/// <summary>
		///		Process in-proc communication receiving response.
		/// </summary>
		/// <param name="state"><see cref="ClientResponseContext"/>.</param>
		private void ReceiveCore( object state )
		{
			var context = state as ClientResponseContext;
			MemoryStream buffer;
			if ( !this._pendingResponseTable.TryGetValue( context, out buffer ) )
			{
				buffer = new MemoryStream( this._inboundQueue.Take( this._cancellationTokenSource.Token ) );
				this._pendingResponseTable.TryAdd( context, buffer );
			}

			buffer.Read( context.Buffer, context.Offset, context.Buffer.Length - context.Offset );

			if ( buffer.Position == buffer.Length )
			{
				this._pendingResponseTable.TryRemove( context, out buffer );
			}
		}
	}
}
