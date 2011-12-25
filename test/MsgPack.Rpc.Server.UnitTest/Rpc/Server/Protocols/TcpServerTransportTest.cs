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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NUnit.Framework;

namespace MsgPack.Rpc.Server.Protocols
{
	// TODOs
	// Parallel Return -> Implements queue in session layer. And use dedicated SocketAsyncEventArgs (_sendingContext).
	//		-> Serialize -> Session Queue -> Server WatchDog -> Transport
	// Dispatcher -> Shim generator (from NLiblet), Lookup, Invoker
	// Error test
	// Refactor move deserialization from transport to session.

	// Client:
	//   Transport -- Send/Receive, Header deserialization, EventLoop
	//   SessionManager -- SessionTable, ErrorHandler
	//   Client -- sync API, serialization/deserialization
	//   IDL

	// Others
	//   UDP
	//   Client SocketPool
	//   
	[TestFixture]
	public class TcpServerTransportTest
	{
		// TODO: Error packet test.
		private const bool _traceEnabled = false;
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
				using ( var target = new TcpServerTransport( new ServerSocketAsyncEventArgs( server ) ) )
				{
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

						target.Initialize( new IPEndPoint( IPAddress.Any, _portNumber ) );

						TestEchoRequestCore( ref isOk, waitHandle );

						waitHandle.Reset();
						isOk = false;
						// Again
						TestEchoRequestCore( ref isOk, waitHandle );

						waitHandle.Reset();
						isOk = false;
						// Again 2
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
					Console.WriteLine( "---- Client sending request ----" );

					packer.PackArrayHeader( 4 );
					packer.Pack( 0 );
					packer.Pack( 123 );
					packer.Pack( "Echo" );
					packer.PackArrayHeader( 2 );
					packer.Pack( "Hello, world" );
					packer.Pack( now );

					Console.WriteLine( "---- Client sent request ----" );

					if ( Debugger.IsAttached )
					{
						waitHandle.Wait();
					}
					else
					{
						Assert.That( waitHandle.Wait( TimeSpan.FromSeconds( 3 ) ) );
					}
					Assert.That( isOk, "Server failed" );

					Console.WriteLine( "---- Client receiving response ----" );
					var result = Unpacking.UnpackObject( stream ).Value;
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
					Console.WriteLine( "---- Client received response ----" );
				}
			}
		}

		[Test]
		public void TestEchoRequestContinuous()
		{
			try
			{
				using ( var server = CreateServer() )
				using ( var target = new TcpServerTransport( new ServerSocketAsyncEventArgs( server ) ) )
				{
					const int count = 3;
					bool[] serverStatus = new bool[ count ];
					using ( var waitHandle = new CountdownEvent( count ) )
					{
						target.MessageReceived +=
							( sender, e ) =>
							{
								// Simple echo.
								try
								{
									Assert.That( e.MethodName, Is.EqualTo( "Echo" ) );
									e.Transport.Send( e.Arguments.ToArray() );
									serverStatus[ e.Id.Value ] = true;
								}
								finally
								{
									waitHandle.Signal();
								}
							};

						target.Initialize( new IPEndPoint( IPAddress.Any, _portNumber ) );

						TestEchoRequestContinuousCore( serverStatus, waitHandle, count );

						waitHandle.Reset();
						Array.Clear( serverStatus, 0, serverStatus.Length );
						// Again
						TestEchoRequestContinuousCore( serverStatus, waitHandle, count );
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

		private static void TestEchoRequestContinuousCore( bool[] serverStatus, CountdownEvent waitHandle, int count )
		{
			using ( var client = new TcpClient() )
			{
				client.Connect( new IPEndPoint( IPAddress.Loopback, _portNumber ) );

				var now = MessagePackConvert.FromDateTime( DateTime.Now );
				bool[] resposeStatus = new bool[ count ];

				//var sends =
				//    Enumerable.Range( 0, count )
				//    .Select( i =>
				//        {
				//            using ( var buffer = new MemoryStream() )
				//            using ( var packer = Packer.Create( buffer ) )
				//            {
				//                packer.PackArrayHeader( 4 );
				//                packer.Pack( 0 );
				//                packer.Pack( i );
				//                packer.Pack( "Echo" );
				//                packer.PackArrayHeader( 2 );
				//                packer.Pack( "Hello, world" );
				//                packer.Pack( now );

				//                return buffer.ToArray();
				//            }
				//        }
				//    ).Select( buffer =>
				//        Task.Factory.FromAsync<int>( client.Client.BeginSend( buffer, 0, buffer.Length, SocketFlags.None, null, null ), client.Client.EndSend )
				//    );

				//var receive =
				//    new Task(
				//        () =>
				//        {
				//            byte[] buffer = new byte[ 65536 ];
				//            using ( var stream = new MemoryStream() )
				//            {
				//                using ( var socket = client.Client.Accept() )
				//                {
				//                    for ( int received = socket.Receive( buffer ); 0 < received ; received = socket.Receive( buffer ) )
				//                    {
				//                        stream.Write( buffer, 0, received );
				//                    }

				//                    using ( var unpacker = Unpacker.Create( stream ) )
				//                    {
				//                        for ( int i = 0; i < count; i++ )
				//                        {
				//                            var result = unpacker.TryUnpackObject();
				//                            Assert.That( result.Value.IsArray );
				//                            var array = result.Value.AsList();
				//                            Assert.That( array.Count, Is.EqualTo( 4 ) );
				//                            Assert.That( array[ 0 ] == 1, array[ 0 ].ToString() );
				//                            Assert.That( array[ 1 ].IsTypeOf<int>().GetValueOrDefault() );
				//                            resposeStatus[ array[ 1 ].AsInt32() ] = true;
				//                            Assert.That( array[ 2 ] == MessagePackObject.Nil, array[ 2 ].ToString() );
				//                            Assert.That( array[ 3 ].IsArray, array[ 3 ].ToString() );
				//                            var returnValue = array[ 3 ].AsList();
				//                            Assert.That( returnValue.Count, Is.EqualTo( 2 ) );
				//                            Assert.That( returnValue[ 0 ] == "Hello, world", returnValue[ 0 ].ToString() );
				//                            Assert.That( returnValue[ 1 ] == now, returnValue[ 1 ].ToString() );
				//                        }
				//                    }
				//                }
				//            }
				//        }
				//    );

				using ( var stream = client.GetStream() )
				using ( var packer = Packer.Create( stream ) )
				{
					for ( int i = 0; i < count; i++ )
					{
						Console.WriteLine( "---- Client sending request ----" );
						packer.PackArrayHeader( 4 );
						packer.Pack( 0 );
						packer.Pack( i );
						packer.Pack( "Echo" );
						packer.PackArrayHeader( 2 );
						packer.Pack( "Hello, world" );
						packer.Pack( now );
						Console.WriteLine( "---- Client sent request ----" );
					}

					if ( Debugger.IsAttached )
					{
						waitHandle.Wait();
					}
					else
					{
						Assert.That( waitHandle.Wait( TimeSpan.FromSeconds( count * 3 ) ) );
					}

					using ( var unpacker = Unpacker.Create( stream ) )
					{
						for ( int i = 0; i < count; i++ )
						{
							Console.WriteLine( "---- Client receiving response ----" );
							Assert.That( unpacker.Read() );
							var result = unpacker.Data.Value;
							Assert.That( result.IsArray );
							var array = result.AsList();
							Assert.That( array.Count, Is.EqualTo( 4 ) );
							Assert.That( array[ 0 ] == 1, array[ 0 ].ToString() );
							Assert.That( array[ 1 ].IsTypeOf<int>().GetValueOrDefault() );
							resposeStatus[ array[ 1 ].AsInt32() ] = true;
							Assert.That( array[ 2 ] == MessagePackObject.Nil, array[ 2 ].ToString() );
							Assert.That( array[ 3 ].IsArray, array[ 3 ].ToString() );
							var returnValue = array[ 3 ].AsList();
							Assert.That( returnValue.Count, Is.EqualTo( 2 ) );
							Assert.That( returnValue[ 0 ] == "Hello, world", returnValue[ 0 ].ToString() );
							Assert.That( returnValue[ 1 ] == now, returnValue[ 1 ].ToString() );
							Console.WriteLine( "---- Client received response ----" );
						}
					}

					Assert.That( serverStatus, Is.All.True );
					Assert.That( resposeStatus, Is.All.True );
				}
			}
		}

		private static RpcServer CreateServer()
		{
			return new RpcServer( new RpcServerConfiguration() { MinimumConcurrency = 1 } );
		}
	}
}
