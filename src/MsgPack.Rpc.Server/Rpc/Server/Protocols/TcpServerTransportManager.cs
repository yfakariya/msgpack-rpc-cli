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
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace MsgPack.Rpc.Server.Protocols
{
	public sealed class TcpServerTransportManager : ServerTransportManager<TcpServerTransport>
	{
		private readonly Socket _listeningSocket;
		private readonly ObjectPool<ListeningContext> _listeningContextPool;

		public TcpServerTransportManager( RpcServer server )
			: base( server )
		{
			base.SetTransportPool( server.Configuration.TcpTransportPoolProvider( () => new TcpServerTransport( this ), server.Configuration.CreateTcpTransportPoolConfiguration() ) );
			this._listeningContextPool = server.Configuration.ListeningContextPoolProvider( () => new ListeningContext(), server.Configuration.CreateListeningContextPoolConfiguration() );
			this._listeningSocket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
			var bindingEndPoint = this.Configuration.BindingEndPoint ?? NetworkEnvironment.GetDefaultEndPoint( server.Configuration.PortNumber );
			this._listeningSocket.Bind( bindingEndPoint );
			this._listeningSocket.Listen( server.Configuration.ListenBackLog );
			Tracer.Protocols.TraceEvent( Tracer.EventType.StartListen, Tracer.EventId.StartListen, "Start listen. [ \"endPoint\" : \"{0}\", \"backLog\" : {1} ]", bindingEndPoint, server.Configuration.ListenBackLog );
			this.StartAccept();
		}

		protected sealed override void DisposeCore( bool disposing )
		{
			if ( disposing )
			{
				this._listeningSocket.Close();
			}

			base.DisposeCore( disposing );
		}

		protected sealed override void BeginShutdownCore()
		{
			base.BeginShutdownCore();
			this._listeningSocket.Shutdown( SocketShutdown.Both );
		}

		private void StartAccept()
		{
			var concurrency = this.Configuration.MinimumConnection;
			for ( int i = 0; i < concurrency; i++ )
			{
				var context = this._listeningContextPool.Borrow();
				context.Completed += this.OnCompleted;
				this.Accept( context );
			}
		}

		private void OnCompleted( object sender, SocketAsyncEventArgs e )
		{
			if ( !this.HandleError( sender, e ) )
			{
				return;
			}

			switch ( e.LastOperation )
			{
				case SocketAsyncOperation.Accept:
				{
					var context = e as ListeningContext;
					Contract.Assert( context != null );
					this.OnAcceptted( context );
					break;
				}
				default:
				{
					Tracer.Protocols.TraceEvent(
						Tracer.EventType.UnexpectedLastOperation,
						Tracer.EventId.UnexpectedLastOperation,
						"Unexpected operation. [ \"Soekct\" : 0x{0}, \"RemoteEndPoint\" : \"{1}\", \"LastOperation\" : \"{2}\" ]",
						( ( Socket )sender ).Handle,
						e.RemoteEndPoint,
						e.LastOperation
					);
					break;
				}
			}
		}

		private void Accept( ListeningContext context )
		{
			// Ensure buffers are cleared to avoid unepxected data feeding on Accept
			context.SetBuffer( null, 0, 0 );
			context.BufferList = null;

			try
			{
				if ( this.IsInShutdown )
				{
					// TODO: Trace
					return;
				}

				Tracer.Protocols.TraceEvent( Tracer.EventType.BeginAccept, Tracer.EventId.BeginAccept, "Wait for connection. [ \"LocalEndPoint\" : \"{0}\" ]", this._listeningSocket.LocalEndPoint );

				if ( !this._listeningSocket.AcceptAsync( context ) )
				{
					// Avoid recursive acceptance and the subsequent request delay.
					// Task is bit heavy here.
					ThreadPool.QueueUserWorkItem( _ => this.OnAcceptted( context ) );
				}
			}
			catch ( ObjectDisposedException )
			{
				if ( !this.IsDisposed )
				{
					throw;
				}
			}
		}

		private void OnAcceptted( ListeningContext context )
		{
			Tracer.Protocols.TraceEvent( 
				Tracer.EventType.AcceptInboundTcp,
				Tracer.EventId.AcceptInboundTcp, 
				"Accept. [ \"RemoteEndPoit\" : \"{0}\", \"LocalEndPoint\" : \"{1}\" ]", context.AcceptSocket.RemoteEndPoint, context.AcceptSocket.LocalEndPoint 
			);

			var transport = this.GetTransport( context.AcceptSocket );
			context.AcceptSocket = null;
			this.Accept( context );
			this.ProcessRequet( transport );
		}
	}
}
