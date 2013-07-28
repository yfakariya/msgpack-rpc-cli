#region -- License Terms --
//
// MessagePack for CLI
//
// Copyright (C) 2010-2013 FUJIWARA, Yusuke
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
using System.Threading;
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
		internal bool UnpackRequestHeader( ServerRequestContext context )
		{
			Contract.Assert( context != null );

			if ( context.RootUnpacker == null )
			{
				context.UnpackingBuffer = new ByteArraySegmentStream( context.ReceivedData );
				context.RootUnpacker = Unpacker.Create( context.UnpackingBuffer, false );
				Interlocked.Increment( ref this._processing );
				context.RenewSessionId();
				if ( this._manager.Server.Configuration.ReceiveTimeout != null )
				{
					context.Timeout += this.OnReceiveTimeout;
					context.StartWatchTimeout( this._manager.Server.Configuration.ReceiveTimeout.Value );
				}

				if ( MsgPackRpcServerProtocolsTrace.ShouldTrace( MsgPackRpcServerProtocolsTrace.NewSession ) )
				{
					MsgPackRpcServerProtocolsTrace.TraceEvent(
						MsgPackRpcServerProtocolsTrace.NewSession,
						"New session is created. {{ \"SessionID\" : {0} }}",
						context.SessionId
					);
				}
			}

			if ( !context.ReadFromRootUnpacker() )
			{
				MsgPackRpcServerProtocolsTrace.TraceEvent( 
					MsgPackRpcServerProtocolsTrace.NeedRequestHeader,
					"Array header is needed. {{ \"SessionID\" : {0} }}", 
					context.SessionId 
				);
				return false;
			}

			if ( !context.RootUnpacker.IsArrayHeader )
			{
				this.HandleDeserializationError( context, "Invalid request/notify message stream. Message must be array.", () => context.UnpackingBuffer.ToArray() );
				return true;
			}

			if ( context.RootUnpacker.ItemsCount != 3 && context.RootUnpacker.ItemsCount != 4 )
			{
				this.HandleDeserializationError(
					context,
					String.Format(
						CultureInfo.CurrentCulture,
						"Invalid request/notify message stream. Message must be valid size array. Actual size is {0}.",
						context.RootUnpacker.ItemsCount
					),
					() => context.UnpackingBuffer.ToArray()
				);
				return true;
			}

			context.HeaderUnpacker = context.RootUnpacker.ReadSubtree();
			context.NextProcess = UnpackMessageType;
			return context.NextProcess( context );
		}

		/// <summary>
		///		Unpack Message Type part on request/notification message.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the pipeline is finished;
		///		<c>false</c>, the pipeline is interruppted because extra data is needed.
		/// </returns>
		private bool UnpackMessageType( ServerRequestContext context )
		{
			Contract.Assert( context != null );
			
			if ( !context.ReadFromHeaderUnpacker() )
			{
				MsgPackRpcServerProtocolsTrace.TraceEvent( 
					MsgPackRpcServerProtocolsTrace.NeedMessageType,
					"Message Type is needed. {{ \"SessionID\" : {0} }}",
					context.SessionId 
				);
				return false;
			}

			int numericType;
			try
			{
				numericType = context.HeaderUnpacker.LastReadData.AsInt32();
			}
			catch ( InvalidOperationException )
			{
				this.HandleDeserializationError( context, "Invalid request/notify message stream. Message Type must be Int32 compatible integer.", () => context.UnpackingBuffer.ToArray() );
				return true;
			}

			MessageType type = ( MessageType )numericType;
			context.MessageType = type;

			switch ( type )
			{
				case MessageType.Request:
				{
					context.NextProcess = this.UnpackMessageId;
					break;
				}
				case MessageType.Notification:
				{
					context.NextProcess = this.UnpackMethodName;
					break;
				}
				default:
				{
					this.HandleDeserializationError(
						context,
						String.Format( CultureInfo.CurrentCulture, "Unknown message type '{0:x8}'", numericType ),
						() => context.UnpackingBuffer.ToArray()
					);
					return true;
				}
			}

			return context.NextProcess( context );
		}

		/// <summary>
		///		Unpack Message ID part on request message.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the pipeline is finished;
		///		<c>false</c>, the pipeline is interruppted because extra data is needed.
		/// </returns>
		private bool UnpackMessageId( ServerRequestContext context )
		{
			Contract.Assert( context != null );

			if ( !context.ReadFromHeaderUnpacker() )
			{
				MsgPackRpcServerProtocolsTrace.TraceEvent( 
					MsgPackRpcServerProtocolsTrace.NeedMessageId,
					"Message ID is needed. {{ \"SessionID\" : {0} }}", 
					context.SessionId 
				);
				return false;
			}

			try
			{
				context.MessageId = unchecked( ( int )context.HeaderUnpacker.LastReadData.AsUInt32() );
			}
			catch ( InvalidOperationException )
			{
				this.HandleDeserializationError(
					context,
					"Invalid request message stream. ID must be UInt32 compatible integer.",
					() => context.UnpackingBuffer.ToArray()
				);
				return true;
			}

			context.NextProcess = this.UnpackMethodName;
			return context.NextProcess( context );
		}

		/// <summary>
		///		Unpack Method Name part on request/notification message.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the pipeline is finished;
		///		<c>false</c>, the pipeline is interruppted because extra data is needed.
		/// </returns>
		private bool UnpackMethodName( ServerRequestContext context )
		{
			Contract.Assert( context != null );

			if ( !context.ReadFromHeaderUnpacker() )
			{
				MsgPackRpcServerProtocolsTrace.TraceEvent( 
					MsgPackRpcServerProtocolsTrace.NeedMethodName,
					"Method Name is needed. {{ \"SessionID\" : {0} }}",
					context.SessionId 
				);
				return false;
			}

			try
			{
				context.MethodName = context.HeaderUnpacker.LastReadData.AsString();
			}
			catch ( InvalidOperationException )
			{
				this.HandleDeserializationError(
					context,
					"Invalid request/notify message stream. Method name must be UTF-8 string.",
					() => context.UnpackingBuffer.ToArray()
				);
				return true;
			}

			context.NextProcess = this.UnpackArgumentsHeader;
			return context.NextProcess( context );
		}

		/// <summary>
		///		Unpack array header of Arguments part on request/notification message.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the pipeline is finished;
		///		<c>false</c>, the pipeline is interruppted because extra data is needed.
		/// </returns>
		private bool UnpackArgumentsHeader( ServerRequestContext context )
		{
			Contract.Assert( context != null );

			if ( !context.ReadFromHeaderUnpacker() )
			{
				MsgPackRpcServerProtocolsTrace.TraceEvent( 
					MsgPackRpcServerProtocolsTrace.NeedArgumentsArrayHeader,
					"Arguments array header is needed. {{ \"SessionID\" : {0} }}",
					context.SessionId 
				);
				return false;
			}

			if ( !context.HeaderUnpacker.IsArrayHeader )
			{
				this.HandleDeserializationError(
					context,
					"Invalid request/notify message stream. Arguments must be array.",
					() => context.UnpackingBuffer.ToArray()
				);
				return true;
			}

			// TODO: Avoid actual unpacking to improve performance.
			context.ArgumentsBufferUnpacker = context.HeaderUnpacker.ReadSubtree();

			if ( Int32.MaxValue < context.ArgumentsBufferUnpacker.ItemsCount )
			{
				this.HandleDeserializationError(
					context,
					RpcError.MessageTooLargeError,
					"Too many arguments.",
					context.ArgumentsBufferUnpacker.ItemsCount.ToString( "#,0", CultureInfo.CurrentCulture ),
					() => context.UnpackingBuffer.ToArray()
				);
				return true;
			}

			context.ArgumentsCount = unchecked( ( int )( context.ArgumentsBufferUnpacker.ItemsCount ) );
			context.ArgumentsBufferPacker = Packer.Create( context.ArgumentsBuffer, false );
			context.ArgumentsBufferPacker.PackArrayHeader( context.ArgumentsCount );
			context.UnpackedArgumentsCount = 0;
			context.NextProcess = this.UnpackArguments;
			return context.NextProcess( context );
		}

		/// <summary>
		///		Unpack array elements of Arguments part on request/notification message.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the pipeline is finished;
		///		<c>false</c>, the pipeline is interruppted because extra data is needed.
		/// </returns>
		private bool UnpackArguments( ServerRequestContext context )
		{
			Contract.Assert( context != null );
			
			while ( context.UnpackedArgumentsCount < context.ArgumentsCount )
			{
				if ( !context.ReadFromArgumentsBufferUnpacker() )
				{
					MsgPackRpcServerProtocolsTrace.TraceEvent( 
						MsgPackRpcServerProtocolsTrace.NeedArgumentsElement,
						"Arguments array element is needed. {0}/{1}  {{ \"SessionID\" : {2} }}",
						context.UnpackedArgumentsCount, 
						context.ArgumentsCount, 
						context.SessionId 
					);
					return false;
				}

				context.ArgumentsBufferPacker.Pack( context.ArgumentsBufferUnpacker.LastReadData );
				context.UnpackedArgumentsCount++;
			}

			context.ArgumentsBuffer.Position = 0;
			context.ArgumentsUnpacker = Unpacker.Create( context.ArgumentsBuffer, false );
			context.NextProcess = this.Dispatch;
			return context.NextProcess( context );
		}

		/// <summary>
		///		Dispatch request/notification message via the <see cref="MsgPack.Rpc.Server.Dispatch.Dispatcher"/>.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the pipeline is finished;
		///		<c>false</c>, the pipeline is interruppted because extra data is needed.
		/// </returns>
		private bool Dispatch( ServerRequestContext context )
		{
			Contract.Assert( context != null );

			context.StopWatchTimeout();
			context.Timeout -= this.OnReceiveTimeout;

			context.ClearBuffers();

			var isNotification = context.MessageType == MessageType.Notification;
			try
			{
				this._dispatcher.Dispatch(
					this,
					context
				);
			}
			finally
			{
				context.ClearDispatchContext();

				if ( isNotification )
				{
					this.OnSessionFinished();
				}
			}

			context.NextProcess = this.UnpackRequestHeader;

			if ( context.UnpackingBuffer.Length > 0 )
			{
				// Subsequent request is already arrived.
				return context.NextProcess( context );
			}
			else
			{
				// Try receive subsequent.
				return true;
			}
		}
	}
}
