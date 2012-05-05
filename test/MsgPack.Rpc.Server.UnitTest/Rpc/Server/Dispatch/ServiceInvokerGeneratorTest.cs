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
using MsgPack.Rpc.Server.Protocols;
using MsgPack.Serialization;
using NUnit.Framework;

namespace MsgPack.Rpc.Server.Dispatch
{
	[TestFixture()]
	public class ServiceInvokerGeneratorTest
	{
		private readonly bool _isDumpEnabled = false;
		private readonly SerializationContext _serializationContext = new SerializationContext();

		[Test()]
		public void TestConstructorServiceInvokerGenerator_IsDebuggable_CanDump()
		{
			var isDebuggable = true;

			using ( var target = new ServiceInvokerGenerator( isDebuggable ) )
			{
				target.Dump();
				File.Delete( target.AssemblyName.Name + ".dll" );
				File.Delete( target.AssemblyName.Name + ".pdb" );
			}
		}

		[Test()]
		[ExpectedException( typeof( InvalidOperationException ) )]
		public void TestConstructorServiceInvokerGenerator_IsNotDebuggable_CannotDump()
		{
			var isDebuggable = false;

			using ( var target = new ServiceInvokerGenerator( isDebuggable ) )
			{
				try
				{
					target.Dump();
				}
				finally
				{
					File.Delete( target.AssemblyName.Name + ".dll" );
				}
			}
		}

		private void TestGetServiceInvokerCore<TArg1, TArg2, TResult>(
			EventHandler<ServiceInvokedEventArgs<TResult>> invoked,
			RpcServerConfiguration configuration,
			TArg1 arg1,
			TArg2 arg2,
			Action<ServerResponseContext> assertion
		)
		{
			using ( var target = new ServiceInvokerGenerator( true ) )
			using ( var server = new RpcServer() )
			using ( var transportManager = new NullServerTransportManager( server ) )
			using ( var transport = new NullServerTransport( transportManager ) )
			{
				var service = new Service<TArg1, TArg2, TResult>();
				service.Invoked += invoked;

				var serviceDescription = new ServiceDescription( "Service", () => service );
				var targetOperation = service.GetType().GetMethod( "Invoke" );

				using ( var requestContext = new ServerRequestContext() )
				{
					requestContext.ArgumentsBufferPacker = Packer.Create( requestContext.ArgumentsBuffer, false );
					requestContext.ArgumentsBufferPacker.PackArrayHeader( 2 );
					requestContext.ArgumentsBufferPacker.Pack( arg1 );
					requestContext.ArgumentsBufferPacker.Pack( arg2 );
					requestContext.ArgumentsBuffer.Position = 0;
					requestContext.MessageId = 123;
					requestContext.ArgumentsUnpacker = Unpacker.Create( requestContext.ArgumentsBuffer, false );

					var responseContext = new ServerResponseContext();
					responseContext.SetTransport( transport );
					try
					{
						var result = target.GetServiceInvoker( RpcServerRuntime.Create( configuration, this._serializationContext ), serviceDescription, targetOperation );

						result.InvokeAsync( requestContext, responseContext ).Wait( TimeSpan.FromSeconds( 1 ) );

						assertion( responseContext );
					}
					finally
					{
						if ( this._isDumpEnabled )
						{
							try
							{
								target.Dump();
							}
							catch ( Exception ex )
							{
								Console.Error.WriteLine( "Failed to dump: {0}", ex );
							}
						}
					}
				}
			}
		}

		[Test()]
		public void TestGetServiceInvoker_ReturnsValidInvoker_NonErrorCase()
		{
			using ( var target = new ServiceInvokerGenerator( true ) )
			{
				var arg1 = Guid.NewGuid().ToString();
				var arg2 = Environment.TickCount;
				var returnValue = true;
				this.TestGetServiceInvokerCore<string, int, bool>(
					( sender, e ) =>
					{
						Assert.That( e.Arguments.Length, Is.EqualTo( 2 ) );
						Assert.That( e.Arguments[ 0 ], Is.EqualTo( arg1 ) );
						Assert.That( e.Arguments[ 1 ], Is.EqualTo( arg2 ) );
						e.ReturnValue = returnValue;
					},
					new RpcServerConfiguration() { IsDebugMode = true },
					arg1,
					arg2,
					responseContext =>
					{
						Assert.That( Unpacking.UnpackObject( responseContext.GetErrorData() ).Value.IsNil );
						Assert.That( Unpacking.UnpackObject( responseContext.GetReturnValueData() ).Value.AsBoolean() );
					}
				);
			}
		}

		[Test()]
		public void TestGetServiceInvoker_ReturnsValidInvoker_ErrorCase_IsNotDebugMode_NotRpcError_DefaultMessage()
		{
			using ( var target = new ServiceInvokerGenerator( true ) )
			{
				var arg1 = Guid.NewGuid().ToString();
				var arg2 = Environment.TickCount;
				var rpcError = RpcError.CallError;

				this.TestGetServiceInvokerCore<string, int, bool>(
					( sender, e ) =>
					{
						Assert.That( e.Arguments.Length, Is.EqualTo( 2 ) );
						Assert.That( e.Arguments[ 0 ], Is.EqualTo( arg1 ) );
						Assert.That( e.Arguments[ 1 ], Is.EqualTo( arg2 ) );
						e.Exception = new Exception( Guid.NewGuid().ToString() );
					},
					new RpcServerConfiguration() { IsDebugMode = false },
					arg1,
					arg2,
					responseContext =>
					{
						Assert.That( Unpacking.UnpackObject( responseContext.GetErrorData() ).Value.Equals( rpcError.Identifier ), "{0}!={1}", Unpacking.UnpackObject( responseContext.GetErrorData() ).Value, rpcError.Identifier );
						var exception = new RpcException( rpcError, Unpacking.UnpackObject( responseContext.GetReturnValueData() ).Value );
						Assert.That( exception.RpcError.Identifier, Is.EqualTo( rpcError.Identifier ) );
						Assert.That( exception.RpcError.ErrorCode, Is.EqualTo( rpcError.ErrorCode ) );
						Assert.That( exception.Message, Is.EqualTo( rpcError.DefaultMessageInvariant ) );
						Assert.That( exception.DebugInformation, Is.Empty );
					}
				);
			}
		}

		[Test()]
		public void TestGetServiceInvoker_ReturnsValidInvoker_ErrorCase_IsDebugMode_NotRpcError_SpecifiedMessage()
		{
			using ( var target = new ServiceInvokerGenerator( true ) )
			{
				var arg1 = Guid.NewGuid().ToString();
				var arg2 = Environment.TickCount;
				var message = Guid.NewGuid().ToString();
				var rpcError = RpcError.CallError;

				this.TestGetServiceInvokerCore<string, int, bool>(
					( sender, e ) =>
					{
						Assert.That( e.Arguments.Length, Is.EqualTo( 2 ) );
						Assert.That( e.Arguments[ 0 ], Is.EqualTo( arg1 ) );
						Assert.That( e.Arguments[ 1 ], Is.EqualTo( arg2 ) );
						e.Exception = new Exception( message );
					},
					new RpcServerConfiguration() { IsDebugMode = true },
					arg1,
					arg2,
					responseContext =>
					{
						Assert.That( Unpacking.UnpackObject( responseContext.GetErrorData() ).Value.Equals( rpcError.Identifier ) );
						var exception = new RpcException( rpcError, Unpacking.UnpackObject( responseContext.GetReturnValueData() ).Value );
						Assert.That( exception.RpcError.Identifier, Is.EqualTo( rpcError.Identifier ) );
						Assert.That( exception.RpcError.ErrorCode, Is.EqualTo( rpcError.ErrorCode ) );
						Assert.That( exception.Message, Is.EqualTo( message ) );
						Assert.That( exception.DebugInformation, Is.StringContaining( message ).And.Not.EqualTo( message ) );
					}
				);
			}
		}

		[Test()]
		public void TestGetServiceInvoker_ReturnsValidInvoker_ErrorCase_IsNotDebugMode_RpcError_WithMessageAndDebugInformation()
		{
			using ( var target = new ServiceInvokerGenerator( true ) )
			{
				var arg1 = Guid.NewGuid().ToString();
				var arg2 = Environment.TickCount;
				var message = Guid.NewGuid().ToString();
				var debugInformation = Guid.NewGuid().ToString();
				var rpcError = RpcError.CustomError( Guid.NewGuid().ToString(), Math.Abs( Environment.TickCount ) );

				this.TestGetServiceInvokerCore<string, int, bool>(
					( sender, e ) =>
					{
						Assert.That( e.Arguments.Length, Is.EqualTo( 2 ) );
						Assert.That( e.Arguments[ 0 ], Is.EqualTo( arg1 ) );
						Assert.That( e.Arguments[ 1 ], Is.EqualTo( arg2 ) );
						e.Exception = new RpcException( rpcError, message, debugInformation );
					},
					new RpcServerConfiguration() { IsDebugMode = false },
					arg1,
					arg2,
					responseContext =>
					{
						Assert.That( Unpacking.UnpackObject( responseContext.GetErrorData() ).Value.Equals( rpcError.Identifier ) );
						var exception = new RpcException( rpcError, Unpacking.UnpackObject( responseContext.GetReturnValueData() ).Value );
						Assert.That( exception.RpcError.Identifier, Is.EqualTo( rpcError.Identifier ) );
						Assert.That( exception.RpcError.ErrorCode, Is.EqualTo( rpcError.ErrorCode ) );
						Assert.That( exception.Message, Is.EqualTo( rpcError.DefaultMessageInvariant ) );
						Assert.That( exception.DebugInformation, Is.Empty );
					}
				);

			}
		}

		[Test()]
		public void TestGetServiceInvoker_ReturnsValidInvoker_ErrorCase_IsDebugMode_RpcError_WithMessageAndDebugInformation()
		{
			using ( var target = new ServiceInvokerGenerator( true ) )
			{
				var arg1 = Guid.NewGuid().ToString();
				var arg2 = Environment.TickCount;
				var message = Guid.NewGuid().ToString();
				var debugInformation = Guid.NewGuid().ToString();
				var rpcError = RpcError.CustomError( Guid.NewGuid().ToString(), Math.Abs( Environment.TickCount ) );

				this.TestGetServiceInvokerCore<string, int, bool>(
					( sender, e ) =>
					{
						Assert.That( e.Arguments.Length, Is.EqualTo( 2 ) );
						Assert.That( e.Arguments[ 0 ], Is.EqualTo( arg1 ) );
						Assert.That( e.Arguments[ 1 ], Is.EqualTo( arg2 ) );
						e.Exception = new RpcException( rpcError, message, debugInformation );
					},
					new RpcServerConfiguration() { IsDebugMode = true },
					arg1,
					arg2,
					responseContext =>
					{
						Assert.That( Unpacking.UnpackObject( responseContext.GetErrorData() ).Value.Equals( rpcError.Identifier ) );
						var exception = new RpcException( rpcError, Unpacking.UnpackObject( responseContext.GetReturnValueData() ).Value );
						Assert.That( exception.RpcError.Identifier, Is.EqualTo( rpcError.Identifier ) );
						Assert.That( exception.RpcError.ErrorCode, Is.EqualTo( rpcError.ErrorCode ) );
						Assert.That( exception.Message, Is.EqualTo( message ) );
						Assert.That( exception.DebugInformation, Is.EqualTo( debugInformation ) );
					}
				);
			}
		}

		[MessagePackRpcServiceContract()]
		public class Service<TArg1, TArg2, TResult>
		{
			public event EventHandler<ServiceInvokedEventArgs<TResult>> Invoked;

			[MessagePackRpcMethod]
			public TResult Invoke( TArg1 arg1, TArg2 arg2 )
			{
				var e = new ServiceInvokedEventArgs<TResult>() { Arguments = new object[] { arg1, arg2 } };
				var handler = this.Invoked;
				if ( handler != null )
				{
					handler( this, e );
				}

				if ( e.Exception != null )
				{
					throw e.Exception;
				}

				return ( TResult )e.ReturnValue;
			}
		}

		public sealed class ServiceInvokedEventArgs<TResult> : EventArgs
		{
			public object[] Arguments { get; set; }
			public TResult ReturnValue { get; set; }
			public Exception Exception { get; set; }
		}
	}
}
