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
using System.Net.Sockets;
using NUnit.Framework;

namespace MsgPack.Rpc.Protocols
{
	// These tests ensures backward compatibility and prevents regression.

	[TestFixture]
	public class SocketErrorCodeExtensionTest
	{
		private static readonly HashSet<SocketError> _caseByCases =
			new HashSet<SocketError>()
			{
				SocketError.AlreadyInProgress,
				SocketError.Disconnecting,
				SocketError.IsConnected,
				SocketError.Shutdown,
			};

		private static readonly HashSet<SocketError> _noProblems =
			new HashSet<SocketError>()
				{
					SocketError.InProgress,
					SocketError.Interrupted,
					SocketError.IOPending,
					SocketError.OperationAborted,
					SocketError.Success,
					SocketError.WouldBlock,
				};

		private static readonly Dictionary<SocketError, RpcError> _rpcErrorMapping =
			new Dictionary<SocketError, RpcError>()
			{
				{ SocketError.AccessDenied, RpcError.TransportError },
				{ SocketError.AddressAlreadyInUse, RpcError.TransportError },
				{ SocketError.AddressFamilyNotSupported, RpcError.TransportError },
				{ SocketError.AddressNotAvailable, RpcError.TransportError },
				{ SocketError.AlreadyInProgress, RpcError.TransportError },
				{ SocketError.ConnectionAborted, RpcError.TransportError },
				{ SocketError.ConnectionRefused, RpcError.ConnectionRefusedError },
				{ SocketError.ConnectionReset, RpcError.TransportError },
				{ SocketError.DestinationAddressRequired, RpcError.TransportError },
				{ SocketError.Disconnecting, RpcError.TransportError },
				{ SocketError.Fault, RpcError.TransportError },
				{ SocketError.HostDown, RpcError.TransportError },
				{ SocketError.HostNotFound, RpcError.NetworkUnreacheableError },
				{ SocketError.HostUnreachable, RpcError.NetworkUnreacheableError },
				{ SocketError.InProgress, RpcError.TransportError },
				{ SocketError.Interrupted, RpcError.TransportError },
				{ SocketError.InvalidArgument, RpcError.TransportError },
				{ SocketError.IOPending, RpcError.TransportError },
				{ SocketError.IsConnected, RpcError.TransportError },
				{ SocketError.MessageSize, RpcError.MessageTooLargeError },
				{ SocketError.NetworkDown, RpcError.TransportError },
				{ SocketError.NetworkReset, RpcError.TransportError },
				{ SocketError.NetworkUnreachable, RpcError.NetworkUnreacheableError },
				{ SocketError.NoBufferSpaceAvailable, RpcError.TransportError },
				{ SocketError.NoData, RpcError.TransportError },
				{ SocketError.NoRecovery, RpcError.TransportError },
				{ SocketError.NotConnected, RpcError.TransportError },
				{ SocketError.NotInitialized, RpcError.TransportError },
				{ SocketError.NotSocket, RpcError.TransportError },
				{ SocketError.OperationAborted, RpcError.TransportError },
				{ SocketError.OperationNotSupported, RpcError.TransportError },
				{ SocketError.ProcessLimit, RpcError.TransportError },
				{ SocketError.ProtocolFamilyNotSupported, RpcError.TransportError },
				{ SocketError.ProtocolNotSupported, RpcError.TransportError },
				{ SocketError.ProtocolOption, RpcError.TransportError },
				{ SocketError.ProtocolType, RpcError.TransportError },
				{ SocketError.Shutdown, RpcError.TransportError },
				{ SocketError.SocketError, RpcError.TransportError },
				{ SocketError.SocketNotSupported, RpcError.TransportError },
				{ SocketError.Success, RpcError.TransportError },
				{ SocketError.SystemNotReady, RpcError.TransportError },
				{ SocketError.TimedOut, RpcError.ConnectionTimeoutError },
				{ SocketError.TooManyOpenSockets, RpcError.TransportError },
				{ SocketError.TryAgain, RpcError.TransportError },
				{ SocketError.TypeNotFound, RpcError.TransportError },
				{ SocketError.VersionNotSupported, RpcError.TransportError },
				{ SocketError.WouldBlock, RpcError.TransportError },
			};

		[Test()]
		public void TestIsError()
		{
			foreach ( SocketError value in Enum.GetValues( typeof( SocketError ) ) )
			{
				var expected = _caseByCases.Contains( value ) ? default( bool? ) : _noProblems.Contains( value ) ? false : true;
				var actual = value.IsError();
				Assert.AreEqual( expected, actual, value.ToString() );
			}
		}

		[Test()]
		public void TestToRpcError()
		{
			foreach ( SocketError value in Enum.GetValues( typeof( SocketError ) ) )
			{
				RpcError expected;
				if ( value.IsError().GetValueOrDefault() )
				{
					Assert.IsTrue( _rpcErrorMapping.TryGetValue( value, out expected ), "No Mapping" );
				}
				else
				{
					expected = null;
				}

				var actual = value.ToRpcError();
				Assert.AreEqual( expected, actual, value.ToString() );
			}
		}
	}
}
