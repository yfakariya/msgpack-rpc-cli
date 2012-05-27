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
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.IO;

namespace MsgPack.IO
{
	/// <summary>
	///		Adds efficient thread-safe in-order buffer around existing <see cref="TextWriter"/> to help debugging concurrent problems.
	/// </summary>
	public class ConcurrentTextWriter : TextWriter
	{
		private readonly TextWriter _underlying;
		private readonly ConcurrentQueue<Entry> _buffer;

		/// <summary>
		///		Gets the <see cref="System.Text.Encoding"/> of underlying <see cref="TextWriter"/>.
		/// </summary>
		/// <value>
		///		The <see cref="System.Text.Encoding"/> of underlying <see cref="TextWriter"/>.
		/// </value>
		public override System.Text.Encoding Encoding
		{
			get { return this._underlying.Encoding; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConcurrentTextWriter"/> class.
		/// </summary>
		/// <param name="underlying">The underlying <see cref="TextWriter"/>.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="underlying"/> is <c>null</c>.
		/// </exception>
		public ConcurrentTextWriter( TextWriter underlying )
		{
			if ( underlying == null )
			{
				throw new ArgumentNullException( "underlying" );
			}

			Contract.EndContractBlock();

			this._buffer = new ConcurrentQueue<Entry>();
			this._underlying = underlying;
		}

		public override void Write( string value )
		{
			this._buffer.Enqueue( new Entry( false, value ) );
		}

		public override void Write( string format, object arg0 )
		{
			this._buffer.Enqueue( new Entry( false, format, arg0 ) );
		}

		public override void Write( string format, object arg0, object arg1 )
		{
			this._buffer.Enqueue( new Entry( false, format, arg0, arg1 ) );
		}

		public override void Write( string format, object arg0, object arg1, object arg2 )
		{
			this._buffer.Enqueue( new Entry( false, format, arg0, arg1, arg2 ) );
		}

		public override void Write( string format, params object[] arg )
		{
			this._buffer.Enqueue( new Entry( false, format, arg ) );
		}

		public override void WriteLine( string value )
		{
			this._buffer.Enqueue( new Entry( true, value ) );
		}

		public override void WriteLine( string format, object arg0 )
		{
			this._buffer.Enqueue( new Entry( true, format, arg0 ) );
		}

		public override void WriteLine( string format, object arg0, object arg1 )
		{
			this._buffer.Enqueue( new Entry( true, format, arg0, arg1 ) );
		}

		public override void WriteLine( string format, object arg0, object arg1, object arg2 )
		{
			this._buffer.Enqueue( new Entry( true, format, arg0, arg1, arg2 ) );
		}

		public override void WriteLine( string format, params object[] arg )
		{
			this._buffer.Enqueue( new Entry( true, format, arg ) );
		}

		/// <summary>
		///		Flushes current buffer to the underlying <see cref="TextWriter"/>.
		/// </summary>
		public override void Flush()
		{
			Entry entry;
			while ( this._buffer.TryDequeue( out entry ) )
			{
				if ( entry.IsWriteLine )
				{
					this._underlying.WriteLine( entry.Format, entry.Arg );
				}
				else
				{
					this._underlying.Write( entry.Format, entry.Arg );
				}
			}
		}

		private struct Entry
		{
			public readonly string Format;
			public readonly object[] Arg;
			public readonly bool IsWriteLine;

			public Entry( bool isWriteLine, string format, params object[] arg )
			{
				this.IsWriteLine = isWriteLine;
				this.Format = format;
				this.Arg = arg;
			}
		}
	}
}
