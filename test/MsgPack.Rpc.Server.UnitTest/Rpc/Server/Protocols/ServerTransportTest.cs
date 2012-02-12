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
using System.Diagnostics;
using System.IO;
using System.Threading;
using MsgPack.Rpc.Protocols;
using MsgPack.Rpc.Server.Dispatch;
using NUnit.Framework;

namespace MsgPack.Rpc.Server.Protocols
{
	[TestFixture]
	[Timeout( 3000 )]
	public class ServerTransportTest
	{
		private void TestCore( Action<InProcServerTransport> test )
		{
			TestCore( ( target, _ ) => test( target ), ( _0, _1 ) => MessagePackObject.Nil );
		}

		private void TestCore( Action<InProcServerTransport, InProcServerTransportController> test, Func<int?, MessagePackObject[], MessagePackObject> callback )
		{
			// TODO: Timeout setting
			InProcServerTransportManager serverTransportManager = null;
			var config = new RpcServerConfiguration();
			config.IsDebugMode = true;
			config.TransportManagerProvider =
				s => serverTransportManager = new InProcServerTransportManager( s, m => new SingletonObjectPool<InProcServerTransport>( new InProcServerTransport( m ) ) );
			config.DispatcherProvider =
				s => new CallbackDispatcher( s, callback );

			using ( var server = CallbackServer.Create( config ) )
			using ( var target = serverTransportManager.NewSession() )
			using ( var controller = InProcServerTransportController.Create( serverTransportManager ) )
			{
				test( target, controller );
			}
		}

		[Test]
		public void TestDispose_IsDisposedSet()
		{
			TestCore(
				target =>
				{
					Assert.That( target.IsDisposed, Is.False );

					target.Dispose();
					Assert.That( target.IsDisposed, Is.True );
				}
			);
		}

		#region -- BeginShutdown --

		[Test()]
		public void TestBeginShutdown_NoPendingRequest_Harmless()
		{
			TestCore( target => target.BeginShutdown() );
		}

		private void TestBeginShutdownCore(
			Action<InProcServerTransport> onReceive,
			Action<InProcServerTransport> onExecute,
			Action<InProcServerTransport> onSend,
			bool willBeConnectionReset
		)
		{
			var arg = Environment.TickCount % 3;
			var returnValue = Environment.TickCount % 5;
			bool isExecuted = false;
			InProcServerTransport targetTransport = null;
			TestCore(
				( target, controller ) =>
				{
					targetTransport = target;
					using ( var waitHandle = new ManualResetEventSlim() )
					using ( var receivedWaitHandle = new ManualResetEventSlim() )
					{
						byte[] response = null;

						if ( onReceive != null )
						{
							target.Received += ( sender, e ) => receivedWaitHandle.Set();
						}

						controller.Response +=
							( sender, e ) =>
							{
								if ( response != null )
								{
									return;
								}

								response = e.Data;

								if ( onSend != null )
								{
									onSend( targetTransport );
								}

								waitHandle.Set();
							};

						int messageId = Environment.TickCount % 10;
						using ( var buffer = new MemoryStream() )
						{
							using ( var packer = Packer.Create( buffer, false ) )
							{
								packer.PackArrayHeader( 4 );
								packer.Pack( ( int )MessageType.Request );
								packer.Pack( messageId );
								packer.PackString( "Test" );
							}

							controller.FeedReceiveBuffer( buffer.ToArray() );
							buffer.SetLength( 0 );

							if ( onReceive != null )
							{
								if ( Debugger.IsAttached )
								{
									receivedWaitHandle.Wait();
								}
								else
								{
									Assert.That( receivedWaitHandle.Wait( TimeSpan.FromSeconds( 1 ) ), Is.True, "Not receiving second packets." );
								}

								onReceive( targetTransport );
							}

							using ( var packer = Packer.Create( buffer, false ) )
							{
								packer.PackArrayHeader( 1 );
								packer.Pack( arg );
							}

							controller.FeedReceiveBuffer( buffer.ToArray() );

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
								Assert.That( response, Is.Not.Null.And.Empty );
								return;
							}

							var result = Unpacking.UnpackObject( response ).Value.AsList();

							Assert.That( isExecuted );
							Assert.That( result.Count, Is.EqualTo( 4 ) );
							Assert.That( result[ 0 ] == ( int )MessageType.Response );
							Assert.That( result[ 1 ] == messageId );
							Assert.That( result[ 2 ].IsNil );
							Assert.That( result[ 3 ] == returnValue );
						}
					}
				},
				( messageId, args ) =>
				{
					Assert.That( args[ 0 ] == arg );

					if ( onExecute != null )
					{
						onExecute( targetTransport );
					}

					isExecuted = true;
					return returnValue;
				}
			);
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

		#region -- Client shutdown --


		[Test]
		public void TestClientShutdown()
		{
			TestCore(
				( target, controller ) =>
				{
					using ( var waitHandle = new ManualResetEventSlim() )
					{
						target.Received += ( sender, e ) => waitHandle.Set();
						controller.FeedReceiveBuffer( new byte[ 0 ] );
						waitHandle.Wait();
					}

					Assert.That( target.IsClientShutdown );
				},
				( id, args ) =>
				{
					return args;
				}
			);
		}

		// FIXME: Client shutdown during request
		// FIXME: Client shutdown after request

		#endregion

		#region -- Receive --

		#region ---- Common Private Method ---

		private void TestReceiveCore(
			Action<InProcServerTransportController, MemoryStream, MessageType, int?, string, MessagePackObject[]> sending,
			Func<MessagePackObject, MessagePackObject> onExecute,
			Action<MessagePackObject, MessagePackObject> responseAssertion,
			bool willExecute,
			bool willBeConnectionReset,
			MessageType messageType,
			int? messageId,
			string methodName,
			params MessagePackObject[] arguments
		)
		{
			bool isExecuted = false;
			string returnValue = Guid.NewGuid().ToString();

			using ( var executionWaitHandle = new ManualResetEventSlim() )
			{
				TestCore(
					( target, controller ) =>
					{
						using ( var responseWaitHandle = new ManualResetEventSlim() )
						{
							byte[] response = null;
							controller.Response +=
								( sender, e ) =>
								{
									response = e.Data;
									responseWaitHandle.Set();
								};

							using ( var buffer = new MemoryStream() )
							{
								if ( sending == null )
								{
									using ( var packer = Packer.Create( buffer, false ) )
									{
										if ( messageType == MessageType.Request )
										{
											packer.PackArrayHeader( 4 );
											packer.Pack( ( int )messageType );
											packer.Pack( messageId.Value );
										}
										else
										{
											packer.PackArrayHeader( 3 );
											packer.Pack( ( int )messageType );
										}

										packer.PackString( methodName );
										packer.PackArrayHeader( arguments.Length );
										foreach ( var argument in arguments )
										{
											packer.Pack( argument );
										}
									}

									controller.FeedReceiveBuffer( buffer.ToArray() );
								}
								else
								{
									sending( controller, buffer, messageType, messageId, methodName, arguments );
								}

								if ( willExecute )
								{
									if ( Debugger.IsAttached )
									{
										executionWaitHandle.Wait();
									}
									else
									{
										Assert.That( executionWaitHandle.Wait( TimeSpan.FromSeconds( 1 ) ), Is.True, "Not executed." );
									}
								}

								if ( messageType == MessageType.Request )
								{
									if ( Debugger.IsAttached )
									{
										responseWaitHandle.Wait();
									}
									else
									{
										Assert.That( responseWaitHandle.Wait( TimeSpan.FromSeconds( 1 ) ), Is.True, "Not respond." );
									}
								}

								if ( willBeConnectionReset )
								{
									Assert.That( response, Is.Not.Null.And.Empty );
									return;
								}

								Assert.That( isExecuted, Is.EqualTo( willExecute ) );

								if ( messageType == MessageType.Request )
								{
									var result = Unpacking.UnpackObject( response ).Value.AsList();
									Assert.That( result.Count, Is.EqualTo( 4 ) );
									Assert.That( result[ 0 ] == ( int )MessageType.Response, "{0}", result[ 0 ] );
									Assert.That( result[ 1 ] == messageId, "{0}!={1}", result[ 1 ], messageId );
									if ( responseAssertion == null )
									{
										Assert.That( result[ 2 ].IsNil, "{0}", result[ 2 ] );
										Assert.That( result[ 3 ] == returnValue, "{0}!={1}", result[ 3 ], returnValue );
									}
									else
									{
										responseAssertion( result[ 2 ], result[ 3 ] );
									}
								}
								else
								{
									Assert.That( response, Is.Null );
								}
							}
						}
					},
					( actualMessageId, actualArguments ) =>
					{
						Assert.That( actualMessageId == messageId, "{0}!={1}", actualMessageId, messageId );
						Assert.That( actualArguments, Is.EqualTo( arguments ), "{0}!={1}", actualMessageId, arguments );
						try
						{
							if ( onExecute == null )
							{
								return returnValue;
							}
							else
							{
								return onExecute( returnValue );
							}
						}
						finally
						{
							isExecuted = true;
							executionWaitHandle.Set();
						}
					}
				);
			}
		}

		#endregion

		#region ---- Normal Cases ----

		[Test]
		public void TestReceive_Request_Success_ResponseReturns()
		{
			TestReceiveCore(
				( controller, buffer, sendingType, sendingId, sendingMethod, sendingArguments ) =>
				{
					using ( var packer = Packer.Create( buffer, false ) )
					{
						packer.PackArrayHeader( 4 );
						packer.Pack( ( int )sendingType );
						packer.Pack( sendingId.Value );
						packer.Pack( sendingMethod );
						packer.Pack( sendingArguments );
					}

					controller.FeedReceiveBuffer( buffer.ToArray() );
				},
				null,
				null,
				true,
				false,
				MessageType.Request,
				1,
				"Test",
				"Arg1"
			);
		}

		[Test]
		public void TestReceive_Request_Fail_ResponseReturns()
		{
			TestReceiveCore(
				( controller, buffer, sendingType, sendingId, sendingMethod, sendingArguments ) =>
				{
					using ( var packer = Packer.Create( buffer, false ) )
					{
						packer.PackArrayHeader( 4 );
						packer.Pack( ( int )sendingType );
						packer.Pack( sendingId.Value );
						packer.Pack( sendingMethod );
						packer.Pack( sendingArguments );
					}

					controller.FeedReceiveBuffer( buffer.ToArray() );
				},
				returnValue =>
				{
					throw new RpcException( RpcError.CustomError( "TestId", 1 ), "TestMessage", null );
				},
				( error, returnValue ) =>
				{
					Assert.That( error == "TestId", "\"{0}\"!=\"Test\", {1}", error, returnValue );
					Assert.That( returnValue.AsDictionary()[ "Message" ] == "TestMessage", "Unexpected. {0}{1}", Environment.NewLine, returnValue );
				},
				true,
				false,
				MessageType.Request,
				1,
				"Test",
				"Arg1"
			);
		}

		[Test]
		public void TestReceive_Notify_Success_ResponseNotReturns()
		{
			TestReceiveCore(
				( controller, buffer, sendingType, sendingId, sendingMethod, sendingArguments ) =>
				{
					using ( var packer = Packer.Create( buffer, false ) )
					{
						packer.PackArrayHeader( 3 );
						packer.Pack( ( int )sendingType );
						packer.Pack( sendingMethod );
						packer.Pack( sendingArguments );
					}

					controller.FeedReceiveBuffer( buffer.ToArray() );
				},
				null,
				null,
				true,
				false,
				MessageType.Notification,
				null,
				"Test",
				"Arg1"
			);
		}

		[Test]
		public void TestReceive_Notify_Fail_ResponseNotReturns()
		{
			TestReceiveCore(
				( controller, buffer, sendingType, sendingId, sendingMethod, sendingArguments ) =>
				{
					using ( var packer = Packer.Create( buffer, false ) )
					{
						packer.PackArrayHeader( 3 );
						packer.Pack( ( int )sendingType );
						packer.Pack( sendingMethod );
						packer.Pack( sendingArguments );
					}

					controller.FeedReceiveBuffer( buffer.ToArray() );
				},
				returnValue =>
				{
					throw new RpcException( RpcError.CustomError( "TestId", 1 ), "TestMessage", null );
				},
				null,
				true,
				false,
				MessageType.Notification,
				null,
				"Test",
				"Arg1"
			);
		}

		#endregion

		#region ---- Invalid Requests --

		private void TestReceiveInvalidRequestCore(
			Action<Packer, int, int?> invalidRequestPacking,
			RpcError expectedError,
			bool willBeConnectionReset,
			MessageType messageType,
			int? messageId
		)
		{
			TestReceiveCore(
				( controller, buffer, _0, _1, _2, _3 ) =>
				{
					using ( var packer = Packer.Create( buffer, false ) )
					{
						invalidRequestPacking( packer, ( int )messageType, messageId );
					}

					controller.FeedReceiveBuffer( buffer.ToArray() );
				},
				returnValue =>
				{
					Assert.Fail( "Should not be dispatched" );
					return MessagePackObject.Nil;
				},
				( error, returnValue ) =>
				{
					Assert.That( RpcError.FromIdentifier( error.AsString(), null ), Is.EqualTo( expectedError ), "{0}:{1}", error, returnValue );
				},
				false,
				willBeConnectionReset,
				MessageType.Request, // dummy
				messageId,
				null,
				null
			);
		}

		#region ------ Entire Array ------

		[Test]
		public void TestReceive_RequestNotArray_MessageRefused()
		{
			TestReceiveInvalidRequestCore(
				( packer, mesageType, messageId ) => packer.PackString( "Request" ),
				RpcError.MessageRefusedError,
				true,
				MessageType.Request,
				null
			);
		}

		[Test]
		public void TestReceive_EmptyArray_MessageRefused()
		{
			TestReceiveInvalidRequestCore(
				( packer, mesageType, messageId ) => packer.PackArrayHeader( 0 ),
				RpcError.MessageRefusedError,
				true,
				MessageType.Request,
				null
			);
		}

		[Test]
		public void TestReceive_Request_ArrayLengthIs2_MessageRefused()
		{
			TestReceiveInvalidRequestCore(
				( packer, messageType, messageId ) =>
				{
					packer.PackArrayHeader( 2 );
					packer.Pack( messageType );
					packer.Pack( messageId );
				},
				RpcError.MessageRefusedError,
				true,
				MessageType.Request,
				null
			);
		}

		[Test]
		public void TestReceive_Request_ArrayLengthIs3_MessageRefused()
		{
			TestReceiveInvalidRequestCore(
				( packer, messageType, messageId ) =>
				{
					packer.PackArrayHeader( 3 );
					packer.Pack( messageType );
					packer.Pack( messageId );
					packer.Pack( "Test" );
				},
				RpcError.MessageRefusedError,
				true,
				MessageType.Request,
				null
			);
		}

		[Test]
		public void TestReceive_Request_ArrayLengthIs5_MessageRefused()
		{
			TestReceiveInvalidRequestCore(
				( packer, messageType, messageId ) =>
				{
					packer.PackArrayHeader( 5 );
					packer.Pack( messageType );
					packer.Pack( messageId );
					packer.Pack( "Test" );
					packer.PackArrayHeader( 0 );
					packer.PackArrayHeader( 0 );
				},
				RpcError.MessageRefusedError,
				true,
				MessageType.Request,
				null
			);
		}

		[Test]
		public void TestReceive_Notification_ArrayLengthIs2_MessageRefused()
		{
			TestReceiveInvalidRequestCore(
				( packer, messageType, messageId ) =>
				{
					packer.PackArrayHeader( 2 );
					packer.Pack( messageType );
					packer.Pack( messageId );
				},
				RpcError.MessageRefusedError,
				true,
				MessageType.Notification,
				null
			);
		}

		[Test]
		public void TestReceive_Notification_ArrayLengthIs4_MessageRefused()
		{
			TestReceiveInvalidRequestCore(
				( packer, messageType, messageId ) =>
				{
					packer.PackArrayHeader( 4 );
					packer.Pack( messageType );
					packer.Pack( messageId );
					packer.Pack( "Test" );
					packer.PackArrayHeader( 0 );
				},
				RpcError.MessageRefusedError,
				true,
				MessageType.Notification,
				null
			);
		}

		[Test]
		public void TestReceive_Notification_ArrayLengthIs5_MessageRefused()
		{
			TestReceiveInvalidRequestCore(
				( packer, messageType, messageId ) =>
				{
					packer.PackArrayHeader( 5 );
					packer.Pack( messageType );
					packer.Pack( messageId );
					packer.Pack( "Test" );
					packer.PackArrayHeader( 0 );
					packer.PackArrayHeader( 0 );
				},
				RpcError.MessageRefusedError,
				true,
				MessageType.Notification,
				null
			);
		}

		#endregion

		#region ------ Message Type ------

		[Test]
		public void TestReceive_ResponseMessageType_MessageRefused()
		{
			TestReceiveInvalidRequestCore(
				( packer, messageType, messageId ) =>
				{
					packer.PackArrayHeader( 4 );
					packer.Pack( ( int )MessageType.Response );
					packer.Pack( messageId );
					packer.PackString( "Test" );
					packer.PackString( "Test" );
				},
				RpcError.MessageRefusedError,
				true,
				MessageType.Request,
				null
			);
		}

		[Test]
		public void TestReceive_UnknownMessageType_MessageRefused()
		{
			TestReceiveInvalidRequestCore(
				( packer, messageType, messageId ) =>
				{
					packer.PackArrayHeader( 3 );
					packer.Pack( 3 );
					packer.Pack( messageId );
					packer.PackString( "Test" );
				},
				RpcError.MessageRefusedError,
				true,
				MessageType.Request,
				null
			);
		}

		#endregion

		#region ------ Method Name ------

		[Test]
		public void TestReceive_Request_EmptyMethodName_Halmless()
		{
			TestReceiveCore(
				null,
				null,
				null,
				true,
				false,
				MessageType.Request,
				1,
				String.Empty
			);
		}

		[Test]
		public void TestReceive_Notification_EmptyMethodName_MessageRefused()
		{
			TestReceiveCore(
				null,
				null,
				null,
				true,
				false,
				MessageType.Notification,
				null,
				String.Empty
			);
		}

		[Test]
		public void TestReceive_Request_NullMethodName_MessageRefused()
		{
			TestReceiveCore(
				null,
				null,
				null,
				true,
				false,
				MessageType.Request,
				1,
				null
			);
		}

		[Test]
		public void TestReceive_Notification_NullMethodName_MessageRefused()
		{
			TestReceiveCore(
				null,
				null,
				null,
				true,
				false,
				MessageType.Notification,
				null,
				null
			);
		}

		#endregion

		#region ------ Arguments ( actually OK ) ------

		[Test]
		public void TestReceive_Request_EmptyArguments_Ok()
		{
			TestReceiveCore(
				null,
				null,
				null,
				true,
				false,
				MessageType.Request,
				1,
				"Test"
			);
		}

		[Test]
		public void TestReceive_Notification_EmptyArguments_Ok()
		{
			TestReceiveCore(
				null,
				null,
				null,
				true,
				false,
				MessageType.Notification,
				null,
				"Test"
			);
		}

		#endregion

		#endregion

		#region ---- Interruption ----

		private void TestReceive_InterruptCore(
			bool willExecute,
			RpcError expectedError,
			bool willResetConnection,
			byte messageId,
			params Action<Packer, MemoryStream, byte, byte?, MessagePackObject[]>[] partitionedSendings
		)
		{
			TestReceive_InterruptCore( willExecute, expectedError, willResetConnection, MessageType.Request, messageId, new MessagePackObject[ 0 ], partitionedSendings );
		}

		private void TestReceive_InterruptCore(
			bool willExecute,
			RpcError expectedError,
			bool willResetConnection,
			params Action<Packer, MemoryStream, byte, byte?, MessagePackObject[]>[] partitionedSendings
		)
		{
			TestReceive_InterruptCore( willExecute, expectedError, willResetConnection, MessageType.Notification, null, new MessagePackObject[ 0 ], partitionedSendings );
		}

		private void TestReceive_InterruptCore(
			bool willExecute,
			RpcError expectedError,
			bool willResetConnection,
			byte messageId,
			MessagePackObject[] arguments,
			params Action<Packer, MemoryStream, byte, byte?, MessagePackObject[]>[] partitionedSendings
		)
		{
			TestReceive_InterruptCore( willExecute, expectedError, willResetConnection, MessageType.Request, messageId, arguments, partitionedSendings );
		}

		private void TestReceive_InterruptCore(
			bool willExecute,
			RpcError expectedError,
			bool willResetConnection,
			MessagePackObject[] arguments,
			params Action<Packer, MemoryStream, byte, byte?, MessagePackObject[]>[] partitionedSendings
		)
		{
			TestReceive_InterruptCore( willExecute, expectedError, willResetConnection, MessageType.Notification, null, arguments, partitionedSendings );
		}
		
		private void TestReceive_InterruptCore(
			bool willExecute,
			RpcError expectedError,
			bool willResetConnection,
			MessageType messageType,
			byte? messageId,
			MessagePackObject[] arguments,
			params Action<Packer, MemoryStream, byte, byte?, MessagePackObject[]>[] partitionedSendings
		)
		{
			TestReceiveCore(
				( controller, buffer, _0, _1, _2, _3 ) =>
				{
					using ( var packer = Packer.Create( buffer, false ) )
					{
						foreach ( var sending in partitionedSendings )
						{
							sending( packer, buffer, ( byte )messageType, messageId, arguments );
							controller.FeedReceiveBuffer( buffer.ToArray() );
							buffer.SetLength( 0 );
						}
					}
				},
				null,
				expectedError == null
				? default( Action<MessagePackObject, MessagePackObject> )
				: ( error, returnValue ) =>
				{
					Assert.That( error == expectedError.Identifier );
				},
				willExecute,
				willResetConnection,
				messageType,
				messageId,
				null, // dummy
				arguments ?? new MessagePackObject[ 0 ]
			);
		}

		#region ------ Entire Array ------

		[Test]
		public void TestReceive_Request_InterupptOnArrayAndResume_Ok()
		{
			TestReceive_InterruptCore(
				true,
				null,
				false,
				1,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0xDC ); // array16
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x0 );
					buffer.WriteByte( 0x4 );
					buffer.WriteByte( messageType );
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptOnArrayAndResume_Ok()
		{
			TestReceive_InterruptCore(
				true,
				null,
				false,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0xDC ); // array16
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x0 );
					buffer.WriteByte( 0x3 );
					buffer.WriteByte( messageType );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		[Test]
		public void TestReceive_Request_InterupptOnArrayAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				false,
				RpcError.TimeoutError,
				true,
				1,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0xDC ); // array16
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x0 );
					buffer.WriteByte( 0x4 );
					buffer.WriteByte( messageType );
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptOnArrayAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				false,
				RpcError.TimeoutError,
				true,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0xDC ); // array16
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x0 );
					buffer.WriteByte( 0x3 );
					buffer.WriteByte( messageType );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		[Test]
		public void TestReceive_Request_InterupptAfterArrayAndResume_Ok()
		{
			TestReceive_InterruptCore(
				true,
				null,
				false,
				1,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed-array4
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( messageType );
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptAfterArrayAndResume_Ok()
		{
			TestReceive_InterruptCore(
				true,
				null,
				false,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x93 ); // fixed-array3
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( messageType );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		[Test]
		public void TestReceive_Request_InterupptAfterArrayAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				false,
				RpcError.TimeoutError,
				true,
				1,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed-array4
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( messageType );
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptAfterArrayAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				false,
				RpcError.TimeoutError,
				true,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x93 ); // fixed-array3
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( messageType );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		#endregion

		#region ------ Message Type ------

		[Test]
		public void TestReceive_Request_InterupptOnMessageTypeAndResume_Ok()
		{
			TestReceive_InterruptCore(
				true,
				null,
				false,
				1,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-4
					buffer.WriteByte( 0xD0 ); // i8
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( messageType );
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptOnMessageTypeAndResume_Ok()
		{
			TestReceive_InterruptCore(
				true,
				null,
				false,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x93 ); // fixed array-3
					buffer.WriteByte( 0xD0 ); // i8
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( messageType );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		[Test]
		public void TestReceive_Request_InterupptOnMessageTypeAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				false,
				RpcError.TimeoutError,
				true,
				1,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-4
					buffer.WriteByte( 0xD0 ); // i8
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( messageType );
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptOnMessageTypeAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				false,
				RpcError.TimeoutError,
				true,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x93 ); // fixed array-3
					buffer.WriteByte( 0xD0 ); // i8
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( messageType );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		[Test]
		public void TestReceive_Request_InterupptAfterMessageTypeAndResume_Ok()
		{
			TestReceive_InterruptCore(
				true,
				null,
				false,
				1,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-4
					buffer.WriteByte( messageType );
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptAfterMessageTypeAndResume_Ok()
		{
			TestReceive_InterruptCore(
				true,
				null,
				false,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x93 ); // fixed array-3
					buffer.WriteByte( messageType );
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		[Test]
		public void TestReceive_Request_InterupptAfterMessageTypeAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				false,
				RpcError.TimeoutError,
				true,
				1,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-4
					buffer.WriteByte( messageType );
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptAfterMessageTypeAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				false,
				RpcError.TimeoutError,
				true,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-3
					buffer.WriteByte( messageType );
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		#endregion

		#region ------ Message ID ------

		[Test]
		public void TestReceive_Request_InterupptOnMessageIdAndResume_Ok()
		{
			TestReceive_InterruptCore(
				true,
				null,
				false,
				1,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-4
					buffer.WriteByte( messageType );
					buffer.WriteByte( 0xD0 ); // int8
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		[Test]
		public void TestReceive_Request_InterupptOnMessageIdAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				false,
				RpcError.TimeoutError,
				true,
				1,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-4
					buffer.WriteByte( messageType );
					buffer.WriteByte( 0xD0 ); // int8
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		[Test]
		public void TestReceive_Request_InterupptAfterMessageIdAndResume_Ok()
		{
			TestReceive_InterruptCore(
				true,
				null,
				false,
				1,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-4
					buffer.WriteByte( messageType );
					buffer.WriteByte( 0xD0 ); // int8
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		[Test]
		public void TestReceive_Request_InterupptAfterMessageIdAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				false,
				RpcError.TimeoutError,
				true,
				1,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-4
					buffer.WriteByte( messageType );
					buffer.WriteByte( 0xD0 ); // int8
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		#endregion

		#region ------ Method Name ------

		[Test]
		public void TestReceive_Request_InterupptOnMethodNameAndResume_Ok()
		{
			TestReceive_InterruptCore(
				true,
				null,
				false,
				1,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-4
					buffer.WriteByte( messageType );
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptOnMethodNameAndResume_Ok()
		{
			TestReceive_InterruptCore(
				true,
				null,
				false,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x93 ); // fixed array-3
					buffer.WriteByte( messageType );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		[Test]
		public void TestReceive_Request_InterupptOnMethodNameAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				false,
				RpcError.TimeoutError,
				false,
				1,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-4
					buffer.WriteByte( messageType );
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptOnMethodNameAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				false,
				RpcError.TimeoutError,
				false,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x93 ); // fixed array-3
					buffer.WriteByte( messageType );
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		[Test]
		public void TestReceive_Request_InterupptAfterMethodNameAndResume_Ok()
		{
			TestReceive_InterruptCore(
				true,
				null,
				false,
				1,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-4
					buffer.WriteByte( messageType );
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptAfterMethodNameAndResume_Ok()
		{
			TestReceive_InterruptCore(
				true,
				null,
				false,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x93 ); // fixed array-3
					buffer.WriteByte( messageType );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		[Test]
		public void TestReceive_Request_InterupptAfterMethodNameAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				false,
				RpcError.TimeoutError,
				false,
				1,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-4
					buffer.WriteByte( messageType );
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptAfterMethodNameAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				false,
				RpcError.TimeoutError,
				false,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x93 ); // fixed array-3
					buffer.WriteByte( messageType );
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x90 ); // empty array
				}
			);
		}

		#endregion

		#region ------ Argument Header ------

		[Test]
		public void TestReceive_Request_InterupptOnArgumentsHeaderAndResume_Ok()
		{
			TestReceive_InterruptCore(
				true,
				null,
				false,
				1,
				new MessagePackObject[] { 1 },
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-4
					buffer.WriteByte( messageType );
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0xDC ); // array 16
					buffer.WriteByte( 0x0 );
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x1 ); // array size
					buffer.WriteByte( 0x1 ); // entry(positive fix num-1)
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptOnArgumentsHeaderAndResume_Ok()
		{
			TestReceive_InterruptCore(
				true,
				null,
				false,
				new MessagePackObject[] { 1 },
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x93 ); // fixed array-3
					buffer.WriteByte( messageType );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0xDC ); // array 16
					buffer.WriteByte( 0x0 );
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x1 ); // array size
					buffer.WriteByte( 0x1 ); // entry(positive fix num-1)
				}
			);
		}

		[Test]
		public void TestReceive_Request_InterupptOnArgumentsHeaderAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				false,
				RpcError.TimeoutError,
				false,
				1,
				new MessagePackObject[] { 1 },
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-4
					buffer.WriteByte( messageType );
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0xDC ); // array 16
					buffer.WriteByte( 0x0 );
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x1 ); // array size
					buffer.WriteByte( 0x1 ); // entry(positive fix num-1)
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptOnArgumentsHeaderAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				false,
				RpcError.TimeoutError,
				false,
				new MessagePackObject[] { 1 },
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x93 ); // fixed array-3
					buffer.WriteByte( messageType );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0xDC ); // array 16
					buffer.WriteByte( 0x0 );
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x1 ); // array size
					buffer.WriteByte( 0x1 ); // entry(positive fix num-1)
				}
			);
		}

		[Test]
		public void TestReceive_Request_InterupptAfterArgumentsHeaderAndResume_Ok()
		{
			TestReceive_InterruptCore(
				true,
				null,
				false,
				1,
				new MessagePackObject[] { 1 },
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-4
					buffer.WriteByte( messageType );
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x91 ); // fixed array-1
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x1 ); // entry(positive fix num-1)
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptAfterArgumentsHeaderAndResume_Ok()
		{
			TestReceive_InterruptCore(
				true,
				null,
				false,
				new MessagePackObject[] { 1 },
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x93 ); // fixed array-3
					buffer.WriteByte( messageType );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x91 ); // fixed array-1
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x1 ); // entry(positive fix num-1)
				}
			);
		}

		[Test]
		public void TestReceive_Request_InterupptAfterArgumentsHeaderAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				false,
				RpcError.TimeoutError,
				false,
				1,
				new MessagePackObject[] { 1 },
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-4
					buffer.WriteByte( messageType );
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x91 ); // fixed array-1
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x1 ); // entry(positive fix num-1)
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptAfterArgumentsHeaderAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				false,
				RpcError.TimeoutError,
				false,
				new MessagePackObject[] { 1 },
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x93 ); // fixed array-3
					buffer.WriteByte( messageType );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x91 ); // fixed array-1
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x1 ); // entry(positive fix num-1)
				}
			);
		}

		#endregion

		#region ------ Argument Body ------

		[Test]
		public void TestReceive_Request_InterupptOnArgumentsBodyAndResume_Ok()
		{
			TestReceive_InterruptCore(
				true,
				null,
				false,
				1,
				new MessagePackObject[] { 1 },
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-4
					buffer.WriteByte( messageType );
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x91 ); // fixed array-1
					buffer.WriteByte( 0xD0 ); // int8
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x1 );
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptOnArgumentsBodyAndResume_Ok()
		{
			TestReceive_InterruptCore(
				true,
				null,
				false,
				new MessagePackObject[] { 1 },
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x93 ); // fixed array-3
					buffer.WriteByte( messageType );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x91 ); // fixed array-1
					buffer.WriteByte( 0xD0 ); // int8
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x1 );
				}
			);
		}

		[Test]
		public void TestReceive_Request_InterupptOnArgumentsBodyAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				false,
				RpcError.TimeoutError,
				false,
				1,
				new MessagePackObject[] { 1 },
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-4
					buffer.WriteByte( messageType );
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x91 ); // fixed array-1
					buffer.WriteByte( 0xD0 ); // int8
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x1 );
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptOnArgumentsBodyAndNotResume_Timeout()
		{
			Assert.Inconclusive( "Send/Receive/Execution timeout is not implemented yet." );
			TestReceive_InterruptCore(
				false,
				RpcError.TimeoutError,
				false,
				new MessagePackObject[] { 1 },
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x93 ); // fixed array-3
					buffer.WriteByte( messageType );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x91 ); // fixed array-1
					buffer.WriteByte( 0xD0 ); // int8
				},
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x1 );
				}
			);
		}

		#endregion

		#endregion

		#endregion

		#region -- Send --

		/// <summary>
		/// Tests the Send 
		/// </summary>
		[Test()]
		public void TestSend()
		{
			/*
			 * void
			 * error
			 * normal
			 * timeout
			 */
			Assert.Inconclusive();
		}

		private void TestSendCore(
			MessagePackObject returnValue,
			RpcError error
		)
		{
			var messageId = 1;
			var errorMessage = Guid.NewGuid().ToString();

			TestCore(
				( controller, target ) =>
				{
					using ( var waitHandle = new ManualResetEventSlim() )
					{
						byte[] response = null;
						controller.Response +=
							( sender, e ) =>
							{
								response = e.Data;
								waitHandle.Set();
							};

						using ( var buffer = new MemoryStream() )
						{
							using ( var packer = Packer.Create( buffer, false ) )
							{
								packer.PackArrayHeader( 4 );
								packer.Pack( ( int )MessageType.Request );
								packer.Pack( messageId );
								packer.PackString( "Test" );
								packer.PackArrayHeader( 0 );
							}

							controller.FeedData( buffer.ToArray() );
						}

						if ( Debugger.IsAttached )
						{
							waitHandle.Wait();
						}
						else
						{
							Assert.That( waitHandle.Wait( TimeSpan.FromSeconds( 1 ) ), Is.True, "No respond." );
						}

						var result = Unpacking.UnpackObject( response ).Value.AsList();

						Assert.That( result.Count, Is.EqualTo( 4 ) );
						Assert.That( result[ 0 ] == ( int )MessageType.Response, "{0}!={1}", result[ 0 ], ( int )MessageType.Response );
						Assert.That( result[ 1 ] == messageId, "{0}!={1}", result[ 1 ], messageId );
						if ( error == null )
						{
							Assert.That( result[ 2 ].IsNil, "is not Nil" );
							Assert.That( result[ 3 ] == returnValue, "{0}!={1}", result[ 3 ], returnValue );
						}
						else
						{
							Assert.That( result[ 2 ] == error.Identifier, "{0}!={1}", result[ 2 ], error.Identifier );
							Assert.That( result[ 3 ].AsDictionary()[ "Message" ] == errorMessage, "{0} does not contain \"{1}\"", result[ 3 ], errorMessage );
						}
					}
				},
				( id, args ) =>
				{
					if ( error != null )
					{
						throw new RpcException( error, errorMessage, String.Empty );
					}

					return returnValue;
				}
			);
		}

		[Test]
		public void TestSend_Void_ErrorAndReturnValueAreNil()
		{
			TestSendCore( MessagePackObject.Nil, null );
		}

		[Test]
		public void TestSend_NotVoid_ErrorAndReturnValueAreNil()
		{
			TestSendCore( new MessagePackObject( new MessagePackObject[] { 1, 2, 3 }, true ), null );
		}

		[Test]
		public void TestSend_Error_ErrorAndReturnValueAreFilled()
		{
			TestSendCore( MessagePackObject.Nil, RpcError.CustomError( "Test.Error", 1234 ) );
		}

		[Test]
		public void TestSend_Timeout_ConnectionReset()
		{
			Assert.Inconclusive( "Not implemented yet" );
		}

		#endregion

		#region -- Execution --

		[Test]
		public void TestExecute_Timeout_TimeoutError()
		{
			Assert.Inconclusive( "Not implemented yet" );
		}

		#endregion
	}
}
