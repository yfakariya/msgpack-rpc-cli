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
using System.Threading;
using System.Net;

namespace MsgPack.Rpc.Protocols
{
	// TODO: Extract abstract class and implemnt UDP Transport.
	/// <summary>
	///		TCP binding of <see cref="IClinentTransport"/>.
	/// </summary>
	internal sealed class TcpClientTransport : ConnectionOrientedClientTransport
	{
		public TcpClientTransport( EndPoint remoteEndPoint, RpcTransportProtocol protocol, ClientEventLoop eventLoop, RpcClientOptions options )
			: base( remoteEndPoint, protocol, eventLoop, options )
		{
			if ( protocol.ProtocolType != ProtocolType.Tcp )
			{
				throw new ArgumentException( "socket must be connected TCP socket.", "protocol" );
			}

			Contract.EndContractBlock();
		}
		
		protected sealed override void SendCore( SendingContext context )
		{
			this.EventLoop.Send( context );
		}
	}
}
