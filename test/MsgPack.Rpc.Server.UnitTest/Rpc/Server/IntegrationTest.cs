#region -- License Terms --
//
// MessagePack for CLI
//
// Copyright (C) 2010-2013 FUJIWARA, Yusuke
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
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MsgPack.Rpc.Server.Dispatch;
using MsgPack.Rpc.Server.Protocols;
using NUnit.Framework;

namespace MsgPack.Rpc.Server
{
	/// <summary>
	///		Prototype test.
	/// </summary>
	[TestFixture]
	[Explicit]
	public class IntegrationTest
	{
		private const bool _traceEnabled = true;
		private DebugTraceSourceSetting _debugTrace;
		private readonly TraceSource _trace = new TraceSource( "MsgPack.Rpc.Server.PreTest" );

		[SetUp]
		public void SetUp()
		{
			this._debugTrace = new DebugTraceSourceSetting( _traceEnabled, MsgPackRpcServerTrace.Source, MsgPackRpcServerProtocolsTrace.Source, _trace );
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
				bool isOk = false;
				string message = "Hello, world";
				using ( var waitHandle = new ManualResetEventSlim() )
				{
					using ( var server =
						CallbackServer.Create(
							CreateConfiguration(
								host => new TcpServerTransportManager( host ),
								( id, args ) =>
								{
									try
									{
										Assert.That( args[ 0 ] == message, args[ 0 ].ToString() );
										Assert.That( args[ 1 ].IsTypeOf<Int64>().GetValueOrDefault(), args[ 1 ].ToString() );
										isOk = true;
										return args;
									}
									finally
									{
										waitHandle.Set();
									}
								}
							)
						)
					)
					{
						TestEchoRequestCore( ref isOk, waitHandle, message );

						waitHandle.Reset();
						isOk = false;
						// Again
						TestEchoRequestCore( ref isOk, waitHandle, message );

						waitHandle.Reset();
						isOk = false;
						// Again 2
						TestEchoRequestCore( ref isOk, waitHandle, message );
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

		private void TestEchoRequestCore( ref bool isOk, ManualResetEventSlim waitHandle, string message )
		{
			using ( var client = new TcpClient() )
			{
				client.Connect( new IPEndPoint( IPAddress.Loopback, CallbackServer.PortNumber ) );

				var now = MessagePackConvert.FromDateTime( DateTime.Now );

				using ( var stream = client.GetStream() )
				using ( var packer = Packer.Create( stream ) )
				{
					this._trace.TraceInformation( "---- Client sending request ----" );

					packer.PackArrayHeader( 4 );
					packer.Pack( 0 );
					packer.Pack( 123 );
					packer.Pack( "Echo" );
					packer.PackArrayHeader( 2 );
					packer.Pack( message );
					packer.Pack( now );

					this._trace.TraceInformation( "---- Client sent request ----" );

					if ( Debugger.IsAttached )
					{
						waitHandle.Wait();
					}
					else
					{
						Assert.That( waitHandle.Wait( TimeSpan.FromSeconds( 3 ) ) );
					}

					this._trace.TraceInformation( "---- Client receiving response ----" );
					var result = Unpacking.UnpackObject( stream );
					Assert.That( result.IsArray );
					var array = result.AsList();
					Assert.That( array.Count, Is.EqualTo( 4 ) );
					Assert.That(
						array[ 0 ] == 1,
						String.Format(
							CultureInfo.CurrentCulture,
							"Expected: {1}{0}Actual : {2}",
							Environment.NewLine,
							1,
							array[ 0 ].ToString() ) );
					Assert.That(
						array[ 1 ] == 123,
						String.Format(
							CultureInfo.CurrentCulture,
							"Expected: {1}{0}Actual : {2}",
							Environment.NewLine,
							123,
							array[ 1 ].ToString() ) );
					Assert.That(
						array[ 2 ] == MessagePackObject.Nil,
						String.Format(
							CultureInfo.CurrentCulture,
							"Expected: {1}{0}Actual : {2}",
							Environment.NewLine,
							MessagePackObject.Nil,
							array[ 2 ].ToString() ) );
					Assert.That( array[ 3 ].IsArray, array[ 3 ].ToString() );
					var returnValue = array[ 3 ].AsList();
					Assert.That( returnValue.Count, Is.EqualTo( 2 ) );
					Assert.That(
						returnValue[ 0 ] == message,
						String.Format(
							CultureInfo.CurrentCulture,
							"Expected: {1}{0}Actual : {2}",
							Environment.NewLine,
							message,
							returnValue[ 0 ].ToString() ) );
					Assert.That(
						returnValue[ 1 ] == now,
						String.Format(
							CultureInfo.CurrentCulture,
							"Expected: {1}{0}Actual : {2}",
							Environment.NewLine,
							now,
							returnValue[ 1 ].ToString() ) );
					this._trace.TraceInformation( "---- Client received response ----" );
				}
			}
		}

		[Test]
		public void TestEchoRequestContinuous()
		{
			try
			{
				const int count = 3;
				bool[] serverStatus = new bool[ count ];
				string message = "Hello, world";
				using ( var waitHandle = new CountdownEvent( count ) )
				using ( var server =
					CallbackServer.Create(
						CreateConfiguration(
							host => new TcpServerTransportManager( host ),
							( id, args ) =>
							{
								try
								{
									Assert.That( args[ 0 ] == message );
									Assert.That( args[ 1 ].IsTypeOf<Int64>().GetValueOrDefault() );
									lock ( serverStatus )
									{
										serverStatus[ id.Value ] = true;
									}

									return args;
								}
								finally
								{
									waitHandle.Signal();
								}
							}
						)
					)
				)
				{
					TestEchoRequestContinuousCore( serverStatus, waitHandle, count, message );

					waitHandle.Reset();
					Array.Clear( serverStatus, 0, serverStatus.Length );
					// Again
					TestEchoRequestContinuousCore( serverStatus, waitHandle, count, message );
				}
			}
			catch ( SocketException sockEx )
			{
				Console.Error.WriteLine( "{0}({1}:0x{1:x8})", sockEx.SocketErrorCode, sockEx.ErrorCode );
				Console.Error.WriteLine( sockEx );
				throw;
			}
		}

		private void TestEchoRequestContinuousCore( bool[] serverStatus, CountdownEvent waitHandle, int count, string message )
		{
			using ( var client = new TcpClient() )
			{
				client.Connect( new IPEndPoint( IPAddress.Loopback, CallbackServer.PortNumber ) );

				var now = MessagePackConvert.FromDateTime( DateTime.Now );
				bool[] resposeStatus = new bool[ count ];

				using ( var stream = client.GetStream() )
				using ( var packer = Packer.Create( stream ) )
				{
					for ( int i = 0; i < count; i++ )
					{
						this._trace.TraceInformation( "---- Client sending request ----" );
						packer.PackArrayHeader( 4 );
						packer.Pack( 0 );
						packer.Pack( i );
						packer.Pack( "Echo" );
						packer.PackArrayHeader( 2 );
						packer.Pack( message );
						packer.Pack( now );
						this._trace.TraceInformation( "---- Client sent request ----" );
					}

					if ( Debugger.IsAttached )
					{
						waitHandle.Wait();
					}
					else
					{
						Assert.That( waitHandle.Wait( TimeSpan.FromSeconds( count * 3 ) ) );
					}

					for ( int i = 0; i < count; i++ )
					{
						this._trace.TraceInformation( "---- Client receiving response ----" );
						var array = Unpacking.UnpackArray( stream );
						Assert.That( array.Count, Is.EqualTo( 4 ) );
						Assert.That( array[ 0 ] == 1, array[ 0 ].ToString() );
						Assert.That( array[ 1 ].IsTypeOf<int>().GetValueOrDefault() );
						resposeStatus[ array[ 1 ].AsInt32() ] = true;
						Assert.That( array[ 2 ] == MessagePackObject.Nil, array[ 2 ].ToString() );
						Assert.That( array[ 3 ].IsArray, array[ 3 ].ToString() );
						var returnValue = array[ 3 ].AsList();
						Assert.That( returnValue.Count, Is.EqualTo( 2 ) );
						Assert.That( returnValue[ 0 ] == message, returnValue[ 0 ].ToString() );
						Assert.That( returnValue[ 1 ] == now, returnValue[ 1 ].ToString() );
						this._trace.TraceInformation( "---- Client received response ----" );
					}

					lock ( serverStatus )
					{
						Assert.That( serverStatus, Is.All.True, String.Join( ", ", serverStatus ) );
					}
					Assert.That( resposeStatus, Is.All.True );
				}
			}
		}

		private static RpcServerConfiguration CreateConfiguration(
			Func<RpcServer, ServerTransportManager> transportManagerProvider,
			Func<int?, MessagePackObject[], MessagePackObject> callback
		)
		{
			return
				new RpcServerConfiguration()
				{
					PreferIPv4 = true,
					BindingEndPoint = new IPEndPoint( IPAddress.Any, 57319 ),
					MinimumConcurrentRequest = 1,
					MaximumConcurrentRequest = 10,
					MinimumConnection = 1,
					MaximumConnection = 1,
					TransportManagerProvider = transportManagerProvider,
					DispatcherProvider = server => new CallbackDispatcher( server, callback )
				};
		}
	}
}
