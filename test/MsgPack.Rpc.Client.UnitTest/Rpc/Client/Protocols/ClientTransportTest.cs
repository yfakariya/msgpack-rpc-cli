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
using System.Threading.Tasks;
using MsgPack.Rpc.Protocols;
using MsgPack.Rpc.Protocols.Filters;
using MsgPack.Rpc.Server;
using MsgPack.Rpc.Server.Protocols;
using NUnit.Framework;

namespace MsgPack.Rpc.Client.Protocols
{
	[TestFixture]
	[Timeout( 3000 )]
	public class ClientTransportTest
	{
		private static readonly EndPoint _endPoint =
			new IPEndPoint( IPAddress.Loopback, 0 );

		// Tweak point
		private static readonly TimeSpan _testTimeout = TimeSpan.FromSeconds( 5 );
		private static readonly TimeSpan _waitTimeout = TimeSpan.FromMilliseconds( 500 );

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

		private void TestCore( Action<ClientTransport, Server.Protocols.InProcServerTransportManager> test )
		{
			TestCore( ( target, manager ) => test( target, manager ), ( _0, _1 ) => MessagePackObject.Nil );
		}

		private void TestCore( Action<ClientTransport, Server.Protocols.InProcServerTransportManager> test, Func<int?, MessagePackObject[], MessagePackObject> callback )
		{
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

		private void TestShutdownCore(
			bool isClientShutdown,
			bool isServerReceiving,
			bool isEmpty
		)
		{
			var arg = Environment.TickCount % 3;
			var returnValue = Environment.TickCount % 5;
			Tuple<ClientTransport, Server.Protocols.InProcServerTransportManager> testEnvironment = null;
			TestCore(
				( target, serverTransportManager ) =>
				{
					testEnvironment = Tuple.Create( target, serverTransportManager );

					using ( var serverShutdownWaitHandle = new ManualResetEventSlim() )
					using ( var serverReceivedWaitHandle = new ManualResetEventSlim() )
					using ( var clientShutdownWaitHandle = new ManualResetEventSlim() )
					using ( var responseReceivedWaitHandle = new ManualResetEventSlim() )
					{
						var context = target.GetClientRequestContext();
						try
						{
							if ( isServerReceiving )
							{
								serverTransportManager.TransportReceived += ( sender, e ) => serverReceivedWaitHandle.Set();
							}

							serverTransportManager.TransportShutdownCompleted += ( sender, e ) => serverShutdownWaitHandle.Set();
							target.ShutdownCompleted += ( sender, e ) => clientShutdownWaitHandle.Set();

							int messageId = Math.Abs( Environment.TickCount % 10 );
							int? resultMessageId = null;
							MessagePackObject? resultReturn = null;
							RpcErrorMessage resultError = RpcErrorMessage.Success;
							Exception sendingError = null;
							Task sendingMayFail = null;

							if ( !isEmpty )
							{
								context.SetRequest(
									messageId,
									"Test",
									( responseContext, error, completedSynchronously ) =>
									{
										if ( error == null )
										{
											resultMessageId = responseContext.MessageId;
											resultReturn = Unpacking.UnpackObject( responseContext.ResultBuffer );
											resultError = ErrorInterpreter.UnpackError( responseContext );
										}
										else
										{
											sendingError = error;
										}

										responseReceivedWaitHandle.Set();
									}
								);

								context.ArgumentsPacker.PackArrayHeader( 0 );

								if ( isServerReceiving )
								{
									( ( InProcClientTransport )target ).DataSending +=
										( sender, e ) =>
										{
											e.Data = e.Data.Take( e.Data.Length / 2 ).ToArray();
										};
								}

								if ( isServerReceiving )
								{
									( ( InProcClientTransport )target ).MessageSent +=
										( sender, e ) => target.BeginShutdown();
									sendingMayFail =
										Task.Factory.StartNew(
											() => target.Send( context )
										);
								}
								else
								{
									target.Send( context );
								}

								// Else, shutdown will be initiated in callback.
							}
							else
							{
								// Initiate shutdown now.

								if ( isClientShutdown )
								{
									target.BeginShutdown();
								}
								else
								{
									serverTransportManager.BeginShutdown();
								}
							}

							if ( !isEmpty )
							{
								if ( isServerReceiving )
								{
									Assert.That( serverReceivedWaitHandle.Wait( _testTimeout ) );
								}

								Assert.That( responseReceivedWaitHandle.Wait( _testTimeout ) );
							}
							else
							{
								if ( !isClientShutdown )
								{
									// Client will never detect server shutdown when there are no sessions.
								}
								else
								{
									Assert.That( clientShutdownWaitHandle.Wait( _testTimeout ) );
								}
							}

							Assert.That( serverShutdownWaitHandle.Wait( _testTimeout ) );

							if ( !isEmpty )
							{
								if ( isServerReceiving )
								{
									Assert.That( sendingError, Is.Not.Null );
								}
								else
								{
									Assert.That( resultMessageId == messageId );
									Assert.That( resultError.IsSuccess );
									Assert.That( resultReturn == returnValue );
								}
							}
						}
						finally
						{
							if ( context.BoundTransport != null )
							{
								// Return only if the shutdown was not occurred.
								target.ReturnContext( context );
							}
						}
					}
				},
				( messageId, args ) =>
				{
					if ( !isServerReceiving )
					{
						// Initiate shutdown.
						if ( isClientShutdown )
						{
							testEnvironment.Item1.BeginShutdown();
						}
						else
						{
							testEnvironment.Item2.BeginShutdown();
						}
					}

					return returnValue;
				}
			);
		}

		#region -- BeginShutdown --

		private void TestBeginShutdownCore( bool isServerReceiving, bool isEmpty )
		{
			TestShutdownCore( isClientShutdown: true, isServerReceiving: isServerReceiving, isEmpty: isEmpty );
		}

		[Test()]
		public void TestBeginShutdown_NoPendingRequest_Harmless()
		{
			TestBeginShutdownCore( isServerReceiving: false, isEmpty: true );
		}

		[Test]
		public void TestBeginShutdown_DuringReceiving_PendingRequestsAreCanceled_ShutdownPacketReplyed_ServerShutdownCompleted()
		{
			TestBeginShutdownCore( isServerReceiving: true, isEmpty: false );
		}

		[Test]
		public void TestBeginShutdown_DuringExecuting_PendingRequestsAreSent_ShutdownPacketReplyed_ServerShutdownCompleted()
		{
			TestBeginShutdownCore( isServerReceiving: false, isEmpty: false );
		}

		#endregion

		#region -- Server shutdown --

		private void TestServerShutdownCore( bool isServerReceiving, bool isEmpty )
		{
			TestShutdownCore( isClientShutdown: false, isServerReceiving: isServerReceiving, isEmpty: isEmpty );
		}

		[Test]
		public void TestServerShutdown_NoPendingRequest_Harmless()
		{
			TestServerShutdownCore( isServerReceiving: false, isEmpty: true );
		}

		[Test]
		public void TestServerShutdown_DuringSending_PendingRequestsAreCanceled_ShutdownPacketReplyed_ServerShutdownCompleted()
		{
			TestServerShutdownCore( isServerReceiving: true, isEmpty: false );
		}

		[Test]
		public void TestServerShutdown_DuringExecuting_PendingRequestsAreSent_ShutdownPacketReplyed_ServerShutdownCompleted()
		{
			TestServerShutdownCore( isServerReceiving: false, isEmpty: false );
		}

		#endregion

		#region -- Send --

		private static void TestSendCore(
			MessageType messageType,
			SocketError? socketErrorOnSend,
			TimeSpan? waitTimeout
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

			var configuration = new RpcClientConfiguration();
			configuration.WaitTimeout = waitTimeout;

			using ( var server = new EchoServer( callback ) )
			using ( var manager = new InProcClientTransportManager( configuration, server.TransportManager ) )
			using ( var transport = manager.ConnectAsync( _endPoint ).Result as InProcClientTransport )
			using ( var waitHandle = new ManualResetEventSlim() )
			{
				if ( waitTimeout != null )
				{
					transport.DataSending +=
						( sender, e ) => Thread.Sleep( TimeSpan.FromMilliseconds( waitTimeout.Value.TotalMilliseconds * 3 ) );
				}

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
				RpcErrorMessage responseError = RpcErrorMessage.Success;
				Exception clientRuntimeError = null;

				if ( messageType == MessageType.Request )
				{
					context.SetRequest(
						messageId.Value,
						"Test",
						( responseContext, error, completedSynchronously ) =>
						{
							result = responseContext == null ? default( MessagePackObject? ) : Unpacking.UnpackObject( responseContext.ResultBuffer );

							if ( responseContext != null )
							{
								responseError = ErrorInterpreter.UnpackError( responseContext );
							}

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
					Assert.That( waitHandle.Wait( _testTimeout ), "timeout" );
				}

				if ( socketErrorOnSend != null )
				{
					Assert.That( clientRuntimeError, Is.Not.Null.And.AssignableTo( typeof( RpcException ) ) );
					return;
				}

				Assert.That( clientRuntimeError, Is.Null, "Unexpected runtime error:{0}", clientRuntimeError );

				if ( messageType == MessageType.Request )
				{
					if ( waitTimeout != null )
					{
						Assert.That( responseError.Error, Is.EqualTo( RpcError.TimeoutError ) );
					}
					else
					{
						Assert.That( result, Is.Not.Null );
						Assert.That( result.Value.AsList().Count, Is.EqualTo( 1 ) );
						Assert.That( result.Value.AsList()[ 0 ] == argument, "{0} != {1}", result.Value.AsList()[ 0 ], argument );
					}
				}
			}
		}

		[Test]
		public void TestSend_RequestSuccess_CallbackInvokedAndSendAsRequest()
		{
			TestSendCore( MessageType.Request, null, null );
		}

		[Test]
		public void TestSend_RequestFail_CallbackInvokedWithTransportError()
		{
			TestSendCore( MessageType.Request, SocketError.ConnectionReset, null );
		}

		[Test]
		public void TestSend_NotifySuccess_CallbackInvokedAndSendAsNotification()
		{
			TestSendCore( MessageType.Notification, null, null );
		}

		[Test]
		public void TestSend_NotifyFail_CallbackInvokedWithTransportError()
		{
			TestSendCore( MessageType.Notification, SocketError.ConnectionReset, null );
		}

		[Test]
		public void TestSend_RequestTimeout_TimeoutError()
		{
			TestSendCore( MessageType.Request, SocketError.OperationAborted, TimeSpan.FromMilliseconds( 20 ) );
		}

		[Test]
		public void TestSend_NotificationTimeout_TimeoutError()
		{
			TestSendCore( MessageType.Request, SocketError.OperationAborted, TimeSpan.FromMilliseconds( 20 ) );
		}

		#endregion

		#region -- Receive --

		#region ---- Common Private Method ----

		private void TestReceiveCore(
			MessageType messageType,
			RpcError expectedError,
			MessagePackObject? expectedResult,
			Action<InProcResponseReceivedEventArgs> receivedBinaryModifier,
			bool willBeUnknown,
			TimeSpan? waitTimeout
		)
		{
			int? messageId = messageType == MessageType.Request ? Math.Abs( Environment.TickCount % 100 ) : default( int? );
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

			var configuration = new RpcClientConfiguration();
			configuration.WaitTimeout = waitTimeout;

			using ( var server = new EchoServer( callback ) )
			using ( var manager = new InProcClientTransportManager( configuration, server.TransportManager ) )
			using ( var transport = manager.ConnectAsync( _endPoint ).Result as InProcClientTransport )
			using ( var waitHandle = new ManualResetEventSlim() )
			{
				if ( receivedBinaryModifier != null )
				{
					transport.ResponseReceived += ( sender, e ) => receivedBinaryModifier( e );
				}

				var context = transport.GetClientRequestContext();
				int? resultMessageId = null;
				MessagePackObject? resultReturn = null;
				RpcErrorMessage resultError = RpcErrorMessage.Success;
				Exception clientRuntimeError = null;
				bool wasUnknown = false;

				if ( messageType == MessageType.Request )
				{
					context.SetRequest(
						messageId.Value,
						"Test",
						( responseContext, error, completedSynchronously ) =>
						{
							if ( responseContext != null )
							{
								resultMessageId = responseContext.MessageId;
								resultReturn = Unpacking.UnpackObject( responseContext.ResultBuffer );
								resultError = ErrorInterpreter.UnpackError( responseContext );
							}

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

				manager.UnknownResponseReceived +=
					( sender, e ) =>
					{
						resultMessageId = e.MessageId;
						resultReturn = e.ReturnValue;
						resultError = e.Error;
						wasUnknown = true;
						waitHandle.Set();
					};

				context.ArgumentsPacker.PackArrayHeader( 1 );
				context.ArgumentsPacker.Pack( argument );

				transport.Send( context );

				if ( Debugger.IsAttached )
				{
					waitHandle.Wait();
				}
				else
				{
					Assert.That( waitHandle.Wait( _testTimeout ), "timeout" );
				}

				if ( waitTimeout != null )
				{
					if ( willBeUnknown )
					{
						Assert.That( wasUnknown, "Received known handler." );
					}
					else
					{
						Assert.That( clientRuntimeError, Is.InstanceOf<RpcTimeoutException>() );
						Assert.That( ( clientRuntimeError as RpcTimeoutException ).RpcError, Is.EqualTo( RpcError.TimeoutError ) );
						Assert.That( ( clientRuntimeError as RpcTimeoutException ).ClientTimeout, Is.EqualTo( waitTimeout ) );
					}
				}
				else
				{
					Assert.That( clientRuntimeError, Is.Null, "Unexpected runtime error:{0}", clientRuntimeError );

					if ( willBeUnknown )
					{
						Assert.That( wasUnknown, "Received known handler." );
					}
					else if ( messageType == MessageType.Request )
					{
						Assert.That( resultMessageId, Is.EqualTo( messageId ) );
						if ( expectedError == null )
						{
							Assert.That( resultReturn == expectedResult, "{0} != {1}", resultReturn, expectedResult );
						}
						else
						{
							Assert.That( resultError.Error.Identifier, Is.EqualTo( expectedError.Identifier ), resultError.ToString() );
						}
					}
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
				receivedBinaryModifier: null,
				willBeUnknown: false,
				waitTimeout: null
			);
		}

		[Test]
		public void TestReceive_Request_Fail_ResponseReturns()
		{
			TestReceiveCore(
				MessageType.Request,
				RpcError.CustomError( "AppError.DummyError", 1 ),
				"DoNotReturnThis",
				receivedBinaryModifier: null,
				willBeUnknown: false,
				waitTimeout: null
			);
		}

		[Test]
		public void TestReceive_Notify_Success_ResponseNotReturns()
		{
			TestReceiveCore(
				MessageType.Notification,
				null,
				"DoNotReturnThis",
				receivedBinaryModifier: null,
				willBeUnknown: false,
				waitTimeout: null
			);
		}

		[Test]
		public void TestReceive_Notify_Fail_ResponseNotReturns()
		{
			TestReceiveCore(
				MessageType.Notification,
				RpcError.CustomError( "AppError.DummyError", 1 ),
				"DoNotReturnThis",
				receivedBinaryModifier: null,
				willBeUnknown: false,
				waitTimeout: null
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
				expectedResult: null,
				receivedBinaryModifier: e =>
				{
					var originalResult = Unpacking.UnpackArray( e.ReceivedData ).Value;

					using ( var buffer = new MemoryStream() )
					using ( var packer = Packer.Create( buffer, false ) )
					{
						invalidRequestPacking( packer, originalResult[ 1 ].AsInt32(), originalResult[ 2 ].IsNil ? null : RpcError.FromIdentifier( originalResult[ 2 ].AsString(), null ), originalResult[ 3 ] );
						e.ChunkedReceivedData = new byte[][] { buffer.ToArray() };
					}
				},
				willBeUnknown: willBeConnectionReset,
				waitTimeout: null
			);
		}

		#region ------ Entire Array ------

		[Test]
		public void TestReceive_RequestNotArray_Orphan()
		{
			TestReceiveInvalidRequestCore(
				( packer, originalMessageId, originalError, originalReturnValue ) => packer.PackString( "Request" ),
				RpcError.RemoteRuntimeError,
				true
			);
		}

		[Test]
		public void TestReceive_EmptyArray_Orphan()
		{
			TestReceiveInvalidRequestCore(
				( packer, originalMessageId, originalError, originalReturnValue ) => packer.PackArrayHeader( 0 ),
				RpcError.RemoteRuntimeError,
				true
			);
		}

		[Test]
		public void TestReceive_ArrayLengthIs3_Orphan()
		{
			TestReceiveInvalidRequestCore(
				( packer, originalMessageId, originalError, originalReturnValue ) =>
				{
					packer.PackArrayHeader( 3 );
					packer.Pack( ( int )MessageType.Response );
					packer.Pack( originalMessageId );
					packer.Pack( originalError == null ? null : originalError.Identifier );
				},
				RpcError.RemoteRuntimeError,
				true
			);
		}

		[Test]
		public void TestReceive_ArrayLengthIs5_Orphan()
		{
			TestReceiveInvalidRequestCore(
				( packer, originalMessageId, originalError, originalReturnValue ) =>
				{
					packer.PackArrayHeader( 5 );
					packer.Pack( ( int )MessageType.Response );
					packer.Pack( originalMessageId );
					packer.Pack( "Test" );
					packer.Pack( originalError == null ? null : originalError.Identifier );
					packer.Pack( originalReturnValue );
				},
				RpcError.RemoteRuntimeError,
				true
			);
		}

		#endregion

		#region ------ Message Type ------

		[Test]
		public void TestReceive_RequestMessageType_Orphan()
		{
			TestReceiveInvalidRequestCore(
				( packer, originalMessageId, originalError, originalReturnValue ) =>
				{
					packer.PackArrayHeader( 4 );
					packer.Pack( ( int )MessageType.Request );
					packer.Pack( originalMessageId );
					packer.Pack( originalError == null ? null : originalError.Identifier );
					packer.Pack( originalReturnValue );
				},
				RpcError.RemoteRuntimeError,
				true
			);
		}

		[Test]
		public void TestReceive_UnknownMessageType_Orphan()
		{
			TestReceiveInvalidRequestCore(
				( packer, originalMessageId, originalError, originalReturnValue ) =>
				{
					packer.PackArrayHeader( 4 );
					packer.Pack( 3 );
					packer.Pack( originalMessageId );
					packer.Pack( originalError == null ? null : originalError.Identifier );
					packer.Pack( originalReturnValue );
				},
				RpcError.RemoteRuntimeError,
				true
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
			using ( var transport = manager.ConnectAsync( _endPoint ).Result as InProcClientTransport )
			using ( var waitHandle = new ManualResetEventSlim() )
			{
				manager.UnknownResponseReceived +=
					( sender, e ) =>
					{
						if ( error == null )
						{
							Assert.That( e.Error.IsSuccess );
							Assert.That( e.ReturnValue == result, "{0}({1}) != {2}({3})", e.ReturnValue, e.ReturnValue.Value.UnderlyingType, result, result.UnderlyingType );
						}
						else
						{
							Assert.That( e.Error.Error.Identifier, Is.EqualTo( error.Identifier ) );
						}

						waitHandle.Set();
					};

				transport.ResponseReceived +=
					( sender, e ) =>
					{
						var originalResult = Unpacking.UnpackArray( e.ReceivedData ).Value;

						using ( var buffer = new MemoryStream() )
						using ( var packer = Packer.Create( buffer, false ) )
						{
							packer.PackArrayHeader( 4 );
							packer.Pack( ( int )MessageType.Response );
							packer.Pack( 2 );
							packer.Pack( error == null ? MessagePackObject.Nil : error.Identifier );
							packer.Pack( originalResult[ 3 ].AsList()[ 0 ] ); // EchoServer returns args as MPO[], so pick up first element.
							e.ChunkedReceivedData = new byte[][] { buffer.ToArray() };
						}
					};

				var context = transport.GetClientRequestContext();
				context.SetRequest( 1, "Test", ( responseContext, exception, completedSynchronously ) => { } );
				context.ArgumentsPacker.PackArrayHeader( 1 );
				context.ArgumentsPacker.Pack( result );
				transport.Send( context );
				Assert.That( waitHandle.Wait( _testTimeout ) );
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

		#region ---- Interruption ----

		private void TestReceive_InterruptCore(
			Func<byte, RpcError, MessagePackObject, IEnumerable<byte[]>> splitter,
			bool mayOrphan,
			TimeSpan? waitTimeout
		)
		{
			var error = new RpcErrorMessage( RpcError.CustomError( "AppError.Dummy", 1 ), Guid.NewGuid().ToString() );
			TestReceiveCore(
				MessageType.Request,
				error.Error,
				error.Detail,
				e =>
				{
					var response = Unpacking.UnpackObject( e.ReceivedData );
					e.ChunkedReceivedData = splitter( response.Value.AsList()[ 1 ].AsByte(), RpcError.FromIdentifier( response.Value.AsList()[ 2 ].AsString(), null ), response.Value.AsList()[ 3 ] );
				},
				mayOrphan,
				waitTimeout
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
					new byte[]{ 0x0, 0x4, ( byte )MessageType.Response, messageId }.Concat( ToBytes( error.Identifier ) ).Concat( ToBytes( returnValue ) ).ToArray()
				},
				mayOrphan: false,
				waitTimeout: null
			);
		}

		[Test]
		public void TestReceive_InterupptOnArrayAndNotResume_Timeout()
		{
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
				new byte[][]
				{
					new byte[]{ 0xDC },
				},
				true,
				_waitTimeout
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
					new byte[]{ ( byte )MessageType.Response, messageId }.Concat( ToBytes( error.Identifier ) ).Concat( ToBytes( returnValue ) ).ToArray()
				},
				mayOrphan: false,
				waitTimeout: null
			);
		}

		[Test]
		public void TestReceive_InterupptAfterArrayAndNotResume_Timeout()
		{
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
				new byte[][]
				{
					new byte[]{ 0x94 },
				},
				true,
				_waitTimeout
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
					new byte[]{ ( byte )MessageType.Response, messageId }.Concat( ToBytes( error.Identifier ) ).Concat( ToBytes( returnValue ) ).ToArray()
				},
				mayOrphan: false,
				waitTimeout: null
			);
		}

		[Test]
		public void TestReceive_InterupptOnMessageTypeAndNotResume_Timeout()
		{
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
				new byte[][]
				{
					new byte[]{ 0x94, 0xD0 },
				},
				true,
				_waitTimeout
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
					new byte[]{ messageId }.Concat( ToBytes( error.Identifier ) ).Concat( ToBytes( returnValue ) ).ToArray()
				},
				mayOrphan: false,
				waitTimeout: null
			);
		}

		[Test]
		public void TestReceive_InterupptAfterMessageTypeAndNotResume_Timeout()
		{
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
				new byte[][]
				{
					new byte[]{ 0x94, ( byte )MessageType.Response },
				},
				true,
				_waitTimeout
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
					new byte[]{ messageId }.Concat( ToBytes( error.Identifier ) ).Concat( ToBytes( returnValue ) ).ToArray()
				},
				mayOrphan: false,
				waitTimeout: null
			);
		}

		[Test]
		public void TestReceive_InterupptOnMessageIdAndNotResume_Timeout()
		{
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
				new byte[][]
				{
					new byte[]{ 0x94, ( byte )MessageType.Response, 0xD0 },
				},
				true,
				_waitTimeout
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
					ToBytes( error.Identifier ).Concat( ToBytes( returnValue ) ).ToArray()
				},
				mayOrphan: false,
				waitTimeout: null
			);
		}

		[Test]
		public void TestReceive_InterupptAfterMessageIdAndNotResume_Timeout()
		{
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
				new byte[][]
				{
					new byte[]{ 0x94, ( byte )MessageType.Response, messageId },
				},
				false,
				_waitTimeout
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
					var errorBytes = ToBytes( error.Identifier );
					return
						new byte[][]
						{
							new byte[]{ 0x94, ( byte )MessageType.Response, messageId, errorBytes[ 0 ] },
							errorBytes.Skip( 1 ).Concat( ToBytes( returnValue ) ).ToArray()
						};
				},
				mayOrphan: false,
				waitTimeout: null
			);
		}

		[Test]
		public void TestReceive_InterupptOnErrorAndNotResume_Timeout()
		{
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
				{
					var errorBytes = ToBytes( error.Identifier );
					return
						new byte[][]
						{
							new byte[]{ 0x94, ( byte )MessageType.Response, messageId, errorBytes[ 0 ] },
						};
				},
				false,
				_waitTimeout
			);
		}

		[Test]
		public void TestReceive_InterupptAfterErrorAndResume_Ok()
		{
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
				new byte[][]
				{
					new byte[]{ 0x94, ( byte )MessageType.Response, messageId }.Concat( ToBytes( error.Identifier ) ).ToArray(),
					ToBytes( returnValue )
				},
				mayOrphan: false,
				waitTimeout: null
			);
		}

		[Test]
		public void TestReceive_InterupptAfterErrorAndNotResume_Timeout()
		{
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
				new byte[][]
				{
					new byte[]{ 0x94, ( byte )MessageType.Response, messageId }.Concat( ToBytes( error.Identifier ) ).ToArray(),
				},
				false,
				_waitTimeout
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
							new byte[]{ 0x94, ( byte )MessageType.Response, messageId }.Concat( ToBytes( error.Identifier ) ).Concat( returnValueBytes.Take( 1 ) ).ToArray(),
							returnValueBytes.Skip( 1 ).ToArray()
						};
				},
				mayOrphan: false,
				waitTimeout: null
			);
		}

		[Test]
		public void TestReceive_Request_InterupptOnArgumentsBodyAndNotResume_Timeout()
		{
			TestReceive_InterruptCore(
				( messageId, error, returnValue ) =>
				{
					var returnValueBytes = ToBytes( returnValue );
					return
						new byte[][]
						{
							new byte[]{ 0x94, ( byte )MessageType.Response, messageId }.Concat( ToBytes( error.Identifier ) ).Concat( returnValueBytes.Take( 1 ) ).ToArray(),
						};
				},
				false,
				_waitTimeout
			);
		}

		#endregion

		#endregion

		#endregion


		#region -- Filter --

		[Test]
		public void TestFilters_Initialization_AppliedDeserializationsAreInOrder_AppliedSerializationsAreReverseOrder()
		{
			TestFiltersCore(
				null,
				target =>
				{
					CheckFilters( target.AfterSerializationFilters, MessageFilteringLocation.AfterSerialization, 0, 1 );
					CheckFilters( target.BeforeDeserializationFilters, MessageFilteringLocation.BeforeDeserialization, 3, 2 );
				},
				null,
				new ClientRequestTestMessageFilterProvider( 0 ),
				new ClientRequestTestMessageFilterProvider( 1 ),
				new ClientResponseTestMessageFilterProvider( 2 ),
				new ClientResponseTestMessageFilterProvider( 3 )
			);
		}

		private static void CheckFilters<T>( IList<MessageFilter<T>> target, MessageFilteringLocation location, params int[] indexes )
			where T : MessageContext
		{
			Assert.That( target.Count, Is.EqualTo( indexes.Length ) );
			for ( int i = 0; i < indexes.Length; i++ )
			{
				var testFilter = target[ i ] as ITestMessageFilter;
				Assert.That( testFilter, Is.Not.Null, "@{0}:{1}", i, target[ i ].GetType().ToString() );
				Assert.That( testFilter.Index, Is.EqualTo( indexes[ i ] ), "@{0}", i );
				Assert.That( testFilter.Location, Is.EqualTo( location ), "@{0}", i );
			}
		}

		[Test]
		public void TestFilters_RequestResponse_Invoked()
		{
			int beforeDeserializationApplied = 0;
			int afterSerializationApplied = 0;

			var requestFilterProvider = new ClientRequestTestMessageFilterProvider( 0 );
			requestFilterProvider.FilterApplied +=
				( sender, e ) =>
				{
					switch ( e.AppliedLocation )
					{
						case MessageFilteringLocation.AfterSerialization:
						{
							Interlocked.Exchange( ref afterSerializationApplied, 1 );
							break;
						}
					}

					return;
				};

			var responseFilterProvider = new ClientResponseTestMessageFilterProvider( 1 );
			responseFilterProvider.FilterApplied +=
				( sender, e ) =>
				{
					switch ( e.AppliedLocation )
					{
						case MessageFilteringLocation.BeforeDeserialization:
						{
							Interlocked.Exchange( ref beforeDeserializationApplied, 1 );
							break;
						}
					}

					return;
				};

			TestFiltersCore(
				null,
				null,
				( requestData, rawResponseData, fatalError, responseError ) =>
				{
					Assert.That( Interlocked.CompareExchange( ref beforeDeserializationApplied, 0, 0 ), Is.EqualTo( 1 ) );
					Assert.That( Interlocked.CompareExchange( ref afterSerializationApplied, 0, 0 ), Is.EqualTo( 1 ) );
				},
				requestFilterProvider,
				responseFilterProvider
			);
		}

		/// <summary>
		///		Do filter invocation test.
		/// </summary>
		/// <param name="argumentTweak">The logic to tweak arguments. If null, <c>args</c> will be empty. The arguments to the delegate are <see cref="Packer"/> for request stream and existent stream length.</param>
		/// <param name="transportAssertion">The assertion logic for the transport.</param>
		/// <param name="resultAssertion">The assertion logic for the data. The arguments are request data bytes, raw response data bytes, fatal error, and error response.</param>
		/// <param name="providers">The filter providers to be invoked.</param>
		internal static void TestFiltersCore(
			Func<string, int?, MessagePackObject[], MessagePackObject> serverCallback,
			Action<ClientTransport> transportAssertion,
			Action<byte[], byte[], Exception, RpcErrorMessage> resultAssertion,
			params MessageFilterProvider[] providers
		)
		{
			var configuration = new RpcClientConfiguration();

			foreach ( var provider in providers )
			{
				configuration.FilterProviders.Add( provider );
			}

			Func<string, int?, MessagePackObject[], MessagePackObject> echo = ( method, messageId, args ) => args;

			using ( var server = CallbackServer.Create( serverCallback ?? echo, true ) )
			using ( var serverTransportManager = new InProcServerTransportManager( server.Server as RpcServer, m => new SingletonObjectPool<InProcServerTransport>( new InProcServerTransport( m ) ) ) )
			using ( var manager = new InProcClientTransportManager( configuration, serverTransportManager ) )
			using ( var task = manager.ConnectAsync( _endPoint ) )
			using ( var target = task.Result )
			using ( var responseWaitHandle = new ManualResetEventSlim() )
			{
				if ( transportAssertion != null )
				{
					transportAssertion( target );
				}

				var requestContext = target.GetClientRequestContext();
				byte[] requestData = null;
				byte[] responseData = null;
				object responseError = null;
				var singletonServerTransport = serverTransportManager.NewSession();
				singletonServerTransport.Response += ( sender, e ) => Interlocked.Exchange( ref responseData, e.Data );
				requestContext.SetRequest(
					123,
					"Test",
					( responseContext, error, completedSynchronously ) =>
					{
						object boxedError = error ?? ( object )ErrorInterpreter.UnpackError( responseContext );
						Interlocked.Exchange( ref responseError, boxedError );
						responseWaitHandle.Set();
					}
				);

				requestContext.ArgumentsPacker.PackArrayHeader( 0 );

				target.Send( requestContext );

				Assert.That( responseWaitHandle.Wait( _testTimeout ) );

				if ( resultAssertion != null )
				{
					Exception exception = null;
					var errorResponse = RpcErrorMessage.Success;
					if ( responseError is RpcErrorMessage )
					{
						errorResponse = ( RpcErrorMessage )responseError;
					}
					else
					{
						exception = responseError as Exception;
					}

					resultAssertion( requestData, responseData, exception, errorResponse );
				}
			}
		}

		private interface ITestMessageFilter
		{
			int Index { get; }
			MessageFilteringLocation Location { get; }
		}

		private abstract class TestMessageFilter<T> : MessageFilter<T>, ITestMessageFilter
			where T : MessageContext
		{
			private readonly int _index;

			public int Index
			{
				get { return this._index; }
			}

			private readonly MessageFilteringLocation _location;

			public MessageFilteringLocation Location
			{
				get { return this._location; }
			}

			public event EventHandler<FilterAppliedEventArgs> FilterApplied;

			protected void OnFilterApplied( FilterAppliedEventArgs e )
			{
				var handler = this.FilterApplied;
				if ( handler != null )
				{
					handler( this, e );
				}
			}
			protected TestMessageFilter( int index, MessageFilteringLocation location )
			{
				this._index = index;
				this._location = location;
			}

			protected override void ProcessMessageCore( T context )
			{
				this.OnFilterApplied( new FilterAppliedEventArgs( context, this._location ) );
			}
		}

		private sealed class ClientRequestTestMessageFilter : TestMessageFilter<ClientRequestContext>
		{
			public ClientRequestTestMessageFilter( int index, MessageFilteringLocation location ) : base( index, location ) { }
		}

		private sealed class ClientResponseTestMessageFilter : TestMessageFilter<ClientResponseContext>
		{
			public ClientResponseTestMessageFilter( int index, MessageFilteringLocation location ) : base( index, location ) { }
		}

		private sealed class FilterAppliedEventArgs : EventArgs
		{
			private readonly MessageContext _context;

			public MessageContext Context
			{
				get { return this._context; }
			}

			private readonly MessageFilteringLocation _appliedLocation;

			public MessageFilteringLocation AppliedLocation
			{
				get { return this._appliedLocation; }
			}

			public FilterAppliedEventArgs( MessageContext context, MessageFilteringLocation appliedLocation )
			{
				this._context = context;
				this._appliedLocation = appliedLocation;
			}
		}

		private interface ITestMessageFilterProvider
		{
			event EventHandler<FilterAppliedEventArgs> FilterApplied;
		}

		private abstract class TestMessageFilterProvider<T> : MessageFilterProvider<T>, ITestMessageFilterProvider
			where T : MessageContext
		{
			public event EventHandler<FilterAppliedEventArgs> FilterApplied;

			protected void OnFilterApplied( FilterAppliedEventArgs e )
			{
				var handler = this.FilterApplied;
				if ( handler != null )
				{
					handler( this, e );
				}
			}

			protected TestMessageFilterProvider() { }
		}

		private sealed class ClientRequestTestMessageFilterProvider : TestMessageFilterProvider<ClientRequestContext>
		{
			private readonly int _index;

			public ClientRequestTestMessageFilterProvider( int index )
			{
				this._index = index;
			}

			public override MessageFilter<ClientRequestContext> GetFilter( MessageFilteringLocation location )
			{
				var result = new ClientRequestTestMessageFilter( this._index, location );
				result.FilterApplied += ( sender, e ) => this.OnFilterApplied( e );
				return result;
			}
		}

		private sealed class ClientResponseTestMessageFilterProvider : TestMessageFilterProvider<ClientResponseContext>
		{
			private readonly int _index;

			public ClientResponseTestMessageFilterProvider( int index )
			{
				this._index = index;
			}

			public override MessageFilter<ClientResponseContext> GetFilter( MessageFilteringLocation location )
			{
				var result = new ClientResponseTestMessageFilter( this._index, location );
				result.FilterApplied += ( sender, e ) => this.OnFilterApplied( e );
				return result;
			}
		}

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

		private sealed class EchoServer : IDisposable
		{
			private readonly Server.RpcServer _server;
			private readonly Server.Protocols.InProcServerTransportManager _transportManager;

			public Server.Protocols.InProcServerTransportManager TransportManager
			{
				get { return this._transportManager; }
			}

			public EchoServer() : this( ( id, args ) => args ) { }

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

	}
}
