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
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MsgPack.Rpc.Server.Protocols;

namespace MsgPack.Rpc.Client.Protocols
{
	/// <summary>
	///		Implements <see cref="ClientTransportManager{T}"/> for <see cref="InProcClientTransport"/>.
	/// </summary>
	/// <remarks>
	///		This transport only support one session per manager.
	/// </remarks>
	public sealed class InProcClientTransportManager : ClientTransportManager<InProcClientTransport>
	{
		/// <summary>
		///		The target server to be invoked.
		/// </summary>
		private readonly InProcServerTransportManager _target;

		/// <summary>
		///		<see cref="CancellationTokenSource"/> to process cancellation.
		/// </summary>
		private readonly CancellationTokenSource _cancellationTokenSource;

		/// <summary>
		///		Gets the cancellation token to subscribe cancellation.
		/// </summary>
		/// <value>
		///		The cancellation token to subscribe cancellation.
		/// </value>
		public CancellationToken CancellationToken
		{
			get { return this._cancellationTokenSource.Token; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="InProcClientTransportManager"/> class.
		/// </summary>
		/// <param name="configuration">
		///		The <see cref="RpcClientConfiguration"/> which contains various configuration information.
		/// </param>
		/// <param name="target">
		///		The target server to be invoked.
		///	</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="configuration"/> is <c>null</c>.
		///		Or, <paramref name="target"/> is <c>null</c>.
		/// </exception>
		public InProcClientTransportManager( RpcClientConfiguration configuration, InProcServerTransportManager target )
			: base( configuration )
		{
			if ( target == null )
			{
				throw new ArgumentNullException( "target" );
			}

			this._target = target;
			this._cancellationTokenSource = new CancellationTokenSource();
			this.SetTransportPool( new OnTheFlyObjectPool<InProcClientTransport>( conf => new InProcClientTransport( this ), new ObjectPoolConfiguration() ) );
		}

		protected sealed override void Dispose( bool disposing )
		{
			this._target.Dispose();
			if ( !this._cancellationTokenSource.IsCancellationRequested )
			{
				this._cancellationTokenSource.Cancel();
			}

			this._cancellationTokenSource.Dispose();
			base.Dispose( disposing );
		}

		protected sealed override void BeginShutdownCore()
		{
			this._cancellationTokenSource.Cancel();
			base.BeginShutdownCore();
		}

		protected override Task<ClientTransport> ConnectAsyncCore( EndPoint targetEndPoint )
		{
			return Task.Factory.StartNew( () => this.GetTransport( null ) as ClientTransport );
		}

		protected sealed override InProcClientTransport GetTransportCore( Socket bindingSocket )
		{
			var result = base.GetTransportCore( bindingSocket );
			result.SetDestination( this._target.NewSession() );
			return result;
		}

		protected sealed override void ReturnTransportCore( InProcClientTransport transport )
		{
			transport.Dispose();
		}
	}
}
