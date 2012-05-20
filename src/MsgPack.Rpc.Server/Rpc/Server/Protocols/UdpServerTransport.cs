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
using System.Threading.Tasks;

namespace MsgPack.Rpc.Server.Protocols
{
	/// <summary>
	///		<see cref="ServerTransport"/> implementation for UDP/IP.
	/// </summary>
	public sealed class UdpServerTransport : ServerTransport
	{
		/// <summary>
		/// Gets a value indicating whether this instance can resume receiving.
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
		/// 	This implementation returns <c>false</c>.
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
		///		Initializes a new instance of the <see cref="UdpServerTransport"/> class.
		/// </summary>
		/// <param name="manager">The manager which will manage this instance.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="manager"/> is <c>null</c>.
		/// </exception>
		public UdpServerTransport( UdpServerTransportManager manager ) : base( manager ) { }

		/// <summary>
		///		Performs protocol specific asynchronous 'Send' operation.
		/// </summary>
		/// <param name="context">Context information.</param>
		protected sealed override void SendCore( ServerResponseContext context )
		{
			// Manager stores the socket which is dedicated socket to this transport in the AcceptSocket property.
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
		protected sealed override void ReceiveCore( ServerRequestContext context )
		{
			// Manager stores the socket which is dedicated socket to this transport in the AcceptSocket property.
			bool isAsyncOperationStarted;
			try
			{
				isAsyncOperationStarted = this.BoundSocket.ReceiveFromAsync( context.SocketContext );
			}
			catch( ObjectDisposedException )
			{
				// Canceled.
				return;
			}

			if ( !isAsyncOperationStarted )
			{
				context.SetCompletedSynchronously();
				this.OnReceived( context );
			}
		}

		/// <summary>
		///		Called when asynchronous 'Receive' operation is completed.
		/// </summary>
		/// <param name="context">Context information.</param>
		///	<exception cref="InvalidOperationException">
		///		This instance is not in 'Idle' nor 'Receiving' state.
		///	</exception>
		///	<exception cref="ObjectDisposedException">
		///		This instance is disposed.
		///	</exception>
		protected override void OnReceived( ServerRequestContext context )
		{
			Task.Factory.StartNew(
				state => base.OnReceived( state as ServerRequestContext ),
				context
			);
		}
	}
}
