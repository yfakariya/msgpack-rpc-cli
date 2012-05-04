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
using MsgPack.Rpc.Protocols;
using MsgPack.Rpc.Server.Protocols;
using MsgPack.Serialization;
using System.Threading;

namespace MsgPack.Rpc.Server.Dispatch
{
	/// <summary>
	///		Callback based <see cref="Dispacther"/> implementation for testing purposes.
	/// </summary>
	public sealed class CallbackDispatcher : Dispatcher
	{
		private static readonly SerializationContext _serializationContext = new SerializationContext();
		private readonly Func<string, int?, MessagePackObject[], MessagePackObject> _dispatch;

		/// <summary>
		/// Initializes a new instance of the <see cref="CallbackDispatcher"/> class.
		/// </summary>
		/// <param name="server">The server.</param>
		/// <param name="dispatch">The callback with method name.</param>
		public CallbackDispatcher( RpcServer server, Func<string, int?, MessagePackObject[], MessagePackObject> dispatch )
			: base( server )
		{
			this._dispatch = dispatch;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CallbackDispatcher"/> class.
		/// </summary>
		/// <param name="server">The server.</param>
		/// <param name="callback">The callback without method name.</param>
		public CallbackDispatcher( RpcServer server, Func<int?, MessagePackObject[], MessagePackObject> callback )
			: base( server )
		{
			this._dispatch = ( method, id, args ) => callback( id, args );
		}

		protected sealed override Func<ServerRequestContext, ServerResponseContext, Task> Dispatch( string methodName )
		{
			// Ignore methodName
			return
				( requestContext, responseContext ) =>
				{
					var argumentsUnpacker = requestContext.ArgumentsUnpacker;
					argumentsUnpacker.Read();
					MessagePackObject[] args = MessagePackSerializer.Create<MessagePackObject[]>( _serializationContext ).UnpackFrom( argumentsUnpacker );
					var messageId = requestContext.MessageId;

					return
						Task.Factory.StartNew(
							() =>
							{
								MessagePackObject returnValue;

								try
								{
									this.BeginOperation();
									try
									{
										returnValue = this._dispatch( methodName, messageId, args );
									}
									catch ( ThreadAbortException ex )
									{
										this.HandleThreadAbortException( ex );
										returnValue = MessagePackObject.Nil;
									}
									finally
									{
										this.EndOperation();
									}
								}
								catch ( Exception exception )
								{
									if ( responseContext != null )
									{
										base.SetException( responseContext, methodName, exception );
									}
									else
									{
										// notification
										InvocationHelper.HandleInvocationException( requestContext.SessionId, MessageType.Notification, requestContext.MessageId, requestContext.MethodName, exception, this.IsDebugMode );
									}
									return;
								}

								if ( responseContext != null )
								{
									base.SetReturnValue( responseContext, returnValue );
								}
							}
						);
				};
		}
	}
}
