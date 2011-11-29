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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Define basic features and interfaces for every factory of <see cref="ClientEventLoop"/>.
	/// </summary>
	public abstract class ClientEventLoopFactory
	{
		/// <summary>
		///		Create <see cref="ClientEventLoop"/> with specified options.
		/// </summary>
		/// <param name="options">Client side RPC options. This value can be null.</param>
		/// <param name="errorHandler">Aggreated error handler which will be associated to creating event loop. This value can be null.</param>
		/// <param name="cancellationTokenSource"><see cref="CancellationTokenSource"/> to cancel asynchronous operation. This value can be null.</param>
		/// <returns><see cref="ClientEventLoop"/>, which is specific to concrete factory class.</returns>
		public ClientEventLoop Create( RpcClientOptions options, EventHandler<RpcTransportErrorEventArgs> errorHandler, CancellationTokenSource cancellationTokenSource )
		{
			return this.CreateCore( options, errorHandler, cancellationTokenSource );
		}

		/// <summary>
		///		Create <see cref="ClientEventLoop"/> with specified options.
		/// </summary>
		/// <param name="options">Client side RPC options. This value may be null.</param>
		/// <param name="errorHandler">Aggreated error handler which will be associated to creating event loop. This value may be null.</param>
		/// <param name="cancellationTokenSource"><see cref="CancellationTokenSource"/> to cancel asynchronous operation. This value may be null.</param>
		/// <returns><see cref="ClientEventLoop"/>, which is specific to concrete factory class.</returns>
		protected abstract ClientEventLoop CreateCore( RpcClientOptions options, EventHandler<RpcTransportErrorEventArgs> errorHandler, CancellationTokenSource cancellationTokenSource );
	}

}
