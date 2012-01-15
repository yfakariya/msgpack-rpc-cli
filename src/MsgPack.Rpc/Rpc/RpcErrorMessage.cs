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
using System.Diagnostics.Contracts;
using System.Globalization;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Represents MsgPack-RPC error instance.
	/// </summary>
	public struct RpcErrorMessage : IEquatable<RpcErrorMessage>
	{
		/// <summary>
		///		Gets the instance which represents success (that is, not error.)
		/// </summary>
		/// <value>
		///		The instance which represents success (that is, not error.)
		/// </value>
		public static RpcErrorMessage Success
		{
			get { return new RpcErrorMessage(); }
		}

		/// <summary>
		///		Get the value whether this instance represents success.
		/// </summary>
		/// <value>
		///		If this instance represents success then true.
		/// </value>
		public bool IsSuccess
		{
			get { return this._error == null; }
		}

		private readonly RpcError _error;

		/// <summary>
		///		Get error information for this error.
		/// </summary>
		/// <value>
		///		Error information for this error.
		/// </value>
		/// <exception cref="InvalidOperationException">
		///		<see cref="IsSuccess"/> is true.
		/// </exception>
		public RpcError Error
		{
			get
			{
				if ( this._error == null )
				{
					throw new InvalidOperationException( "Operation success." );
				}

				return this._error;
			}
		}

		private readonly MessagePackObject _detail;

		/// <summary>
		///		Get detailed error information for this error.
		/// </summary>
		/// <value>
		///		Detailed error information for this error.
		/// </value>
		/// <exception cref="InvalidOperationException">
		///		<see cref="IsSuccess"/> is true.
		/// </exception>
		public MessagePackObject Detail
		{
			get
			{
				if ( this._error == null )
				{
					throw new InvalidOperationException( "Operation success." );
				}

				return this._detail;
			}
		}

		/// <summary>
		///		Initializes new instance.
		/// </summary>
		/// <param name="error">Error information of the error.</param>
		/// <param name="detail">Unpacked detailed information of the error which was occurred in remote endpoint.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="error"/> is null.
		/// </exception>
		public RpcErrorMessage( RpcError error, MessagePackObject detail )
		{
			if ( error == null )
			{
				throw new ArgumentNullException( "error" );
			}

			Contract.EndContractBlock();

			this._error = error;
			this._detail = detail;
		}

		/// <summary>
		///		Initializes new instance.
		/// </summary>
		/// <param name="error">The metadata of the error.</param>
		/// <param name="description">The description of the error which was occurred in local.</param>
		/// <param name="debugInformation">The detailed debug information of the error which was occurred in local.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="error"/> is <c>null</c>.
		/// </exception>
		public RpcErrorMessage( RpcError error, string description, string debugInformation )
		{
			if ( error == null )
			{
				throw new ArgumentNullException( "error" );
			}

			Contract.EndContractBlock();

			this._error = error;

			var data = new MessagePackObjectDictionary( 2 );
			if ( description != null )
			{
				data.Add( RpcException.MessageKeyUtf8, description );
			}

			if ( debugInformation != null )
			{
				data.Add( RpcException.DebugInformationKeyUtf8, debugInformation );
			}

			this._detail = new MessagePackObject( data );
		}

		/// <summary>
		///		Determines whether the specified <see cref="System.Object"/> is equal to this instance.
		/// </summary>
		/// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
		/// <returns>
		///		<c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
		/// </returns>
		public override bool Equals( object obj )
		{
			if ( Object.ReferenceEquals( obj, null ) )
			{
				return false;
			}

			if ( !( obj is RpcErrorMessage ) )
			{
				return false;
			}

			return this.Equals( ( RpcErrorMessage )obj );
		}

		/// <summary>
		///		Indicates whether the current object is equal to another object of the same type.
		/// </summary>
		/// <param name="other">
		///		An object to compare with this object.
		/// </param>
		/// <returns>
		///		<x>true</x> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <c>false</c>.
		/// </returns>
		public bool Equals( RpcErrorMessage other )
		{
			return this._error == other._error && this._detail == other._detail;
		}

		/// <summary>
		///		Returns a hash code for this instance.
		/// </summary>
		/// <returns>
		///		A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
		/// </returns>
		public override int GetHashCode()
		{
			return ( this._error == null ? 0 : this._error.GetHashCode() ) ^ this._detail.GetHashCode();
		}

		/// <summary>
		///		Returns string representation of this error.
		/// </summary>
		/// <returns>
		///		String representation of this error.
		/// </returns>
		public override string ToString()
		{
			if ( this.IsSuccess )
			{
				return String.Empty;
			}
			else
			{
				return String.Format( CultureInfo.CurrentCulture, "{{ \"ID\" : \"{0}\", \"Code\" : {1}, \"Detail\" : {2} }}", this._error.Identifier, this._error.ErrorCode, this._detail );
			}
		}

		/// <summary>
		///		Get <see cref="RpcException"/> which corresponds to this error.
		/// </summary>
		/// <returns><see cref="RpcException"/> which corresponds to this error.</returns>
		/// <exception cref="InvalidOperationException">
		///		<see cref="IsSuccess"/> is true.
		/// </exception>
		public RpcException ToException()
		{
			if ( this.IsSuccess )
			{
				throw new InvalidOperationException( "Operation has been succeeded." );
			}

			return this._error.ToException( this._detail );
		}

		/// <summary>
		///		Determines whether two <see cref="RpcErrorMessage"/> instances have the same value. 
		/// </summary>
		/// <param name="left">A <see cref="RpcErrorMessage"/> instance to compare with <paramref name="right"/>.</param>
		/// <param name="right">A <see cref="RpcErrorMessage"/> instance to compare with <paramref name="left"/>.</param>
		/// <returns>
		///		<c>true</c> if the <see cref="RpcErrorMessage"/> instances are equivalent; otherwise, <c>false</c>.
		/// </returns>
		public static bool operator ==( RpcErrorMessage left, RpcErrorMessage right )
		{
			return left.Equals( right );
		}

		/// <summary>
		///		Determines whether two <see cref="RpcErrorMessage"/> instances do not have the same value. 
		/// </summary>
		/// <param name="left">A <see cref="RpcErrorMessage"/> instance to compare with <paramref name="right"/>.</param>
		/// <param name="right">A <see cref="RpcErrorMessage"/> instance to compare with <paramref name="left"/>.</param>
		/// <returns>
		///		<c>true</c> if the <see cref="RpcErrorMessage"/> instances are not equivalent; otherwise, <c>false</c>.
		/// </returns>
		public static bool operator !=( RpcErrorMessage left, RpcErrorMessage right )
		{
			return !left.Equals( right );
		}
	}
}
