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
using System.IO;
using System.Linq;
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc.Client.Protocols
{
	/// <summary>
	///		Represents context information for the client side response.
	/// </summary>
	public sealed class ClientResponseContext : MessageContext
	{
		/// <summary>
		///		The initial process of the deserialization pipeline.
		/// </summary>
		private Func<ClientResponseContext, bool> _initialProcess;

		/// <summary>
		///		Next (that is, resuming) process on the deserialization pipeline.
		/// </summary>
		internal Func<ClientResponseContext, bool> NextProcess;

		private byte[] _currentReceivingBuffer;

		/// <summary>
		///		Gets the buffer to receive data.
		/// </summary>
		/// <value>
		///		The buffer to receive data.
		///		This value will not be <c>null</c>.
		///		Available section is started with _receivingBufferOffset.
		/// </value>
		internal byte[] CurrentReceivingBuffer
		{
			get { return this._currentReceivingBuffer; }
		}

		private int _currentReceivingBufferOffset;

		/// <summary>
		///		Gets the current offset of the <see cref="CurrentReceivingBuffer"/>.
		/// </summary>
		/// <value>
		///		The current offset of the <see cref="CurrentReceivingBuffer"/>.
		/// </value>
		internal int CurrentReceivingBufferOffset
		{
			get { return _currentReceivingBufferOffset; }
		}

		private readonly List<ArraySegment<byte>> _receivedData;

		/// <summary>
		///		Gets the received data.
		/// </summary>
		/// <value>
		///		The received data.
		///		This value wlll not be <c>null</c>.
		/// </value>
		internal IList<ArraySegment<byte>> ReceivedData
		{
			get { return this._receivedData; }
		}

		/// <summary>
		///		Buffer that stores unpacking binaries received.
		/// </summary>
		internal ByteArraySegmentStream UnpackingBuffer;


		/// <summary>
		///		<see cref="Unpacker"/> to unpack entire request/notification message.
		/// </summary>
		internal Unpacker RootUnpacker;

		/// <summary>
		///		Subtree <see cref="Unpacker"/> to unpack request/notification message as array.
		/// </summary>
		internal Unpacker HeaderUnpacker;

		internal long ErrorStartAt;

		/// <summary>
		///		Subtree <see cref="Unpacker"/> to parse error value as opaque sequence.
		/// </summary>
		internal ByteArraySegmentStream ErrorBuffer;

		internal long ResultStartAt;

		/// <summary>
		///		Subtree <see cref="Unpacker"/> to parse return value as opaque sequence.
		/// </summary>
		internal ByteArraySegmentStream ResultBuffer;


		/// <summary>
		///		<see cref="Stream"/> to dump corrupt response for the future manual recovery by humans.
		/// </summary>
		internal Stream DumpStream;

		/// <summary>
		///		Initializes a new instance of the <see cref="ClientResponseContext"/> class.
		/// </summary>
		public ClientResponseContext()
		{
			// TODO: Configurable
			this._currentReceivingBuffer = new byte[ 65536 ];
			// TODO: ArrayDeque is preferred.
			this._receivedData = new List<ArraySegment<byte>>( 1 );
			this.ErrorStartAt = -1;
			this.ResultStartAt = -1;
		}

		/// <summary>
		///		Sets the bound <see cref="ClientTransport"/>.
		/// </summary>
		/// <param name="transport">The binding transport.</param>
		internal void SetTransport( ClientTransport transport )
		{
			this._initialProcess = transport.UnpackResponseHeader;
			this.NextProcess = transport.UnpackResponseHeader;
			base.SetTransport( transport );
		}

		private static bool InvalidFlow( ClientResponseContext context )
		{
			throw new InvalidOperationException( "Invalid state transition." );
		}


		/// <summary>
		///		Shifts the current receiving buffer offset with transferred bytes,
		///		and reallocates buffer for receiving if necessary.
		/// </summary>
		public void ShiftCurrentReceivingBuffer()
		{
			int shift = this.BytesTransferred;
			this._receivedData.Add( new ArraySegment<byte>( this._currentReceivingBuffer, this.Offset, shift ) );
			this._currentReceivingBufferOffset += shift;
			if ( this._currentReceivingBufferOffset == this._currentReceivingBuffer.Length )
			{
				// Replace with new buffer.
				this._currentReceivingBuffer = new byte[ this._currentReceivingBuffer.Length ];
				this._currentReceivingBufferOffset = 0;
			}

			// Set new offset and length.
			this.SetBuffer( this._currentReceivingBuffer, this._currentReceivingBufferOffset, this._currentReceivingBuffer.Length - this._currentReceivingBufferOffset );
		}

		/// <summary>
		///		Clears this instance internal buffers for reuse.
		/// </summary>
		internal sealed override void Clear()
		{
			this.ClearBuffers();
			if ( this.UnpackingBuffer != null )
			{
				this.UnpackingBuffer.Dispose();
				this.UnpackingBuffer = null;
			}
			this.NextProcess = InvalidFlow;
			base.Clear();
		}

		/// <summary>
		///		Clears the buffers to deserialize message.
		/// </summary>
		internal void ClearBuffers()
		{
			if ( this.ErrorBuffer != null )
			{
				this.ErrorBuffer.Dispose();
				this.ErrorBuffer = null;
			}

			if ( this.ResultBuffer != null )
			{
				this.ResultBuffer.Dispose();
				this.ResultBuffer = null;
			}

			this.MessageId = 0;

			if ( this.HeaderUnpacker != null )
			{
				this.HeaderUnpacker.Dispose();
				this.HeaderUnpacker = null;
			}

			if ( this.RootUnpacker != null )
			{
				this.RootUnpacker.Dispose();
				this.RootUnpacker = null;
			}

			this.ErrorStartAt = -1;
			this.ResultStartAt = -1;

			if ( this.UnpackingBuffer != null )
			{
				this.TruncateUsedReceivedData();
			}
		}

		/// <summary>
		///		Truncates the used segments from the received data.
		/// </summary>
		private void TruncateUsedReceivedData()
		{
			long removals = this.UnpackingBuffer.Position;
			var segments = this.UnpackingBuffer.GetBuffer();
			while ( segments.Any() && 0 < removals )
			{
				if ( segments[ 0 ].Count <= removals )
				{
					removals -= segments[ 0 ].Count;
					segments.RemoveAt( 0 );
				}
				else
				{
					int newCount = segments[ 0 ].Count - unchecked( ( int )removals );
					int newOffset = segments[ 0 ].Offset + unchecked( ( int )removals );
					segments[ 0 ] = new ArraySegment<byte>( segments[ 0 ].Array, newOffset, newCount );
					removals = 0;
				}
			}
		}
	}
}
