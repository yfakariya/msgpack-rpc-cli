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
using System.Net.Sockets;
using System.Threading;
using NUnit.Framework;

namespace MsgPack.Rpc.Server.Protocols
{
	[TestFixture]
	public class ServerTransportManager_1Test
	{
		[Test]
		public void TestSetTransportPool_NotNull_IsTransportPoolTrue()
		{
			using ( var server = new RpcServer() )
			using ( var target = new Target( server ) )
			using ( var transportManager = new NullServerTransportManager( server ) )
			using ( var transportPool =
				new OnTheFlyObjectPool<NullServerTransport>(
					conf => new NullServerTransport( transportManager ),
					server.Configuration.CreateTransportPoolConfiguration()
				)
			)
			{
				target.InvokeSetTransportPool( transportPool );
				Assert.That( target.GetIsTransportPoolSet() );
			}
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestSetTransportPool_Null_Fail()
		{
			using ( var server = new RpcServer() )
			using ( var target = new Target( server ) )
			{
				target.InvokeSetTransportPool( null );
			}
		}

		[Test]
		[ExpectedException( typeof( InvalidOperationException ) )]
		public void TestSetTransportPool_Twice_Fail()
		{
			using ( var server = new RpcServer() )
			using ( var target = new Target( server ) )
			using ( var transportManager = new NullServerTransportManager( server ) )
			using ( var transportPool =
				new OnTheFlyObjectPool<NullServerTransport>(
					conf => new NullServerTransport( transportManager ),
					server.Configuration.CreateTransportPoolConfiguration()
				)
			)
			{
				target.InvokeSetTransportPool( transportPool );
				target.InvokeSetTransportPool( transportPool );
			}
		}

		[Test]
		public void TestBeginShutdown_ShutdownCompletedOccurredAndSocketShutdowned()
		{
			using ( var server = new RpcServer() )
			using ( var target = new Target( server ) )
			using ( var transport = new NullServerTransport( target ) )
			using ( var transportPool = new SingletonObjectPool<NullServerTransport>( transport ) )
			using ( var socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp ) )
			{
				var listener = new TcpListener( IPAddress.Loopback, 19860 );
				try
				{
					listener.Start();
					var ar = listener.BeginAcceptSocket( null, null );

					target.InvokeSetTransportPool( transportPool );
					// activate
					var activeTransport = target.InvokeGetTransport( socket );
					// maually set transport
					activeTransport.BoundSocket = socket;

					using ( var waitHandle = new ManualResetEventSlim() )
					{
						target.ShutdownCompleted += ( sender, e ) => waitHandle.Set();

						activeTransport.BoundSocket.Connect( IPAddress.Loopback, 19860 );

						using ( var acceptted = listener.EndAcceptSocket( ar ) )
						{
							target.BeginShutdown();

							if ( Debugger.IsAttached )
							{
								waitHandle.Wait();
							}
							else
							{
								bool signaled = waitHandle.Wait( TimeSpan.FromSeconds( 3 ) );
								Assert.That( signaled );
							}
						}
					}
				}
				finally
				{
					try
					{
						listener.Stop();
					}
					catch ( SocketException ) { }
				}
			}
		}

		[Test]
		public void TestGetTransport_NotNull_ReturnsTransportWithSpecifiedSocket()
		{
			using ( var server = new RpcServer() )
			using ( var target = new Target( server ) )
			using ( var transport = new NullServerTransport( target ) )
			using ( var transportPool = new SingletonObjectPool<NullServerTransport>( transport ) )
			using ( var socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp ) )
			{
				target.InvokeSetTransportPool( transportPool );
				var result = target.InvokeGetTransport( socket );
				// Default implementation does not treat bindingSocket
				Assert.That( result.BoundSocket, Is.Null );
			}
		}

		[Test]
		public void TestGetTransport_Null_Harmless()
		{
			using ( var server = new RpcServer() )
			using ( var target = new Target( server ) )
			using ( var transport = new NullServerTransport( target ) )
			using ( var transportPool = new SingletonObjectPool<NullServerTransport>( transport ) )
			{
				target.InvokeSetTransportPool( transportPool );
				var result = target.InvokeGetTransport( null );
				// Default implementation does not treat bindingSocket
				Assert.That( result.BoundSocket, Is.Null );
			}
		}

		[Test]
		[ExpectedException( typeof( InvalidOperationException ) )]
		public void TestGetTransport_PoolWasNotSet_Fail()
		{
			using ( var server = new RpcServer() )
			using ( var target = new Target( server ) )
			using ( var transport = new NullServerTransport( target ) )
			using ( var transportPool = new SingletonObjectPool<NullServerTransport>( transport ) )
			using ( var socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp ) )
			{
				target.InvokeGetTransport( socket );
			}
		}

		[Test]
		public void TestReturnTransport_NotNull_ReturnsToPool()
		{
			using ( var server = new RpcServer() )
			using ( var target = new Target( server ) )
			using ( var transport = new NullServerTransport( target ) )
			using ( var transportPool = new SingletonObjectPool<NullServerTransport>( transport ) )
			using ( var socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp ) )
			{
				target.InvokeSetTransportPool( transportPool );
				NullServerTransport returned = null;
				transportPool.ObjectReturned += ( sender, e ) => returned = e.ReturnedObject;
				target.ReturnTransport( target.InvokeGetTransport( socket ) );
				Assert.That( returned, Is.Not.Null.And.SameAs( transport ) );
			}
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestReturnTransport_Null_Fail()
		{
			using ( var server = new RpcServer() )
			using ( var target = new Target( server ) )
			using ( var transport = new NullServerTransport( target ) )
			using ( var transportPool = new SingletonObjectPool<NullServerTransport>( transport ) )
			{
				target.InvokeSetTransportPool( transportPool );
				target.ReturnTransport( null );
			}
		}

		[Test]
		[ExpectedException( typeof( InvalidOperationException ) )]
		public void TestReturnTransport_PoolWasNotSet_Fail()
		{
			using ( var server = new RpcServer() )
			using ( var target = new Target( server ) )
			using ( var transport = new NullServerTransport( target ) )
			{
				target.ReturnTransport( transport );
			}
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestReturnTransport_OwnedAnotherManager_Fail()
		{
			using ( var server = new RpcServer() )
			using ( var target = new Target( server ) )
			using ( var another = new Target( server ) )
			using ( var transport = new NullServerTransport( another ) )
			using ( var transportPool = new SingletonObjectPool<NullServerTransport>( transport ) )
			{
				target.InvokeSetTransportPool( transportPool );
				target.ReturnTransport( transport );
			}
		}

		[Test]
		public void TestGetRequestContext_NotNull_ReturnsWithTransportBound()
		{
			using ( var server = new RpcServer() )
			using ( var target = new Target( server ) )
			using ( var transport = new NullServerTransport( target ) )
			using ( var transportPool = new SingletonObjectPool<NullServerTransport>( transport ) )
			using ( var socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp ) )
			{
				target.InvokeSetTransportPool( transportPool );
				var result = target.InvokeGetRequestContext( target.InvokeGetTransport( socket ) );
				Assert.That( result.BoundTransport, Is.SameAs( transport ) );
			}
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestGetRequestContext_Null_Fail()
		{
			using ( var server = new RpcServer() )
			using ( var target = new Target( server ) )
			{
				target.InvokeGetRequestContext( null );
			}
		}

		private sealed class Target : ServerTransportManager<NullServerTransport>
		{
			public Target( RpcServer server ) : base( server ) { }

			public bool GetIsTransportPoolSet()
			{
				return this.IsTransportPoolSet;
			}

			public void InvokeSetTransportPool( ObjectPool<NullServerTransport> transportPool )
			{
				this.SetTransportPool( transportPool );
			}

			public NullServerTransport InvokeGetTransport( Socket bindingSocket )
			{
				return this.GetTransport( bindingSocket );
			}

			public ServerRequestContext InvokeGetRequestContext( NullServerTransport transport )
			{
				return this.GetRequestContext( transport );
			}
		}
	}
}
