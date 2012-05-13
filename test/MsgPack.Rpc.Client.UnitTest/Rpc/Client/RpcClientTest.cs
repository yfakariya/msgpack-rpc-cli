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
using System.Reflection;
using System.Threading;
using MsgPack.Rpc.Client.Protocols;
using MsgPack.Serialization;
using NUnit.Framework;
using MsgPack.Rpc.Protocols;
using System.Net.Sockets;

namespace MsgPack.Rpc.Client
{
	[TestFixture()]
	[Timeout( 3000 )]
	public class RpcClientTest
	{
		private static readonly IPEndPoint _loopbackEndPoint = new IPEndPoint( IPAddress.Loopback, MsgPack.Rpc.Server.CallbackServer.PortNumber );
		public const int TimeoutMilliseconds = 3000;

		[Test]
		public void TestConstructorRpcClient_Normal_SetPropertyAsIs()
		{
			using ( var environment = new InProcTestEnvironment() )
			{
				var configuration = RpcClientConfiguration.Default.Clone();
				configuration.TransportManagerProvider = conf => environment.ClientTransportManager;
				SerializationContext serializationContext = new SerializationContext();

				using ( RpcClient target = new RpcClient( environment.EndPoint, configuration, serializationContext ) )
				{
					Assert.That( target.SerializationContext, Is.SameAs( serializationContext ) );
				}
			}
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestConstructorRpcClient_TransportIsNull()
		{
			SerializationContext serializationContext = new SerializationContext();
			new RpcClient( null );
		}

		[Test]
		public void TestConstructorRpcClient_ConfigurationAndSerializationContextIsNull_DefaultsAreUsed()
		{
			using ( var environment = new InProcTestEnvironment() )
			{
				using ( RpcClient target = new RpcClient( environment.EndPoint, null, null ) )
				{
					Assert.That( target.SerializationContext, Is.Not.Null );
				}
			}
		}

		[Test]
		public void TestDispose_TransportDisposed()
		{
			using ( var environment = new InProcTestEnvironment() )
			{
				var target = new RpcClient( _loopbackEndPoint, environment.Configuration, null );
				target.EnsureConnected();
				target.Dispose();
				Assert.That( target.Transport.IsDisposed );
				Assert.That( target.TransportManager.IsDisposed );
			}
		}

		[Test]
		public void TestDispose_Twise_Harmless()
		{
			using ( var environment = new InProcTestEnvironment() )
			{
				var target = new RpcClient( _loopbackEndPoint, environment.Configuration, null );
				target.Dispose();
				target.Dispose();
			}
		}

		[Test]
		public void TestShutdown_TransportShutdownIsInitiated()
		{
			using ( var environment = new InProcTestEnvironment() )
			using ( var target = new RpcClient( _loopbackEndPoint, environment.Configuration, null ) )
			{
				int isShutdownCompleted = 0;
				target.EnsureConnected();
				target.Transport.ShutdownCompleted += ( sender, e ) => Interlocked.Exchange( ref isShutdownCompleted, 1 );
				target.Shutdown();
				Assert.That( target.Transport.IsClientShutdown );
				Assert.That( isShutdownCompleted, Is.EqualTo( 1 ) );
			}
		}

		[Test]
		public void TestShutdownAsync_TransportShutdownIsInitiated()
		{
			using ( var environment = new InProcTestEnvironment() )
			using ( var target = new RpcClient( _loopbackEndPoint, environment.Configuration, null ) )
			{
				int isShutdownCompleted = 0;
				target.EnsureConnected();
				target.Transport.ShutdownCompleted += ( sender, e ) => Interlocked.Exchange( ref isShutdownCompleted, 1 );
				using ( var task = target.ShutdownAsync() )
				{
					Assert.That( target.Transport.IsClientShutdown );
					Assert.That( task.Wait( TimeSpan.FromSeconds( 1 ) ) );
					Assert.That( isShutdownCompleted, Is.EqualTo( 1 ) );
				}
			}
		}

		[Test()]
		public void TestCall_Normal_Returned()
		{
			using ( var environment = new InProcTestEnvironment() )
			using ( var target = environment.CreateClient() )
			{
				var args = new object[] { 1, "3" };
				var result = target.Call( "Test", args );
				// CallbackServer just returns args.
				Assert.That( result.AsList()[ 0 ] == 1, result.AsList()[ 0 ].ToString() );
				Assert.That( result.AsList()[ 1 ] == "3", result.AsList()[ 1 ].ToString() );
			}
		}

		[Test()]
		public void TestCall_RemoteError_Thrown()
		{
			using ( var environment = new InProcTestEnvironment( new RpcMethodInvocationException( RpcError.CallError, "Test" ) ) )
			using ( var target = environment.CreateClient() )
			{
				var args = new object[] { 1, "3" };
				try
				{
					target.Call( "Test", args );
					Assert.Fail();
				}
				catch ( RpcException ex )
				{
					Assert.That( ex.RpcError.Identifier, Is.EqualTo( RpcError.CallError.Identifier ) );
				}
			}
		}

		[Test]
		public void TestCall_NetworkError_Exception()
		{
			using ( var environment = new InProcTestEnvironment() )
			using ( var target = environment.CreateClient() )
			{
				target.EnsureConnected();
				( target.Transport as InProcClientTransport ).DataSending += EmulateSocketError;
				var args = new object[] { 1, "3" };
				Assert.Catch<SocketException>( () => target.Call( "Test", args ) );
			}
		}

		[Test()]
		public void TestCallAsync_Normal_Returned()
		{
			using ( var environment = new InProcTestEnvironment() )
			using ( var target = environment.CreateClient() )
			{
				var args = new object[] { 1, "3" };
				var task = target.CallAsync( "Test", args, null );
				// CallbackServer just returns args.
				var result = task.Result;
				Assert.That( result.AsList()[ 0 ] == 1, result.AsList()[ 0 ].ToString() );
				Assert.That( result.AsList()[ 1 ] == "3", result.AsList()[ 1 ].ToString() );
			}
		}

		[Test()]
		public void TestCallAsync_RemoteError_Thrown()
		{
			using ( var environment = new InProcTestEnvironment( new RpcMethodInvocationException( RpcError.CallError, "Test" ) ) )
			using ( var target = environment.CreateClient() )
			{
				var args = new object[] { 1, "3" };
				try
				{
					var task = target.CallAsync( "Test", args, null );
					task.Wait();
					Assert.Fail();
				}
				catch ( AggregateException ex )
				{
					var rpcException = ex.InnerException as RpcException;
					Assert.That( rpcException, Is.Not.Null );
					Assert.That( rpcException.RpcError.Identifier, Is.EqualTo( RpcError.CallError.Identifier ) );
				}
			}
		}

		[Test]
		public void TestCallAsync_NetworkError_ExceptionOnAsyncItSelf()
		{
			using ( var environment = new InProcTestEnvironment() )
			using ( var target = environment.CreateClient() )
			{
				target.EnsureConnected();
				( target.Transport as InProcClientTransport ).DataSending += EmulateSocketError;
				var args = new object[] { 1, "3" };
				Assert.Catch<SocketException>( () => target.CallAsync( "Test", args, null ) );
			}
		}

		[Test()]
		public void TestBeginCallEndCall_Normal_Returned()
		{
			using ( var environment = new InProcTestEnvironment() )
			using ( var target = environment.CreateClient() )
			{
				var args = new object[] { 1, "3" };
				var ar = target.BeginCall( "Test", args, null, null );
				// CallbackServer just returns args.
				var result = target.EndCall( ar );
				Assert.That( result.AsList()[ 0 ] == 1, result.AsList()[ 0 ].ToString() );
				Assert.That( result.AsList()[ 1 ] == "3", result.AsList()[ 1 ].ToString() );
			}
		}

		[Test()]
		public void TestBeginCallEndCall_RemoteError_Thrown()
		{
			using ( var environment = new InProcTestEnvironment( new RpcMethodInvocationException( RpcError.CallError, "Test" ) ) )
			using ( var target = environment.CreateClient() )
			{
				var args = new object[] { 1, "3" };
				try
				{
					var ar = target.BeginCall( "Test", args, null, null );
					Assert.That( ar.AsyncWaitHandle.WaitOne( TimeSpan.FromSeconds( 1 ) ) );
					target.EndCall( ar );
					Assert.Fail();
				}
				catch ( RpcException ex )
				{
					Assert.That( ex.RpcError.Identifier, Is.EqualTo( RpcError.CallError.Identifier ) );
				}
			}
		}

		[Test]
		public void TestBeginCallEndCall_NetworkError_ExceptionOnBeginCall()
		{
			using ( var environment = new InProcTestEnvironment() )
			using ( var target = environment.CreateClient() )
			{
				target.EnsureConnected();
				( target.Transport as InProcClientTransport ).DataSending += EmulateSocketError;
				var args = new object[] { 1, "3" };
				Assert.Catch<SocketException>( () => target.BeginCall( "Test", args, null, null ) );
			}
		}

		private const int _notificationIsOk = 1;
		private const int _notificationInvalidArguments = 2;

		[Test()]
		public void TestNotify_Normal_ReachToServer()
		{
			var clientArgs = new object[] { 1, "3" };
			int result = 0;
			using ( var waitHandle = new ManualResetEventSlim() )
			using ( var environment = new InProcTestEnvironment(
				( id, args ) =>
				{
					try
					{
						if ( args[ 0 ] != 1 || args[ 1 ] != "3" )
						{
							Interlocked.Exchange( ref result, _notificationInvalidArguments );
						}
						else
						{
							Interlocked.Exchange( ref result, _notificationIsOk );
						}

						return MessagePackObject.Nil;
					}
					finally
					{
						waitHandle.Set();
					}
				}
			) )
			using ( var target = environment.CreateClient() )
			{
				target.Notify( "Test", clientArgs );
				Assert.That( waitHandle.Wait( TimeSpan.FromSeconds( 1 ) ) );
				Assert.That( Interlocked.CompareExchange( ref result, 0, 0 ), Is.EqualTo( _notificationIsOk ) );
			}
		}

		[Test()]
		public void TestNotify_RemoteError_NoEffect()
		{
			var clientArgs = new object[] { 1, "3" };
			using ( var environment = new InProcTestEnvironment(
				( id, args ) =>
				{
					throw new RpcMethodInvocationException( RpcError.CallError, "Test" );
				}
			) )
			using ( var target = environment.CreateClient() )
			{
				target.Notify( "Test", clientArgs );
			}
		}

		[Test]
		public void TestNotify_NetworkError_Exception()
		{
			using ( var environment = new InProcTestEnvironment() )
			using ( var target = environment.CreateClient() )
			{
				target.EnsureConnected();
				( target.Transport as InProcClientTransport ).DataSending += EmulateSocketError;
				var args = new object[] { 1, "3" };
				Assert.Catch<SocketException>( () => target.Notify( "Test", args ) );
			}
		}

		[Test()]
		public void TestNotifyAsync_Normal_ReachToServer()
		{
			var clientArgs = new object[] { 1, "3" };
			int result = 0;
			using ( var waitHandle = new ManualResetEventSlim() )
			using ( var environment = new InProcTestEnvironment(
				( id, args ) =>
				{
					if ( args[ 0 ] != 1 || args[ 1 ] != "3" )
					{
						Interlocked.Exchange( ref result, _notificationInvalidArguments );
					}
					else
					{
						Interlocked.Exchange( ref result, _notificationIsOk );
					}

					waitHandle.Set();
					return MessagePackObject.Nil;
				}
			) )
			using ( var target = environment.CreateClient() )
			{
				using ( var task = target.NotifyAsync( "Test", clientArgs, null ) )
				{
					Assert.That( task.Wait( TimeSpan.FromSeconds( 1 ) ) );
					Assert.That( waitHandle.Wait( TimeSpan.FromSeconds( 1 ) ) );
					Assert.That( Interlocked.CompareExchange( ref result, 0, 0 ), Is.EqualTo( _notificationIsOk ) );
				}
			}
		}

		[Test()]
		public void TestNotifyAsync_RemoteError_NoEffect()
		{
			var clientArgs = new object[] { 1, "3" };
			using ( var environment = new InProcTestEnvironment(
				( id, args ) =>
				{
					throw new RpcMethodInvocationException( RpcError.CallError, "Test" );
				}
			) )
			using ( var target = environment.CreateClient() )
			{
				using ( var task = target.NotifyAsync( "Test", clientArgs, null ) )
				{
					Assert.That( task.Wait( TimeSpan.FromSeconds( 1 ) ) );
				}
			}
		}

		[Test]
		public void TestNotifyAsync_NetworkError_ExceptionOnAsyncItSelf()
		{
			using ( var environment = new InProcTestEnvironment() )
			using ( var target = environment.CreateClient() )
			{
				target.EnsureConnected();
				( target.Transport as InProcClientTransport ).DataSending += EmulateSocketError;
				var args = new object[] { 1, "3" };
				Assert.Catch<SocketException>( () => target.NotifyAsync( "Test", args, null ) );
			}
		}

		[Test()]
		public void TestBeginNotifyEndNotify_Normal_ReachToServer()
		{
			var clientArgs = new object[] { 1, "3" };
			int result = 0;
			using ( var waitHandle = new ManualResetEventSlim() )
			using ( var environment = new InProcTestEnvironment(
				( id, args ) =>
				{
					if ( args[ 0 ] != 1 || args[ 1 ] != "3" )
					{
						Interlocked.Exchange( ref result, _notificationInvalidArguments );
					}
					else
					{
						Interlocked.Exchange( ref result, _notificationIsOk );
					}

					waitHandle.Set();
					return MessagePackObject.Nil;
				}
			) )
			using ( var target = environment.CreateClient() )
			{
				var ar = target.BeginNotify( "Test", clientArgs, null, null );
				Assert.That( ar.AsyncWaitHandle.WaitOne( TimeSpan.FromSeconds( 1 ) ) );
				target.EndNotify( ar );
				Assert.That( waitHandle.Wait( TimeSpan.FromSeconds( 1 ) ) );
				Assert.That( Interlocked.CompareExchange( ref result, 0, 0 ), Is.EqualTo( _notificationIsOk ) );
			}
		}

		[Test()]
		public void TestBeginNotifyEndNotify_RemoteError_NoEffect()
		{
			var clientArgs = new object[] { 1, "3" };
			using ( var environment = new InProcTestEnvironment(
				( id, args ) =>
				{
					throw new RpcMethodInvocationException( RpcError.CallError, "Test" );
				}
			) )
			using ( var target = environment.CreateClient() )
			{
				var ar = target.BeginNotify( "Test", clientArgs, null, null );
				Assert.That( ar.AsyncWaitHandle.WaitOne( TimeSpan.FromSeconds( 1 ) ) );
				target.EndNotify( ar );
			}
		}

		[Test]
		public void TestBeginNotifyEndNotify_NetworkError_ExceptionOnBeginCall()
		{
			using ( var environment = new InProcTestEnvironment() )
			using ( var target = environment.CreateClient() )
			{
				target.EnsureConnected();
				( target.Transport as InProcClientTransport ).DataSending += EmulateSocketError;
				var args = new object[] { 1, "3" };
				Assert.Catch<SocketException>( () => target.BeginNotify( "Test", args, null, null ) );
			}
		}

		private static void EmulateSocketError( object sender, InProcDataSendingEventArgs e )
		{
			throw new SocketException( ( int )SocketError.ConnectionReset );
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

			private RpcClientConfiguration _configuration;

			public RpcClientConfiguration Configuration
			{
				get { return this._configuration; }
				set { this._configuration = value; }
			}

			private readonly InProcClientTransportManager _clientTransportManager;

			public ClientTransportManager ClientTransportManager
			{
				get { return this._clientTransportManager; }
			}

			public InProcTestEnvironment() : this( ( id, args ) => args ) { }

			public InProcTestEnvironment( RpcException errorToBeRaised )
				: this( ( id, args ) => { throw errorToBeRaised; } )
			{
			}

			public InProcTestEnvironment( Func<int?, MessagePackObject[], MessagePackObject> callback )
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

			public void ShutdownServer()
			{
				this._server.Dispose();
			}

			public RpcClient CreateClient()
			{
				return new RpcClient( this._endPoint, this._configuration, null );
			}
		}

		private sealed class TcpTestEnvironment : IDisposable
		{
			private readonly TcpListener _server;
			private readonly TcpClientTransportManager _clientTransportManager;

			public ClientTransportManager ClientTransportManager
			{
				get { return this._clientTransportManager; }
			}

			public TcpTestEnvironment()
			{
				this._server = new TcpListener( IPAddress.Loopback, MsgPack.Rpc.Server.CallbackServer.PortNumber );
				this._clientTransportManager = new TcpClientTransportManager( new RpcClientConfiguration() );
				this._server.Start();
			}

			public void Dispose()
			{
				this._clientTransportManager.Dispose();
				try
				{
					this._server.Stop();
				}
				catch { }
			}
		}
	}
}
