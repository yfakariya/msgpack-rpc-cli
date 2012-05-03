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

namespace MsgPack.Rpc.Protocols.Filters
{
	/// <summary>
	///		Defines interfaces of the message filter.
	/// </summary>
	/// <remarks>
	///		Message filters intercept serialization/deserialization pipeline of the RPC runtime,
	///		applies own custom logic mainly inspection like auditing or data stream tweaking like compression.
	/// </remarks>
	/// <typeparam name="T"></typeparam>
	public abstract class MessageFilter<T>
		where T : MessageContext
	{
		/// <summary>
		///		Initializes a new instance of the <see cref="MessageFilter&lt;T&gt;"/> class.
		/// </summary>
		protected MessageFilter() { }

		/// <summary>
		///		Applies this filter to the specified message.
		/// </summary>
		/// <param name="context">The message context.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="context"/> is <c>null</c>.
		/// </exception>
		public void ProcessMessage( T context )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			Contract.EndContractBlock();

			this.ProcessMessageCore( context );
		}

		/// <summary>
		///		Applies this filter to the specified message.
		/// </summary>
		/// <param name="context">The message context. This value is not <c>null</c>.</param>
		protected abstract void ProcessMessageCore( T context );
	}
}
