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
using System.Net.Sockets;
using NUnit.Framework;

namespace MsgPack.Rpc.Server.Protocols
{
	[TestFixture]
	public class ServerTransportManagerTest
	{
		[Test()]
		public void TestRaiseClientError_RpcServerClientErrorEventOccurredWithSpeicifiedValues()
		{
			bool occurred = false;
			using ( var server = new RpcServer() )
			using ( var context = new ServerRequestContext() )
			{
				var error = RpcError.ArgumentError;
				server.ClientError +=
					( sender, e ) =>
					{
						Assert.That( e.MessageId, Is.EqualTo( context.MessageId ) );
						Assert.That( e.RemoteEndPoint, Is.EqualTo( context.RemoteEndPoint ) );
						Assert.That( e.RpcError.Error, Is.EqualTo( error ) );
						Assert.That( e.SessionId, Is.EqualTo( context.SessionId ) );
						occurred = true;
					};
				using ( var target = new Target( server ) )
				{
					target.RaiseClientError( context, new RpcErrorMessage( error, "Test" ) );
					Assert.That( occurred );
				}
			}
		}

		[Test()]
		public void TestRaiseServerError_RpcServerServerErrorEventOccurredWithSpeicifiedValues()
		{
			bool occurred = false;
			using ( var server = new RpcServer() )
			using ( var context = new ServerRequestContext() )
			{
				var exception = new InvalidOperationException();
				server.ServerError +=
					( sender, e ) =>
					{
						Assert.That( e.Exception, Is.EqualTo( exception ) );
						occurred = true;
					};
				using ( var target = new Target( server ) )
				{
					target.RaiseServerError( exception );
					Assert.That( occurred );
				}
			}
		}

		[Test()]
		public void TestDispose()
		{
			using ( var server = new RpcServer() )
			using ( var target = new Target( server ) )
			{
				target.Dispose();

				Assert.That( target.GetIsDisposed(), Is.True );
				Assert.That( target.DisposeCalled, Is.True );
			}
		}

		[Test()]
		public void TestBeginShutdown()
		{
			using ( var server = new RpcServer() )
			using ( var target = new Target( server ) )
			{
				target.BeginShutdown();

				Assert.That( target.IsInShutdown, Is.True );
				Assert.That( target.BeginShutdownCalled, Is.True );
			}
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestConstructor_Null_Fail()
		{
			new Target( null );
		}

		[Test]
		public void TestHandleSocketError_IgnoreableError_True()
		{
			using ( var server = new RpcServer() )
			using ( var target = new Target( server ) )
			using ( var socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp ) )
			using ( var e = new SocketAsyncEventArgs() )
			{
				e.SocketError = SocketError.Shutdown;
				var result = target.HandleSocketError( socket, e );

				Assert.That( result, Is.True );
			}
		}

		[Test]
		public void TestHandleSocketError_NotError_True()
		{
			using ( var server = new RpcServer() )
			using ( var target = new Target( server ) )
			using ( var socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp ) )
			using ( var e = new SocketAsyncEventArgs() )
			{
				e.SocketError = SocketError.Success;
				var result = target.HandleSocketError( socket, e );

				Assert.That( result, Is.True );
			}
		}

		[Test]
		public void TestHandleSocketError_FatalError_False()
		{
			using ( var server = new RpcServer() )
			using ( var target = new Target( server ) )
			using ( var socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp ) )
			using ( var e = new SocketAsyncEventArgs() )
			{
				e.SocketError = SocketError.AccessDenied;
				var result = target.HandleSocketError( socket, e );

				Assert.That( result, Is.False );
			}
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestHandleSocketError_SocketIsNull_Fail()
		{
			using ( var server = new RpcServer() )
			using ( var target = new Target( server ) )
			using ( var e = new SocketAsyncEventArgs() )
			{
				target.HandleSocketError( null, e );
			}
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestHandleSocketError_SocketAsyncEventArgsIsNull_Fail()
		{
			using ( var server = new RpcServer() )
			using ( var target = new Target( server ) )
			using ( var socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp ) )
			{
				target.HandleSocketError( socket, null );
			}
		}

		private sealed class Target : ServerTransportManager
		{
			public bool GetIsDisposed()
			{
				return this.IsDisposed;
			}

			public bool? DisposeCalled;
			public bool BeginShutdownCalled;

			public Target( RpcServer server ) : base( server ) { }

			protected override void Dispose( bool disposing )
			{
				this.DisposeCalled = disposing;
				base.Dispose( disposing );
			}

			protected override void BeginShutdownCore()
			{
				this.BeginShutdownCalled = true;
				base.BeginShutdownCore();
			}

			internal override void ReturnTransport( ServerTransport transport )
			{

			}
		}
	}
}
