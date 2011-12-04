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
using System.Globalization;
using System.IO;
using System.Linq;
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc.Server.Protocols
{
	partial class ServerTransport
	{
		/// <summary>
		///		Unpack request/notification message array header.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the pipeline is finished;
		///		<c>false</c>, the pipeline is interruppted because extra data is needed.
		/// </returns>
		private bool UnpackRequestHeader( ServerSocketAsyncEventArgs context )
		{
			if ( this._deserializationState.RootUnpacker == null )
			{
				this._deserializationState.UnpackingBuffer = new ByteArraySegmentStream( context.ReceivedData );
				this._deserializationState.RootUnpacker = Unpacker.Create( this._deserializationState.UnpackingBuffer, false );
			}

			if ( !this._deserializationState.RootUnpacker.Read() )
			{
				Tracer.Protocols.TraceEvent( Tracer.EventType.NeedRequestHader, Tracer.EventId.NeedRequestHader, "Array header is needed." );
				return false;
			}

			if ( !this._deserializationState.RootUnpacker.IsArrayHeader )
			{
				// FIXME: Error handler
				var array = this._deserializationState.UnpackingBuffer.ToArray();
				Tracer.Protocols.TraceData( Tracer.EventType.DumpInvalidRequestHeader, Tracer.EventId.DumpInvalidRequestHeader, BitConverter.ToString( array ), array );
				throw new InvalidMessagePackStreamException( "Invalid request/notify message stream. Message must be array." );
			}

			if ( this._deserializationState.RootUnpacker.ItemsCount != 3 && this._deserializationState.RootUnpacker.ItemsCount != 4 )
			{
				// FIXME: Error handler
				throw new InvalidMessagePackStreamException(
					String.Format(
						CultureInfo.CurrentCulture,
						"Invalid request/notify message stream. Message must be valid size array. Actual size is {0}.",
						this._deserializationState.RootUnpacker.ItemsCount
					)
				);
			}

			this._deserializationState.HeaderUnpacker = this._deserializationState.RootUnpacker.ReadSubtree();
			this._deserializationState.NextProcess = UnpackMessageType;
			return this._deserializationState.NextProcess( context );
		}

		/// <summary>
		///		Unpack Message Type part on request/notification message.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the pipeline is finished;
		///		<c>false</c>, the pipeline is interruppted because extra data is needed.
		/// </returns>
		private bool UnpackMessageType( ServerSocketAsyncEventArgs context )
		{
			if ( !this._deserializationState.HeaderUnpacker.Read() )
			{
				Tracer.Protocols.TraceEvent( Tracer.EventType.NeedMessageType, Tracer.EventId.NeedMessageType, "Message Type is needed." );
				return false;
			}

			int numericType;
			try
			{
				numericType = this._deserializationState.HeaderUnpacker.Data.Value.AsInt32();
			}
			catch ( InvalidOperationException ex )
			{
				// FIXME: Error handler
				throw new InvalidMessagePackStreamException( "Invalid request/notify message stream. Message Type must be Int32 compatible integer.", ex );
			}

			MessageType type = ( MessageType )numericType;
			this._deserializationState.MessageType = type;

			switch ( type )
			{
				case MessageType.Request:
				{
					this._deserializationState.NextProcess = this.UnpackMessageId;
					break;
				}
				case MessageType.Notification:
				{
					this._deserializationState.NextProcess = this.UnpackMethodName;
					break;
				}
				default:
				{
					// FIXME: Error handler
					throw new InvalidMessagePackStreamException( String.Format( CultureInfo.CurrentCulture, "Unknown message type '{0:x8}'", numericType ) );
				}
			}

			return this._deserializationState.NextProcess( context );
		}

		/// <summary>
		///		Unpack Message ID part on request message.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the pipeline is finished;
		///		<c>false</c>, the pipeline is interruppted because extra data is needed.
		/// </returns>
		private bool UnpackMessageId( ServerSocketAsyncEventArgs context )
		{
			if ( !this._deserializationState.HeaderUnpacker.Read() )
			{
				Tracer.Protocols.TraceEvent( Tracer.EventType.NeedMessageId, Tracer.EventId.NeedMessageId, "Message ID is needed." );
				return false;
			}

			try
			{
				context.Id = this._deserializationState.HeaderUnpacker.Data.Value.AsUInt32();
			}
			catch ( InvalidOperationException ex )
			{
				// FIXME: Error handler
				throw new InvalidMessagePackStreamException( "Invalid request message stream. ID must be UInt32 compatible integer.", ex );
			}

			this._deserializationState.NextProcess = this.UnpackMethodName;
			return this._deserializationState.NextProcess( context );
		}

		/// <summary>
		///		Unpack Method Name part on request/notification message.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the pipeline is finished;
		///		<c>false</c>, the pipeline is interruppted because extra data is needed.
		/// </returns>
		private bool UnpackMethodName( ServerSocketAsyncEventArgs context )
		{
			if ( !this._deserializationState.HeaderUnpacker.Read() )
			{
				Tracer.Protocols.TraceEvent( Tracer.EventType.NeedMethodName, Tracer.EventId.NeedMethodName, "Method Name is needed." );
				return false;
			}

			try
			{
				this._deserializationState.MethodName = this._deserializationState.HeaderUnpacker.Data.Value.AsString();
			}
			catch ( InvalidOperationException ex )
			{
				// FIXME: Error handler
				throw new InvalidMessagePackStreamException( "Invalid request/notify message stream. Method name must be UTF-8 string.", ex );
			}

			this._deserializationState.NextProcess = this.UnpackArgumentsHeader;
			return this._deserializationState.NextProcess( context );
		}

		/// <summary>
		///		Unpack array header of Arguments part on request/notification message.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the pipeline is finished;
		///		<c>false</c>, the pipeline is interruppted because extra data is needed.
		/// </returns>
		private bool UnpackArgumentsHeader( ServerSocketAsyncEventArgs context )
		{
			if ( !this._deserializationState.HeaderUnpacker.Read() )
			{
				Tracer.Protocols.TraceEvent( Tracer.EventType.NeedArgumentsArrayHeader, Tracer.EventId.NeedArgumentsArrayHeader, "Arguments array header is needed." );
				return false;
			}

			if ( !this._deserializationState.HeaderUnpacker.IsArrayHeader )
			{
				// FIXME: Error handler
				throw new InvalidMessagePackStreamException( "Invalid request/notify message stream. Arguments must be array." );
			}

			this._deserializationState.ArgumentsBufferUnpacker = this._deserializationState.HeaderUnpacker.ReadSubtree();

			if ( Int32.MaxValue < this._deserializationState.ArgumentsBufferUnpacker.ItemsCount )
			{
				// FIXME: Error handler
				throw new NotSupportedException( "Too many arguments." );
			}

			this._deserializationState.ArgumentsCount = unchecked( ( int )( this._deserializationState.ArgumentsBufferUnpacker.ItemsCount ) );
			this._deserializationState.ArgumentsBufferPacker = Packer.Create( this._deserializationState.ArgumentsBuffer, false );
			this._deserializationState.ArgumentsBufferPacker.PackArrayHeader( this._deserializationState.ArgumentsCount );
			this._deserializationState.UnpackedArgumentsCount = 0;
			this._deserializationState.NextProcess = this.UnpackArguments;
			return this._deserializationState.NextProcess( context );
		}

		/// <summary>
		///		Unpack array elements of Arguments part on request/notification message.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the pipeline is finished;
		///		<c>false</c>, the pipeline is interruppted because extra data is needed.
		/// </returns>
		private bool UnpackArguments( ServerSocketAsyncEventArgs context )
		{
			while ( this._deserializationState.UnpackedArgumentsCount < this._deserializationState.ArgumentsCount )
			{
				if ( !this._deserializationState.ArgumentsBufferUnpacker.Read() )
				{
					Tracer.Protocols.TraceEvent( Tracer.EventType.NeedArgumentsElement, Tracer.EventId.NeedArgumentsElement, "Arguments array element is needed. {0}/{1}", this._deserializationState.UnpackedArgumentsCount, this._deserializationState.ArgumentsCount );
					return false;
				}

				this._deserializationState.ArgumentsBufferPacker.Pack( this._deserializationState.ArgumentsBufferUnpacker.Data.Value );
				this._deserializationState.UnpackedArgumentsCount++;
			}

			this._deserializationState.ArgumentsBuffer.Position = 0;
			this._deserializationState.ArgumentsUnpacker = Unpacker.Create( this._deserializationState.ArgumentsBuffer, false );
			this._deserializationState.NextProcess = this.Dispatch;
			return this._deserializationState.NextProcess( context );
		}

		/// <summary>
		///		Dispatch request/notification message via the <see cref="MessageRecieved"/> event.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the pipeline is finished;
		///		<c>false</c>, the pipeline is interruppted because extra data is needed.
		/// </returns>
		private bool Dispatch( ServerSocketAsyncEventArgs context )
		{
			this._state = TransportState.Reserved;

			this._deserializationState.ClearBuffers();

			this.OnMessageReceivedCore(
				new RpcMessageReceivedEventArgs(
					this,
					this._deserializationState.MessageType,
					this._deserializationState.MessageType == MessageType.Notification ? default( int? ) : unchecked( ( int )context.Id ),
					this._deserializationState.MethodName,
					this._deserializationState.ArgumentsUnpacker
				)
			);

			if ( this._deserializationState.MessageType == MessageType.Notification )
			{
				this.Free();
			}

			return true;
		}

		/// <summary>
		///		Encapselates deserialization state.
		/// </summary>
		private sealed class DeserializationState
		{
			/// <summary>
			///		The initial process of the deserialization pipeline.
			/// </summary>
			private readonly Func<ServerSocketAsyncEventArgs, bool> _initialProcess;

			/// <summary>
			///		Next (that is, resuming) process on the deserialization pipeline.
			/// </summary>
			public Func<ServerSocketAsyncEventArgs, bool> NextProcess;

			/// <summary>
			///		Buffer that stores unpacking binaries received.
			/// </summary>
			public ByteArraySegmentStream UnpackingBuffer;


			/// <summary>
			///		<see cref="Unpacker"/> to unpack entire request/notification message.
			/// </summary>
			public Unpacker RootUnpacker;

			/// <summary>
			///		Subtree <see cref="Unpacker"/> to unpack request/notification message as array.
			/// </summary>
			public Unpacker HeaderUnpacker;


			/// <summary>
			///		Buffer to store binaries for arguments array for subsequent deserialization.
			/// </summary>
			public readonly MemoryStream ArgumentsBuffer;

			/// <summary>
			///		<see cref="Packer"/> to re-pack to binaries of arguments for subsequent deserialization.
			/// </summary>
			public Packer ArgumentsBufferPacker;

			/// <summary>
			///		Subtree <see cref="Unpacker"/> to parse arguments array as opaque sequence.
			/// </summary>
			public Unpacker ArgumentsBufferUnpacker;

			/// <summary>
			///		The count of declared method arguments.
			/// </summary>
			public int ArgumentsCount;

			/// <summary>
			///		The count of unpacked method arguments.
			/// </summary>
			public int UnpackedArgumentsCount;

			
			/// <summary>
			///		Unpacked Message Type part value.
			/// </summary>
			public MessageType MessageType;

			/// <summary>
			///		Unpacked Method Name part value.
			/// </summary>
			public string MethodName;

			/// <summary>
			///		<see cref="Unpacker"/> to deserialize arguments on the dispatcher.
			/// </summary>
			public Unpacker ArgumentsUnpacker;

			/// <summary>
			///		Initializes a new instance of the <see cref="DeserializationState"/> class.
			/// </summary>
			/// <param name="initialProcess">
			///		The initial process of the deserialization pipeline.
			///	</param>
			public DeserializationState( Func<ServerSocketAsyncEventArgs, bool> initialProcess )
			{
				this._initialProcess = initialProcess;
				this.NextProcess = initialProcess;
				// TODO: Configurable
				this.ArgumentsBuffer = new MemoryStream( 65536 );
			}

			private static bool InvalidFlow( ServerSocketAsyncEventArgs context )
			{
				throw new InvalidOperationException( "Invalid state transition." );
			}

			/// <summary>
			///		Clears the buffers to deserialize message, which is not required to dispatch and invoke server method.
			/// </summary>
			public void ClearBuffers()
			{
				this.NextProcess = InvalidFlow;

				if ( this.ArgumentsBufferUnpacker != null )
				{
					this.ArgumentsBufferUnpacker.Dispose();
					this.ArgumentsBufferUnpacker = null;
				}

				if ( this.ArgumentsBufferPacker != null )
				{
					this.ArgumentsBufferPacker.Dispose();
					this.ArgumentsBufferPacker = null;
				}

				this.ArgumentsCount = 0;
				this.UnpackedArgumentsCount = 0;
				if ( this.HeaderUnpacker != null )
				{
					this.HeaderUnpacker.Dispose();
					this.HeaderUnpacker = null;
				}

				if ( this.RootUnpacker != null )
				{
					this.RootUnpacker.Dispose();
					this.RootUnpacker = null;
				}

				if ( this.UnpackingBuffer != null )
				{
					this.TruncateUsedReceivedData();
					this.UnpackingBuffer.Dispose();
					this.UnpackingBuffer = null;
				}
			}

			/// <summary>
			///		Truncates the used segments from the received data.
			/// </summary>
			private void TruncateUsedReceivedData()
			{
				long remaining = this.UnpackingBuffer.Position;
				var segments = this.UnpackingBuffer.GetBuffer();
				while ( segments.Any() && 0 < remaining )
				{
					if ( segments[ 0 ].Count < remaining )
					{
						remaining -= segments[ 0 ].Count;
						segments.RemoveAt( 0 );
					}
					else
					{
						int newCount = segments[ 0 ].Count - unchecked( ( int )remaining );
						int newOffset = segments[ 0 ].Offset + unchecked( ( int )remaining );
						segments[ 0 ] = new ArraySegment<byte>( segments[ 0 ].Array, newOffset, newCount );
						remaining -= newCount;
					}
				}
			}

			/// <summary>
			///		Clears the dispatch context information.
			/// </summary>
			public void ClearDispatchContext()
			{
				this.NextProcess = this._initialProcess;

				this.MethodName = null;
				this.MessageType = MessageType.Response; // Invalid value.
				if ( this.ArgumentsUnpacker != null )
				{
					this.ArgumentsUnpacker.Dispose();
					this.ArgumentsUnpacker = null;
				}

				this.ArgumentsBuffer.SetLength( 0 );
			}
		}
	}
}
