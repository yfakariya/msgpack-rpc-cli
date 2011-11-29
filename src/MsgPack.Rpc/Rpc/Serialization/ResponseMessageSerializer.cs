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
using System.IO;
using System.Linq;
using MsgPack.Rpc.Protocols;
using MsgPack.Collections;

namespace MsgPack.Rpc.Serialization
{
	// FIXME: test
	/// <summary>
	///		Serialize outgoing response message and deserialize incoming response message.
	/// </summary>
	public sealed class ResponseMessageSerializer
	{
		private readonly IList<IFilterProvider<ResponseMessageSerializationFilter>> _preSerializationFilters;
		private readonly IList<IFilterProvider<SerializedMessageFilter<MessageSerializationContext>>> _postSerializationFilters;
		private readonly IList<IFilterProvider<SerializedMessageFilter<MessageDeserializationContext>>> _preDeserializationFilters;
		private readonly IList<IFilterProvider<ResponseMessageDeserializationFilter>> _postDeserializationFilters;
		private readonly int? _maxRequestLength;

		/// <summary>
		///		Initialize new instance.
		/// </summary>
		/// <param name="preSerializationFilters">
		///		Filters to process unserialized value before serialization. This value can be null.
		/// </param>
		/// <param name="postSerializationFilters">
		///		Filters to process serialized binary stream after serialization. This value can be null.
		/// </param>
		/// <param name="preDeserializationFilters">
		///		Filters to process undeserialized binary stream before deserialization. This value can be null.
		/// </param>
		/// <param name="postDeserializationFilters">
		///		Filters to process deserialized value after deserialization. This value can be null.
		/// </param>
		/// <param name="maxRequestLength">
		///		Quota value for incoming binary length. This value can be null.
		/// </param>
		public ResponseMessageSerializer(
			IList<IFilterProvider<ResponseMessageSerializationFilter>> preSerializationFilters,
			IList<IFilterProvider<SerializedMessageFilter<MessageSerializationContext>>> postSerializationFilters,
			IList<IFilterProvider<SerializedMessageFilter<MessageDeserializationContext>>> preDeserializationFilters,
			IList<IFilterProvider<ResponseMessageDeserializationFilter>> postDeserializationFilters,
			int? maxRequestLength
			)
		{
			Contract.Assert( maxRequestLength.GetValueOrDefault() >= 0 );

			this._preSerializationFilters = preSerializationFilters ?? Arrays<IFilterProvider<ResponseMessageSerializationFilter>>.Empty;
			this._postSerializationFilters = postSerializationFilters ?? Arrays<IFilterProvider<SerializedMessageFilter<MessageSerializationContext>>>.Empty;
			this._preDeserializationFilters = preDeserializationFilters ?? Arrays<IFilterProvider<SerializedMessageFilter<MessageDeserializationContext>>>.Empty;
			this._postDeserializationFilters = postDeserializationFilters ?? Arrays<IFilterProvider<ResponseMessageDeserializationFilter>>.Empty;
			this._maxRequestLength = maxRequestLength;
		}

		/// <summary>
		///		Serialize response message to specified buffer.
		/// </summary>
		/// <param name="messageId">ID of message.</param>
		/// <param name="returnValue">Return value of the method.</param>
		/// <param name="isVoid">If the method is void, then true.</param>
		/// <param name="exception">Exception thrown from the method.</param>
		/// <param name="buffer">Buffer to be stored serialized response stream.</param>
		/// <returns>Error information.</returns>
		public RpcErrorMessage Serialize( int messageId, object returnValue, bool isVoid, RpcException exception, RpcOutputBuffer buffer )
		{
			var context =
				exception != null
				? new ResponseMessageSerializationContext( buffer, messageId, exception, isVoid )
				: ( isVoid ? new ResponseMessageSerializationContext( buffer, messageId ) : new ResponseMessageSerializationContext( buffer, messageId, returnValue ) );

			foreach ( var preSerializationFilter in this._preSerializationFilters )
			{
				preSerializationFilter.GetFilter().Process( context );
				if ( !context.SerializationError.IsSuccess )
				{
					return context.SerializationError;
				}
			}

			SerializeCore( MessageType.Response, messageId, context );
			if ( !context.SerializationError.IsSuccess )
			{
				return context.SerializationError;
			}

			foreach ( var postSerializationFilter in this._postSerializationFilters )
			{
				using ( var swapper = buffer.CreateSwapper() )
				{
					swapper.WriteBytes( postSerializationFilter.GetFilter().Process( swapper.ReadBytes(), context ) );
					if ( !context.SerializationError.IsSuccess )
					{
						return context.SerializationError;
					}
				}
			}

			return RpcErrorMessage.Success;
		}

		private static void SerializeCore( MessageType messageType, int messageId, ResponseMessageSerializationContext context )
		{
			using ( var stream = context.Buffer.OpenWriteStream() )
			using ( var packer = Packer.Create( stream ) )
			{
				packer.PackArrayHeader( 4 );
				packer.Pack( ( int )messageType );
				packer.Pack( unchecked( ( uint )messageId ) );
				if ( context.Exception == null )
				{
					packer.PackNull();
					packer.PackObject( context.ReturnValue );
				}
				else
				{
					packer.PackString( context.Exception.RpcError.Identifier );
					packer.Pack( context.Exception.GetExceptionMessage( false ) );
				}
			}
		}

		/// <summary>
		///		Deserialize response message from specified buffer.
		/// </summary>
		/// <param name="input">Buffer which stores serialized request/notification stream.</param>
		/// <param name="result">Deserialied packed message will be stored.</param>
		/// <returns>Error information.</returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="input"/> is null.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///		Some filters violate contract.
		/// </exception>
		public RpcErrorMessage Deserialize( IEnumerable<byte> input, out ResponseMessage result )
		{
			if ( input == null )
			{
				throw new ArgumentNullException( "input" );
			}

			Contract.EndContractBlock();

			var context = new ResponseMessageDeserializationContext( input, this._maxRequestLength );
			var sequence = context.ReadBytes();

			foreach ( var preDeserializationFilter in this._preDeserializationFilters )
			{
				var processed = preDeserializationFilter.GetFilter().Process( sequence, context );
				if ( !context.SerializationError.IsSuccess )
				{
					result = default( ResponseMessage );
					return context.SerializationError;
				}

				if ( processed == null )
				{
					throw new InvalidOperationException( "Deserialization filter did not return sequence." );
				}

				sequence = processed;
			}

			DeserializeCore( sequence, context );

			if ( !context.SerializationError.IsSuccess )
			{
				result = default( ResponseMessage );
				return context.SerializationError;
			}

			foreach ( var postDeserializationFilter in this._postDeserializationFilters )
			{
				postDeserializationFilter.GetFilter().Process( context );
				if ( !context.SerializationError.IsSuccess )
				{
					result = default( ResponseMessage );
					return context.SerializationError;
				}
			}

			if ( !context.Error.IsNil )
			{
				result = new ResponseMessage( context.MessageId, RpcException.FromMessage( context.Error, context.DeserializedResult ) );
			}
			else
			{
				result = new ResponseMessage( context.MessageId, context.DeserializedResult );
			}

			return RpcErrorMessage.Success;
		}

		private static void DeserializeCore( IEnumerable<byte> sequence, ResponseMessageDeserializationContext context )
		{
			using ( var unpacker = new Unpacker( sequence ) )
			{
				MessagePackObject? response = unpacker.UnpackObject();
				if ( response == null )
				{
					if ( context.SerializationError.IsSuccess )
					{
						// Since entire stream was readed and its length was in quota, the stream may be coruppted.
						context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Cannot deserialize message stream." ) );
					}

					return;
				}

				if ( !response.Value.IsTypeOf<IList<MessagePackObject>>().GetValueOrDefault() )
				{
					context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Response message is not array." ) );
					return;
				}

				var requestFields = response.Value.AsList();
				if ( requestFields.Count != 4 )
				{
					context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Response message is not 4 element array." ) );
					return;
				}

				if ( !requestFields[ 0 ].IsTypeOf<int>().GetValueOrDefault() )
				{
					context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Message type of response message is not int 32." ) );
					return;
				}

				if ( requestFields[ 0 ].AsInt32() != ( int )MessageType.Response )
				{
					context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Message type of response message is not Response(2)." ) );
					return;
				}

				if ( !requestFields[ 1 ].IsTypeOf<uint>().GetValueOrDefault() )
				{
					context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Message ID of response message is not int32." ) );
					return;
				}

				// For CLS compliance store uint32 value as int32.
				unchecked
				{
					context.MessageId = ( int )requestFields[ 1 ].AsUInt32();
				}

				// Error is should be string identifier of error, but arbitary objects are supported.
				context.Error = requestFields[ 2 ];

				// If error is specified, this value should be nil by original spec, but currently should specify error information.
				context.DeserializedResult = requestFields[ 3 ];
			}
		}
	}
}
