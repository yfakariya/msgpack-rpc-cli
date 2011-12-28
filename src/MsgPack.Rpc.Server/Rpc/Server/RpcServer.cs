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
		private readonly ServiceTypeLocator _locator;
		private readonly Dictionary<string, OperationDescription> _operations;

		public RpcServer() : this( null ) { }

		public RpcServer( RpcServerConfiguration configuration )
		{
			var safeConfiguration = ( configuration ?? RpcServerConfiguration.Default ).AsFrozen();
			this._operations = new Dictionary<string, OperationDescription>();
			this._locator = safeConfiguration.ServiceTypeLocatorProvider();
			this._serializationContext = new SerializationContext();
			this._configuration = safeConfiguration;
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

		public void Start( EndPoint bindingEndPoint )
		{
			if ( this._transportManager != null )
			{
				throw new InvalidOperationException( "Already started." );
			}

			Tracer.Server.TraceEvent( Tracer.EventType.StartServer, Tracer.EventId.StartServer, "Start server. Configuration:[{0}]", this._configuration );

			this.PopluateOperations();
			this._transportManager = this.CreateTransportManager();
		}

		private void PopluateOperations()
		{
			foreach ( var service in this._locator.FindServices() )
			{
				foreach ( var operation in OperationDescription.FromServiceDescription( this._serializationContext, service ) )
				{
					this._operations.Add( operation.Id, operation );
				}
			}
		}

		public void Stop()
		{
			this._transportManager.BeginShutdown();
			this._transportManager.Dispose();
			this._transportManager = null;
			this._operations.Clear();
		}
				
		private void OnMessageReceived( object source, RpcMessageReceivedEventArgs e )
		{
			ServerResponseContext responseContext = null;
			if ( e.MessageType == MessageType.Request )
			{
				responseContext = this._transportManager.ResponseContextPool.Borrow();
				responseContext.Id = e.Id.Value;
			}

			OperationDescription operation;
			if ( !this._operations.TryGetValue( e.MethodName, out operation ) )
			{
				var error = new RpcErrorMessage( RpcError.NoMethodError, "Operation does not exist.", null );
				InvocationHelper.TraceInvocationResult<object>(
					e.MessageType,
					e.Id.GetValueOrDefault(),
					e.MethodName,
					error,
					null
				);

				if ( responseContext != null )
				{
					responseContext.Serialize<object>( null, error, null );
				}

				return;
			}

			var task = operation.Operation( e.ArgumentsUnpacker, e.Id.GetValueOrDefault(), responseContext );

#if NET_4_5
			task.ContinueWith( ( previous, state ) =>
				{
					previous.Dispose();
					( state as IDisposable ).Dispose();
				},
				responseContext
			);
#else
			task.ContinueWith( previous =>
				{
					previous.Dispose();
					e.Transport.Send( responseContext );
				}
			);
#endif
		}
	}
}
