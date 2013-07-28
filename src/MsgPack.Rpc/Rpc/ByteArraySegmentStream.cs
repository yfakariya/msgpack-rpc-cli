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
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace MsgPack.Rpc
{
	[DebuggerTypeProxy( typeof( DebuggerProxy ) )]
	internal sealed class ByteArraySegmentStream : Stream
	{
		private readonly IList<ArraySegment<byte>> _segments;

		private int _segmentIndex;
		private int _offsetInCurrentSegment;

		public sealed override bool CanRead
		{
			get { return true; }
		}

		public sealed override bool CanSeek
		{
			get { return true; }
		}

		public sealed override bool CanWrite
		{
			get { return false; }
		}

		public sealed override long Length
		{
			get { return this._segments.Sum( item => ( long )item.Count ); }
		}

		private long _position;

		public sealed override long Position
		{
			get
			{
				return this._position;
			}
			set
			{
				if ( value < 0 )
				{
					throw new ArgumentOutOfRangeException( "value" );
				}

				this.Seek( value - this._position );
			}
		}

		public ByteArraySegmentStream( IList<ArraySegment<byte>> underlying )
		{
			this._segments = underlying;
		}

		public sealed override int Read( byte[] buffer, int offset, int count )
		{
			int remains = count;
			int result = 0;
			while ( 0 < remains && this._segmentIndex < this._segments.Count )
			{
				int copied = this._segments[ this._segmentIndex ].CopyTo( this._offsetInCurrentSegment, buffer, offset + result, remains );
				result += copied;
				remains -= copied;
				this._offsetInCurrentSegment += copied;

				if ( this._offsetInCurrentSegment == this._segments[ this._segmentIndex ].Count )
				{
					this._segmentIndex++;
					this._offsetInCurrentSegment = 0;
				}

				this._position += copied;
			}

			return result;
		}

		public sealed override long Seek( long offset, SeekOrigin origin )
		{
			long length = this.Length;
			long offsetFromCurrent;
			switch ( origin )
			{
				case SeekOrigin.Begin:
				{
					offsetFromCurrent = offset - this._position;
					break;
				}
				case SeekOrigin.Current:
				{
					offsetFromCurrent = offset;
					break;
				}
				case SeekOrigin.End:
				{
					offsetFromCurrent = length + offset - this._position;
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException( "origin" );
				}
			}

			if ( offsetFromCurrent + this._position < 0 || length < offsetFromCurrent + this._position )
			{
				throw new ArgumentOutOfRangeException( "offset" );
			}

			this.Seek( offsetFromCurrent );
			return this._position;
		}

		private void Seek( long offsetFromCurrent )
		{
#if DEBUG
			Contract.Assert( 0 <= offsetFromCurrent + this._position, offsetFromCurrent + this._position + " < 0" );
			Contract.Assert( offsetFromCurrent + this._position <= this.Length, this.Length + " <= " + offsetFromCurrent + this._position );
#endif

			if ( offsetFromCurrent < 0 )
			{
				for ( long i = 0; offsetFromCurrent < i; i-- )
				{
					if ( this._offsetInCurrentSegment == 0 )
					{
						this._segmentIndex--;
						Contract.Assert( 0 <= this._segmentIndex );
						this._offsetInCurrentSegment = this._segments[ this._segmentIndex ].Count - 1;
					}
					else
					{
						this._offsetInCurrentSegment--;
					}

					this._position--;
				}
			}
			else
			{
				for ( long i = 0; i < offsetFromCurrent; i++ )
				{
					if ( this._offsetInCurrentSegment == this._segments[ this._segmentIndex ].Count - 1 )
					{
						this._segmentIndex++;
						Contract.Assert( this._segmentIndex <= this._segments.Count );
						this._offsetInCurrentSegment = 0;
					}
					else
					{
						this._offsetInCurrentSegment++;
					}

					this._position++;
				}
			}
		}

		public IList<ArraySegment<byte>> GetBuffer()
		{
			return this._segments;
		}

		public IList<ArraySegment<byte>> GetBuffer( long start, long length )
		{
			if ( start < 0 )
			{
				throw new ArgumentOutOfRangeException( "start" );
			}

			if ( length < 0 )
			{
				throw new ArgumentOutOfRangeException( "length" );
			}

			var result = new List<ArraySegment<byte>>( this._segments.Count );
			long taken = 0;
			long toBeSkipped = start;
			foreach ( var segment in this._segments )
			{
				int skipped = 0;
				if ( toBeSkipped > 0 )
				{
					if ( segment.Count <= toBeSkipped )
					{
						toBeSkipped -= segment.Count;
						continue;
					}

					skipped = unchecked( ( int )toBeSkipped );
					toBeSkipped = 0;
				}

				int available = segment.Count - skipped;
				long required = length - taken;
				if ( required <= available )
				{
					taken += required;
					result.Add( new ArraySegment<byte>( segment.Array, segment.Offset + skipped, unchecked( ( int )required ) ) );
					break;
				}
				else
				{
					taken += available;
					result.Add( new ArraySegment<byte>( segment.Array, segment.Offset + skipped, available ) );
				}
			}

			return result;
		}

		public byte[] ToArray()
		{
			if ( this._segments.Count == 0 )
			{
				return new byte[ 0 ];
			}

			IEnumerable<byte> result = this._segments[ 0 ].AsEnumerable();
			for ( int i = 1; i < this._segments.Count; i++ )
			{
				result = result.Concat( this._segments[ i ].AsEnumerable() );
			}

			return result.ToArray();
		}

		public sealed override void Flush()
		{
			// nop
		}

		public override void SetLength( long value )
		{
			throw new NotSupportedException();
		}

		public sealed override IAsyncResult BeginWrite( byte[] buffer, int offset, int count, AsyncCallback callback, object state )
		{
			throw new NotSupportedException();
		}

		public sealed override void EndWrite( IAsyncResult asyncResult )
		{
			throw new NotSupportedException();
		}

		public sealed override void Write( byte[] buffer, int offset, int count )
		{
			throw new NotSupportedException();
		}

		public sealed override void WriteByte( byte value )
		{
			throw new NotSupportedException();
		}

		internal sealed class DebuggerProxy
		{
			private readonly ByteArraySegmentStream _source;

			public bool CanSeek
			{
				get { return this._source.CanSeek; }
			}

			public bool CanRead
			{
				get { return this._source.CanRead; }
			}

			public bool CanWrite
			{
				get { return this._source.CanWrite; }
			}

			public bool CanTimeout
			{
				get { return this._source.CanTimeout; }
			}

			public int ReadTimeout
			{
				get { return this._source.ReadTimeout; }
				set { this._source.ReadTimeout = value; }
			}

			public int WriteTimeout
			{
				get { return this._source.WriteTimeout; }
				set { this._source.WriteTimeout = value; }
			}

			public long Position
			{
				get { return this._source.Position; }
				set { this._source.Position = value; }
			}

			public long Length
			{
				get { return this._source.Length; }
			}

			public IList<ArraySegment<byte>> Segments
			{
				get { return this._source._segments ?? new ArraySegment<byte>[ 0 ]; }
			}

			public string Data
			{
				get
				{
					return
						"[" +
						String.Join(
							",",
							this.Segments.Select(
								s => s.AsEnumerable().Select( b => b.ToString( "X2" ) )
							).Aggregate( ( current, subsequent ) => current.Concat( subsequent ) )
						) + "]";
				}
			}

			public DebuggerProxy( ByteArraySegmentStream source )
			{
				this._source = source;
			}
		}
	}
}
