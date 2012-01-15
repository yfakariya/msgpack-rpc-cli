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
using System.Net.Sockets;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Defines common interface for the transports which is bindable to the message context and async socket.
	/// </summary>
	[ContractClass( typeof( IContextBoundableTransportContract ) )]
	internal interface IContextBoundableTransport
	{
		/// <summary>
		///		Gets the bound socket.
		/// </summary>
		Socket BoundSocket { get; }

		/// <summary>
		///		Called when the async socket operation is completed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.Net.Sockets.SocketAsyncEventArgs"/> instance containing the event data.</param>
		void OnSocketOperationCompleted( object sender, SocketAsyncEventArgs e );
	}

	[ContractClassFor( typeof( IContextBoundableTransport ) )]
	internal abstract class IContextBoundableTransportContract : IContextBoundableTransport
	{
		public Socket BoundSocket
		{
			get { return null; } // No contract
		}

		public void OnSocketOperationCompleted( object sender, SocketAsyncEventArgs e )
		{
			Contract.Requires( sender != null );
			Contract.Requires( e != null );
		}
	}

}
