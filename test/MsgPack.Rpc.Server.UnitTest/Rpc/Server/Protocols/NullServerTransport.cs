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

namespace MsgPack.Rpc.Server.Protocols
{
	/// <summary>
	///		The null object of <see cref="ServerTransport"/> class.
	/// </summary>
	internal sealed class NullServerTransport : ServerTransport
	{
		protected override bool CanResumeReceiving
		{
			get { return true; }
		}

		/// <summary>
		///		Occurs when response is sent.
		/// </summary>
		public event EventHandler<SentEventArgs> Sent;

		/// <summary>
		/// Initializes a new instance of the <see cref="NullServerTransport"/> class.
		/// </summary>
		/// <param name="manager">The manager.</param>
		public NullServerTransport( ServerTransportManager<NullServerTransport> manager ) : base( manager ) { }

		/// <summary>
		/// Performs protocol specific asynchronous 'Receive' operation.
		/// </summary>
		/// <param name="context">Context information.</param>
		protected override void ReceiveCore( ServerRequestContext context )
		{
			return;
		}

		/// <summary>
		/// Performs protocol specific asynchronous 'Send' operation.
		/// </summary>
		/// <param name="context">Context information.</param>
		protected override void SendCore( ServerResponseContext context )
		{
			var handler = this.Sent;
			if ( handler != null )
			{
				handler( this, new SentEventArgs( context ) );
			}

			return;
		}

		/// <summary>
		///		Contains event data for <see cref="Sent"/> event.
		/// </summary>
		public sealed class SentEventArgs : EventArgs
		{
			private readonly ServerResponseContext _context;

			/// <summary>
			///		Gets the <see cref="ServerResponseContext"/> to be sent.
			/// </summary>
			/// <value>
			///		The <see cref="ServerResponseContext"/> to be sent.
			/// </value>
			public ServerResponseContext Context
			{
				get { return this._context; }
			}

			internal SentEventArgs( ServerResponseContext context )
			{
				this._context = context;
			}
		}
	}
}
