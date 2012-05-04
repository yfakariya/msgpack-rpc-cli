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
using System.Threading.Tasks;
using MsgPack.Rpc.Server.Protocols;
using NUnit.Framework;

namespace MsgPack.Rpc.Server.Dispatch
{
	[TestFixture()]
	public class DispatcherTest
	{
		[Test()]
		public void TestDispatch_NonNull_Dispatch_String_Invoked()
		{
			using ( var server = new RpcServer() )
			using ( var transportManager = new NullServerTransportManager( server ) )
			using ( var transport = new NullServerTransport( transportManager ) )
			using ( var requestContext = DispatchTestHelper.CreateRequestContext() )
			{
				var target = new Target( server );
				bool invoked = false;
				target.Dispatching += ( sender, e ) => { invoked = true; };
				requestContext.MethodName = "Method";
				requestContext.MessageId = 1;
				requestContext.SetTransport( transport );

				target.Dispatch( transport, requestContext );

				Assert.That( invoked, Is.True );
			}
		}

		[Test]
		public void TestSetReturnValue_ReturnValueIsNotNull_Serialized()
		{
			var returnValue = Guid.NewGuid().ToString();
			using ( var server = new RpcServer() )
			using ( var transportManager = new NullServerTransportManager( server ) )
			using ( var transport = new NullServerTransport( transportManager ) )
			using ( var responseContext = DispatchTestHelper.CreateResponseContext( transport ) )
			{
				var target = new Target( server );
				target.InvokeSetReturnValue( responseContext, returnValue );

				// Details should be tested in ServerResponseContextTest.TestSerialize...
				Assert.That( Unpacking.UnpackObject( responseContext.GetErrorData() ).Value.IsNil );
				Assert.That( Unpacking.UnpackObject( responseContext.GetReturnValueData() ).Value == returnValue );
			}
		}

		[Test]
		public void TestSetReturnValue_ReturnValueIsNull_Serialized()
		{
			var returnValue = default( string );
			using ( var server = new RpcServer() )
			using ( var transportManager = new NullServerTransportManager( server ) )
			using ( var transport = new NullServerTransport( transportManager ) )
			using ( var responseContext = DispatchTestHelper.CreateResponseContext( transport ) )
			{
				var target = new Target( server );
				target.InvokeSetReturnValue( responseContext, returnValue );

				// Details should be tested in ServerResponseContextTest.TestSerialize...
				Assert.That( Unpacking.UnpackObject( responseContext.GetErrorData() ).Value.IsNil );
				Assert.That( Unpacking.UnpackObject( responseContext.GetReturnValueData() ).Value == returnValue );
			}
		}

		[Test]
		public void TestSetReturnValue_ReturnValueIsValueType_Serialized()
		{
			var returnValue = Environment.TickCount;
			using ( var server = new RpcServer() )
			using ( var transportManager = new NullServerTransportManager( server ) )
			using ( var transport = new NullServerTransport( transportManager ) )
			using ( var responseContext = DispatchTestHelper.CreateResponseContext( transport ) )
			{
				var target = new Target( server );
				target.InvokeSetReturnValue( responseContext, returnValue );

				// Details should be tested in ServerResponseContextTest.TestSerialize...
				Assert.That( Unpacking.UnpackObject( responseContext.GetErrorData() ).Value.IsNil );
				Assert.That( Unpacking.UnpackObject( responseContext.GetReturnValueData() ).Value == returnValue );
			}
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestSetReturnValue_ResponseContextIsNull()
		{
			using ( var server = new RpcServer() )
			{
				var target = new Target( server );
				target.InvokeSetReturnValue( null, String.Empty );
			}
		}

		[Test]
		public void TestSetException_ExceptionIsNotNull_Serialized()
		{
			using ( var server = new RpcServer() )
			using ( var transportManager = new NullServerTransportManager( server ) )
			using ( var transport = new NullServerTransport( transportManager ) )
			using ( var responseContext = DispatchTestHelper.CreateResponseContext( transport ) )
			{
				var target = new Target( server );
				target.InvokeSetException( responseContext, "Method", new RpcMissingMethodException( "Method" ) );

				// Details should be tested in ServerResponseContextTest.TestSerialize...
				Assert.That( Unpacking.UnpackString( responseContext.GetErrorData() ).Value == RpcError.NoMethodError.Identifier );
			}
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestSetException_ExceptionIsNull()
		{
			using ( var server = new RpcServer() )
			using ( var transportManager = new NullServerTransportManager( server ) )
			using ( var transport = new NullServerTransport( transportManager ) )
			using ( var responseContext = DispatchTestHelper.CreateResponseContext( transport ) )
			{
				var target = new Target( server );
				target.InvokeSetException( responseContext, "Method", null );
			}
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestSetException_ResponseContextIsNull()
		{
			using ( var server = new RpcServer() )
			{
				var target = new Target( server );
				target.InvokeSetException( null, "Method", new RpcMissingMethodException( "Method" ) );
			}
		}

		private sealed class Target : Dispatcher
		{
			public event EventHandler Dispatching;

			public Target( RpcServer server ) : base( server ) { }

			protected override Func<ServerRequestContext, ServerResponseContext, Task> Dispatch( string methodName )
			{
				var handler = this.Dispatching;
				if ( handler != null )
				{
					handler( this, EventArgs.Empty );
				}

				return ( _1, _2 ) => Task.Factory.StartNew( () => { } );
			}

			public void InvokeSetReturnValue<T>( ServerResponseContext responseContext, T returnValue )
			{
				this.SetReturnValue( responseContext, returnValue );
			}

			public void InvokeSetException( ServerResponseContext responseContext, string operationId, Exception exception )
			{
				this.SetException( responseContext, operationId, exception );
			}
		}

	}
}
