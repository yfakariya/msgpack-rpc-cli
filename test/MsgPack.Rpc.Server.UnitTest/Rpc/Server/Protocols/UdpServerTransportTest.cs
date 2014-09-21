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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MsgPack.Rpc.Server.Dispatch;
using NUnit.Framework;

namespace MsgPack.Rpc.Server.Protocols
{
	[TestFixture()]
	[Timeout( TimeoutMilliseconds )]
	public class UdpServerTransportTest
	{
		public const int TimeoutMilliseconds = 3000;

		private static void TestSendReceiveRequest( Action<IPEndPoint> test )
		{
			TestSendReceiveRequest( ( endPoint, _ ) => test( endPoint ) );
		}

		private static void TestSendReceiveRequest( Action<IPEndPoint, UdpServerTransportManager> test )
		{
			var endPoint = new IPEndPoint( IPAddress.Loopback, 57319 );
			var config = new RpcServerConfiguration();
			config.BindingEndPoint = endPoint;
			config.DispatcherProvider =
				s =>
					new CallbackDispatcher(
						s,
						( id, args ) => args
					);
			config.PreferIPv4 = true;

			using ( var server = new RpcServer( config ) )
			using ( var transportManager = new UdpServerTransportManager( server ) )
			{
				test( endPoint, transportManager );
			}
		}

		private static void PackRequest( Packer packer, string id )
		{
			packer.PackArrayHeader( 4 );
			packer.Pack( 0 );
			packer.Pack( 1 );
			packer.PackString( "Test" );
			packer.PackArrayHeader( 1 );
			packer.PackString( id );
		}

		private static void AssertResponse( IList<MessagePackObject> result, params string[] ids )
		{
			Assert.That( result, Is.Not.Null );
			Assert.That( result.Count, Is.EqualTo( 4 ) );
			Assert.That( result[ 0 ] == 1, result[ 0 ].ToString() );
			Assert.That( result[ 1 ] == 1, result[ 1 ].ToString() );
			Assert.That( result[ 2 ].IsNil, result[ 2 ].ToString() );
			Assert.That( result[ 3 ].IsArray, result[ 3 ].ToString() );
			Assert.That( result[ 3 ].AsList().Count, Is.EqualTo( 1 ), result[ 3 ].ToString() );
			Assert.That( ids.Contains( result[ 3 ].AsList()[ 0 ].ToString() ), "[{0}] contains '{1}'", String.Join( ", ", ids ), result[ 3 ].AsList()[ 0 ].ToString() );
		}

		private static void TestSendReceiveRequestCore( IPEndPoint endPoint, int count, CountdownEvent latch )
		{
			using ( var udpClient = new UdpClient( AddressFamily.InterNetwork ) )
			{
				udpClient.Connect( endPoint );

				for ( int i = 0; i < count; i++ )
				{
					if ( latch != null )
					{
						latch.Reset();
					}

					var ids = Enumerable.Repeat( 0, latch == null ? 1 : latch.InitialCount ).Select( _ => Guid.NewGuid().ToString() ).ToArray();

					if ( !Task.WaitAll(
						ids.Select(
							id =>
								Task.Factory.StartNew(
									_ =>
									{
										using ( var buffer = new MemoryStream() )
										{
											using ( var packer = Packer.Create( buffer, false ) )
											{
												PackRequest( packer, id );
											}

											buffer.Position = 0;

											if ( latch != null )
											{
												latch.Signal();
												if ( !latch.Wait( Debugger.IsAttached ? Timeout.Infinite : TimeoutMilliseconds ) )
												{
													throw new TimeoutException();
												}
											}

											// send
											udpClient.Send( buffer.ToArray(), ( int )buffer.Length );
										}
									},
									id
								)
						).ToArray(),
						Debugger.IsAttached ? Timeout.Infinite : TimeoutMilliseconds
					) )
					{
						throw new TimeoutException();
					}

					// receive
					IPEndPoint received = endPoint;
					var result = Unpacking.UnpackArray( udpClient.Receive( ref endPoint ) ).Value;
					AssertResponse( result, ids );
				}
			}
		}

		[Test()]
		public void TestSendReceiveRequest_Once_Ok()
		{
			TestSendReceiveRequest(
				endPoint => TestSendReceiveRequestCore( endPoint, 1, null )
			);
		}

		[Test()]
		public void TestSendReceiveRequest_Twice_Ok()
		{
			TestSendReceiveRequest(
				endPoint => TestSendReceiveRequestCore( endPoint, 2, null )
			);
		}

		[Test()]
		public void TestSendReceiveRequest_Parallel_Ok()
		{
			TestSendReceiveRequest(
				endPoint =>
				{
					using ( var latch = new CountdownEvent( 2 ) )
					{
						TestSendReceiveRequestCore( endPoint, 1, latch );
					}
				}
			);
		}

		private static void TestSendNotify( int concurrency, Action<IPEndPoint, CountdownEvent, IProducerConsumerCollection<string>> test )
		{
			var endPoint = new IPEndPoint( IPAddress.Loopback, 57319 );
			var config = new RpcServerConfiguration();
			config.BindingEndPoint = endPoint;

			using ( var arrivalLatch = new CountdownEvent( concurrency ) )
			{
				var arriveds = new ConcurrentQueue<string>();
				config.DispatcherProvider =
					s =>
						new CallbackDispatcher(
							s,
							( id, args ) =>
							{
								arriveds.Enqueue( args[ 0 ].ToString() );
								arrivalLatch.Signal();
								return args;
							}
						);
				config.PreferIPv4 = true;

				using ( var server = new RpcServer( config ) )
				using ( var transportManager = new UdpServerTransportManager( server ) )
				{
					test( endPoint, arrivalLatch, arriveds );
				}
			}
		}

		private static void PackNotify( Packer packer, string id )
		{
			packer.PackArrayHeader( 3 );
			packer.Pack( 2 );
			packer.PackString( "Test" );
			packer.PackArrayHeader( 1 );
			packer.PackString( id );
		}

		private static void TestSendNotifyCore( IPEndPoint endPoint, CountdownEvent arrivalLatch, IProducerConsumerCollection<string> arrivedIds, int count )
		{
			using ( var udpClient = new UdpClient( AddressFamily.InterNetwork ) )
			using ( var concurrencyLatch = new CountdownEvent( arrivalLatch.InitialCount ) )
			{
				udpClient.Connect( endPoint );

				for ( int i = 0; i < count; i++ )
				{
					if ( concurrencyLatch != null )
					{
						concurrencyLatch.Reset();
					}

					arrivalLatch.Reset();

					// Clear ids.
					string dummy;
					while ( arrivedIds.TryTake( out dummy ) ) { }

					var ids = Enumerable.Repeat( 0, concurrencyLatch == null ? 1 : concurrencyLatch.InitialCount ).Select( _ => Guid.NewGuid().ToString() ).ToArray();

					if ( !Task.WaitAll(
						ids.Select(
							id =>
								Task.Factory.StartNew(
									_ =>
									{
										using ( var buffer = new MemoryStream() )
										{
											using ( var packer = Packer.Create( buffer, false ) )
											{
												PackRequest( packer, id );
											}

											buffer.Position = 0;

											if ( concurrencyLatch != null )
											{
												concurrencyLatch.Signal();
												if ( !concurrencyLatch.Wait( Debugger.IsAttached ? Timeout.Infinite : TimeoutMilliseconds ) )
												{
													throw new TimeoutException();
												}
											}

											// send
											udpClient.Send( buffer.ToArray(), ( int )buffer.Length );
										}
									},
									id
								)
						).ToArray(),
						Debugger.IsAttached ? Timeout.Infinite : TimeoutMilliseconds
					) )
					{
						throw new TimeoutException();
					}

					// wait
					if ( !arrivalLatch.Wait( Debugger.IsAttached ? Timeout.Infinite : TimeoutMilliseconds ) )
					{
						throw new TimeoutException();
					}

					Assert.That( arrivedIds, Is.EquivalentTo( ids ) );
				}
			}
		}

		[Test()]
		public void TestSendNotify_Once_Ok()
		{
			TestSendNotify(
				1,
				( endPoint, arrivalLatch, arrivedIds ) => TestSendNotifyCore( endPoint, arrivalLatch, arrivedIds, 1 )
			);
		}

		[Test()]
		public void TestSendNotify_Twice_Ok()
		{
			TestSendNotify(
				1,
				( endPoint, arrivalLatch, arrivedIds ) => TestSendNotifyCore( endPoint, arrivalLatch, arrivedIds, 2 )
			);
		}

		[Test()]
		public void TestSendNotify_Parallel_Ok()
		{
			TestSendNotify(
				2,
				( endPoint, arrivalLatch, arrivedIds ) => TestSendNotifyCore( endPoint, arrivalLatch, arrivedIds, 1 )
			);
		}

		[Test()]
		public void TestClientShutdown_NotAffectOthers()
		{
			TestSendReceiveRequest(
				endPoint =>
				{
					using ( var activeUdpClient = new UdpClient( AddressFamily.InterNetwork ) )
					using ( var inactiveUdpClient = new UdpClient( AddressFamily.InterNetwork ) )
					{
						activeUdpClient.Connect( endPoint );
						inactiveUdpClient.Connect( endPoint );

						var id1 = Guid.NewGuid().ToString();

						using ( var buffer = new MemoryStream() )
						{
							using ( var packer = Packer.Create( buffer, false ) )
							{
								PackRequest( packer, id1 );
							}

							buffer.Position = 0;

							// send
							activeUdpClient.Send( buffer.ToArray(), ( int )buffer.Length );
						}

						// receive
						IPEndPoint received1 = endPoint;
						var result1 = Unpacking.UnpackArray( activeUdpClient.Receive( ref received1 ) ).Value;
						AssertResponse( result1, id1 );

						inactiveUdpClient.Client.Shutdown( SocketShutdown.Send );

						var id2 = Guid.NewGuid().ToString();

						using ( var buffer = new MemoryStream() )
						{
							using ( var packer = Packer.Create( buffer, false ) )
							{
								PackRequest( packer, id2 );
							}

							buffer.Position = 0;

							// send
							activeUdpClient.Send( buffer.ToArray(), ( int )buffer.Length );
						}

						// receive
						IPEndPoint received = endPoint;
						var result2 = Unpacking.UnpackArray( activeUdpClient.Receive( ref received ) ).Value;
						AssertResponse( result2, id2 );
					}
				}
			);
		}

		[Test]
		public void TestListen_NotIPv6OnlyOnWinNT6OrLator()
		{
			if ( Environment.OSVersion.Platform != PlatformID.Win32NT || Environment.OSVersion.Version.Major < 6 )
			{
				Assert.Ignore( "This test can be run on WinNT 6 or later." );
			}

			var config = new RpcServerConfiguration() { PreferIPv4 = false };
			using ( var server = new RpcServer() )
			using ( var target = new UdpServerTransportManager( server ) )
			{
				Socket listeningSocket = null;
				target.GetListeningSocket( ref listeningSocket );
#if !MONO
				Assert.That( listeningSocket.GetSocketOption( SocketOptionLevel.IPv6, SocketOptionName.IPv6Only ), Is.EqualTo( 0 ) );
#endif // !MONO
			}
		}
	}
}
