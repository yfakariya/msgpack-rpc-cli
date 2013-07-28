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
using System.IO;
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc.Client.Protocols
{
	/// <summary>
	///		Represents context information for the client side response.
	/// </summary>
	public sealed class ClientResponseContext : InboundMessageContext
	{
		/// <summary>
		///		Next (that is, resuming) process on the deserialization pipeline.
		/// </summary>
		internal Func<ClientResponseContext, bool> NextProcess;

		internal long ErrorStartAt;

		/// <summary>
		///		Subtree <see cref="Unpacker"/> to parse error value as opaque sequence.
		/// </summary>
		internal ByteArraySegmentStream ErrorBuffer;

		internal long ResultStartAt;

		/// <summary>
		///		Subtree <see cref="Unpacker"/> to parse return value as opaque sequence.
		/// </summary>
		internal ByteArraySegmentStream ResultBuffer;

		/// <summary>
		///		Initializes a new instance of the <see cref="ClientResponseContext"/> class with default settings.
		/// </summary>
		public ClientResponseContext()
			: this( null )
		{
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="ClientResponseContext"/> class with specified configuration.
		/// </summary>
		/// <param name="configuration">
		///		An <see cref="RpcClientConfiguration"/> to tweak this instance initial state.
		/// </param>
		public ClientResponseContext( RpcClientConfiguration configuration )
			: base( ( configuration ?? RpcClientConfiguration.Default ).InitialReceiveBufferLength )
		{
			this.ErrorStartAt = -1;
			this.ResultStartAt = -1;
		}
		
		internal long? SkipResultSegment()
		{
#if DEBUG
			Contract.Assert( this.ResultStartAt > -1 );
#endif
			return this.SkipHeader( this.ResultStartAt );
		}

		internal long? SkipErrorSegment()
		{
#if DEBUG
			Contract.Assert( this.ErrorStartAt > -1 );
#endif
			return this.SkipHeader( this.ErrorStartAt );
		}

		private long? SkipHeader( long origin )
		{
			long? result = this.HeaderUnpacker.Skip();
			if ( result == null )
			{
				// Revert buffer position to handle next attempt.
				this.UnpackingBuffer.Position = origin;
			}

			return result;
		}

		/// <summary>
		///		Sets the bound <see cref="ClientTransport"/>.
		/// </summary>
		/// <param name="transport">The binding transport.</param>
		internal void SetTransport( ClientTransport transport )
		{
			Contract.Requires( transport != null );

			this.NextProcess = transport.UnpackResponseHeader;
			base.SetTransport( transport );
		}

		private static bool InvalidFlow( ClientResponseContext context )
		{
			throw new InvalidOperationException( "Invalid state transition." );
		}

		/// <summary>
		///		Clears this instance internal buffers for reuse.
		/// </summary>
		internal sealed override void Clear()
		{
			this.ClearBuffers();
			this.NextProcess = InvalidFlow;
			base.Clear();
		}

		/// <summary>
		///		Clears the buffers to deserialize message.
		/// </summary>
		internal override void ClearBuffers()
		{
			if ( this.ErrorBuffer != null )
			{
				this.ErrorBuffer.Dispose();
				this.ErrorBuffer = null;
			}

			if ( this.ResultBuffer != null )
			{
				this.ResultBuffer.Dispose();
				this.ResultBuffer = null;
			}

			this.ErrorStartAt = -1;
			this.ResultStartAt = -1;

			base.ClearBuffers();
		}
	}
}
