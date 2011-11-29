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
using System.Runtime.Serialization;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Exception in unexpected error.
	/// </summary>
	/// <remarks>
	///		If server returns error but its structure is not compatible with de-facto standard, client library will throw this exception.
	/// </remarks>
	[Serializable]
	public sealed class UnexpcetedRpcException : RpcException
	{
		private const string _errorFieldKey = "UnexpectedError";
		private const string _errorDetailFieldKey = "UnexpectedErrorDetail";

		private readonly MessagePackObject _error;

		/// <summary>
		///		Get the value of error field of response.
		/// </summary>
		/// <value>
		///		Value of error field of response.
		///		This value is not nil, but its content is arbitary.
		/// </value>
		public MessagePackObject Error
		{
			get { return this._error; }
		}

		private readonly MessagePackObject _errorDetail;

		/// <summary>
		///		Get the value of return field of response in error.
		/// </summary>
		/// <value>
		///		Value of return field of response in error.
		///		This value may be nil, but server can set any value.
		/// </value>
		public MessagePackObject ErrorDetail
		{
			get { return this._errorDetail; }
		}

		/// <summary>
		///		Initialize new instance.
		/// </summary>
		/// <param name="error">
		///		Value of error field of response.
		///		This value is not nil, but its content is arbitary.
		/// </param>
		/// <param name="errorDetail">
		///		Value of return field of response in error.
		///		This value may be nil, but server can set any value.
		/// </param>
		public UnexpcetedRpcException( MessagePackObject error, MessagePackObject errorDetail )
			: base( RpcError.Unexpected, RpcError.Unexpected.DefaultMessage, null )
		{
			this._error = error;
			this._errorDetail = errorDetail;
		}

		/// <summary>
		///		Restore state from specified info.
		/// </summary>
		/// <param name="info"><see cref="SerializationInfo"/>.</param>
		/// <param name="context"><see cref="StreamingContext"/>.</param>
		private UnexpcetedRpcException( SerializationInfo info, StreamingContext context )
			: base( info, context )
		{
			this._error = ( MessagePackObject )info.GetValue( _errorFieldKey, typeof( MessagePackObject ) );
			this._errorDetail = ( MessagePackObject )info.GetValue( _errorDetailFieldKey, typeof( MessagePackObject ) );
		}

		/// <summary>
		///		Get serialization data and put into <paramref name="info"/>.
		/// </summary>
		/// <param name="info"><see cref="SerializationInfo"/>.</param>
		/// <param name="context"><see cref="StreamingContext"/>.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="info"/> is null.
		/// </exception>
		public sealed override void GetObjectData( SerializationInfo info, StreamingContext context )
		{
			base.GetObjectData( info, context );

			info.AddValue( _errorFieldKey, this.Error, typeof( MessagePackObject ) );
			info.AddValue( _errorDetailFieldKey, this.ErrorDetail, typeof( MessagePackObject ) );
		}
	}
}
