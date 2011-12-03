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

		// TODO: auto-scaling
		// _maximumConcurrency, _currentConcurrency, _minimumIdle, _maximumIdle

		private readonly List<ServerTransport> _transports;
		private readonly int _minimumConcurrency;

		public RpcServer() : this( Environment.ProcessorCount * 2 ) { }

		public RpcServer( int minimumConcurrency )
		{
			this._minimumConcurrency = minimumConcurrency;
			this._transports = new List<ServerTransport>( minimumConcurrency );
			this._serializationContext = new SerializationContext();
		}

		public void Dispose()
		{
			this.Stop();
		}

		private ServerTransport CreateTransport( ServerSocketAsyncEventArgs context )
		{
			// TODO: transport factory.
			return new TcpServerTransport( context );
		}

		public void Start( EndPoint bindingEndPoint )
		{
			Tracer.Server.TraceEvent( Tracer.EventType.StartServer, Tracer.EventId.StartServer, "Start server. [\"minimumConcurrency\":{0}]", this._minimumConcurrency );
			// FIXME: Verification
			for ( int i = 0; i < this._minimumConcurrency; i++ )
			{
				var transport = this.CreateTransport( new ServerSocketAsyncEventArgs( this ) );
				transport.Initialize( bindingEndPoint );
				this._transports.Add( transport );
			}
		}

		public void Stop()
		{
			// FIXME: Verification
			foreach ( var transport in this._transports )
			{
				transport.Dispose();
			}

			this._transports.Clear();
		}
	}
}
