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
	public sealed class ClientRequestContext : MessageContext, ILeaseable<ClientRequestContext>
	{
		/// <summary>
		///		Constant part of the request header.
		/// </summary>
		private static readonly ArraySegment<byte> _requestHeader =
			new ArraySegment<byte>( new byte[] { 0x94, 0x00 } ); // [FixArray4], [Request:0]

		/// <summary>
		///		Constant part of the request header.
		/// </summary>
		private static readonly ArraySegment<byte> _notificationHeader =
			new ArraySegment<byte>( new byte[] { 0x94, 0x02 } ); // [FixArray4], [Notification:2]

		private static readonly ArraySegment<byte> _emptyBuffer =
			new ArraySegment<byte>( new byte[ 0 ], 0, 0 );

		private MessageType _messageType;

		public MessageType MessageType
		{
			get { return this._messageType; }
		}

		private Packer _argumentsPacker;

		public Packer ArgumentsPacker
		{
			get { return this._argumentsPacker; }
		}

		private string _methodName;

		public string MethodName
		{
			get { return this._methodName; }
		}

		/// <summary>
		///		The reusable buffer to pack <see cref="Id"/>.
		///		This value will not be <c>null</c>.
		/// </summary>
		private readonly MemoryStream _idBuffer;

		/// <summary>
		///		The reusable buffer to pack method name.
		///		This value will not be <c>null</c>.
		/// </summary>
		private readonly MemoryStream _methodNameBuffer;

		/// <summary>
		///		The reusable buffer to pack arguments.
		///		This value will not be <c>null</c>.
		/// </summary>
		private readonly MemoryStream _argumentsBuffer;

		/// <summary>
		///		The resusable buffer to hold sending response data.
		/// </summary>
		/// <remarks>
		///		Each segment corresponds to the message segment.
		///		<list type="table">
		///			<listheader>
		///				<term>Index</term>
		///				<description>Content</description>
		///			</listheader>
		///			<item>
		///				<term>0</term>
		///				<description>
		///					Common response header, namely array header and message type.
		///					Do not change this element.
		///				</description>
		///			</item>
		///			<item>
		///				<term>1</term>
		///				<description>
		///					Message ID to correpond the response to the request.
		///				</description>
		///			</item>
		///			<item>
		///				<term>2</term>
		///				<description>
		///					Error identifier.
		///				</description>
		///			</item>
		///			<item>
		///				<term>3</term>
		///				<description>
		///					Return value.
		///				</description>
		///			</item>
		///		</list>
		/// </remarks>
		internal readonly ArraySegment<byte>[] SendingBuffer;

		private Action<Exception, bool> _notificationComplectionCallback;

		public Action<Exception, bool> NotificationComplectionCallback
		{
			get { return this._notificationComplectionCallback; }
		}

		private Action<ClientResponseContext, Exception, bool> _requestCompletionCallback;

		public Action<ClientResponseContext, Exception, bool> RequestCompletionCallback
		{
			get { return this._requestCompletionCallback; }
		}

		public ClientRequestContext()
		{
			this._idBuffer = new MemoryStream( 5 );
			// TODO: Configurable
			this._methodNameBuffer = new MemoryStream( 256 );
			// TODO: Configurable
			this._argumentsBuffer = new MemoryStream( 65536 );
			this.SendingBuffer = new ArraySegment<byte>[ 4 ];
			this._argumentsPacker = Packer.Create( this._argumentsBuffer, false );
		}

		public void SetRequest( int messageId, string methodName, Action<ClientResponseContext, Exception, bool> completionCallback )
		{
			if ( methodName == null )
			{
				throw new ArgumentNullException( "methodName" );
			}

			if ( methodName.Length == 0 )
			{
				throw new ArgumentException( "Method name cannot be empty.", "methodName" );
			}

			if ( completionCallback == null )
			{
				throw new ArgumentNullException( "completionCallback" );
			}

			this.MessageId = messageId;
			this._methodName =methodName;
			this._requestCompletionCallback = completionCallback;
			this._notificationComplectionCallback = null;
		}

		public void SetNotification( string methodName, Action<Exception, bool> completionCallback )
		{
			if ( methodName == null )
			{
				throw new ArgumentNullException( "methodName" );
			}

			if ( methodName.Length == 0 )
			{
				throw new ArgumentException( "Method name cannot be empty.", "methodName" );
			}

			if ( completionCallback == null )
			{
				throw new ArgumentNullException( "completionCallback" );
			}

			this._methodName = methodName;
			this._notificationComplectionCallback = completionCallback;
			this._requestCompletionCallback = null;
			this.MessageId = null;
		}
				
		internal void Prepare()
		{
			if ( this._messageType == MessageType.Response )
			{
				throw new InvalidOperationException( "MessageType is not set." );
			}

			Contract.Assert( this._methodName != null );

			using ( var packer = Packer.Create( this._methodNameBuffer, false ) )
			{
				packer.Pack( this._methodName );
			}

			if ( this._messageType == MessageType.Request )
			{
				Contract.Assert( this.MessageId != null );
				Contract.Assert( this._requestCompletionCallback != null );

				this.SendingBuffer[ 0 ] = _requestHeader;
				using ( var packer = Packer.Create( this._idBuffer, false ) )
				{
					packer.Pack( this.MessageId );
				}

				this.SendingBuffer[ 1 ] = new ArraySegment<byte>( this._idBuffer.GetBuffer(), 0, unchecked( ( int )this._idBuffer.Length ) );
				this.SendingBuffer[ 2 ] = new ArraySegment<byte>( this._methodNameBuffer.GetBuffer(), 0, unchecked( ( int )this._methodNameBuffer.Length ) );
				this.SendingBuffer[ 3 ] = new ArraySegment<byte>( this._argumentsBuffer.GetBuffer(), 0, unchecked( ( int )this._argumentsBuffer.Length ) );
			}
			else
			{
				Contract.Assert( this._notificationComplectionCallback != null );

				this.SendingBuffer[ 0 ] = _notificationHeader;
				this.SendingBuffer[ 1 ] = new ArraySegment<byte>( this._methodNameBuffer.GetBuffer(), 0, unchecked( ( int )this._methodNameBuffer.Length ) );
				this.SendingBuffer[ 2 ] = new ArraySegment<byte>( this._argumentsBuffer.GetBuffer(), 0, unchecked( ( int )this._argumentsBuffer.Length ) );
				this.SendingBuffer[ 3 ] = _emptyBuffer;
			}

			this.SetBuffer( null, 0, 0 );
			this.BufferList = this.SendingBuffer;
		}

		internal sealed override void Clear()
		{
			this._idBuffer.SetLength( 0 );
			this._methodNameBuffer.SetLength( 0 );
			this._argumentsBuffer.SetLength( 0 );
			this.BufferList = null;
			this._argumentsPacker.Dispose();
			this._argumentsPacker = Packer.Create( this._argumentsBuffer, false );
			this._methodName = null;
			this._messageType = MessageType.Response; // Invalid.
			this.MessageId = null;
			this._requestCompletionCallback = null;
			this._notificationComplectionCallback = null;
			base.Clear();
		}

		void ILeaseable<ClientRequestContext>.SetLease( ILease<ClientRequestContext> lease )
		{
			base.SetLease( lease );
		}
	}
}
