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
using System.Diagnostics.Contracts;
using System.IO;
using MsgPack.Collections;

namespace MsgPack.Rpc.Serialization
{
	// FIXME: refactor
	/// <summary>
	///		Represents RPC output buffer. This class is NOT thread safe.
	/// </summary>
	/// <remarks>
	///		It is not thread safe that:
	///		<list type="bullet">
	///			<item>Disposing this instance concurrently.</item>
	///			<item>Use multiple stream returned from <see cref="OpenWriteStream"/> concurrently.</item>
	///		</list>
	/// </remarks>
	public sealed class RpcOutputBuffer : IDisposable
	{
		// Responsible for allocation
		// BufferPool usage is thread safe, but returning and writing is not thread safe.

		private readonly ChunkBuffer _chunks;
		private IEnumerable<byte> _swapped;

		internal ChunkBuffer Chunks
		{
			get { return this._chunks; }
		}

		public RpcOutputBuffer( ChunkBuffer chunkBuffer )
		{
			if ( chunkBuffer == null )
			{
				throw new ArgumentNullException( "chunkBuffer" );
			}

			this._chunks = chunkBuffer;
		}

		/// <summary>
		///		Cleanup internal resources.
		/// </summary>
		public void Dispose()
		{
			if ( this._chunks != null )
			{
				this._chunks.Dispose();
			}
		}

		/// <summary>
		///		Read all bytes from this buffer.
		/// </summary>
		/// <returns>
		///		Bytes in this buffer.
		///	</returns>
		public IEnumerable<byte> ReadBytes() // TODO: It should be OpenReadStream?
		{
			if ( this._swapped != null )
			{
				return this._swapped;
			}
			else
			{
				return this._chunks.ReadAll();
			}
		}

		/// <summary>
		///		Get write only <see cref="Stream"/> to write contents to this buffer.
		/// </summary>
		/// <returns>
		///		Write only <see cref="Stream"/> to write contents to this buffer.
		/// </returns>
		public Stream OpenWriteStream()
		{
			return new ZeroCopyingRpcOutputBufferStream( this._chunks );
		}

		internal RpcOutputBufferSwapper CreateSwapper()
		{
			return new RpcOutputBufferSwapper( this );
		}

		private abstract class WriteOnlyStream : Stream
		{
			public sealed override bool CanRead
			{
				get { return false; }
			}

			public sealed override bool CanSeek
			{
				get { return false; }
			}

			public sealed override bool CanWrite
			{
				get { return true; }
			}

			public sealed override long Length
			{
				get { throw new NotSupportedException(); }
			}

			public sealed override long Position
			{
				get { throw new NotSupportedException(); }
				set { throw new NotSupportedException(); }
			}

			private bool _isDisposed;

			protected WriteOnlyStream() { }

			protected override void Dispose( bool disposing )
			{
				base.Dispose( disposing );
				this._isDisposed = true;
			}

			public sealed override void Write( byte[] buffer, int offset, int count )
			{
				if ( buffer == null )
				{
					throw new ArgumentNullException( "buffer" );
				}

				if ( offset < 0 )
				{
					throw new ArgumentOutOfRangeException( "offset" );
				}

				if ( count < 0 )
				{
					throw new ArgumentOutOfRangeException( "count" );
				}

				if ( buffer.Length < offset + count )
				{
					throw new ArgumentException( "'buffer' too small.", "buffer" );
				}

				if ( this._isDisposed )
				{
					throw new ObjectDisposedException( this.GetType().FullName );
				}

				Contract.EndContractBlock();

				this.WriteCore( buffer, offset, count );
			}

			protected abstract void WriteCore( byte[] buffer, int offset, int count );

			public sealed override void Flush()
			{
				// nop
			}

			public sealed override int Read( byte[] buffer, int offset, int count )
			{
				throw new NotSupportedException();
			}

			public sealed override long Seek( long offset, SeekOrigin origin )
			{
				throw new NotSupportedException();
			}

			public sealed override void SetLength( long value )
			{
				throw new NotSupportedException();
			}
		}

		private sealed class ZeroCopyingRpcOutputBufferStream : WriteOnlyStream
		{
			// Do not dispose it.
			private readonly ChunkBuffer _chunks;

			public ZeroCopyingRpcOutputBufferStream( ChunkBuffer chunks )
			{
				this._chunks = chunks;
			}

			protected sealed override void WriteCore( byte[] buffer, int offset, int count )
			{
				this._chunks.Feed( new ArraySegment<byte>( buffer, offset, count ) );
			}
		}

		internal sealed class RpcOutputBufferSwapper : IDisposable
		{
			private readonly RpcOutputBuffer _enclosing;
			private IEnumerable<byte> _swapping;

			public RpcOutputBufferSwapper( RpcOutputBuffer enclosing )
			{
				Contract.Assert( enclosing != null );
				this._enclosing = enclosing;
			}

			public void Dispose()
			{
				if ( this._swapping != null )
				{
					this._enclosing._swapped = this._swapping;
					this._swapping = null;
				}
			}

			public IEnumerable<byte> ReadBytes()
			{
				return this._enclosing.ReadBytes();
			}

			public void WriteBytes( IEnumerable<byte> sequence )
			{
				if ( sequence == null )
				{
					throw new ArgumentNullException( "sequence" );
				}

				Contract.EndContractBlock();

				this._swapping = sequence;
			}
		}
	}
}
