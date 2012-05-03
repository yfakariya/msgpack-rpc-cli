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
using System.Diagnostics.Contracts;
using System.Linq;
using MsgPack.Rpc.Diagnostics;

namespace MsgPack.Rpc.Protocols.Filters
{
	/// <summary>
	///		Implements common functionalities of inbound message stream logging filter.
	/// </summary>
	/// <typeparam name="T">The type of <see cref="InboundMessageContext"/>.</typeparam>
	public abstract class StreamLoggingMessageFilter<T> : MessageFilter<T>
		where T : InboundMessageContext
	{
		private readonly IMessagePackStreamLogger _logger;

		/// <summary>
		///		Initializes a new instance of the <see cref="StreamLoggingMessageFilter&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="logger">The <see cref="IMessagePackStreamLogger"/> which will be log sink.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="logger"/> is <c>null</c>.
		/// </exception>
		protected StreamLoggingMessageFilter( IMessagePackStreamLogger logger )
		{
			if ( logger == null )
			{
				throw new ArgumentNullException( "logger" );
			}

			Contract.EndContractBlock();

			this._logger = logger;
		}

		/// <summary>
		///		Applies this filter to the specified message.
		/// </summary>
		/// <param name="context">The message context. This value is not <c>null</c>.</param>
		protected override void ProcessMessageCore( T context )
		{
			this._logger.Write( context.SessionStartedAt, context.RemoteEndPoint, context.ReceivedData.SelectMany( s => s.AsEnumerable() ) );
		}
	}
}
