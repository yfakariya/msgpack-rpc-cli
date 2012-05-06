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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using MsgPack.Rpc.Protocols;
using MsgPack.Rpc.Protocols.Filters;
using MsgPack.Rpc.Server.Dispatch;
using NUnit.Framework;

namespace MsgPack.Rpc.Server.Protocols
{
	[TestFixture]
	[Timeout( 3000 )]
	public class ServerTransportTest
	{
		private static readonly TimeSpan _receiveTimeout = TimeSpan.FromMilliseconds( 20 );

		private void TestCore( Action<InProcServerTransport> test )
		{
			TestCore( ( target, _ ) => test( target ), ( _0, _1 ) => MessagePackObject.Nil );
		}

		private void TestCore( Action<InProcServerTransport, InProcServerTransportController> test, Func<int?, MessagePackObject[], MessagePackObject> callback )
		{
			this.TestCore( test, callback, null );
		}

		private void TestCore( Action<InProcServerTransport, InProcServerTransportController> test, Func<int?, MessagePackObject[], MessagePackObject> callback, Action<RpcServerConfiguration> configurationTweak )
		{
			InProcServerTransportManager serverTransportManager = null;
			var config = new RpcServerConfiguration();
			config.IsDebugMode = true;
			config.TransportManagerProvider =
				s => serverTransportManager = new InProcServerTransportManager( s, m => new SingletonObjectPool<InProcServerTransport>( new InProcServerTransport( m ) ) );
			config.DispatcherProvider =
				s => new CallbackDispatcher( s, callback );

			if ( configurationTweak != null )
			{
				configurationTweak( config );
			}

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

		private void TestShutdownCore(
			bool isClientShutdown,
			bool isReceiving,
			bool isEmpty
		)
		{
			var arg = Environment.TickCount % 3;
			var returnValue = Environment.TickCount % 5;
			Tuple<InProcServerTransport, InProcServerTransportController> testEnvironment = null;
			TestCore(
				( target, controller ) =>
				{
					testEnvironment = Tuple.Create( target, controller );
					using ( var serverShutdownWaitHandle = new ManualResetEventSlim() )
					using ( var receivingWaitHandle = new ManualResetEventSlim() )
					using ( var shutdownPacketWaitHandle = new ManualResetEventSlim() )
					using ( var responseWaitHandle = new ManualResetEventSlim() )
					{
						byte[] response = null;

						if ( isReceiving )
						{
							// detects recursive dequeue.
							target.Receiving += ( sender, e ) => receivingWaitHandle.Set();
						}

						target.ShutdownCompleted += ( sender, e ) => serverShutdownWaitHandle.Set();

						controller.Response +=
							( sender, e ) =>
							{
								if ( e.Data.Length == 0 )
								{
									shutdownPacketWaitHandle.Set();
								}
								else
								{
									Interlocked.Exchange( ref response, e.Data );
									responseWaitHandle.Set();
								}
							};

						int messageId = Math.Abs( Environment.TickCount % 10 );
						if ( !isEmpty )
						{
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

								var sendingData = buffer.ToArray();

								controller.FeedReceiveBuffer( sendingData.Take( sendingData.Length / 2 ).ToArray() );

								if ( isReceiving )
								{
									Assert.That( receivingWaitHandle.Wait( TimeSpan.FromSeconds( 1 ) ) );
									if ( isClientShutdown )
									{
										controller.FeedReceiveBuffer( new byte[ 0 ] );
									}
									else
									{
										target.BeginShutdown();
										controller.FeedReceiveBuffer( sendingData.Skip( sendingData.Length / 2 ).ToArray() );
									}
								}
								else
								{
									// Shutdown will be initiated in callback.
									controller.FeedReceiveBuffer( sendingData.Skip( sendingData.Length / 2 ).ToArray() );
								}
							}
						}
						else
						{
							// Initiate shutdown now.

							if ( isClientShutdown )
							{
								controller.FeedReceiveBuffer( new byte[ 0 ] );
							}
							else
							{
								target.BeginShutdown();
							}
						}

						Assert.That( serverShutdownWaitHandle.Wait( TimeSpan.FromSeconds( 1 ) ) );
						Assert.That( shutdownPacketWaitHandle.Wait( TimeSpan.FromSeconds( 1 ) ) );

						if ( !isReceiving && !isEmpty )
						{
							Assert.That( responseWaitHandle.Wait( TimeSpan.FromSeconds( 1 ) ) );
							var result = Unpacking.UnpackObject( response ).Value.AsList();

							Assert.That( result.Count, Is.EqualTo( 4 ) );
							Assert.That( result[ 0 ] == ( int )MessageType.Response );
							Assert.That( result[ 1 ] == messageId, "{0} != {1}", result[ 1 ], messageId );
							Assert.That( result[ 2 ].IsNil, "{0}:{1}", result[ 2 ], result[ 3 ] );
							Assert.That( result[ 3 ] == returnValue );
						}
					}
				},
				( messageId, args ) =>
				{
					if ( !isReceiving )
					{
						// Initiate shutdown now.
						if ( isClientShutdown )
						{
							testEnvironment.Item2.FeedReceiveBuffer( new byte[ 0 ] );
						}
						else
						{
							testEnvironment.Item1.BeginShutdown();
						}
					}

					return returnValue;
				}
			);
		}

		#region -- BeginShutdown --

		private void TestBeginShutdownCore( bool isReceiving, bool isEmpty )
		{
			TestShutdownCore( isClientShutdown: false, isReceiving: isReceiving, isEmpty: isEmpty );
		}

		[Test()]
		public void TestBeginShutdown_NoPendingRequest_Harmless()
		{
			TestBeginShutdownCore( isReceiving: false, isEmpty: true );
		}

		[Test]
		public void TestBeginShutdown_DuringReceiving_PendingRequestsAreCanceled_ShutdownPacketReplyed_ServerShutdownCompleted()
		{
			TestBeginShutdownCore( isReceiving: true, isEmpty: false );
		}

		[Test]
		public void TestBeginShutdown_DuringExecuting_PendingRequestsAreSent_ShutdownPacketReplyed_ServerShutdownCompleted()
		{
			TestBeginShutdownCore( isReceiving: false, isEmpty: false );
		}

		#endregion

		#region -- Client shutdown --

		private void TestClientShutdownCore( bool isReceiving, bool isEmpty )
		{
			TestShutdownCore( isClientShutdown: true, isReceiving: isReceiving, isEmpty: isEmpty );
		}

		[Test]
		public void TestClientShutdown_NoPendingRequest_Harmless()
		{
			TestClientShutdownCore( isReceiving: false, isEmpty: true );
		}

		[Test]
		public void TestClientShutdown_DuringSending_PendingRequestsAreCanceled_ShutdownPacketReplyed_ServerShutdownCompleted()
		{
			TestClientShutdownCore( isReceiving: true, isEmpty: false );
		}

		[Test]
		public void TestClientShutdown_DuringExecuting_PendingRequestsAreSent_ShutdownPacketReplyed_ServerShutdownCompleted()
		{
			TestClientShutdownCore( isReceiving: false, isEmpty: false );
		}

		#endregion

		#region -- Receive --

		#region ---- Common Private Method ---

		private void TestReceiveCore(
			Action<InProcServerTransportController, MemoryStream, MessageType, int?, string, MessagePackObject[]> sending,
			Func<MessagePackObject, MessagePackObject> onExecute,
			Action<MessagePackObject, MessagePackObject> responseAssertion,
			bool willExecute,
			bool willBeConnectionReset,
			TimeSpan? receiveTimeout,
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
									if ( ( willBeConnectionReset && e.Data.Length == 0 )
										|| e.Data.Length > 0 )
									{
										Interlocked.Exchange( ref response, e.Data );
										responseWaitHandle.Set();
									}
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
								else if ( receiveTimeout != null )
								{
									Assert.That( responseWaitHandle.Wait( TimeSpan.FromSeconds( 1 ) ), Is.True, "Not respond." );
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
					},
					config => config.ReceiveTimeout = receiveTimeout
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
				null,
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
				null,
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
				null,
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
				null,
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
				null,
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
		public void TestReceive_Request_EmptyMethodName_Harmless()
		{
			TestReceiveCore(
				null,
				null,
				null,
				true,
				false,
				null,
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
				null,
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
				null,
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
				null,
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
				null,
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
				null,
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
			TimeSpan? receiveTimeout,
			byte messageId,
			params Action<Packer, MemoryStream, byte, byte?, MessagePackObject[]>[] partitionedSendings
		)
		{
			TestReceive_InterruptCore( willExecute, expectedError, willResetConnection, receiveTimeout, MessageType.Request, messageId, new MessagePackObject[ 0 ], partitionedSendings );
		}

		private void TestReceive_InterruptCore(
			bool willExecute,
			RpcError expectedError,
			bool willResetConnection,
			TimeSpan? receiveTimeout,
			params Action<Packer, MemoryStream, byte, byte?, MessagePackObject[]>[] partitionedSendings
		)
		{
			TestReceive_InterruptCore( willExecute, expectedError, willResetConnection, receiveTimeout, MessageType.Notification, null, new MessagePackObject[ 0 ], partitionedSendings );
		}

		private void TestReceive_InterruptCore(
			bool willExecute,
			RpcError expectedError,
			bool willResetConnection,
			TimeSpan? receiveTimeout,
			byte messageId,
			MessagePackObject[] arguments,
			params Action<Packer, MemoryStream, byte, byte?, MessagePackObject[]>[] partitionedSendings
		)
		{
			TestReceive_InterruptCore( willExecute, expectedError, willResetConnection, receiveTimeout, MessageType.Request, messageId, arguments, partitionedSendings );
		}

		private void TestReceive_InterruptCore(
			bool willExecute,
			RpcError expectedError,
			bool willResetConnection,
			TimeSpan? receiveTimeout,
			MessagePackObject[] arguments,
			params Action<Packer, MemoryStream, byte, byte?, MessagePackObject[]>[] partitionedSendings
		)
		{
			TestReceive_InterruptCore( willExecute, expectedError, willResetConnection, receiveTimeout, MessageType.Notification, null, arguments, partitionedSendings );
		}

		private void TestReceive_InterruptCore(
			bool willExecute,
			RpcError expectedError,
			bool willResetConnection,
			TimeSpan? receiveTimeout,
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
				receiveTimeout,
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
				null,
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
				null,
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
		public void TestReceive_Request_InterupptOnArrayAndNotResume_TimeoutAsConnectionReset()
		{
			TestReceive_InterruptCore(
				false, // willExecute
				RpcError.MessageRefusedError,
				true, // willConnectionReset
				_receiveTimeout,
				1,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0xDC ); // array16
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptOnArrayAndNotResume_TimeoutAsConnectionReset()
		{
			TestReceive_InterruptCore(
				false, // willExecute
				RpcError.MessageRefusedError,
				true, // willConnectionReset
				_receiveTimeout,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0xDC ); // array16
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
				null,
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
				null,
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
		public void TestReceive_Request_InterupptAfterArrayAndNotResume_TimeoutAsConnectionReset()
		{
			TestReceive_InterruptCore(
				false, // willExecute
				RpcError.MessageRefusedError,
				true, // willConnectionReset
				_receiveTimeout,
				1,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed-array4
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptAfterArrayAndNotResume_TimeoutAsConnectionReset()
		{
			TestReceive_InterruptCore(
				false, // willExecute
				RpcError.MessageRefusedError,
				true, // willConnectionReset
				_receiveTimeout,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x93 ); // fixed-array3
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
				null,
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
				null,
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
		public void TestReceive_Request_InterupptOnMessageTypeAndNotResume_TimeoutAsConnectionReset()
		{
			TestReceive_InterruptCore(
				false, // willExecute
				RpcError.MessageRefusedError,
				true, // willConnectionReset
				_receiveTimeout,
				1,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-4
					buffer.WriteByte( 0xD0 ); // i8
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptOnMessageTypeAndNotResume_TimeoutAsConnectionReset()
		{
			TestReceive_InterruptCore(
				false, // willExecute
				RpcError.MessageRefusedError,
				true, // willConnectionReset
				_receiveTimeout,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x93 ); // fixed array-3
					buffer.WriteByte( 0xD0 ); // i8
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
				null,
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
				null,
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
		public void TestReceive_Request_InterupptAfterMessageTypeAndNotResume_TimeoutAsConnectionReset()
		{
			TestReceive_InterruptCore(
				false, // willExecute
				RpcError.MessageRefusedError,
				true, // willConnectionReset
				_receiveTimeout,
				1,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-4
					buffer.WriteByte( messageType );
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptAfterMessageTypeAndNotResume_TimeoutAsConnectionReset()
		{
			TestReceive_InterruptCore(
				false, // willExecute
				RpcError.MessageRefusedError,
				true, // willConnectionReset
				_receiveTimeout,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-3
					buffer.WriteByte( messageType );
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
				null,
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
		public void TestReceive_Request_InterupptOnMessageIdAndNotResume_TimeoutAsConnectionReset()
		{
			TestReceive_InterruptCore(
				false, // willExecute
				RpcError.MessageRefusedError,
				true, // willConnectionReset
				_receiveTimeout,
				1,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-4
					buffer.WriteByte( messageType );
					buffer.WriteByte( 0xD0 ); // int8
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
				null,
				1,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-4
					buffer.WriteByte( messageType );
					buffer.WriteByte( messageId.Value );
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
		public void TestReceive_Request_InterupptAfterMessageIdAndNotResume_TimeoutAsMessageRefusedError()
		{
			TestReceive_InterruptCore(
				false, // willExecute
				RpcError.MessageRefusedError,
				false, // willConnectionReset
				_receiveTimeout,
				1,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-4
					buffer.WriteByte( messageType );
					buffer.WriteByte( messageId.Value );
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
				null,
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
				null,
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
		public void TestReceive_Request_InterupptOnMethodNameAndNotResume_TimeoutAsMessageRefusedError()
		{
			TestReceive_InterruptCore(
				false, // willExecute
				RpcError.MessageRefusedError,
				false, // willConnectionReset
				_receiveTimeout,
				1,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-4
					buffer.WriteByte( messageType );
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptOnMethodNameAndNotResume_TimeoutAsConnectionReset()
		{
			TestReceive_InterruptCore(
				false, // willExecute
				RpcError.MessageRefusedError,
				true, // willConnectionReset
				_receiveTimeout,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x93 ); // fixed array-3
					buffer.WriteByte( messageType );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
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
				null,
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
				null,
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
		public void TestReceive_Request_InterupptAfterMethodNameAndNotResume_TimeoutAsMessageRefusedError()
		{
			TestReceive_InterruptCore(
				false, // willExecute
				RpcError.MessageRefusedError,
				false, // willConnectionReset
				_receiveTimeout,
				1,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x94 ); // fixed array-4
					buffer.WriteByte( messageType );
					buffer.WriteByte( messageId.Value );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptAfterMethodNameAndNotResume_TimeoutAsConnectionReset()
		{
			TestReceive_InterruptCore(
				false, // willExecute
				RpcError.MessageRefusedError,
				true, // willConnectionReset
				_receiveTimeout,
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x93 ); // fixed array-3
					buffer.WriteByte( messageType );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
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
				null,
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
				null,
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
		public void TestReceive_Request_InterupptOnArgumentsHeaderAndNotResume_TimeoutAsMessageRefusedError()
		{
			TestReceive_InterruptCore(
				false, // willExecute
				RpcError.MessageRefusedError,
				false, // willConnectionReset
				_receiveTimeout,
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
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptOnArgumentsHeaderAndNotResume_TimeoutAsConnectionReset()
		{
			TestReceive_InterruptCore(
				false, // willExecute
				RpcError.MessageRefusedError,
				true, // willConnectionReset
				_receiveTimeout,
				new MessagePackObject[] { 1 },
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x93 ); // fixed array-3
					buffer.WriteByte( messageType );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0xDC ); // array 16
					buffer.WriteByte( 0x0 );
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
				null,
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
				null,
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
		public void TestReceive_Request_InterupptAfterArgumentsHeaderAndNotResume_TimeoutAsMessageRefusedError()
		{
			TestReceive_InterruptCore(
				false, // willExecute
				RpcError.MessageRefusedError,
				false, // willConnectionReset
				_receiveTimeout,
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
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptAfterArgumentsHeaderAndNotResume_TimeoutAsConnectionReset()
		{
			TestReceive_InterruptCore(
				false, // willExecute
				RpcError.MessageRefusedError,
				true, // willConnectionReset
				_receiveTimeout,
				new MessagePackObject[] { 1 },
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x93 ); // fixed array-3
					buffer.WriteByte( messageType );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x91 ); // fixed array-1
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
				null,
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
				null,
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
		public void TestReceive_Request_InterupptOnArgumentsBodyAndNotResume_TimeoutAsMessageRefusedError()
		{
			TestReceive_InterruptCore(
				false, // willExecute
				RpcError.MessageRefusedError,
				false, // willConnectionReset
				_receiveTimeout,
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
				}
			);
		}

		[Test]
		public void TestReceive_Notification_InterupptOnArgumentsBodyAndNotResume_TimeoutAsConnectionReset()
		{
			TestReceive_InterruptCore(
				false, // willExecute
				RpcError.MessageRefusedError,
				true, // willConnectionReset
				_receiveTimeout,
				new MessagePackObject[] { 1 },
				( packer, buffer, messageType, messageId, arguments ) =>
				{
					buffer.WriteByte( 0x93 ); // fixed array-3
					buffer.WriteByte( messageType );
					buffer.WriteByte( 0xA1 );// fixed-raw 1
					buffer.WriteByte( ( byte )'A' );
					buffer.WriteByte( 0x91 ); // fixed array-1
					buffer.WriteByte( 0xD0 ); // int8
				}
			);
		}

		#endregion

		#endregion

		#endregion

		#region -- Send --

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
			TestTimeoutCore(
				responseData =>
				{
					Assert.That( responseData, Is.Not.Null.And.Empty );
				},
				isSendingTimeout: true
			);
		}

		#endregion

		#region -- Execution --

		[Test]
		public void TestExecute_Timeout_TimeoutError()
		{
			TestTimeoutCore(
				responseData =>
				{
					var response = Unpacking.UnpackArray( responseData ).Value;
					Assert.That( response[ 2 ] == RpcError.ServerError.Identifier, new MessagePackObject( response, true ).ToString() );
				},
				isExecutionTimeout: true,
				isHardTimeout: false
			);
		}

		[Test]
		public void TestExecute_Timeout_HardTimeoutError()
		{
			TestTimeoutCore(
				responseData =>
				{
					var response = Unpacking.UnpackArray( responseData ).Value;
					Assert.That( response[ 2 ] == RpcError.ServerError.Identifier, new MessagePackObject( response, true ).ToString() );
				},
				isExecutionTimeout: true,
				isHardTimeout: true
			);
		}

		#endregion

		// receive timeout is tested via TestReceive_Interrupt_*
		private void TestTimeoutCore(
			Action<byte[]> responseAssertion,
			bool isExecutionTimeout = false,
			bool isSendingTimeout = false,
			bool isHardTimeout = false
		)
		{
			// Tweak for your machine
			TimeSpan timeout = TimeSpan.FromMilliseconds( 50 );
			TimeSpan hanging = TimeSpan.FromMilliseconds( timeout.TotalMilliseconds * 1.5 );

			TestCore(
				( transport, controller ) =>
				{
					using ( var responseWaitHandle = new ManualResetEventSlim() )
					{
						byte[] responseData = null;

						if ( isSendingTimeout )
						{
							transport.Response +=
								( sender, e ) =>
								{
									if ( e.Data.Length > 0 )
									{
										// Causes send timeout by handing sending.
										// In-Proc shutdown packate will overstride this message.
										Thread.Sleep( hanging );
									}
								};
						}

						controller.Response +=
							( sender, e ) =>
							{
								if ( isSendingTimeout )
								{
									// Interesting in shutdown package only.
									if ( e.Data.Length == 0 )
									{
										Interlocked.Exchange( ref responseData, e.Data );
										responseWaitHandle.Set();
									}
								}
								else
								{
									Interlocked.Exchange( ref responseData, e.Data );
									responseWaitHandle.Set();
								}
							};

						using ( var buffer = new MemoryStream() )
						{
							using ( var packer = Packer.Create( buffer, false ) )
							{
								packer.PackArrayHeader( 4 );
								packer.Pack( ( int )MessageType.Request );
								packer.Pack( 1 );
								packer.PackString( "Test" );
								packer.PackArrayHeader( 0 );
							}

							controller.FeedReceiveBuffer( buffer.ToArray() );
						}

						Assert.That( responseWaitHandle.Wait( TimeSpan.FromSeconds( 1 ) ), Is.True, "Not respond." );

						responseAssertion( responseData );
					}
				},
				( id, args ) =>
				{
					if ( isExecutionTimeout )
					{
						// Causes execution timeout by handing execution.
						Thread.Sleep( hanging );

						if ( isHardTimeout )
						{
							// Causes hard execution timeout by handing execution again.
							Thread.Sleep( hanging );
						}

						Assert.That( RpcApplicationContext.IsCanceled );
					}

					return args;
				},
				configuration =>
				{
					if ( isExecutionTimeout )
					{
						configuration.ExecutionTimeout = timeout;
						configuration.HardExecutionTimeout = timeout;
					}

					if ( isSendingTimeout )
					{
						configuration.SendTimeout = timeout;
					}
				}
			);
		}

		#region -- Filter --

		[Test]
		public void TestFilters_Initialization_AppliedDeserializationsAreInOrder_AppliedSerializationsAreReverseOrder()
		{
			TestFiltersCore(
				null,
				target =>
				{
					CheckFilters( target.BeforeDeserializationFilters, MessageFilteringLocation.BeforeDeserialization, 0, 1 );
					CheckFilters( target.AfterSerializationFilters, MessageFilteringLocation.AfterSerialization, 3, 2 );
				},
				null,
				new ServerRequestTestMessageFilterProvider( 0 ),
				new ServerRequestTestMessageFilterProvider( 1 ),
				new ServerResponseTestMessageFilterProvider( 2 ),
				new ServerResponseTestMessageFilterProvider( 3 )
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

			var requestFilterProvider = new ServerRequestTestMessageFilterProvider( 0 );
			requestFilterProvider.FilterApplied +=
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

			var responseFilterProvider = new ServerResponseTestMessageFilterProvider( 1 );
			responseFilterProvider.FilterApplied +=
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

			TestFiltersCore(
				null,
				null,
				( requestData, responseData ) =>
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
		/// <param name="resultAssertion">The assertion logic for the data. The arguments are request data bytes and response bytes.</param>
		/// <param name="providers">The filter providers to be invoked.</param>
		internal static void TestFiltersCore(
			Action<Packer, long> argumentTweak,
			Action<ServerTransport> transportAssertion,
			Action<byte[], byte[]> resultAssertion,
			params MessageFilterProvider[] providers
		)
		{
			var configuration = new RpcServerConfiguration();

			foreach ( var provider in providers )
			{
				configuration.FilterProviders.Add( provider );
			}

			configuration.DispatcherProvider =
				s => new CallbackDispatcher( s, ( id, args ) => args );

			using ( var server = new RpcServer( configuration ) )
			using ( var manager = new InProcServerTransportManager( server, m => new SingletonObjectPool<InProcServerTransport>( new InProcServerTransport( m ) ) ) )
			using ( var target = manager.NewSession() )
			using ( var controller = InProcServerTransportController.Create( manager ) )
			using ( var responseWaitHandle = new ManualResetEventSlim() )
			{
				if ( transportAssertion != null )
				{
					transportAssertion( target );
				}

				byte[] requestData = null;
				byte[] responseData = null;
				controller.Response +=
					( sender, e ) =>
					{
						Interlocked.Exchange( ref responseData, e.Data );
						responseWaitHandle.Set();
					};

				using ( var buffer = new MemoryStream() )
				using ( var requestPacker = Packer.Create( buffer ) )
				{
					requestPacker.PackArrayHeader( 4 );
					requestPacker.Pack( ( int )MessageType.Request );
					requestPacker.Pack( 1 );
					requestPacker.Pack( "Echo" );
					if ( argumentTweak == null )
					{
						requestPacker.PackArrayHeader( 0 );
					}
					else
					{
						argumentTweak( requestPacker, buffer.Length );
					}

					requestData = buffer.ToArray();
					controller.FeedReceiveBuffer( requestData );
				}

				Assert.That( responseWaitHandle.Wait( TimeSpan.FromSeconds( 1 ) ) );

				if ( resultAssertion != null )
				{
					resultAssertion( requestData, responseData );
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

		private sealed class ServerRequestTestMessageFilter : TestMessageFilter<ServerRequestContext>
		{
			public ServerRequestTestMessageFilter( int index, MessageFilteringLocation location ) : base( index, location ) { }
		}

		private sealed class ServerResponseTestMessageFilter : TestMessageFilter<ServerResponseContext>
		{
			public ServerResponseTestMessageFilter( int index, MessageFilteringLocation location ) : base( index, location ) { }
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

		private sealed class ServerRequestTestMessageFilterProvider : TestMessageFilterProvider<ServerRequestContext>
		{
			private readonly int _index;

			public ServerRequestTestMessageFilterProvider( int index )
			{
				this._index = index;
			}

			public override MessageFilter<ServerRequestContext> GetFilter( MessageFilteringLocation location )
			{
				var result = new ServerRequestTestMessageFilter( this._index, location );
				result.FilterApplied += ( sender, e ) => this.OnFilterApplied( e );
				return result;
			}
		}

		private sealed class ServerResponseTestMessageFilterProvider : TestMessageFilterProvider<ServerResponseContext>
		{
			private readonly int _index;

			public ServerResponseTestMessageFilterProvider( int index )
			{
				this._index = index;
			}

			public override MessageFilter<ServerResponseContext> GetFilter( MessageFilteringLocation location )
			{
				var result = new ServerResponseTestMessageFilter( this._index, location );
				result.FilterApplied += ( sender, e ) => this.OnFilterApplied( e );
				return result;
			}
		}

		#endregion
	}
}
