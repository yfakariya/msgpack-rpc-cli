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
using MsgPack.Rpc.Diagnostics;

namespace MsgPack.Rpc.Protocols.Filters
{
	/// <summary>
	///		Implements common functionalities of providers for <see cref="StreamLoggingMessageFilter{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of <see cref="InboundMessageContext"/>.</typeparam>
	public abstract class StreamLoggingMessageFilterProvider<T> : MessageFilterProvider<T>
		where T : InboundMessageContext
	{
		private readonly IMessagePackStreamLogger _logger;

		/// <summary>
		///		Gets the logger which is the <see cref="IMessagePackStreamLogger"/> which will be log sink.
		/// </summary>
		/// <value>
		///		The logger which is the <see cref="IMessagePackStreamLogger"/> which will be log sink.
		/// </value>
		protected IMessagePackStreamLogger Logger
		{
			get { return this._logger; }
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="StreamLoggingMessageFilter&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="logger">The <see cref="IMessagePackStreamLogger"/> which will be log sink.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="logger"/> is <c>null</c>.
		/// </exception>
		protected StreamLoggingMessageFilterProvider( IMessagePackStreamLogger logger )
		{
			if ( logger == null )
			{
				throw new ArgumentNullException( "logger" );
			}

			Contract.EndContractBlock();

			this._logger = logger;
		}
	}
}
