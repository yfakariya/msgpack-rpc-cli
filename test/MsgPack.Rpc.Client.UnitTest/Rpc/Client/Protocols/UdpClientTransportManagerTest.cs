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
	public class UdpClientTransportManagerTest
	{
		[Test]
		public void TestConnectAsync_Success()
		{
			var endPoint = new IPEndPoint( IPAddress.Loopback, 57319 );

			var listener = new UdpClient( endPoint );
			try
			{
				using ( var target = new UdpClientTransportManager( new RpcClientConfiguration() ) )
				using ( var result = target.ConnectAsync( endPoint ) )
				{
					Assert.That( result.Wait( TimeSpan.FromSeconds( 1 ) ) );
					try
					{
						var transport = result.Result;
						Assert.That( transport.BoundSocket, Is.Not.Null );
						Assert.That( ( transport as UdpClientTransport ).RemoteEndPoint, Is.EqualTo( endPoint ) );
					}
					finally
					{
						result.Result.Dispose();
					}
				}
			}
			finally
			{
				listener.Close();
			}
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestConnectAsync_Null()
		{
			using ( var target = new UdpClientTransportManager( new RpcClientConfiguration() ) )
			{
				target.ConnectAsync( null );
			}
		}
	}
}
