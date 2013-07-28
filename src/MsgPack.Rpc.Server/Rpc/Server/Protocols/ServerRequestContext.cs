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
using System.IO;
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc.Server.Protocols
{
	/// <summary>
	///		Represents context information for the request or notification message.
	/// </summary>
	public sealed class ServerRequestContext : InboundMessageContext
	{
		/// <summary>
		///		Next (that is, resuming) process on the deserialization pipeline.
		/// </summary>
		internal Func<ServerRequestContext, bool> NextProcess;

		/// <summary>
		///		Buffer to store binaries for arguments array for subsequent deserialization.
		/// </summary>
		internal readonly MemoryStream ArgumentsBuffer;

		/// <summary>
		///		<see cref="Packer"/> to re-pack to binaries of arguments for subsequent deserialization.
		/// </summary>
		internal Packer ArgumentsBufferPacker;

		/// <summary>
		///		Subtree <see cref="Unpacker"/> to parse arguments array as opaque sequence.
		/// </summary>
		internal Unpacker ArgumentsBufferUnpacker;

		/// <summary>
		///		The count of declared method arguments.
		/// </summary>
		internal int ArgumentsCount;

		/// <summary>
		///		The count of unpacked method arguments.
		/// </summary>
		internal int UnpackedArgumentsCount;


		/// <summary>
		///		Unpacked Message Type part value.
		/// </summary>
		internal MessageType MessageType;

		/// <summary>
		///		Unpacked Method Name part value.
		/// </summary>
		internal string MethodName;

		private Unpacker _argumentsUnpacker;

		/// <summary>
		///		<see cref="Unpacker"/> to deserialize arguments on the dispatcher.
		/// </summary>
		public Unpacker ArgumentsUnpacker
		{
			get { return this._argumentsUnpacker; }
			internal set { this._argumentsUnpacker = value; }
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="ServerRequestContext"/> class with default settings.
		/// </summary>
		public ServerRequestContext()
			: this( null )
		{
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="ServerRequestContext"/> class with specified configuration.
		/// </summary>
		/// <param name="configuration">
		///		An <see cref="RpcServerConfiguration"/> to tweak this instance initial state.
		/// </param>
		public ServerRequestContext( RpcServerConfiguration configuration )
			: base( ( configuration ?? RpcServerConfiguration.Default ).InitialReceiveBufferLength )
		{
			this.ArgumentsBuffer =
				new MemoryStream( ( configuration ?? RpcServerConfiguration.Default ).InitialArgumentsBufferLength );
		}

		internal bool ReadFromArgumentsBufferUnpacker()
		{
			return this.ArgumentsBufferUnpacker.TryRead( this.ArgumentsBuffer );
		}

		/// <summary>
		///		Set bound transport to this context.
		/// </summary>
		/// <param name="transport">The transport to be bound.</param>
		internal void SetTransport( ServerTransport transport )
		{
			Contract.Requires( transport != null );

			this.NextProcess = transport.UnpackRequestHeader;
			base.SetTransport( transport );
		}

		private static bool InvalidFlow( ServerRequestContext context )
		{
			throw new InvalidOperationException( "Invalid state transition." );
		}

		/// <summary>
		///		Clears this instance internal buffers for reuse.
		/// </summary>
		internal sealed override void Clear()
		{
			this.ClearBuffers();
			this.ClearDispatchContext();
			this.NextProcess = InvalidFlow;
			base.Clear();
		}

		/// <summary>
		///		Clears the buffers to deserialize message, which is not required to dispatch and invoke server method.
		/// </summary>
		internal override void ClearBuffers()
		{
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
			base.ClearBuffers();
		}

		/// <summary>
		///		Clears the dispatch context information.
		/// </summary>
		internal void ClearDispatchContext()
		{
			this.MethodName = null;
			this.MessageType = MessageType.Response; // Invalid value.
			if ( this._argumentsUnpacker != null )
			{
				this._argumentsUnpacker.Dispose();
				this._argumentsUnpacker = null;
			}

			this.ArgumentsBuffer.SetLength( 0 );
			this.ClearSessionId();
		}
	}
}
