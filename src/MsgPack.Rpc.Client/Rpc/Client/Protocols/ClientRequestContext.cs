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
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
#if SILVERLIGHT
using Mono.Diagnostics;
#endif
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc.Client.Protocols
{
	/// <summary>
	///		Represents context information for the client side request including notification.
	/// </summary>
	public sealed class ClientRequestContext : OutboundMessageContext
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

		/// <summary>
		///		Empty array of <see cref="ArraySegment{T}"/> of <see cref="Byte"/>.
		/// </summary>
		private static readonly ArraySegment<byte> _emptyBuffer =
			new ArraySegment<byte>( new byte[ 0 ], 0, 0 );

		private MessageType _messageType;

		/// <summary>
		///		Gets the type of the message.
		/// </summary>
		/// <value>
		///		The type of the message.
		/// </value>
		/// <remarks>
		///		This value can be set via <see cref="SetRequest"/> or <see cref="SetNotification"/> method.
		/// </remarks>
		public MessageType MessageType
		{
			get
			{
				Contract.Ensures( Contract.Result<MessageType>() == Rpc.Protocols.MessageType.Request || Contract.Result<MessageType>() == Rpc.Protocols.MessageType.Notification );

				return this._messageType;
			}
		}

		private Packer _argumentsPacker;

		/// <summary>
		///		Gets the <see cref="Packer"/> to pack arguments array.
		/// </summary>
		/// <value>
		///		The <see cref="Packer"/> to pack arguments array.
		///		This value will not be <c>null</c>.
		/// </value>
		public Packer ArgumentsPacker
		{
			get
			{
				Contract.Ensures( Contract.Result<Packer>() != null );

				return this._argumentsPacker;
			}
		}

		private string _methodName;

		/// <summary>
		///		Gets the name of the calling method.
		/// </summary>
		/// <value>
		///		The name of the calling method.
		///		This value will be <c>null</c> if both of <see cref="SetRequest"/> and <see cref="SetNotification"/> have not been called after previous cleanup or initialization.
		/// </value>
		/// <remarks>
		///		This value can be set via <see cref="SetRequest"/> or <see cref="SetNotification"/> method.
		/// </remarks>
		public string MethodName
		{
			get { return this._methodName; }
		}

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

#if MONO
		private readonly MemoryStream _unifiedSendingBuffer;
#endif

		private Action<Exception, bool> _notificationCompletionCallback;

		/// <summary>
		///		Gets the callback delegate which will be called when the notification is sent.
		/// </summary>
		/// <value>
		///		The callback delegate which will be called when the notification is sent.
		///		The 1st argument is an <see cref="Exception"/> which represents sending error, or <c>null</c> for success.
		///		The 2nd argument indicates that the operation is completed synchronously.
		///		This value will be <c>null</c> if both of <see cref="SetRequest"/> and <see cref="SetNotification"/> have not been called after previous cleanup or initialization.
		/// </value>
		/// <remarks>
		///		This value can be set via <see cref="SetNotification"/> method.
		/// </remarks>
		public Action<Exception, bool> NotificationCompletionCallback
		{
			get { return this._notificationCompletionCallback; }
		}

		private Action<ClientResponseContext, Exception, bool> _requestCompletionCallback;

		/// <summary>
		///		Gets the callback delegate which will be called when the response is received.
		/// </summary>
		/// <value>
		///		The callback delegate which will be called when the notification sent.
		///		The 1st argument is a <see cref="ClientResponseContext"/> which stores any information of the response, it will not be <c>null</c>.
		///		The 2nd argument is an <see cref="Exception"/> which represents sending error, or <c>null</c> for success.
		///		The 3rd argument indicates that the operation is completed synchronously.
		///		This value will be <c>null</c> if both of <see cref="SetRequest"/> and <see cref="SetNotification"/> have not been called after previous cleanup or initialization.
		/// </value>
		/// <remarks>
		///		This value can be set via <see cref="SetRequest"/> method.
		/// </remarks>
		public Action<ClientResponseContext, Exception, bool> RequestCompletionCallback
		{
			get { return this._requestCompletionCallback; }
		}

		private readonly Stopwatch _stopwatch;

		internal TimeSpan ElapsedTime
		{
			get { return this._stopwatch.Elapsed; }
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="ClientRequestContext"/> class with default settings.
		/// </summary>
		public ClientRequestContext()
			: this( null )
		{
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="ClientRequestContext"/> class with specified configuration.
		/// </summary>
		/// <param name="configuration">
		///		An <see cref="RpcClientConfiguration"/> to tweak this instance initial state.
		/// </param>
		public ClientRequestContext( RpcClientConfiguration configuration )
		{
			this._methodNameBuffer =
				new MemoryStream( ( configuration ?? RpcClientConfiguration.Default ).InitialMethodNameBufferLength );
			this._argumentsBuffer =
				new MemoryStream( ( configuration ?? RpcClientConfiguration.Default ).InitialArgumentsBufferLength );
			this.SendingBuffer = new ArraySegment<byte>[ 4 ];
#if MONO
			this._unifiedSendingBuffer = new MemoryStream( ( configuration ?? RpcClientConfiguration.Default ).InitialReceiveBufferLength );
#endif
			this._argumentsPacker = Packer.Create( this._argumentsBuffer, false );
			this._messageType = MessageType.Response;
			this._stopwatch = new Stopwatch();
		}

		internal override void StartWatchTimeout( TimeSpan timeout )
		{
			base.StartWatchTimeout( timeout );
			this._stopwatch.Restart();
		}

		internal override void StopWatchTimeout()
		{
			this._stopwatch.Stop();
			base.StopWatchTimeout();
		}

		/// <summary>
		///		Set ups this context for request message.
		/// </summary>
		/// <param name="messageId">The message id which identifies request/response and associates request and response.</param>
		/// <param name="methodName">Name of the method to be called.</param>
		/// <param name="completionCallback">
		///		The callback which will be called when the response is received.
		///		For details, see <see cref="RequestCompletionCallback"/>.
		///	</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="methodName"/> is <c>null</c>.
		///		Or <paramref name="completionCallback"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="methodName"/> is empty.
		/// </exception>
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

			Contract.Ensures( this.MessageType == Rpc.Protocols.MessageType.Request );
			Contract.Ensures( this.MessageId != null );
			Contract.Ensures( !String.IsNullOrEmpty( this.MethodName ) );
			Contract.Ensures( this.RequestCompletionCallback != null );
			Contract.Ensures( this.NotificationCompletionCallback == null );

			this._messageType = Rpc.Protocols.MessageType.Request;
			this.MessageId = messageId;
			this._methodName = methodName;
			this._requestCompletionCallback = completionCallback;
			this._notificationCompletionCallback = null;
		}

		/// <summary>
		///		Set ups this context for notification message.
		/// </summary>
		/// <param name="methodName">Name of the method to be called.</param>
		/// <param name="completionCallback">
		///		The callback which will be called when the response is received.
		///		For details, see <see cref="NotificationCompletionCallback"/>.
		///	</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="methodName"/> is <c>null</c>.
		///		Or <paramref name="completionCallback"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="methodName"/> is empty.
		/// </exception>
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

			Contract.Ensures( this.MessageType == Rpc.Protocols.MessageType.Notification );
			Contract.Ensures( this.MessageId == null );
			Contract.Ensures( !String.IsNullOrEmpty( this.MethodName ) );
			Contract.Ensures( this.RequestCompletionCallback == null );
			Contract.Ensures( this.NotificationCompletionCallback != null );

			this._messageType = Rpc.Protocols.MessageType.Notification;
			this.MessageId = null;
			this._methodName = methodName;
			this._notificationCompletionCallback = completionCallback;
			this._requestCompletionCallback = null;
		}

		/// <summary>
		///		Prepares this instance to send request or notification message.
		/// </summary>
		internal void Prepare( bool canUseChunkedBuffer )
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
				this.SendingBuffer[ 1 ] = this.GetPackedMessageId();
				this.SendingBuffer[ 2 ] = new ArraySegment<byte>( this._methodNameBuffer.GetBuffer(), 0, unchecked( ( int )this._methodNameBuffer.Length ) );
				this.SendingBuffer[ 3 ] = new ArraySegment<byte>( this._argumentsBuffer.GetBuffer(), 0, unchecked( ( int )this._argumentsBuffer.Length ) );
			}
			else
			{
				Contract.Assert( this._notificationCompletionCallback != null );

				this.SendingBuffer[ 0 ] = _notificationHeader;
				this.SendingBuffer[ 1 ] = new ArraySegment<byte>( this._methodNameBuffer.GetBuffer(), 0, unchecked( ( int )this._methodNameBuffer.Length ) );
				this.SendingBuffer[ 2 ] = new ArraySegment<byte>( this._argumentsBuffer.GetBuffer(), 0, unchecked( ( int )this._argumentsBuffer.Length ) );
				this.SendingBuffer[ 3 ] = _emptyBuffer;
			}

#if MONO
			if ( !canUseChunkedBuffer )
			{
				this._unifiedSendingBuffer.Position = 0;
				this._unifiedSendingBuffer.SetLength( 0 );
				this._unifiedSendingBuffer.Write( this.SendingBuffer[ 0 ].Array, this.SendingBuffer[ 0 ].Offset, this.SendingBuffer[ 0 ].Count );
				this._unifiedSendingBuffer.Write( this.SendingBuffer[ 1 ].Array, this.SendingBuffer[ 1 ].Offset, this.SendingBuffer[ 1 ].Count );
				this._unifiedSendingBuffer.Write( this.SendingBuffer[ 2 ].Array, this.SendingBuffer[ 2 ].Offset, this.SendingBuffer[ 2 ].Count );
				this._unifiedSendingBuffer.Write( this.SendingBuffer[ 3 ].Array, this.SendingBuffer[ 3 ].Offset, this.SendingBuffer[ 3 ].Count );
				this.SocketContext.SetBuffer( this._unifiedSendingBuffer.GetBuffer(), 0, unchecked( ( int )this._unifiedSendingBuffer.Length ) );
				this.SocketContext.BufferList = null;
				return;
			}
#endif
			this.SocketContext.SetBuffer( null, 0, 0 );
			this.SocketContext.BufferList = this.SendingBuffer;
		}

		internal override void ClearBuffers()
		{
			this._methodNameBuffer.SetLength( 0 );
			this._argumentsBuffer.SetLength( 0 );
			this.SocketContext.BufferList = null;
			this._argumentsPacker.Dispose();
			this._argumentsPacker = Packer.Create( this._argumentsBuffer, false );
			this.SendingBuffer[ 0 ] = new ArraySegment<byte>();
			this.SendingBuffer[ 1 ] = new ArraySegment<byte>();
			this.SendingBuffer[ 2 ] = new ArraySegment<byte>();
			this.SendingBuffer[ 3 ] = new ArraySegment<byte>();
			base.ClearBuffers();
		}

		/// <summary>
		///		Clears this instance internal buffers for reuse.
		/// </summary>
		internal sealed override void Clear()
		{
			this.ClearBuffers();
			this._methodName = null;
			this._messageType = MessageType.Response; // Invalid.
			this._requestCompletionCallback = null;
			this._notificationCompletionCallback = null;
			base.Clear();
		}
	}
}
