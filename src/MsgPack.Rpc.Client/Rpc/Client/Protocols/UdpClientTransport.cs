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
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using MsgPack.Rpc.Serialization;
using MsgPack.Collections;

namespace MsgPack.Rpc.Protocols
{
	// TODO: Extract abstract class and implemnt UDP Transport.
	/// <summary>
	///		TCP binding of <see cref="IClinentTransport"/>.
	/// </summary>
	internal sealed class UdpClientTransport : ClientTransport
	{
		private readonly EndPoint _remoteEndPoint;
		private readonly RpcSocket _socket;

		public UdpClientTransport( RpcSocket socket, EndPoint remoteEndPoint, ClientEventLoop eventLoop, RpcClientOptions options )
			: base( socket == null ? RpcTransportProtocol.UdpIp : socket.Protocol, eventLoop, options )
		{
			if ( socket == null )
			{
				throw new ArgumentNullException( "socket" );
			}

			if ( socket.Protocol.ProtocolType != ProtocolType.Udp )
			{
				throw new ArgumentException( "socket must be connected TCP socket.", "socket" );
			}

			if ( remoteEndPoint == null )
			{
				throw new ArgumentNullException( "remoteEndPoint" );
			}

			Contract.EndContractBlock();

			this._socket = socket;
			this._remoteEndPoint = remoteEndPoint;
		}

		protected sealed override void Dispose( bool disposing )
		{
			this._socket.Dispose();
			base.Dispose( disposing );
		}

		protected sealed override SendingContext CreateNewSendingContext( int? messageId, Action<SendingContext, Exception, bool> onMessageSent )
		{
			// TODO: cache buffer
			return
				new SendingContext(
					new ClientSessionContext(
						this,
						this.Options,
						this.EventLoop.CreateSocketContext( this._remoteEndPoint )
					),
					new RpcOutputBuffer( ChunkBuffer.CreateDefault( this.InitialSegmentCount, this.InitialSegmentSize ) ),
					null,
					onMessageSent
				);
		}
		
		protected sealed override void SendCore( SendingContext context )
		{
			this.EventLoop.SendTo( context );
			throw new NotImplementedException();
			//this.EventLoop.ReceiveFrom()
		}

		// FIXME: Dispose session context in OnDent
	}
}
