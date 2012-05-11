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
using MsgPack.Serialization;
using NUnit.Framework;

namespace MsgPack.Rpc.Server.Dispatch
{
	/// <summary>
	///Tests the Async Service Invoker 
	/// </summary>
	[TestFixture()]
	public class AsyncServiceInvokerTest
	{
		[Test()]
		public void TestInvokeAsync_Success_TaskSetSerializedReturnValue()
		{
			using ( var server = new RpcServer() )
			using ( var transportManager = new NullServerTransportManager( server ) )
			using ( var transport = new NullServerTransport( transportManager ) )
			{
				ServerRequestContext requestContext = DispatchTestHelper.CreateRequestContext();
				ServerResponseContext responseContext = DispatchTestHelper.CreateResponseContext( transport );
				using ( var result = new Target( null, RpcErrorMessage.Success ).InvokeAsync( requestContext, responseContext ) )
				{
					result.Wait();
				}

				Assert.That( responseContext.GetReturnValueData(), Is.EqualTo( new byte[] { 123 } ) );
				Assert.That( responseContext.GetErrorData(), Is.EqualTo( new byte[] { 0xC0 } ) );
			}
		}

		[Test()]
		public void TestInvokeAsync_FatalError_TaskSetSerializedError()
		{
			using ( var server = new RpcServer() )
			using ( var transportManager = new NullServerTransportManager( server ) )
			using ( var transport = new NullServerTransport( transportManager ) )
			{
				ServerRequestContext requestContext = DispatchTestHelper.CreateRequestContext();
				ServerResponseContext responseContext = DispatchTestHelper.CreateResponseContext( transport );
				using ( var result = new Target( new Exception( "FAIL" ), RpcErrorMessage.Success ).InvokeAsync( requestContext, responseContext ) )
				{
					result.Wait();
				}

				var error = Unpacking.UnpackObject( responseContext.GetErrorData() );
				var errorDetail = Unpacking.UnpackObject( responseContext.GetReturnValueData() );
				Assert.That( error.Value.Equals( RpcError.CallError.Identifier ) );
				Assert.That( errorDetail.Value.IsNil, Is.False );
			}
		}

		[Test()]
		public void TestInvokeAsync_MethodError_TaskSetSerializedError()
		{
			using ( var server = new RpcServer() )
			using ( var transportManager = new NullServerTransportManager( server ) )
			using ( var transport = new NullServerTransport( transportManager ) )
			{
				ServerRequestContext requestContext = DispatchTestHelper.CreateRequestContext();
				ServerResponseContext responseContext = DispatchTestHelper.CreateResponseContext( transport );
				using ( var result = new Target( null, new RpcErrorMessage( RpcError.ArgumentError, MessagePackObject.Nil ) ).InvokeAsync( requestContext, responseContext ) )
				{
					result.Wait();
				}

				var error = Unpacking.UnpackObject( responseContext.GetErrorData() );
				var errorDetail = Unpacking.UnpackObject( responseContext.GetReturnValueData() );
				Assert.That( error.Value.Equals( RpcError.ArgumentError.Identifier ) );
				Assert.That( errorDetail.Value.IsNil, Is.True );
			}
		}

		private sealed class Target : AsyncServiceInvoker<int>
		{
			private readonly Exception _fatalError;
			private readonly RpcErrorMessage _methodError;

			public Target( Exception fatalError, RpcErrorMessage methodError )
				: base( RpcServerRuntime.Create( RpcServerConfiguration.Default, new SerializationContext() ), new ServiceDescription( "Dummy", () => new object() ), typeof( object ).GetMethod( "ToString" ) )
			{
				this._fatalError = fatalError;
				this._methodError = methodError;
			}

			protected override AsyncInvocationResult InvokeCore( Unpacker arguments )
			{
				if ( this._fatalError != null )
				{
					throw this._fatalError;
				}

				if ( this._methodError.IsSuccess )
				{
					return new AsyncInvocationResult( Task.Factory.StartNew( () => 123 ) );
				}
				else
				{
					return new AsyncInvocationResult( this._methodError );
				}
			}
		}

	}
}
