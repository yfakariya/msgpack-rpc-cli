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

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Defines interfaces of low-level transportation context which uses some Berkley-Socket.
	/// </summary>
	public struct ServerSessionContext
	{
		private readonly ServerSocketAsyncEventArgs _underlying;

		public bool IsValid
		{
			get { return this._underlying != null; }
		}

		internal ServerSocketAsyncEventArgs Underlying
		{
			get { return this._underlying; }
		}

		internal ServerSessionContext( ServerSocketAsyncEventArgs underlying )
		{
			this._underlying = underlying;
		}

		public IEnumerable<byte> ReadReceivingBuffer()
		{
			return this._underlying.ReadReceivingBuffer();
		}
	}
}
