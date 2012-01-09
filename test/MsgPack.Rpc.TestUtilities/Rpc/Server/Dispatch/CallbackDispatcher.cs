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
using System.Linq;
using System.Threading.Tasks;
using MsgPack.Rpc.Server.Protocols;
using MsgPack.Serialization;

namespace MsgPack.Rpc.Server.Dispatch
{
	/// <summary>
	///		Callback based <see cref="Dispacther"/> implementation for testing purposes.
	/// </summary>
	public sealed class CallbackDispatcher : Dispatcher
	{
		private static readonly SerializationContext _serializationContext = new SerializationContext();
		private readonly Func<int?, MessagePackObject[], MessagePackObject> _callback;

		/// <summary>
		/// Initializes a new instance of the <see cref="CallbackDispatcher"/> class.
		/// </summary>
		/// <param name="server">The server.</param>
		/// <param name="callback">The callback.</param>
		public CallbackDispatcher( RpcServer server, Func<int?, MessagePackObject[], MessagePackObject> callback )
			: base( server )
		{
			this._callback = callback;
		}

		protected sealed override Func<ServerRequestContext, ServerResponseContext, Task> Dispatch( string methodName )
		{
			// Ignore methodName
			return
				( requestContext, responseContext ) =>
				{
					MessagePackObject[] args = MessagePackSerializer.Create<MessagePackObject[]>( _serializationContext ).UnpackFrom( requestContext.ArgumentsUnpacker );
					var messageId = requestContext.MessageId;

					return
						Task.Factory.StartNew(
							() =>
							{
								MessagePackObject returnValue;
								try
								{
									returnValue = this._callback( messageId, args );
								}
								catch ( Exception exception )
								{
									base.SetException( responseContext, exception );
									return;
								}

								base.SetReturnValue( responseContext, returnValue );
							}
						);
				};
		}
	}
}
