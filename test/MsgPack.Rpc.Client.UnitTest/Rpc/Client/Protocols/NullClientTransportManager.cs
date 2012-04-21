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
using System.Threading.Tasks;

namespace MsgPack.Rpc.Client.Protocols
{
	/// <summary>
	///		Null object for <see cref="ClientTransportManager{T}"/>.
	/// </summary>
	internal sealed class NullClientTransportManager : ClientTransportManager<NullClientTransport>
	{
		/// <summary>
		///		Initializes a new instance of the <see cref="NullClientTransportManager"/> class.
		/// </summary>
		public NullClientTransportManager() : base( RpcClientConfiguration.Default ) { }

		/// <summary>
		///		Establishes logical connection, which specified to the managed transport protocol, for the server.
		/// </summary>
		/// <param name="targetEndPoint">The end point of target server.</param>
		/// <returns>
		///		<see cref="Task{T}"/> of <see cref="ClientTransport"/> which represents asynchronous establishment process
		///		specific to the managed transport.
		///		This value will not be <c>null</c>.
		/// </returns>
		protected override Task<ClientTransport> ConnectAsyncCore( EndPoint targetEndPoint )
		{
			return Task.Factory.StartNew( () => new NullClientTransport( this ) as ClientTransport );
		}
	}
}
