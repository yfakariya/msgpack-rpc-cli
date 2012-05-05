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
using System.Net;
using System.Net.Sockets;
using NUnit.Framework;

namespace MsgPack.Rpc.Client.Protocols
{
	[TestFixture()]
	public class TcpClientTransportManagerTest
	{
		[Test]
		public void TestConnectAsync_Success()
		{
			var endPoint = new IPEndPoint( IPAddress.Loopback, 57319 );

			var listener = new TcpListener( endPoint );
			try
			{
				listener.Start();

				using ( var target = new TcpClientTransportManager( new RpcClientConfiguration() ) )
				using ( var result = target.ConnectAsync( endPoint ) )
				{
					Assert.That( result.Wait( TimeSpan.FromSeconds( 1 ) ) );
					try
					{
						Assert.That( result.Result.BoundSocket.RemoteEndPoint, Is.EqualTo( endPoint ) );
					}
					finally
					{
						result.Result.Dispose();
					}
				}
			}
			finally
			{
				listener.Stop();
			}
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestConnectAsync_Null()
		{
			using ( var target = new TcpClientTransportManager( new RpcClientConfiguration() ) )
			{
				target.ConnectAsync( null );
			}
		}

		[Test]
		public void TestConnectAsync_Timeout()
		{
			var testNetworkIPEndPont = new IPEndPoint( IPAddress.Parse( "198.51.100.1" ), 12345 ); // c.f. http://tools.ietf.org/html/rfc5737)
			var configuration = new RpcClientConfiguration();
			configuration.ConnectTimeout = TimeSpan.FromMilliseconds( 20 );
			using ( var target = new TcpClientTransportManager( configuration ) )
			{
				var actual = Assert.Throws<AggregateException>( () => target.ConnectAsync( testNetworkIPEndPont ).Wait( TimeSpan.FromSeconds( 1 ) ) );
				Assert.That( actual.InnerExceptions.Count, Is.EqualTo( 1 ) );
				Assert.That( actual.InnerException, Is.InstanceOf<RpcException>() );
				Assert.That( ( actual.InnerException as RpcException ).RpcError, Is.EqualTo( RpcError.ConnectionTimeoutError ), actual.ToString() );
			}
		}

		[Test]
		[Explicit]
		public void TestConnectAsync_ImplicitTimeout_TranslationOk()
		{
			if ( Environment.OSVersion.Platform != PlatformID.Win32NT )
			{
				Assert.Inconclusive( "This test dependes on WinSock2" );
			}

			var testNetworkIPEndPont = new IPEndPoint( IPAddress.Parse( "198.51.100.1" ), 12345 ); // c.f. http://tools.ietf.org/html/rfc5737)
			var configuration = new RpcClientConfiguration();
			using ( var target = new TcpClientTransportManager( configuration ) )
			{
				// WinSock TCP/IP has 20sec timeout...
				var actual = Assert.Throws<AggregateException>( () => target.ConnectAsync( testNetworkIPEndPont ).Result.Dispose() );
				Assert.That( actual.InnerExceptions.Count, Is.EqualTo( 1 ) );
				Assert.That( actual.InnerException, Is.InstanceOf<RpcException>() );
				Assert.That( ( actual.InnerException as RpcException ).RpcError, Is.EqualTo( RpcError.ConnectionTimeoutError ), actual.ToString() );
			}
		}
	}
}
