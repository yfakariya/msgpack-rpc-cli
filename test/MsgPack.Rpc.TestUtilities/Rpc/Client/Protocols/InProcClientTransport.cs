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

namespace MsgPack.Rpc.Client.Protocols
{
	/// <summary>
	///		Implements <see cref="ClientTransport"/> with in-proc method invocation.
	/// </summary>
	public sealed class InProcClientTransport : ClientTransport
	{
		private readonly InProcClientTransportManager _manager;

		/// <summary>
		///		Initializes a new instance of the <see cref="InProcClientTransport"/> class.
		/// </summary>
		/// <param name="manager">The manager which will manage this instance.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="manager"/> is <c>null</c>.
		/// </exception>
		public InProcClientTransport( InProcClientTransportManager manager )
			: base( manager )
		{
			this._manager = manager;
		}

		protected sealed override void SendCore( ClientRequestContext context )
		{
			this._manager.Send( context );
			this.OnSent( context );
		}

		protected sealed override void ReceiveCore( ClientResponseContext context )
		{
			this._manager.ReceiveAsync( context ).ContinueWith( previous =>
				{
					previous.Dispose();
					this.OnReceived( context );
				}
			);
		}
	}
}
