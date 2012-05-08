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
	///		Null object for the <see cref="ClientTransport"/>.
	/// </summary>
	internal sealed class NullClientTransport : ClientTransport
	{
		protected override bool CanResumeReceiving
		{
			get { return true; }
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="NullClientTransport"/> class.
		/// </summary>
		/// <param name="manager">The manager which will manage this instance.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="manager"/> is <c>null</c>.
		/// </exception>
		public NullClientTransport( ClientTransportManager<NullClientTransport> manager ) : base( manager ) { }

		/// <summary>
		///		Performs protocol specific asynchronous 'Send' operation.
		/// </summary>
		/// <param name="context">Context information.</param>
		protected override void SendCore( ClientRequestContext context )
		{
			this.OnSent( context );
		}

		/// <summary>
		///		Performs protocol specific asynchronous 'Receive' operation.
		/// </summary>
		/// <param name="context">Context information.</param>
		protected override void ReceiveCore( ClientResponseContext context )
		{
			this.OnReceived( context );
		}
	}
}
