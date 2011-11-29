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
using System.Globalization;

namespace MsgPack.Rpc.Serialization
{
	// TODO: cleanup
	/// <summary>
	///		Contains context information of message deserialization.
	/// </summary>
	public abstract class MessageDeserializationContext : SerializationErrorSink
	{
		private readonly IEnumerable<byte> _buffer;
		private int _processed;
		private readonly int? _maxLength;

		/// <summary>
		///		Initaialize new instance.
		/// </summary>
		/// <param name="buffer">
		///		<see cref="RpcInputBuffer"/> which is source of inbound message.
		/// </param>
		/// <param name="maxLength">
		///		Max quota length of buffer. Null means unlimited.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="buffer"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///		<paramref name="maxLength"/> is not null but negative.
		/// </exception>
		protected MessageDeserializationContext( IEnumerable<byte> buffer, int? maxLength )
		{
			if ( buffer == null )
			{
				throw new ArgumentNullException( "buffer" );
			}

			if ( maxLength != null && maxLength <= 0 )
			{
				throw new ArgumentOutOfRangeException( "maxLength" );
			}

			Contract.EndContractBlock();

			this._buffer = buffer;
			this._maxLength = maxLength;
		}

		/// <summary>
		///		Read bytes from buffer.
		/// </summary>
		/// <returns>Iterator for read bytes from input buffer.</returns>
		internal IEnumerable<byte> ReadBytes()
		{
			int limit = this._maxLength ?? Int32.MaxValue;
			foreach ( var b in this._buffer )
			{
				if ( ( ++this._processed ) > limit )
				{
					this.SetSerializationError( new RpcErrorMessage( RpcError.MessageTooLargeError, "Incoming stream too large.", String.Format( CultureInfo.CurrentCulture, "MaxLength:{0:#,##0} bytes.", limit ) ) );
					yield break;
				}

				yield return b;
			}
		}
	}
}
