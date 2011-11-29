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
using System.Collections.Generic;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Represents MsgPack-RPC error instance.
	/// </summary>
	public struct RpcErrorMessage
	{
		/// <summary>
		///		Instance which represents success (that is, not error.)
		/// </summary>
		public static readonly RpcErrorMessage Success = new RpcErrorMessage();

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
		///		Initialize new instance.
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
		///		Initialize new instance.
		/// </summary>
		/// <param name="error">Error information of the error.</param>
		/// <param name="description">Description of the error which was occurred in local.</param>
		/// <param name="debugInformation">Detailed debug information of the error which was occurred in local.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="error"/> is null.
		/// </exception>
		public RpcErrorMessage( RpcError error, string description, string debugInformation )
		{
			if ( error == null )
			{
				throw new ArgumentNullException( "error" );
			}

			Contract.EndContractBlock();

			this._error = error;
			var data = new Dictionary<MessagePackObject, MessagePackObject>();
			data.Add( RpcException.MessageKeyUtf8, description );
			data.Add( RpcException.DebugInformationKeyUtf8, debugInformation );
			this._detail = new MessagePackObject( data );
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
				return String.Format( CultureInfo.CurrentCulture, "{0}({1}):{2}", this._error.Identifier, this._error.ErrorCode, this._detail.IsNil ? "(nil)" : this._detail );
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
	}
}
