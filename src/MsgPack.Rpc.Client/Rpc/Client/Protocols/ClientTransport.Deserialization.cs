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
#if SILVERLIGHT
using System.IO.IsolatedStorage;
#endif
using System.Linq;
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc.Client.Protocols
{
	partial class ClientTransport
	{
		/// <summary>
		///		Unpack response message array header.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the pipeline is finished;
		///		<c>false</c>, the pipeline is interruppted because extra data is needed.
		/// </returns>
		internal bool UnpackResponseHeader( ClientResponseContext context )
		{
			Contract.Assert( context != null );

			if ( context.RootUnpacker == null )
			{
				context.UnpackingBuffer = new ByteArraySegmentStream( context.ReceivedData );
				context.RootUnpacker = Unpacker.Create( context.UnpackingBuffer, false );
				context.RenewSessionId();
			}

			if ( !context.ReadFromRootUnpacker() )
			{
				MsgPackRpcClientProtocolsTrace.TraceEvent( MsgPackRpcClientProtocolsTrace.NeedRequestHeader, "Array header is needed. {{ \"SessionID\" : {0} }}", context.SessionId );
				return false;
			}

			if ( !context.RootUnpacker.IsArrayHeader )
			{
				this.HandleDeserializationError( context, "Invalid response message stream. Message must be array.", () => context.UnpackingBuffer.ToArray() );
				return context.NextProcess( context );
			}

			if ( context.RootUnpacker.ItemsCount != 4 )
			{
				this.HandleDeserializationError(
					context,
					String.Format(
						CultureInfo.CurrentCulture,
						"Invalid response message stream. Message must be valid size array. Actual size is {0}.",
						context.RootUnpacker.ItemsCount
					),
					() => context.UnpackingBuffer.ToArray()
				);
				return context.NextProcess( context );
			}

			context.HeaderUnpacker = context.RootUnpacker.ReadSubtree();
			context.NextProcess = UnpackMessageType;
			return context.NextProcess( context );
		}

		/// <summary>
		///		Unpack Message Type part on response message.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the pipeline is finished;
		///		<c>false</c>, the pipeline is interruppted because extra data is needed.
		/// </returns>
		private bool UnpackMessageType( ClientResponseContext context )
		{
			if ( !context.ReadFromHeaderUnpacker() )
			{
				MsgPackRpcClientProtocolsTrace.TraceEvent( MsgPackRpcClientProtocolsTrace.NeedMessageType, "Message Type is needed. {{ \"SessionID\" : {0} }}", context.SessionId );
				return false;
			}

			int numericType;
			try
			{
				numericType = context.HeaderUnpacker.LastReadData.AsInt32();
			}
			catch ( InvalidOperationException )
			{
				this.HandleDeserializationError( context, "Invalid response message stream. Message Type must be Int32 compatible integer.", () => context.UnpackingBuffer.ToArray() );
				return context.NextProcess( context );
			}

			MessageType type = ( MessageType )numericType;
			if ( type != MessageType.Response )
			{
				this.HandleDeserializationError(
					context,
					String.Format( CultureInfo.CurrentCulture, "Unknown message type '{0:x8}'", numericType ),
					() => context.UnpackingBuffer.ToArray()
				);
				return context.NextProcess( context );
			}

			context.NextProcess = this.UnpackMessageId;

			return context.NextProcess( context );
		}

		/// <summary>
		///		Unpack Message ID part on response message.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the pipeline is finished;
		///		<c>false</c>, the pipeline is interruppted because extra data is needed.
		/// </returns>
		private bool UnpackMessageId( ClientResponseContext context )
		{
			if ( !context.ReadFromHeaderUnpacker() )
			{
				MsgPackRpcClientProtocolsTrace.TraceEvent( MsgPackRpcClientProtocolsTrace.NeedMessageId, "Message ID is needed. {{ \"SessionID\" : {0} }}", context.SessionId );
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
					"Invalid response message stream. ID must be UInt32 compatible integer.",
					() => context.UnpackingBuffer.ToArray()
				);
				return context.NextProcess( context );
			}

			context.NextProcess = this.UnpackError;
			return context.NextProcess( context );
		}

		/// <summary>
		///		Unpack error part on response message.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the pipeline is finished;
		///		<c>false</c>, the pipeline is interruppted because extra data is needed.
		/// </returns>
		private bool UnpackError( ClientResponseContext context )
		{
			Contract.Assert( context.UnpackingBuffer.CanSeek );
			if ( context.ErrorStartAt == -1 )
			{
				context.ErrorStartAt = context.UnpackingBuffer.Position;
			}

			var skipped = context.SkipErrorSegment();
			if ( skipped == null )
			{
				MsgPackRpcClientProtocolsTrace.TraceEvent( MsgPackRpcClientProtocolsTrace.NeedError, "Error value is needed. {{ \"SessionID\" : {0} }}", context.SessionId );
				return false;
			}

			context.ErrorBuffer = new ByteArraySegmentStream( context.UnpackingBuffer.GetBuffer( context.ErrorStartAt, context.UnpackingBuffer.Position - context.ErrorStartAt ) );
			context.NextProcess = this.UnpackResult;

			return context.NextProcess( context );
		}

		/// <summary>
		///		Unpack result part on response message.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the pipeline is finished;
		///		<c>false</c>, the pipeline is interruppted because extra data is needed.
		/// </returns>
		private bool UnpackResult( ClientResponseContext context )
		{
			Contract.Assert( context.UnpackingBuffer.CanSeek );
			if ( context.ResultStartAt == -1 )
			{
				context.ResultStartAt = context.UnpackingBuffer.Position;
			}

			var skipped = context.SkipResultSegment();
			if ( skipped == null )
			{
				MsgPackRpcClientProtocolsTrace.TraceEvent( MsgPackRpcClientProtocolsTrace.NeedResult, "Result value is needed. {{ \"SessionID\" : {0} }}", context.SessionId );
				return false;
			}

			context.ResultBuffer = new ByteArraySegmentStream( context.UnpackingBuffer.GetBuffer( context.ResultStartAt, context.UnpackingBuffer.Position - context.ResultStartAt ) );
			context.NextProcess = this.Dispatch;

			return context.NextProcess( context );
		}

		/// <summary>
		///		Dispatch response message.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the pipeline is finished;
		///		<c>false</c>, the pipeline is interruppted because extra data is needed.
		/// </returns>
		private bool Dispatch( ClientResponseContext context )
		{
			Contract.Assert( context.MessageId != null );

			try
			{
				Action<ClientResponseContext, Exception, bool> handler = null;
				try
				{
					this._pendingRequestTable.TryRemove( context.MessageId.Value, out handler );
				}
				finally
				{
					// Best effort to rescue from ThreadAbortException...
					if ( handler != null )
					{
						handler( context, null, context.CompletedSynchronously );
					}
					else
					{
						this.HandleOrphan( context );
					}
				}
			}
			finally
			{
				context.ClearBuffers();
				this.OnProcessFinished();
			}

			if ( context.UnpackingBuffer.Length > 0 )
			{
				// Subsequent request is already arrived.
				context.NextProcess = this.UnpackResponseHeader;
				return context.NextProcess( context );
			}
			else
			{
				// Try receive subsequent.
				return true;
			}
		}

		internal bool DumpCorrupttedData( ClientResponseContext context )
		{
			if ( context.BytesTransferred == 0 )
			{
				context.Clear();
				return false;
			}

			if ( this.Manager.Configuration.DumpCorruptResponse )
			{
#if !SILVERLIGHT
				using ( var dumpStream = OpenDumpStream( context.SessionStartedAt, context.RemoteEndPoint, context.SessionId, MessageType.Response, context.MessageId ) )
#else
				using( var storage = IsolatedStorageFile.GetUserStoreForApplication() )
				using( var dumpStream = OpenDumpStream( storage, context.SessionStartedAt, context.RemoteEndPoint, context.SessionId, MessageType.Response, context.MessageId ) )
#endif
				{
					dumpStream.Write( context.CurrentReceivingBuffer, context.CurrentReceivingBufferOffset, context.BytesTransferred );
					dumpStream.Flush();
				}
			}

			context.ShiftCurrentReceivingBuffer();

			return true;
		}
	}
}
