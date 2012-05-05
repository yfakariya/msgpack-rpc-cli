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
using System.IO;
using System.Threading;
using MsgPack.Rpc.Server.Protocols;
using NUnit.Framework;
using System.Globalization;

namespace MsgPack.Rpc.Server.Dispatch
{
	/// <summary>
	///Tests the Locator Based Dispatcher 
	/// </summary>
	[TestFixture()]
	public class LocatorBasedDispatcherTest
	{
		[Test]
		public void TestDispatch_MethodExists_Success()
		{
			var svcFile = ".\\Services.svc";
			File.WriteAllText(
				svcFile,
				String.Format( CultureInfo.InvariantCulture, "<% @ ServiceHost Service=\"{0}\" %>", typeof( TestService ).FullName )
			);
			try
			{
				var configuration = new RpcServerConfiguration();
				configuration.ServiceTypeLocatorProvider = conf => new FileBasedServiceTypeLocator();

				using ( var server = new RpcServer( configuration ) )
				using ( var transportManager = new NullServerTransportManager( server ) )
				using ( var transport = new NullServerTransport( transportManager ) )
				using ( var requestContext = DispatchTestHelper.CreateRequestContext() )
				using ( var argumentsBuffer = new MemoryStream() )
				using ( var waitHandle = new ManualResetEventSlim() )
				{
					var message = Guid.NewGuid().ToString();
					using ( var argumentsPacker = Packer.Create( argumentsBuffer, false ) )
					{
						argumentsPacker.PackArrayHeader( 1 );
						argumentsPacker.Pack( message );
					}

					argumentsBuffer.Position = 0;

					var target = new LocatorBasedDispatcher( server );
					MessagePackObject response = MessagePackObject.Nil;
					requestContext.MethodName = "Echo:TestService:1";
					requestContext.MessageId = 1;
					requestContext.SetTransport( transport );
					requestContext.ArgumentsUnpacker = Unpacker.Create( argumentsBuffer );
					transport.Sent +=
						( sender, e ) =>
						{
							response = Unpacking.UnpackString( e.Context.GetReturnValueData() ).Value;
							waitHandle.Set();
						};
					target.Dispatch( transport, requestContext );

					Assert.That( waitHandle.Wait( TimeSpan.FromSeconds( 1 ) ) );

					Assert.That( message == response, "{0} != {1}", message, response );
				}
			}
			finally
			{
				File.Delete( svcFile );
			}
		}

		[Test]
		public void TestDispatch_MethodNotExists_NoMethodError()
		{
			using ( var server = new RpcServer() )
			using ( var transportManager = new NullServerTransportManager( server ) )
			using ( var transport = new NullServerTransport( transportManager ) )
			using ( var requestContext = DispatchTestHelper.CreateRequestContext() )
			using ( var argumentsBuffer = new MemoryStream() )
			using ( var waitHandle = new ManualResetEventSlim() )
			{
				var message = Guid.NewGuid().ToString();
				using ( var argumentsPacker = Packer.Create( argumentsBuffer, false ) )
				{
					argumentsPacker.PackArrayHeader( 1 );
					argumentsPacker.Pack( message );
				}

				argumentsBuffer.Position = 0;

				var target = new LocatorBasedDispatcher( server );
				MessagePackObject response = MessagePackObject.Nil;
				requestContext.MethodName = "Echo:TestServices:1";
				requestContext.MessageId = 1;
				requestContext.SetTransport( transport );
				requestContext.ArgumentsUnpacker = Unpacker.Create( argumentsBuffer );
				transport.Sent +=
					( sender, e ) =>
					{
						response = Unpacking.UnpackString( e.Context.GetErrorData() ).Value;
						waitHandle.Set();
					};
				target.Dispatch( transport, requestContext );

				Assert.That( waitHandle.Wait( TimeSpan.FromSeconds( 1 ) ) );

				Assert.That( RpcError.NoMethodError.Identifier == response, "{0} != {1}", message, response );
			}
		}
	}

	[MessagePackRpcServiceContract( Name = "TestService", Version = 1 )]
	public sealed class TestService
	{
		[MessagePackRpcMethod]
		public string Echo( string message )
		{
			return message;
		}
	}
}
