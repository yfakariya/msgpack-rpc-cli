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
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc.Server.Protocols
{
	partial class ServerTransport
	{
		// TODO: Move to other layer e.g. Server.
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

		// TODO: Move to other layer e.g. Server.
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

		// TODO: Move to other layer e.g. Server.
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

		// TODO: Move to other layer e.g. Server.
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

		// TODO: Move to other layer e.g. Server.
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

		private bool Dispatch( ServerSocketAsyncEventArgs context )
		{
			this._state = TransportState.Reserved;
			if ( this._deserializationState.MessageType == MessageType.Notification )
			{
				this.Free();
			}

			try
			{
				this.OnMessageReceivedCore(
					new RpcMessageReceivedEventArgs(
						this,
						this._deserializationState.MessageType,
						this._deserializationState.MessageType == MessageType.Notification ? default( int? ) : unchecked( ( int )context.Id ),
						this._deserializationState.MethodName,
						this._deserializationState.ArgumentsUnpacker
					)
				);
			}
			finally
			{
				this.ClearReceiveContextData( context );
			}

			return true;
		}

		private void ClearReceiveContextData( ServerSocketAsyncEventArgs context )
		{
			this._deserializationState.Clear();
			context.ReceivedData.Clear();
			Array.Clear( context.ReceivingBuffer, 0, context.ReceivingBuffer.Length );
		}

		private sealed class DeserializationState
		{
			private readonly Func<ServerSocketAsyncEventArgs, bool> _initialProcess;

			public Func<ServerSocketAsyncEventArgs, bool> NextProcess;

			public ByteArraySegmentStream UnpackingBuffer;

			public Unpacker RootUnpacker;
			public Unpacker HeaderUnpacker;

			public readonly MemoryStream ArgumentsBuffer;
			public Packer ArgumentsBufferPacker;
			public Unpacker ArgumentsBufferUnpacker;
			public int ArgumentsCount;
			public int UnpackedArgumentsCount;

			public MessageType MessageType;
			public string MethodName;
			public Unpacker ArgumentsUnpacker;

			public DeserializationState( Func<ServerSocketAsyncEventArgs, bool> initialProcess )
			{
				this._initialProcess = initialProcess;
				this.NextProcess = initialProcess;
				// TODO: Configurable
				this.ArgumentsBuffer = new MemoryStream( 65536 );
			}

			public void Clear()
			{
				this.NextProcess = this._initialProcess;

				if ( this.ArgumentsUnpacker != null )
				{
					this.ArgumentsUnpacker.Dispose();
					this.ArgumentsUnpacker = null;
				}

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

				this.ArgumentsBuffer.SetLength( 0 );
				this.ArgumentsCount = 0;
				this.UnpackedArgumentsCount = 0;
				this.MethodName = null;
				this.MessageType = MessageType.Response; // Invalid value.
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
					this.UnpackingBuffer.Dispose();
					this.UnpackingBuffer = null;
				}
			}
		}
	}
}
