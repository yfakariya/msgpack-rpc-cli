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
using System.Threading.Tasks;
using NUnit.Framework;

namespace MsgPack.Rpc.Client.Protocols
{
	[TestFixture]
	public class ClientTransportManagerTest
	{
		[Test]
		public void TestDispose_Called()
		{
			var target = new Target( RpcClientConfiguration.Default );

			target.Dispose();

			Assert.That( target.IsDisposed );
			Assert.That( target.DisposeCalled.GetValueOrDefault() );
		}

		[Test]
		public void TestConnectAsync_ConnectAsyncCoreCalled()
		{
			using ( var target = new Target( RpcClientConfiguration.Default ) )
			using ( var task = target.ConnectAsync( new IPEndPoint( IPAddress.Loopback, 0 ) ) )
			{
				Assert.That( target.ConnectAsyncCoreCalled );
				Assert.That( task.Wait( TimeSpan.FromSeconds( 1 ) ) );
			}
		}

		[Test]
		public void TestBeginShutdown_CallShutdownCalled()
		{
			using ( var target = new Target( RpcClientConfiguration.Default ) )
			{
				target.BeginShutdown();

				Assert.That( target.BeginShutdownCalled );
				Assert.That( target.IsInShutdown, Is.True );
			}
		}

		private sealed class Target : ClientTransportManager
		{
			public bool? DisposeCalled;
			public bool BeginShutdownCalled;
			public bool ConnectAsyncCoreCalled;

			public Target( RpcClientConfiguration configuration ) : base( configuration ) { }

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

			protected override Task<ClientTransport> ConnectAsyncCore( EndPoint targetEndPoint )
			{
				this.ConnectAsyncCoreCalled = true;
				return Task.Factory.StartNew( () => default( ClientTransport ) );
			}

			internal override void ReturnTransport( ClientTransport transport )
			{
				// nop
			}
		}
	}
}
