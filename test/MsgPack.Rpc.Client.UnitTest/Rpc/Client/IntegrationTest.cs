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
using MsgPack.Rpc.Server;
using NUnit.Framework;

namespace MsgPack.Rpc.Client
{
	/// <summary>
	///		Prototype test.
	/// </summary>
	[TestFixture]
	[Explicit]
	public class IntegrationTest
	{
		[Test]
		[Timeout( 3000 )]
		public void TestEcho()
		{
			string mesage = "Hello, MsgPack-RPC!!";
			long timeStamp = MessagePackConvert.FromDateTime( DateTime.Now );
			using ( var server =
				CallbackServer.Create(
					( messageId, args ) =>
					{
						Assert.That( args, Is.Not.Null.And.Length.EqualTo( 2 ) );
						Assert.That( args[ 0 ].Equals( timeStamp ) );
						Assert.That( args[ 1 ].Equals( mesage ) );
						return args;
					},
					isDebugMode: true
				) )
			{
				server.Error += ( sender, e ) => Console.Error.WriteLine( "{0} Error:{1}", e.IsClientError ? "Client" : "Server", e.Exception );

				using ( var client = new RpcClient( new IPEndPoint( IPAddress.Loopback, CallbackServer.PortNumber ) ) )
				{
					var result = client.Call( "Echo", timeStamp, mesage );
					var asArray = result.AsList();
					Assert.That( asArray[ 0 ].Equals( timeStamp ) );
					Assert.That( asArray[ 1 ].Equals( mesage ) );
				}
			}
		}
	}
}
