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
using System.Diagnostics;
using System.Net;
using System.Threading;
using MsgPack.Rpc.Client.Protocols;
using MsgPack.Serialization;
using NUnit.Framework;
using MsgPack.Rpc.Protocols;
using System.Threading.Tasks;

namespace MsgPack.Rpc.Client
{
	[TestFixture]
	public class DynamicRpcProxyTest
	{
		private const int TimeoutMilliseconds = 3000;

		[Test]
		public void TestDynamicInvocation_Success()
		{
			string arg = Guid.NewGuid().ToString();
			int messageId = Environment.TickCount % 1000;
			string methodName = "SomeMethod";
			using ( var environment = new InProcTestEnvironment( ( method, id, args ) => method == methodName && args[ 0 ].AsString() == arg ) )
			using ( dynamic target = new DynamicRpcProxy( environment.EndPoint, environment.Configuration ) )
			{
				MessagePackObject result = target.SomeMethod( arg );
				Assert.That( result == true );
			}
		}

		[Test]
		public void TestDynamicInvocation_BeginEnd_Success()
		{
			string arg = Guid.NewGuid().ToString();
			int messageId = Environment.TickCount % 1000;
			string methodName = "SomeMethod";
			using ( var environment = new InProcTestEnvironment( ( method, id, args ) => method == methodName && args[ 0 ].AsString() == arg ) )
			using ( dynamic target = new DynamicRpcProxy( environment.EndPoint, environment.Configuration ) )
			{
				IAsyncResult ar = target.BeginSomeMethod( arg, null, null );
				MessagePackObject result = target.EndSomeMethod( ar );
				Assert.That( result == true );
			}
		}

		[Test]
		public void TestDynamicInvocation_Async_Success()
		{
			string arg = Guid.NewGuid().ToString();
			int messageId = Environment.TickCount % 1000;
			string methodName = "SomeMethod";
			using ( var environment = new InProcTestEnvironment( ( method, id, args ) => method == methodName && args[ 0 ].AsString() == arg ) )
			using ( dynamic target = new DynamicRpcProxy( environment.EndPoint, environment.Configuration ) )
			{
				using ( Task<MessagePackObject> task = target.SomeMethodAsync( arg, null ) )
				{
					Assert.That( task.Result == true );
				}
			}
		}

		[Test]
		public void TestDynamicInvocation_ServerError()
		{
			using ( var environment = new InProcTestEnvironment( ( method, id, args ) => { throw new InvalidOperationException( "DUMMY" ); } ) )
			using ( dynamic target = new DynamicRpcProxy( environment.EndPoint, environment.Configuration ) )
			{
				try
				{
					target.SomeMethod();
					Assert.Fail();
				}
				catch ( RpcMethodInvocationException ex )
				{
					Assert.That( ex.MethodName, Is.StringContaining( "SomeMethod" ) );
					Assert.That( ex.Message, Is.StringContaining( "DUMMY" ) );
				}
			}
		}
		private sealed class InProcTestEnvironment : IDisposable
		{
			private readonly EndPoint _endPoint;

			public EndPoint EndPoint
			{
				get { return this._endPoint; }
			}

			private readonly MsgPack.Rpc.Server.CallbackServer _server;
			private readonly MsgPack.Rpc.Server.Protocols.InProcServerTransportManager _serverTransportManager;

			private readonly RpcClientConfiguration _configuration;

			public RpcClientConfiguration Configuration
			{
				get { return this._configuration; }
			}

			private readonly InProcClientTransportManager _clientTransportManager;
						
			public InProcTestEnvironment( Func<string, int?, MessagePackObject[], MessagePackObject> callback )
			{
				this._endPoint = new IPEndPoint( IPAddress.Loopback, MsgPack.Rpc.Server.CallbackServer.PortNumber );
				this._server = MsgPack.Rpc.Server.CallbackServer.Create( callback, true );
				this._configuration = RpcClientConfiguration.Default.Clone();
				this._configuration.TransportManagerProvider = conf => this._clientTransportManager;
				this._configuration.Freeze();
				this._serverTransportManager = new MsgPack.Rpc.Server.Protocols.InProcServerTransportManager( this._server.Server as Server.RpcServer, mgr => new SingletonObjectPool<Server.Protocols.InProcServerTransport>( new Server.Protocols.InProcServerTransport( mgr ) ) );
				this._clientTransportManager = new InProcClientTransportManager( new RpcClientConfiguration(), this._serverTransportManager );
			}

			public void Dispose()
			{
				this._clientTransportManager.Dispose();
				this._serverTransportManager.Dispose();
				this._server.Dispose();
			}
		}
	}
}
