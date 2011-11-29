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
using MsgPack.Collections;
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc.Serialization
{
	// FIXME: test
	/// <summary>
	///		Serialize outgoing request/notification message and deserialize incoming request/notification message.
	/// </summary>
	public sealed class RequestMessageSerializer
	{
		private readonly IList<IFilterProvider<RequestMessageSerializationFilter>> _preSerializationFilters;
		private readonly IList<IFilterProvider<SerializedMessageFilter<MessageSerializationContext>>> _postSerializationFilters;
		private readonly IList<IFilterProvider<SerializedMessageFilter<MessageDeserializationContext>>> _preDeserializationFilters;
		private readonly IList<IFilterProvider<RequestMessageDeserializationFilter>> _postDeserializationFilters;
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
		public RequestMessageSerializer(
			IList<IFilterProvider<RequestMessageSerializationFilter>> preSerializationFilters,
			IList<IFilterProvider<SerializedMessageFilter<MessageSerializationContext>>> postSerializationFilters,
			IList<IFilterProvider<SerializedMessageFilter<MessageDeserializationContext>>> preDeserializationFilters,
			IList<IFilterProvider<RequestMessageDeserializationFilter>> postDeserializationFilters,
			int? maxRequestLength
			)
		{
			Contract.Assert( maxRequestLength.GetValueOrDefault() >= 0 );

			this._preSerializationFilters = preSerializationFilters ?? Arrays<IFilterProvider<RequestMessageSerializationFilter>>.Empty;
			this._postSerializationFilters = postSerializationFilters ?? Arrays<IFilterProvider<SerializedMessageFilter<MessageSerializationContext>>>.Empty;
			this._preDeserializationFilters = preDeserializationFilters ?? Arrays<IFilterProvider<SerializedMessageFilter<MessageDeserializationContext>>>.Empty;
			this._postDeserializationFilters = postDeserializationFilters ?? Arrays<IFilterProvider<RequestMessageDeserializationFilter>>.Empty;
			this._maxRequestLength = maxRequestLength;
		}

		/// <summary>
		///		Serialize RPC call to specified buffer.
		/// </summary>
		/// <param name="messageId">ID of message. If message is notification message, specify null.</param>
		/// <param name="method">Method name to be called.</param>
		/// <param name="arguments">Arguments of method call.</param>
		/// <param name="buffer">Buffer to be set serialized stream.</param>
		/// <returns>Error message of serialization.</returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="method"/>, <paramref name="arguments"/>, or <paramref name="buffer"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="method"/> is illegal.
		/// </exception>
		public RpcErrorMessage Serialize( int? messageId, string method, IList<object> arguments, RpcOutputBuffer buffer )
		{
			if ( method == null )
			{
				throw new ArgumentNullException( "method" );
			}

			// TODO: more strict validation.
			if ( String.IsNullOrWhiteSpace( method ) )
			{
				throw new ArgumentException( "'method' must not be empty nor blank.", "method" );
			}

			if ( arguments == null )
			{
				throw new ArgumentNullException( "arguments" );
			}

			if ( buffer == null )
			{
				throw new ArgumentNullException( "buffer" );
			}

			Contract.EndContractBlock();

			var context = new RequestMessageSerializationContext( buffer, messageId, method, arguments );

			foreach ( var preSerializationFilter in this._preSerializationFilters )
			{
				preSerializationFilter.GetFilter().Process( context );
				if ( !context.SerializationError.IsSuccess )
				{
					return context.SerializationError;
				}
			}

			SerializeCore( buffer, messageId, context );
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

		private static void SerializeCore( RpcOutputBuffer buffer, int? messageId, RequestMessageSerializationContext context )
		{
			using ( var stream = buffer.OpenWriteStream() )
			using ( var packer = Packer.Create( stream ) )
			{
				packer.PackArrayHeader( messageId == null ? 3 : 4 );
				packer.Pack( ( int )( messageId == null ? MessageType.Notification : MessageType.Request ) );

				if ( messageId != null )
				{
					packer.Pack( unchecked( ( uint )messageId.Value ) );
				}

				packer.PackString( context.MethodName );
				packer.PackItems( context.Arguments );
			}
		}

		/// <summary>
		///		Deserialize from specified buffer.
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
		public RpcErrorMessage Deserialize( IEnumerable<byte> input, out RequestMessage result )
		{
			if ( input == null )
			{
				throw new ArgumentNullException( "input" );
			}

			Contract.EndContractBlock();

			var context = new RequestMessageDeserializationContext( input, this._maxRequestLength );

			var sequence = context.ReadBytes();

			foreach ( var preDeserializationFilter in this._preDeserializationFilters )
			{
				sequence = preDeserializationFilter.GetFilter().Process( sequence, context );
				if ( !context.SerializationError.IsSuccess )
				{
					result = default( RequestMessage );
					return context.SerializationError;
				}
			}

			DeserializeCore( context );

			if ( !context.SerializationError.IsSuccess )
			{
				result = default( RequestMessage );
				return context.SerializationError;
			}

			Contract.Assert( !String.IsNullOrWhiteSpace( context.MethodName ) );
			Contract.Assert( context.Arguments != null );

			foreach ( var postDeserializationFilter in this._postDeserializationFilters )
			{
				postDeserializationFilter.GetFilter().Process( context );
				if ( !context.SerializationError.IsSuccess )
				{
					result = default( RequestMessage );
					return context.SerializationError;
				}
			}

			if ( String.IsNullOrWhiteSpace( context.MethodName ) )
			{
				throw new InvalidOperationException( "Filter became method null or empty." );
			}

			if ( context.Arguments == null )
			{
				throw new InvalidOperationException( "Filter became arguments null." );
			}

			result = new RequestMessage( context.MessageId, context.MethodName, context.Arguments );
			return RpcErrorMessage.Success;
		}

		private static void DeserializeCore( RequestMessageDeserializationContext context )
		{
			using ( var unpacker = new Unpacker( context.ReadBytes() ) )
			{
				var request = unpacker.UnpackObject();
				if ( request == null )
				{
					if ( context.SerializationError.IsSuccess )
					{
						// Since entire stream was readed and its length was in quota, the stream may be coruppted.
						context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Cannot deserialize message stream." ) );
					}

					return;
				}

				if ( !request.Value.IsTypeOf<IList<MessagePackObject>>().GetValueOrDefault() )
				{
					context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Request message is not array." ) );
					return;
				}

				var requestFields = request.Value.AsList();
				if ( requestFields.Count > 4 || requestFields.Count < 3 )
				{
					context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Request message is not 3 nor 4 element array." ) );
					return;
				}

				if ( !requestFields[ 0 ].IsTypeOf<int>().GetValueOrDefault() )
				{
					context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Message type of request message is not int 32." ) );
					return;
				}

				int nextPosition = 1;
				switch ( ( MessageType )requestFields[ 0 ].AsInt32() )
				{
					case MessageType.Request:
					{
						if ( !requestFields[ nextPosition ].IsTypeOf<uint>().GetValueOrDefault() )
						{
							context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Message ID of request message is not uint32." ) );
							return;
						}

						// For CLS compliance store uint32 value as int32.
						unchecked
						{
							context.MessageId = ( int )requestFields[ nextPosition ].AsUInt32();
						}

						nextPosition++;
						break;
					}
					case MessageType.Notification:
					{
						break;
					}
					default:
					{
						context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Message type of request message is not Request(0) nor Notification(2)." ) );
						return;
					}
				}

				if ( !requestFields[ nextPosition ].IsTypeOf<string>().GetValueOrDefault() )
				{
					context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", String.Format( CultureInfo.CurrentCulture, "Method of request message (ID:{0}) is not raw. ", context.MessageId ) ) );
					return;
				}

				try
				{
					context.MethodName = MessagePackConvert.DecodeStringStrict( requestFields[ nextPosition ].AsBinary() );
				}
				catch ( InvalidOperationException ex )
				{
					context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", String.Format( CultureInfo.CurrentCulture, "Message ID:{0}: {1}", context.MessageId, ex.Message ) ) );
					return;
				}

				nextPosition++;

				if ( !requestFields[ nextPosition ].IsTypeOf<IList<MessagePackObject>>().GetValueOrDefault() )
				{
					context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", String.Format( CultureInfo.CurrentCulture, "Arguments of request message (ID:{0}) is not array.", context.MessageId ) ) );
					return;
				}

				context.Arguments = requestFields[ nextPosition ].AsList();
			}
		}
	}
}
