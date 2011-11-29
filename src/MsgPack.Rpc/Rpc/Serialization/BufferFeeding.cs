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

using MsgPack.Collections;

namespace MsgPack.Rpc.Serialization
{
	// TODO: use T4 template
	// TODO: refactor
	/// <summary>
	///		Represents buffer feeding result.
	/// </summary>
	public struct BufferFeeding : IEquatable<BufferFeeding>
	{
#pragma  warning disable 1591
		private readonly ChunkBuffer _reallocatedBuffer;

		/// <summary>
		/// 
		/// </summary>
		public ChunkBuffer ReallocatedBuffer
		{
			get { return this._reallocatedBuffer; }
		}

		private readonly long _feeded;

		public long Feeded
		{
			get { return this._feeded; }
		}

		public BufferFeeding( long feeded )
			: this( feeded, null ) { }

		public BufferFeeding( long feeded, ChunkBuffer reallocatedBuffer )
		{
			this._feeded = feeded;
			this._reallocatedBuffer = reallocatedBuffer;
		}

		public bool Equals( BufferFeeding other )
		{
			return this._feeded == other._feeded && this._reallocatedBuffer == other._reallocatedBuffer;
		}

		public override bool Equals( object obj )
		{
			if ( !( obj is BufferFeeding ) )
			{
				return false;
			}
			else
			{
				return this.Equals( ( BufferFeeding )obj );
			}
		}

		public override int GetHashCode()
		{
			return this._feeded.GetHashCode() ^ ( this._reallocatedBuffer == null ? 0 : this._reallocatedBuffer.GetHashCode() );
		}

		public override string ToString()
		{
			if ( this._reallocatedBuffer == null )
			{
				return this._feeded.ToString();
			}
			else
			{
				return this._feeded.ToString() + "ReallocatedTo:" + this._reallocatedBuffer.ToString() + "(" + this._reallocatedBuffer.GetHashCode() + ")";
			}
		}

		public static bool operator ==( BufferFeeding left, BufferFeeding right )
		{
			return left.Equals( right );
		}

		public static bool operator !=( BufferFeeding left, BufferFeeding right )
		{
			return !left.Equals( right );
		}
#pragma  warning restore 1591
	}
}
