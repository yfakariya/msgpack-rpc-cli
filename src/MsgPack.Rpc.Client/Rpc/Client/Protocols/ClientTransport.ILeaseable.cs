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
using System.Threading;

namespace MsgPack.Rpc.Client.Protocols
{
	partial class ClientTransport : ILeaseable<ClientTransport>
	{
		private ILease<ClientTransport> _lease;

		void ILeaseable<ClientTransport>.SetLease( ILease<ClientTransport> lease )
		{
			this.SetLease( lease );
		}

		protected void SetLease( ILease<ClientTransport> lease )
		{
			this._lease = lease;
		}

		private void DisposeLease()
		{
			var lease = Interlocked.Exchange( ref this._lease, null );
			if ( lease != null )
			{
				lease.Dispose();
			}
		}
	}
}
