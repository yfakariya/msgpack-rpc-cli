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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NUnit.Framework;

namespace MsgPack.Rpc.Server.Protocols
{
	[TestFixture]
	public class TcpServerTransportTest
	{
		private const bool _traceEnabled = true;
		private const int _portNumber = 57319;
		private DebugTraceSourceSetting _debugTrace;

		[SetUp]
		public void SetUp()
		{
			this._debugTrace = new DebugTraceSourceSetting( Tracer.Protocols, _traceEnabled );
		}

		[TearDown]
		public void TearDown()
		{
			this._debugTrace.Dispose();
		}

		[Test]
		public void TestEchoRequest()
		{
			try
			{
				using ( var server = CreateServer() )
				{
					var target = new TcpServerTransport( new ServerSocketAsyncEventArgs( server ) );
					bool isOk = false;
					using ( var waitHandle = new ManualResetEventSlim() )
					{
						target.MessageReceived +=
							( sender, e ) =>
							{
								// Simple echo.
								try
								{
									Assert.That( e.MethodName, Is.EqualTo( "Echo" ) );
									e.Transport.Send( e.Arguments.ToArray() );
									isOk = true;
								}
								finally
								{
									waitHandle.Set();
								}
							};

						target.Initialize( new IPEndPoint( IPAddress.Loopback, _portNumber ) );

						TestEchoRequestCore( ref isOk, waitHandle );

						waitHandle.Reset();
						isOk = false;
						// Again
						TestEchoRequestCore( ref isOk, waitHandle );
					}
				}
			}
			catch ( SocketException sockEx )
			{
				Console.Error.WriteLine( "{0}({1}:0x{1:x8})", sockEx.SocketErrorCode, sockEx.ErrorCode );
				Console.Error.WriteLine( sockEx );
				throw;
			}
		}

		private static void TestEchoRequestCore( ref bool isOk, ManualResetEventSlim waitHandle )
		{
			using ( var client = new TcpClient() )
			{
				client.Connect( new IPEndPoint( IPAddress.Loopback, _portNumber ) );

				var now = MessagePackConvert.FromDateTime( DateTime.Now );

				using ( var stream = client.GetStream() )
				using ( var packer = Packer.Create( stream ) )
				{
					packer.PackArrayHeader( 4 );
					packer.Pack( 0 );
					packer.Pack( 123 );
					packer.Pack( "Echo" );
					packer.PackArrayHeader( 2 );
					packer.Pack( "Hello, world" );
					packer.Pack( now );

					client.Client.Shutdown( SocketShutdown.Send );

					Assert.That( waitHandle.Wait( TimeSpan.FromSeconds( 3 ) ) );
					Assert.That( isOk, "Server failed" );

					using ( var unpacker = Unpacker.Create( stream ) )
					{
						var result = unpacker.UnpackObject();
						Assert.That( result.IsArray );
						var array = result.AsList();
						Assert.That( array.Count, Is.EqualTo( 4 ) );
						Assert.That( array[ 0 ] == 1, array[ 0 ].ToString() );
						Assert.That( array[ 1 ] == 123, array[ 1 ].ToString() );
						Assert.That( array[ 2 ] == MessagePackObject.Nil, array[ 2 ].ToString() );
						Assert.That( array[ 3 ].IsArray, array[ 3 ].ToString() );
						var returnValue = array[ 3 ].AsList();
						Assert.That( returnValue.Count, Is.EqualTo( 2 ) );
						Assert.That( returnValue[ 0 ] == "Hello, world", returnValue[ 0 ].ToString() );
						Assert.That( returnValue[ 1 ] == now, returnValue[ 1 ].ToString() );
					}
				}
			}
		}

		private static RpcServer CreateServer()
		{
			return new RpcServer( 1 );
		}
	}
}
