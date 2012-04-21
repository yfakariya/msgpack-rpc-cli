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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MsgPack.Rpc.Protocols;
using NUnit.Framework;

namespace MsgPack.Rpc.Client.Protocols
{
	[TestFixture]
	[Timeout( 1000 )]
	public class ClientTransportTest
	{
		private sealed class EchoServer : IDisposable
		{
			private readonly Server.RpcServer _server;
			private readonly Server.Protocols.InProcServerTransportManager _transportManager;

			public Server.Protocols.InProcServerTransportManager TransportManager
			{
				get { return this._transportManager; }
			}

			public EchoServer()
				: this( ( id, args ) => args ) { }

			public EchoServer( Func<int?, MessagePackObject[], MessagePackObject> callback )
			{
				var config = new Server.RpcServerConfiguration();
				config.DispatcherProvider = s => new Server.Dispatch.CallbackDispatcher( s, callback );
				this._server = new Server.RpcServer( config );
				this._transportManager =
					new Server.Protocols.InProcServerTransportManager(
						this._server,
						m => new SingletonObjectPool<Server.Protocols.InProcServerTransport>( new Server.Protocols.InProcServerTransport( m ) )
					);
			}

			public void Dispose()
			{
				this._transportManager.Dispose();
				this._server.Dispose();
			}
		}

		private static readonly EndPoint _endPoint =
			new IPEndPoint( IPAddress.Loopback, 0 );

		[Test]
		public void TestGetClientRequestContext_BoundTransportSetAsCallee()
		{
			using ( var server = new EchoServer() )
			using ( var manager = new InProcClientTransportManager( RpcClientConfiguration.Default, server.TransportManager ) )
			using ( var target = manager.ConnectAsync( _endPoint ).Result )
			{
				var result = target.GetClientRequestContext();
				Assert.AreSame( result.BoundTransport, target );
			}
		}

		[Test]
		public void TestReturnContextContext_ClientRequestContext_SameOrigin_BoundTransportSetNull()
		{
			using ( var server = new EchoServer() )
			using ( var manager = new InProcClientTransportManager( RpcClientConfiguration.Default, server.TransportManager ) )
			using ( var target = manager.ConnectAsync( _endPoint ).Result )
			{
				var context = target.GetClientRequestContext();
				target.ReturnContext( context );
			}
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestReturnContextContext_ClientRequestContext_Null_Fail()
		{
			using ( var server = new EchoServer() )
			using ( var manager = new InProcClientTransportManager( RpcClientConfiguration.Default, server.TransportManager ) )
			using ( var target = manager.ConnectAsync( _endPoint ).Result )
			{
				target.ReturnContext( default( ClientRequestContext ) );
			}
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestReturnContextContext_ClientRequestContext_AnotherOrigin_Fail()
		{
			using ( var server = new EchoServer() )
			using ( var manager = new InProcClientTransportManager( RpcClientConfiguration.Default, server.TransportManager ) )
			using ( var target = manager.ConnectAsync( _endPoint ).Result )
			using ( var another = manager.ConnectAsync( _endPoint ).Result )
			{
				var context = another.GetClientRequestContext();
				target.ReturnContext( context );
			}
		}

		[Test]
		public void TestReturnContextContext_ClientResponseContext_SameOrigin_BoundTransportSetNull()
		{
			using ( var server = new EchoServer() )
			using ( var manager = new InProcClientTransportManager( RpcClientConfiguration.Default, server.TransportManager ) )
			using ( var target = manager.ConnectAsync( _endPoint ).Result )
			{
				var context = new ClientResponseContext();
				context.SetTransport( target );
				target.ReturnContext( context );
			}
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestReturnContextContext_ClientResponseContext_Null_Fail()
		{
			using ( var server = new EchoServer() )
			using ( var manager = new InProcClientTransportManager( RpcClientConfiguration.Default, server.TransportManager ) )
			using ( var target = manager.ConnectAsync( _endPoint ).Result )
			{
				target.ReturnContext( default( ClientResponseContext ) );
			}
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestReturnContextContext_ClientResponseContext_AnotherOrigin_Fail()
		{
			using ( var server = new EchoServer() )
			using ( var manager = new InProcClientTransportManager( RpcClientConfiguration.Default, server.TransportManager ) )
			using ( var target = manager.ConnectAsync( _endPoint ).Result )
			using ( var another = manager.ConnectAsync( _endPoint ).Result )
			{
				var context = new ClientResponseContext();
				context.SetTransport( another );
				target.ReturnContext( context );
			}
		}

		private void TestCore( Action<ClientTransport, Server.Protocols.ServerTransportManager> test )
		{
			TestCore( ( target, manager ) => test( target, manager ), ( _0, _1 ) => MessagePackObject.Nil );
		}

		private void TestCore( Action<ClientTransport, Server.Protocols.ServerTransportManager> test, Func<int?, MessagePackObject[], MessagePackObject> callback )
		{
			// TODO: Timeout setting
			using ( var server = new EchoServer( callback ) )
			using ( var manager = new InProcClientTransportManager( RpcClientConfiguration.Default, server.TransportManager ) )
			using ( var target = manager.ConnectAsync( _endPoint ) )
			{
				test( target.Result, server.TransportManager );
			}
		}

		[Test]
		public void TestDispose_IsDisposedSet()
		{
			TestCore(
				( target, serverTransportManager ) =>
				{
					Assert.That( target.IsDisposed, Is.False );

					target.Dispose();
					Assert.That( target.IsDisposed, Is.True );
				}
			);
		}

		private void TestShutdownCore<T>(
			Action<T> onSend,
			Action<T> onExecute,
			Action<T> onReceive,
			bool willBeConnectionReset,
			Func<Tuple<ClientTransport, Server.Protocols.ServerTransportManager>, T> callbackArgumentSelector
		)
		{
			var arg = Environment.TickCount % 3;
			var returnValue = Environment.TickCount % 5;
			bool isExecuted = false;
			Tuple<ClientTransport, Server.Protocols.ServerTransportManager> tuple = null;
			TestCore(
				( target, manager ) =>
				{
					tuple = Tuple.Create( target, manager );
					var context = target.GetClientRequestContext();
					try
					{
						using ( var waitHandle = new ManualResetEventSlim() )
						using ( var firstResponseWaitHandle = new ManualResetEventSlim() )
						using ( var onReciveWaitHandle = new ManualResetEventSlim() )
						{
							MessagePackObject? resultReturn = null;
							RpcErrorMessage resultError = RpcErrorMessage.Success;
							int? resultMessageId = 0;

							var clientTransport = ( InProcClientTransport )target;
							var serverTransportManager = ( Server.Protocols.InProcServerTransportManager )manager;

							if ( onReceive != null )
							{
								clientTransport.ResponseReceived +=
									( sender, e ) =>
									{
										e.ChunkedReceivedData = new ChunkedReceivedDataEnumerator( e.ReceivedData, () => onReceive( callbackArgumentSelector( tuple ) ) );
									};
							}

							int messageId = Math.Abs( Environment.TickCount % 10 );

							context.SetRequest(
								messageId,
								"Test",
								( responseContext, error, completedSynchronously ) =>
								{
									if ( resultReturn != null )
									{
										return;
									}

									resultMessageId = responseContext.MessageId;
									resultReturn = Unpacking.UnpackObject( responseContext.ResultBuffer );
									resultError = ErrorInterpreter.UnpackError( responseContext );
									waitHandle.Set();
								}
							);

							context.ArgumentsPacker.PackArrayHeader( 1 );
							context.ArgumentsPacker.Pack( arg );

							if ( onSend != null )
							{
								clientTransport.MessageSent += ( sender, e ) => onSend( callbackArgumentSelector( tuple ) );
							}

							clientTransport.Send( context );

							if ( Debugger.IsAttached )
							{
								waitHandle.Wait();
							}
							else
							{
								Assert.That( waitHandle.Wait( TimeSpan.FromSeconds( 1 ) ), Is.True, "Not respond." );
							}

							if ( willBeConnectionReset )
							{
								Assert.That( resultReturn, Is.Not.Null );
								return;
							}

							Assert.That( isExecuted );
							Assert.That( resultMessageId == messageId );
							Assert.That( resultError.IsSuccess );
							Assert.That( resultReturn == returnValue );
						}
					}
					finally
					{
						target.ReturnContext( context );
					}
				},
				( messageId, args ) =>
				{
					Assert.That( args[ 0 ] == arg );

					if ( onExecute != null )
					{
						onExecute( callbackArgumentSelector( tuple ) );
					}

					isExecuted = true;
					return returnValue;
				}
			);
		}

		#region -- BeginShutdown --

		[Test()]
		public void TestBeginShutdown_NoPendingRequest_Harmless()
		{
			TestCore( ( target, _ ) => target.BeginShutdown() );
		}

		private void TestBeginShutdownCore(
			Action<ClientTransport> onReceive,
			Action<ClientTransport> onExecute,
			Action<ClientTransport> onSend,
			bool willBeConnectionReset
		)
		{
			TestShutdownCore( onReceive, onExecute, onSend, willBeConnectionReset, tuple => tuple.Item1 );
		}

		[Test]
		public void TestBeginShutdown_DuringReceiving_ReceivingDrainedAndCanExecuteAndCanSend()
		{
			TestBeginShutdownCore(
				target => target.BeginShutdown(),
				null,
				null,
				true
			);
		}

		[Test]
		public void TestBeginShutdown_DuringExecuting_CanExecuteAndCanSend()
		{
			TestBeginShutdownCore(
				null,
				target => target.BeginShutdown(),
				null,
				false
			);
		}

		[Test]
		public void TestBeginShutdown_DuringSending_CanSend()
		{
			TestBeginShutdownCore(
				null,
				null,
				target => target.BeginShutdown(),
				false
			);
		}

		#endregion

		#region -- Server shutdown --

		[Test]
		public void TestServerShutdown_NoPendingRequest_Harmless()
		{
			TestCore(
				( target, serverTransportManager ) =>
				{
					serverTransportManager.BeginShutdown();
				},
				( id, args ) =>
				{
					return args;
				}
			);
		}

		private void TestServerShutdownCore(
			Action<Server.Protocols.ServerTransportManager> onReceive,
			Action<Server.Protocols.ServerTransportManager> onExecute,
			Action<Server.Protocols.ServerTransportManager> onSend,
			bool willBeConnectionReset
		)
		{
			TestShutdownCore( onReceive, onExecute, onSend, willBeConnectionReset, tuple => tuple.Item2 );
		}

		[Test]
		public void TestServerShutdown_DuringReceiving_ConnectionReset()
		{
			TestServerShutdownCore(
				server => server.BeginShutdown(),
				null,
				null,
				true
			);
		}

		[Test]
		public void TestServerShutdown_DuringExecuting_ConnectionReset()
		{
			TestServerShutdownCore(
				null,
				server => server.BeginShutdown(),
				null,
				true
			);
		}

		[Test]
		public void TestServerShutdown_DuringSending_ConnectionReset()
		{
			TestServerShutdownCore(
				null,
				null,
				server => server.BeginShutdown(),
				true
			);
		}

		#endregion

		#region -- Send --

		private static void TestSendCore(
			MessageType messageType,
			SocketError? socketErrorOnSend
		)
		{
			int? messageId = messageType == MessageType.Request ? Math.Abs( Environment.TickCount % 1000 ) : default( int? );
			string argument = Guid.NewGuid().ToString();
			Func<int?, MessagePackObject[], MessagePackObject> callback =
				( id, args ) =>
				{
					Assert.That( id, Is.EqualTo( messageId ), "Invalid message ID." );
					Assert.That( args.Length, Is.EqualTo( 1 ), "Invalid argument count." );
					Assert.That( args[ 0 ] == argument, "{0} != {1}", args[ 0 ], argument );

					return args;
				};

			using ( var server = new EchoServer( callback ) )
			using ( var manager = new InProcClientTransportManager( RpcClientConfiguration.Default, server.TransportManager ) )
			using ( var transport = manager.ConnectAsync( _endPoint ).Result as InProcClientTransport )
			using ( var waitHandle = new ManualResetEventSlim() )
			{
				if ( socketErrorOnSend != null )
				{
					transport.MessageSent +=
						( sender, e ) =>
						{
							e.Context.SocketError = socketErrorOnSend.Value;
						};
				}
				var context = transport.GetClientRequestContext();
				MessagePackObject? result = null;
				Exception clientRuntimeError = null;

				if ( messageType == MessageType.Request )
				{
					context.SetRequest(
						messageId.Value,
						"Test",
						( responseContext, error, completedSynchronously ) =>
						{
							result = Unpacking.UnpackObject( responseContext.ResultBuffer );
							clientRuntimeError = error;
							waitHandle.Set();
						}
					);
				}
				else
				{
					context.SetNotification(
						"Test",
						( error, completedSynchronously ) =>
						{
							clientRuntimeError = error;
							waitHandle.Set();
						}
					);
				}

				context.ArgumentsPacker.PackArrayHeader( 1 );
				context.ArgumentsPacker.Pack( argument );

				transport.Send( context );

				if ( Debugger.IsAttached )
				{
					waitHandle.Wait();
				}
				else
				{
					Assert.That( waitHandle.Wait( TimeSpan.FromSeconds( 3 ) ), "timeout" );
				}

				if ( socketErrorOnSend != null )
				{
					Assert.That( clientRuntimeError, Is.Not.Null.And.AssignableTo( typeof( RpcException ) ) );
					return;
				}

				Assert.That( clientRuntimeError, Is.Null, "Unexpected runtime error:{0}", clientRuntimeError );

				if ( messageType == MessageType.Request )
				{
					Assert.That( result, Is.Not.Null );
					Assert.That( result.Value.AsList().Count, Is.EqualTo( 1 ) );
					Assert.That( result.Value.AsList()[ 0 ], Is.EqualTo( argument ) );
				}
			}
		}

		[Test]
		public void TestSend_RequestSuccess_CallbackInvokedAndSendAsRequest()
		{
			TestSendCore( MessageType.Request, null );
		}

		[Test]
		public void TestSend_RequestFail_CallbackInvokedWithTransportError()
		{
			TestSendCore( MessageType.Request, SocketError.ConnectionReset );
		}

		[Test]
		public void TestSend_NotifySuccess_CallbackInvokedAndSendAsNotification()
		{
			TestSendCore( MessageType.Notification, null );
		}

		[Test]
		public void TestSend_NotifyFail_CallbackInvokedWithTransportError()
		{
			TestSendCore( MessageType.Notification, SocketError.ConnectionReset );
		}

		[Test]
		public void TestSend_Timeout_ConnectionReset()
		{
			Assert.Inconclusive( "Not implemented yet" );
		}

		#endregion

		#region -- Receive --

		#region ---- Common Private Method ----

		private void TestReceiveCore(
			MessageType messageType,
			RpcError expectedError,
			MessagePackObject? expectedResult,
			Action<InProcResponseReceivedEventArgs> receivedBinaryModifier,
			bool willBeShutdown
		)
		{
			int? messageId = messageType == MessageType.Request ? Math.Abs( Environment.TickCount % 1000 ) : default( int? );
			string argument = Guid.NewGuid().ToString();
			Func<int?, MessagePackObject[], MessagePackObject> callback =
				( id, args ) =>
				{
					Assert.That( id, Is.EqualTo( messageId ), "Invalid message ID." );
					Assert.That( args.Length, Is.EqualTo( 1 ), "Invalid argument count." );
					Assert.That( args[ 0 ] == argument, "{0} != {1}", args[ 0 ], argument );

					if ( expectedError != null )
					{
						throw expectedError.ToException( "Dummy server error." );
					}

					if ( expectedResult == null )
					{
						// Void
						return MessagePackObject.Nil;
					}
					else
					{
						return expectedResult.Value;
					}
				};
			using ( var server = new EchoServer( callback ) )
			using ( var manager = new InProcClientTransportManager( RpcClientConfiguration.Default, server.TransportManager ) )
			using ( var transport = manager.ConnectAsync( _endPoint ).Result as InProcClientTransport )
			using ( var waitHandle = new ManualResetEventSlim() )
			{
				if ( receivedBinaryModifier != null )
				{
					transport.ResponseReceived += ( sender, e ) => receivedBinaryModifier( e );
				}

				var context = transport.GetClientRequestContext();
				ClientResponseContext returnedResponseContext = null;
				Exception clientRuntimeError = null;

				if ( messageType == MessageType.Request )
				{
					context.SetRequest(
						messageId.Value,
						"Test",
						( responseContext, error, completedSynchronously ) =>
						{
							returnedResponseContext = responseContext;
							clientRuntimeError = error;
							waitHandle.Set();
						}
					);
				}
				else
				{
					context.SetNotification(
						"Test",
						( error, completedSynchronously ) =>
						{
							clientRuntimeError = error;
							waitHandle.Set();
						}
					);
				}

				context.ArgumentsPacker.PackArrayHeader( 1 );
				context.ArgumentsPacker.Pack( argument );

				transport.Send( context );

				if ( Debugger.IsAttached )
				{
					waitHandle.Wait();
				}
				else
				{
					Assert.That( waitHandle.Wait( TimeSpan.FromSeconds( 3 ) ), "timeout" );
				}

				Assert.That( clientRuntimeError, Is.Null, "Unexpected runtime error:{0}", clientRuntimeError );

				if ( messageType == MessageType.Request )
				{
					Assert.That( returnedResponseContext, Is.Not.Null );
					if ( expectedError == null )
					{
						var result = Unpacking.UnpackObject( returnedResponseContext.ResultBuffer );
						Assert.That( result == expectedResult, "{0} != {1}", result, expectedResult );
					}
					else
					{
						var error = ErrorInterpreter.UnpackError( returnedResponseContext );
						Assert.That( error.Error, Is.EqualTo( expectedError ) );
					}
				}

				if ( willBeShutdown )
				{
					Assert.That( transport.IsInShutdown, Is.True );
				}
			}
		}

		#endregion

		#region ---- Normal Cases ----

		[Test]
		public void TestReceive_Request_Success_ResponseReturns()
		{
			TestReceiveCore(
				MessageType.Request,
				null,
				"ReturnValue",
				null,
				false
			);
		}

		[Test]
		public void TestReceive_Request_Fail_ResponseReturns()
		{
			TestReceiveCore(
				MessageType.Request,
				RpcError.CustomError( "AppError.DummyError", 1 ),
				"DoNotReturnThis",
				null,
				false
			);
		}

		[Test]
		public void TestReceive_Notify_Success_ResponseNotReturns()
		{
			TestReceiveCore(
				MessageType.Notification,
				null,
				"DoNotReturnThis",
				null,
				false
			);
		}

		[Test]
		public void TestReceive_Notify_Fail_ResponseNotReturns()
		{
			TestReceiveCore(
				MessageType.Notification,
				RpcError.CustomError( "AppError.DummyError", 1 ),
				"DoNotReturnThis",
				null,
				false
			);
		}

		#endregion

		#region ---- Invalid Response --

		private void TestReceiveInvalidRequestCore(
			Action<Packer, int, RpcError, MessagePackObject> invalidRequestPacking,
			RpcError expectedError,
			bool willBeConnectionReset
		)
		{
			TestReceiveCore(
				MessageType.Request,
				expectedError,
				null,
				e =>
				{
					var originalResult = Unpacking.UnpackArray( e.ReceivedData ).Value;

					using ( var buffer = new MemoryStream() )
					using ( var packer = Packer.Create( buffer, false ) )
					{
						invalidRequestPacking( packer, originalResult[ 1 ].AsInt32(), originalResult[ 2 ].IsNil ? null : RpcError.FromIdentifier( originalResult[ 2 ].AsString(), null ), originalResult[ 3 ] );
					}
				},
				willBeConnectionReset
			);
		}

		#region ------ Entire Array ------

		[Test]
		public void TestReceive_RequestNotArray_RemoteRuntimeErrorAndConnectionIsReset()
		{
			TestReceiveInvalidRequestCore(
				( packer, originalMessageId, originalError, originalReturnValue ) => packer.PackString( "Request" ),
				RpcError.RemoteRuntimeError,
				true
			);
		}

		[Test]
		public void TestReceive_EmptyArray_RemoteRuntimeErrorAndConnectionIsReset()
		{
			TestReceiveInvalidRequestCore(
				( packer, originalMessageId, originalError, originalReturnValue ) => packer.PackArrayHeader( 0 ),
				RpcError.RemoteRuntimeError,
				true
			);
		}

		[Test]
		public void TestReceive_ArrayLengthIs3_RemoteRuntimeErrorAndConnectionIsReset()
		{
			TestReceiveInvalidRequestCore(
				( packer, originalMessageId, originalError, originalReturnValue ) =>
				{
					packer.PackArrayHeader( 3 );
					packer.Pack( ( int )MessageType.Response );
					packer.Pack( originalMessageId );
					packer.Pack( originalError );
				},
				RpcError.RemoteRuntimeError,
				true
			);
		}

		[Test]
		public void TestReceive_ArrayLengthIs5_RemoteRuntimErrorAndConnectionIsReset()
		{
			TestReceiveInvalidRequestCore(
				( packer, originalMessageId, originalError, originalReturnValue ) =>
				{
					packer.PackArrayHeader( 5 );
					packer.Pack( ( int )MessageType.Response );
					packer.Pack( originalMessageId );
					packer.Pack( "Test" );
					packer.Pack( originalError );
					packer.Pack( originalReturnValue );
				},
				RpcError.RemoteRuntimeError,
				true
			);
		}

		#endregion

		#region ------ Message Type ------

		[Test]
		public void TestReceive_RequestMessageType_RemoteRuntimErrorAndConnectionIsNotReset()
		{
			TestReceiveInvalidRequestCore(
				( packer, originalMessageId, originalError, originalReturnValue ) =>
				{
					packer.PackArrayHeader( 4 );
					packer.Pack( ( int )MessageType.Request );
					packer.Pack( originalMessageId );
					packer.Pack( originalError );
					packer.Pack( originalReturnValue );
				},
				RpcError.RemoteRuntimeError,
				false
			);
		}

		[Test]
		public void TestReceive_UnknownMessageType_RemoteRuntimErrorAndConnectionIsNotReset()
		{
			TestReceiveInvalidRequestCore(
				( packer, originalMessageId, originalError, originalReturnValue ) =>
				{
					packer.PackArrayHeader( 4 );
					packer.Pack( 3 );
					packer.Pack( originalMessageId );
					packer.Pack( originalError );
					packer.Pack( originalReturnValue );
				},
				RpcError.RemoteRuntimeError,
				false
			);
		}

		#endregion

		#region ------ Message ID ------

		private static void TestReceive_UnknownMessageIdCore(
			RpcError error
			)
		{
			MessagePackObject result = Guid.NewGuid().ToString();
			using ( var server = new EchoServer() )
			using ( var manager = new InProcClientTransportManager( RpcClientConfiguration.Default, server.TransportManager ) )
			using ( var transport = manager.ConnectAsync( _endPoint ).Result )
			{
				bool isEventOccurred = false;
				manager.UnknownResponseReceived +=
					( sender, e ) =>
					{
						isEventOccurred = true;

						if ( error == null )
						{
							Assert.That( e.Error.IsSuccess );
							Assert.That( e.ReturnValue == result, "{0} != {1}", e.ReturnValue, result );
						}
						else
						{
							Assert.That( e.Error.Error, Is.EqualTo( error ) );
						}
					};
				using ( var buffer = new MemoryStream() )
				using ( var packer = Packer.Create( buffer, false ) )
				{
					packer.PackArrayHeader( 4 );
					packer.Pack( ( int )MessageType.Response );
					packer.Pack( 1 );
					packer.Pack( error == null ? MessagePackObject.Nil : error.Identifier );
					packer.Pack( Guid.NewGuid().ToString() );

					server.TransportManager.SendResponseData( buffer.ToArray() );
				}

				Assert.That( isEventOccurred );
			}
		}

		[Test]
		public void TestReceive_UnknownMessageIdNotError_Orphan()
		{
			TestReceive_UnknownMessageIdCore( null );
		}

		[Test]
		public void TestReceive_UnknownMessageIdError_Orphan()
		{
			TestReceive_UnknownMessageIdCore( RpcError.CustomError( "AppError.Dummy", 1 ) );
		}

		#endregion

		#endregion

		#region ---- Timeout ----

		[Test]
		public void TestReceived_ExecutionTimeout()
		{
			Assert.Inconclusive( "Timeout is not implemented yet." );
		}

		#endregion

		#region ---- Interruption ----

		private void TestReceive_InterruptCore(
			Func<byte, RpcError, MessagePackObject, IEnumerable<byte[]>> splitter
		)
		{
			var error = new RpcErrorMessage( RpcError.CustomError( "AppError.Dummy", 1 ), Guid.NewGuid().ToString() );
			TestReceiveCore(
				MessageType.Request,
				error.Error,
				error.Detail,
				e =>
				{
					e.ChunkedReceivedData = splitter( ( byte )( Math.Abs( Environment.TickCount ) % 100 ), error.Error, error.Detail );
				},
				false
			);
		}

		private static byte[] ToBytes( object value )
		{
			using ( var buffer = new MemoryStream() )
			using ( var packer = Packer.Create( buffer, false ) )
			{
				packer.Pack( value );
				return buffer.ToArray();
			}
		}

		#region ------ Entire Array ------

		[Test]
		public void TestReceive_InterupptOnArrayAndResume_Ok()
		{
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
					new byte[][]
					{
						new byte[]{ 0xDC },
						new byte[]{ 0x0, 0x4, ( byte )MessageType.Response, messageId }.Concat( ToBytes( error ) ).Concat( ToBytes( returnValue ) ).ToArray()
					}
			);
		}

		[Test]
		public void TestReceive_InterupptOnArrayAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
					new byte[][]
					{
						new byte[]{ 0xDC },
						new byte[]{ 0x0, 0x4, ( byte )MessageType.Response, messageId }.Concat( ToBytes( error ) ).Concat( ToBytes( returnValue ) ).ToArray()
					}
			);
		}

		[Test]
		public void TestReceive_InterupptAfterArrayAndResume_Ok()
		{
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
					new byte[][]
					{
						new byte[]{ 0x94 },
						new byte[]{ ( byte )MessageType.Response, messageId }.Concat( ToBytes( error ) ).Concat( ToBytes( returnValue ) ).ToArray()
					}
			);
		}

		[Test]
		public void TestReceive_InterupptAfterArrayAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
					new byte[][]
					{
						new byte[]{ 0x94 },
						new byte[]{ ( byte )MessageType.Response, messageId }.Concat( ToBytes( error ) ).Concat( ToBytes( returnValue ) ).ToArray()
					}
			);
		}

		#endregion

		#region ------ Message Type ------

		[Test]
		public void TestReceive_InterupptOnMessageTypeAndResume_Ok()
		{
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
					new byte[][]
					{
						new byte[]{ 0x94, 0xD0 },
						new byte[]{ ( byte )MessageType.Response, messageId }.Concat( ToBytes( error ) ).Concat( ToBytes( returnValue ) ).ToArray()
					}
			);
		}

		[Test]
		public void TestReceive_InterupptOnMessageTypeAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
					new byte[][]
					{
						new byte[]{ 0x94, 0xD0 },
						new byte[]{ ( byte )MessageType.Response, messageId }.Concat( ToBytes( error ) ).Concat( ToBytes( returnValue ) ).ToArray()
					}
			);
		}

		[Test]
		public void TestReceive_InterupptAfterMessageTypeAndResume_Ok()
		{
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
					new byte[][]
					{
						new byte[]{ 0x94, ( byte )MessageType.Response },
						new byte[]{ messageId }.Concat( ToBytes( error ) ).Concat( ToBytes( returnValue ) ).ToArray()
					}
			);
		}

		[Test]
		public void TestReceive_InterupptAfterMessageTypeAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
					new byte[][]
					{
						new byte[]{ 0x94, ( byte )MessageType.Response },
						new byte[]{ messageId }.Concat( ToBytes( error ) ).Concat( ToBytes( returnValue ) ).ToArray()
					}
			);
		}

		#endregion

		#region ------ Message ID ------

		[Test]
		public void TestReceive_InterupptOnMessageIdAndResume_Ok()
		{
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
					new byte[][]
					{
						new byte[]{ 0x94, ( byte )MessageType.Response, 0xD0 },
						new byte[]{ messageId }.Concat( ToBytes( error ) ).Concat( ToBytes( returnValue ) ).ToArray()
					}
			);
		}

		[Test]
		public void TestReceive_InterupptOnMessageIdAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
					new byte[][]
					{
						new byte[]{ 0x94, ( byte )MessageType.Response, 0xD0 },
						new byte[]{ messageId }.Concat( ToBytes( error ) ).Concat( ToBytes( returnValue ) ).ToArray()
					}
			);
		}

		[Test]
		public void TestReceive_InterupptAfterMessageIdAndResume_Ok()
		{
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
					new byte[][]
					{
						new byte[]{ 0x94, ( byte )MessageType.Response, messageId },
						ToBytes( error ).Concat( ToBytes( returnValue ) ).ToArray()
					}
			);
		}

		[Test]
		public void TestReceive_InterupptAfterMessageIdAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
					new byte[][]
					{
						new byte[]{ 0x94, ( byte )MessageType.Response, messageId },
						ToBytes( error ).Concat( ToBytes( returnValue ) ).ToArray()
					}
			);
		}

		#endregion

		#region ------ Error ------

		[Test]
		public void TestReceive_InterupptOnErrorAndResume_Ok()
		{
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
				{
					var errorBytes = ToBytes( error );
					return
						new byte[][]
						{
							new byte[]{ 0x94, ( byte )MessageType.Response, messageId, errorBytes[ 0 ] },
							errorBytes.Skip( 1 ).Concat( ToBytes( returnValue ) ).ToArray()
						};
				}
			);
		}

		[Test]
		public void TestReceive_InterupptOnErrorAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
				{
					var errorBytes = ToBytes( error );
					return
						new byte[][]
						{
							new byte[]{ 0x94, ( byte )MessageType.Response, messageId, errorBytes[ 0 ] },
							errorBytes.Skip( 1 ).Concat( ToBytes( returnValue ) ).ToArray()
						};
				}
			);
		}

		[Test]
		public void TestReceive_InterupptAfterErrorAndResume_Ok()
		{
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
					new byte[][]
					{
						new byte[]{ 0x94, ( byte )MessageType.Response, messageId }.Concat( ToBytes( error ) ).ToArray(),
						ToBytes( returnValue )
					}
			);
		}

		[Test]
		public void TestReceive_InterupptAfterErrorAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
					new byte[][]
					{
						new byte[]{ 0x94, ( byte )MessageType.Response, messageId }.Concat( ToBytes( error ) ).ToArray(),
						ToBytes( returnValue )
					}
			);
		}

		#endregion

		#region ------ Return Value ------

		[Test]
		public void TestReceive_Request_InterupptOnArgumentsBodyAndResume_Ok()
		{
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
				{
					var returnValueBytes = ToBytes( returnValue );
					return
						new byte[][]
						{
							new byte[]{ 0x94, ( byte )MessageType.Response, messageId }.Concat( ToBytes( error ) ).Concat( returnValueBytes.Take( 1 ) ).ToArray(),
							returnValueBytes.Skip( 1 ).ToArray()
						};
				}
			);
		}

		[Test]
		public void TestReceive_Request_InterupptOnArgumentsBodyAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
				{
					var returnValueBytes = ToBytes( returnValue );
					return
						new byte[][]
						{
							new byte[]{ 0x94, ( byte )MessageType.Response, messageId }.Concat( ToBytes( error ) ).Concat( returnValueBytes.Take( 1 ) ).ToArray(),
							returnValueBytes.Skip( 1 ).ToArray()
						};
				}
			);
		}

		#endregion

		#endregion

		#endregion

		private sealed class ChunkedReceivedDataEnumerator : IEnumerable<byte[]>
		{
			private readonly byte[] _former;
			private readonly Action _callback;
			private readonly byte[] _latter;

			public ChunkedReceivedDataEnumerator( byte[] data, Action callback )
			{
				this._former = data.Take( data.Length / 2 ).ToArray();
				this._callback = callback;
				this._latter = data.Skip( data.Length / 2 ).ToArray();
			}

			public IEnumerator<byte[]> GetEnumerator()
			{
				yield return this._former;
				this._callback();
				yield return this._latter;
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}
		}
	}
}
