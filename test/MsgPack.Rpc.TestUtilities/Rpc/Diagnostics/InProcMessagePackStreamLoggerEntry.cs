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
using System.Linq;
using System.Net;

namespace MsgPack.Rpc.Diagnostics
{
	/// <summary>
	///		Entries of <see cref="InProcMessagePackStreamLogger"/>.
	/// </summary>
	public struct InProcMessagePackStreamLoggerEntry : IEquatable<InProcMessagePackStreamLoggerEntry>
	{
		private readonly DateTimeOffset _sessionStartTime;

		/// <summary>
		///		Gets the <see cref="DateTimeOffset"/> when session was started.
		/// </summary>
		/// <value>
		///		The <see cref="DateTimeOffset"/> when session was started.
		/// </value>
		public DateTimeOffset SessionStartTime
		{
			get { return this._sessionStartTime; }
		} 

		private readonly EndPoint _remoteEndPoint;

		/// <summary>
		///		Gets the remote end point.
		/// </summary>
		/// <value>
		///		The <see cref="EndPoint"/> for remote end point.
		/// </value>
		public EndPoint RemoteEndPoint
		{
			get { return this._remoteEndPoint; }
		}

		private readonly byte[] _stream;

		/// <summary>
		///		Gets the stream contents.
		/// </summary>
		/// <value>
		///		The stream contents.
		/// </value>
		public byte[] Stream
		{
			get { return this._stream; }
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="InProcMessagePackStreamLoggerEntry"/> struct.
		/// </summary>
		/// <param name="sessionStartTime">The <see cref="DateTimeOffset"/> when session was started.</param>
		/// <param name="remoteEndPoint">The <see cref="EndPoint"/> for remote end point.</param>
		/// <param name="stream">The stream contents.</param>
		public InProcMessagePackStreamLoggerEntry( DateTimeOffset sessionStartTime, EndPoint remoteEndPoint, IEnumerable<byte> stream )
		{
			this._sessionStartTime = sessionStartTime;
			this._remoteEndPoint = remoteEndPoint;
			this._stream = ( stream ?? Enumerable.Empty<byte>() ).ToArray();
		}

		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		/// <returns>
		/// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
		/// </returns>
		public override int GetHashCode()
		{
			return
				this._sessionStartTime.GetHashCode()
				^ ( this._remoteEndPoint == null ? 0 : this._remoteEndPoint.GetHashCode() )
				^ ( this._stream == null ? 0 : ( this._stream.Length ^ this._stream.Take( 32 ).Select( b => b.GetHashCode() ).Aggregate( ( l, r ) => l ^ r ) ) );
		}

		/// <summary>
		/// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
		/// </summary>
		/// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
		/// <returns>
		///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
		/// </returns>
		public override bool Equals( object obj )
		{
			if ( !( obj is InProcMessagePackStreamLoggerEntry ) )
			{
				return false;
			}
			else
			{
				return this.Equals( ( InProcMessagePackStreamLoggerEntry )obj );
			}
		}

		/// <summary>
		/// Determines whether the specified <see cref="InProcMessagePackStreamLoggerEntry"/> is equal to this instance.
		/// </summary>
		/// <param name="obj">The <see cref="InProcMessagePackStreamLoggerEntry"/> to compare with this instance.</param>
		/// <returns>
		///   <c>true</c> if the specified <see cref="InProcMessagePackStreamLoggerEntry"/> is equal to this instance; otherwise, <c>false</c>.
		/// </returns>
		public bool Equals( InProcMessagePackStreamLoggerEntry other )
		{
			if ( this._sessionStartTime != other._sessionStartTime )
			{
				return false;
			}

			if ( this._remoteEndPoint == null )
			{
				return other._remoteEndPoint == null;
			}

			if ( !this._remoteEndPoint.Equals( other._remoteEndPoint ) )
			{
				return false;
			}

			if ( this._stream == null )
			{
				return other._stream == null;
			}
			else if ( other._stream == null )
			{
				return false;
			}

			return this._stream.SequenceEqual( other._stream );
		}

		/// <summary>
		/// Implements the operator ==.
		/// </summary>
		/// <param name="left">The left.</param>
		/// <param name="right">The right.</param>
		/// <returns>
		/// The result of the operator.
		/// </returns>
		public static bool operator ==( InProcMessagePackStreamLoggerEntry left, InProcMessagePackStreamLoggerEntry right )
		{
			return left.Equals( right );
		}

		/// <summary>
		/// Implements the operator !=.
		/// </summary>
		/// <param name="left">The left.</param>
		/// <param name="right">The right.</param>
		/// <returns>
		/// The result of the operator.
		/// </returns>
		public static bool operator !=( InProcMessagePackStreamLoggerEntry left, InProcMessagePackStreamLoggerEntry right )
		{
			return !left.Equals( right );
		}
	}
}
