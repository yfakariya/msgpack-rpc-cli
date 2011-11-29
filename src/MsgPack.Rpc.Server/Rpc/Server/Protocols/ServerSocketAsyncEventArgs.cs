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
using System.Net.Sockets;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Encapselates low-level transportation context which uses async socket.
	/// </summary>
	public sealed class ServerSocketAsyncEventArgs : SocketAsyncEventArgs
	{
		private Action<ServerSocketAsyncEventArgs> _onAcceptted;
		private Action<ServerSocketAsyncEventArgs> _onReceived;
		private Action<SocketAsyncOperation, SocketError> _onError;

		public SocketAsyncEventArgs SendingContext { get; internal set; }

		internal ServerSocketAsyncEventArgs( Action<ServerSocketAsyncEventArgs> onAcceptted, Action<ServerSocketAsyncEventArgs> onReceived, Action<SocketAsyncOperation, SocketError> onError )
			: base()
		{
			this._onAcceptted = onAcceptted;
			this._onReceived = onReceived;
			this._onError = onError;
		}

		private IList<ArraySegment<byte>> _receivingBuffer = new List<ArraySegment<byte>>();

		public IEnumerable<byte> ReadReceivingBuffer()
		{
			foreach ( var segment in this._receivingBuffer )
			{
				for ( int i = 0; i < segment.Count; i++ )
				{
					yield return segment.Array[ segment.Offset + i ];
				}
			}
		}

		internal void SetReceivingBuffer( byte[] buffer, int offset, int count )
		{
			this.ResetReceivingBuffer();
			this.AppendRecivingBuffer( buffer, offset, count );
		}

		internal void ResetReceivingBuffer()
		{
			this._receivingBuffer.Clear();
		}

		internal void AppendRecivingBuffer( byte[] buffer, int offset, int count )
		{
			this._receivingBuffer.Add( new ArraySegment<byte>( buffer, offset, count ) );
		}
	}

}
