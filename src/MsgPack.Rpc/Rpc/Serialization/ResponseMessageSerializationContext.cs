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
using MsgPack.Collections;

namespace MsgPack.Rpc.Serialization
{
	/// <summary>
	///		Stores context information of response message serialization.
	/// </summary>
	public sealed class ResponseMessageSerializationContext : MessageSerializationContext
	{
		private readonly int _messageId;

		/// <summary>
		///		Get ID of this message.
		/// </summary>
		/// <value>ID of this message.</value>
		public int MessageId
		{
			get { return this._messageId; }
		} 

		private readonly bool _isVoid;

		/// <summary>
		///		Get the value whether invoked method is void.
		/// </summary>
		/// <value>
		///		If invoked method is void then true.
		/// </value>
		public bool IsVoid
		{
			get { return this._isVoid; }
		}

		private RpcException _exception;

		/// <summary>
		///		Get the exception thrown from the method.
		/// </summary>
		/// <value>
		///		Exception thrown from the method.
		///		If method was succeeded, this value is null.
		/// </value>
		public RpcException Exception
		{
			get { return _exception; }
		}

		private object _returnValue;

		/// <summary>
		///		Get the return value from the method.
		/// </summary>
		/// <value>
		///		Return value from the method.
		///		If <see cref="IsVoid"/> is true or <see cref="Exception"/> is not null,
		///		the value of this property is undefined.
		/// </value>
		public object ReturnValue
		{
			get { return this._returnValue; }
		}

		/// <summary>
		///		Initialize new instance for succeeded invocation of void method.
		/// </summary>
		/// <param name="buffer">Buffer to be written from serializer.</param>
		/// <param name="messageId">ID of this message.</param>
		internal ResponseMessageSerializationContext( RpcOutputBuffer buffer, int messageId )
			: this( buffer, messageId, null, null, true ) { }

		/// <summary>
		///		Initialize new instance for succeeded invocation of non-void method.
		/// </summary>
		/// <param name="buffer">Buffer to be written from serializer.</param>
		/// <param name="messageId">ID of this message.</param>
		/// <param name="returnValue">Return value returned from the method.</param>
		internal ResponseMessageSerializationContext( RpcOutputBuffer buffer, int messageId, object returnValue )
			: this( buffer, messageId, returnValue, null, false ) { }

		/// <summary>
		///		Initialize new instance for succeeded invocation of non-void method.
		/// </summary>
		/// <param name="buffer">Buffer to be written from serializer.</param>
		/// <param name="messageId">ID of this message.</param>
		/// <param name="exception">Exception thrown from the method.</param>
		/// <param name="isVoid">If invoked method is void then true.</param>
		internal ResponseMessageSerializationContext( RpcOutputBuffer buffer, int messageId, RpcException exception, bool isVoid )
			: this( buffer, messageId, null, exception, isVoid )
		{
			if ( exception == null )
			{
				throw new ArgumentNullException( "exception" );
			}

			Contract.EndContractBlock();
		}

		private ResponseMessageSerializationContext( RpcOutputBuffer buffer, int messageId, object returnValue, RpcException exception, bool isVoid )
			: base( buffer )
		{
			this._messageId = messageId;
			this._returnValue = returnValue;
			this._isVoid = isVoid;
			this._exception = exception;
		}

		/// <summary>
		///		Swallow exception and reject original exception.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		///		<see cref="IsVoid"/> is false.
		///		When swallow exception for non-void method, alternate return value must be specified.
		/// </exception>
		public void SwallowException()
		{
			if ( !this._isVoid )
			{
				throw new InvalidOperationException( "Must specify alternate return value." );
			}

			Contract.EndContractBlock();

			this._exception = null;
		}

		/// <summary>
		///		Swallow exception and reject original exception.
		/// </summary>
		/// <param name="alternateReturnValue">
		///		Alternate return value to be returned to client.
		/// </param>
		/// <exception cref="InvalidOperationException">
		///		<see cref="IsVoid"/> is true.
		///		When swallow exception for void method, use <see cref="SwallowException()"/> instead.
		/// </exception>
		public void SwallowException( object alternateReturnValue )
		{
			if ( this._isVoid )
			{
				throw new InvalidOperationException( "Cannot specify alternate return value due to this method return value is void." );
			}

			Contract.EndContractBlock();

			this._exception = null;
			this._returnValue = alternateReturnValue;
		}

		/// <summary>
		///		Throw specified exception in client, and reject original return value or exception.
		/// </summary>
		/// <param name="alternateException">
		///		Alaternate exception to be thrown in client.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="alternateException"/> is null.
		/// </exception>
		public void ThrowException( RpcException alternateException )
		{
			if ( alternateException == null )
			{
				throw new ArgumentNullException( "alternateException" );
			}

			Contract.EndContractBlock();

			this._returnValue = null;
			this._exception = alternateException;
		}

		/// <summary>
		///		Throw specified exception in client, and reject original return value.
		/// </summary>
		/// <param name="alternateReturnValue">
		///		Alaternate exception to be return to client.
		/// </param>
		/// <exception cref="InvalidOperationException">
		///		<see cref="IsVoid"/> is true.
		///		Or, <see cref="Exception"/> is not null.
		///		To return value instead of Exception, use <see cref="SwallowException(Object)"/>.
		/// </exception>
		public void ChangeReturnValue( object alternateReturnValue )
		{
			if ( this._isVoid )
			{
				throw new InvalidOperationException( "Cannot specify alternate return value due to this method return value is void." );
			}

			if ( this._exception != null )
			{
				throw new InvalidOperationException( "Cannot specify alternate return value due to an exception was occurred." );
			}

			Contract.EndContractBlock();

			this._returnValue = alternateReturnValue;
		}
	}
}
