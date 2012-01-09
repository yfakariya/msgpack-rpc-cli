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
using System.Threading.Tasks;

namespace MsgPack.Rpc.Server.Protocols
{
	/// <summary>
	///		Implements <see cref="ServerTransport"/> with in-proc method invocation.
	/// </summary>
	public sealed class InProcServerTransport : ServerTransport, ILeaseable<InProcServerTransport>
	{
		private readonly InProcServerTransportManager _manager;

		/// <summary>
		///		Initializes a new instance of the <see cref="InProcServerTransport"/> class.
		/// </summary>
		/// <param name="manager">The manager which will manage this instance.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="manager"/> is <c>null</c>.
		/// </exception>
		public InProcServerTransport( InProcServerTransportManager manager )
			: base( manager )
		{
			this._manager = manager;
		}

		protected override void ReceiveCore( ServerRequestContext context )
		{
			this._manager.Receive( context );
			this.OnReceived( context );
		}

		protected override void SendCore( ServerResponseContext context )
		{
			using ( Task task = this._manager.SendAsync( context ) )
			{
				this.OnSent( context );
				task.Wait( this._manager.CancellationToken );
			}
		}

		void ILeaseable<InProcServerTransport>.SetLease( ILease<InProcServerTransport> lease )
		{
			base.SetLease( lease );
		}
	}
}
