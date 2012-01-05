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
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using MsgPack.Rpc.Protocols;
using MsgPack.Rpc.Server.Dispatch;
using MsgPack.Rpc.Server.Protocols;
using MsgPack.Serialization;

namespace MsgPack.Rpc.Server
{
	/// <summary>
	///		Control stand alone server event loop.
	/// </summary>
	public class RpcServer : IDisposable
	{
		private readonly SerializationContext _serializationContext;

		public SerializationContext SerializationContext
		{
			get { return this._serializationContext; }
		}

		private readonly RpcServerConfiguration _configuration;

		public RpcServerConfiguration Configuration
		{
			get { return this._configuration; }
		}


		// TODO: auto-scaling
		// _maximumConcurrency, _currentConcurrency, _minimumIdle, _maximumIdle

		private ServerTransportManager _transportManager;
		private Dispatcher _dispatcher;

		public RpcServer() : this( null ) { }

		public RpcServer( RpcServerConfiguration configuration )
		{
			var safeConfiguration = ( configuration ?? RpcServerConfiguration.Default ).AsFrozen();
			this._configuration = safeConfiguration;
			this._serializationContext = new SerializationContext();
		}

		public void Dispose()
		{
			this.Stop();
		}

		private ServerTransportManager CreateTransportManager()
		{
			// TODO: transport factory.
			return new TcpServerTransportManager( this );
		}

		public bool Start()
		{
			var currentDispatcher = Interlocked.CompareExchange( ref this._dispatcher, this._configuration.DispatcherProvider( this ), null );
			if ( currentDispatcher != null )
			{
				return false;
			}

			Tracer.Server.TraceEvent( Tracer.EventType.StartServer, Tracer.EventId.StartServer, "Start server. Configuration:[{0}]", this._configuration );

			this._transportManager = this._configuration.TransportManagerProvider( this );
			return true;
		}

		public bool Stop()
		{
			var currentDispatcher = Interlocked.Exchange( ref this._dispatcher, null );
			if ( currentDispatcher == null )
			{
				return false;
			}

			if ( this._transportManager != null )
			{
				this._transportManager.BeginShutdown();
				this._transportManager.Dispose();
				this._transportManager = null;
			}

			return true;
		}
	}
}