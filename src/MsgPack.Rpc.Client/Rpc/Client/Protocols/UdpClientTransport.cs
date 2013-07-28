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
using System.Net;

namespace MsgPack.Rpc.Client.Protocols
{
	/// <summary>
	///		Implements <see cref="ClientTransport"/> for UDP/IP protocol.
	/// </summary>
	public sealed class UdpClientTransport : ClientTransport
	{
		/// <summary>
		///		Gets the remote end point.
		/// </summary>
		/// <value>
		///		The remote end point.
		/// </value>
		public EndPoint RemoteEndPoint { get; internal set; }

		/// <summary>
		/// Gets a value indicating whether the protocol used by this class can resume receiving.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance can resume receiving; otherwise, <c>false</c>.
		/// </value>
		protected override bool CanResumeReceiving
		{
			get { return false; }
		}

#if MONO
		/// <summary>
		///		Gets a value indicating whether the underlying transport used by this instance can accept chunked buffer.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if the underlying transport can use chunked buffer; otherwise, <c>false</c>.
		///		This implementation returns <c>false</c>.
		/// </value>
		protected override bool CanUseChunkedBuffer
		{
			get
			{
				return false;
			}
		}
#endif

		/// <summary>
		///		Initializes a new instance of the <see cref="UdpClientTransport"/> class.
		/// </summary>
		/// <param name="manager">The manager which will manage this instance.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="manager"/> is <c>null</c>.
		/// </exception>
		public UdpClientTransport( UdpClientTransportManager manager ) : base( manager ) { }

		/// <summary>
		///		Gets the <see cref="ClientRequestContext"/> to store context information for request or notification.
		/// </summary>
		/// <returns>
		///		The <see cref="ClientRequestContext"/> to store context information for request or notification.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		///		This object is not ready to invoke this method.
		/// </exception>
		public sealed override ClientRequestContext GetClientRequestContext()
		{
			if ( this.RemoteEndPoint == null )
			{
				throw new InvalidOperationException( "RemoteEndPoint must be set. UdpClientTransport must be retrieved from UdpTClientransportManager.GetTransport." );
			}

			var result = base.GetClientRequestContext();
			result.RemoteEndPoint = this.RemoteEndPoint;
			return result;
		}

		/// <summary>
		///		Performs protocol specific asynchronous 'Send' operation.
		/// </summary>
		/// <param name="context">Context information.</param>
		protected sealed override void SendCore( ClientRequestContext context )
		{
			if ( !this.BoundSocket.SendToAsync( context.SocketContext ) )
			{
				context.SetCompletedSynchronously();
				this.OnSent( context );
			}
		}

		/// <summary>
		///		Performs protocol specific asynchronous 'Receive' operation.
		/// </summary>
		/// <param name="context">Context information.</param>
		protected sealed override void ReceiveCore( ClientResponseContext context )
		{
			if ( !this.BoundSocket.ReceiveFromAsync( context.SocketContext ) )
			{
				context.SetCompletedSynchronously();
				this.OnReceived( context );
			}
		}
	}
}
