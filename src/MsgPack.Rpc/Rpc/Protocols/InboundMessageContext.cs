#region -- License Terms --
//
// MessagePack for CLI
//
// Copyright (C) 2010-2013 FUJIWARA, Yusuke
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Defines basic functionality for inbound message contexts.
	/// </summary>
	public abstract class InboundMessageContext : MessageContext
	{
		private readonly List<ArraySegment<byte>> _receivedData;

		/// <summary>
		///		Gets the received data.
		/// </summary>
		/// <value>
		///		The received data.
		///		This value wlll not be <c>null</c>.
		/// </value>
		[SuppressMessage( "Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Follwing SocketAsyncEventArgs signature." )]
		public IList<ArraySegment<byte>> ReceivedData
		{
			get { return this._receivedData; }
		}

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

		/// <summary>
		///		Initializes a new instance of the <see cref="InboundMessageContext"/> class.
		/// </summary>
		protected InboundMessageContext()
			: this( 65536 ) { }

		/// <summary>
		///		Initializes a new instance of the <see cref="InboundMessageContext"/> class.
		/// </summary>
		/// <param name="initialReceivingBufferSize">
		///		Initial size of receiving buffer.
		/// </param>
		protected InboundMessageContext( int initialReceivingBufferSize )
			: base()
		{
			this._currentReceivingBuffer = new byte[ initialReceivingBufferSize ];
			// TODO: ArrayDeque is preferred.
			this._receivedData = new List<ArraySegment<byte>>( 1 );
		}

		internal bool ReadFromRootUnpacker()
		{
			return this.RootUnpacker.TryRead( this.UnpackingBuffer );
		}

		internal bool ReadFromHeaderUnpacker()
		{
			return this.HeaderUnpacker.TryRead( this.UnpackingBuffer );
		}

		/// <summary>
		///		Set internal received data buffer for testing purposes.
		/// </summary>
		/// <param name="data">Data to be set.</param>
		internal void SetReceivedData( IList<ArraySegment<byte>> data )
		{
			this._receivedData.Clear();
			this._receivedData.AddRange( data );
		}

		/// <summary>
		///		Set internal receiving buffer for testing purposes.
		/// </summary>
		/// <param name="data">Data to be set.</param>
		internal void SetReceivingBuffer( byte[] data )
		{
			this.SocketContext.SetBuffer( data, 0, data.Length );
		}

		/// <summary>
		///		Shifts the current receiving buffer offset with transferred bytes,
		///		and reallocates buffer for receiving if necessary.
		/// </summary>
		internal void ShiftCurrentReceivingBuffer()
		{
			int shift = this.BytesTransferred;
			this._receivedData.Add( new ArraySegment<byte>( this._currentReceivingBuffer, this.SocketContext.Offset, shift ) );
			this._currentReceivingBufferOffset += shift;
			if ( this._currentReceivingBufferOffset == this._currentReceivingBuffer.Length )
			{
				// Replace with new buffer.
				this._currentReceivingBuffer = new byte[ this._currentReceivingBuffer.Length ];
				this._currentReceivingBufferOffset = 0;
			}

			// Set new offset and length.
			this.SocketContext.SetBuffer( this._currentReceivingBuffer, this._currentReceivingBufferOffset, this._currentReceivingBuffer.Length - this._currentReceivingBufferOffset );
		}

		/// <summary>
		///		Prepares socket context array buffer with <see cref="CurrentReceivingBuffer"/>
		/// </summary>
		internal void PrepareReceivingBuffer()
		{
			this.SocketContext.SetBuffer( this.CurrentReceivingBuffer, 0, this.CurrentReceivingBuffer.Length );
		}

		internal override void Clear()
		{
			if ( this.UnpackingBuffer != null )
			{
				this.UnpackingBuffer.Dispose();
				this.UnpackingBuffer = null;
			}

			base.Clear();
		}

		internal virtual void ClearBuffers()
		{
			if ( this.HeaderUnpacker != null )
			{
				try
				{
					this.HeaderUnpacker.Dispose();
				}
				catch ( InvalidMessagePackStreamException )
				{
					// Handles cleanup for corruppted stream.
				}

				this.HeaderUnpacker = null;
			}

			if ( this.RootUnpacker != null )
			{
				try
				{
					this.RootUnpacker.Dispose();
				}
				catch ( InvalidMessagePackStreamException )
				{
					// Handles cleanup for corruppted stream.
				}

				this.RootUnpacker = null;
			}

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
