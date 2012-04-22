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
using System.Diagnostics.Contracts;
using System.Net;

namespace MsgPack.Rpc.Server.Protocols
{
	/// <summary>
	///		Contains event data for client shutdown detection event.
	/// </summary>
	public class ClientShutdownEventArgs : EventArgs
	{
		private readonly ServerTransport _transport;

		/// <summary>
		///		Gets the transport which detects client shutdown.
		/// </summary>
		/// <value>
		///		The transport which detects client shutdown.
		/// </value>
		public ServerTransport Transport
		{
			get { return this._transport; }
		}

		private readonly EndPoint _clientEndPoint;

		/// <summary>
		///		Gets the client <see cref="EndPoint"/>.
		/// </summary>
		/// <value>
		///		The client <see cref="EndPoint"/>. This value can be <c>null</c>.
		/// </value>
		public EndPoint ClientEndPoint
		{
			get { return this._clientEndPoint; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ClientShutdownEventArgs"/> class.
		/// </summary>
		/// <param name="transport">The transport which detects client shutdown.</param>
		/// <param name="clientEndPoint">The client <see cref="EndPoint"/>.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="transport"/> is <c>null</c>.
		/// </exception>
		public ClientShutdownEventArgs( ServerTransport transport, EndPoint clientEndPoint )
		{
			if ( transport == null )
			{
				throw new ArgumentNullException( "transport" );
			}

			Contract.EndContractBlock();

			this._transport = transport;
			this._clientEndPoint = clientEndPoint;
		}
	}
}
