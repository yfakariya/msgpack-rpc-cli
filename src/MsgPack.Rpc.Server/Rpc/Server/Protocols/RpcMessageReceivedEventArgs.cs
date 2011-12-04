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
using System.Collections.ObjectModel;
using MsgPack.Rpc.Protocols;
using System.Diagnostics.Contracts;

namespace MsgPack.Rpc.Server.Protocols
{
	/// <summary>
	///		Represents event data for <see cref="E:ServerTransport.MessageReceived"/> event.
	/// </summary>
	public sealed class RpcMessageReceivedEventArgs : EventArgs
	{
		private readonly ServerTransport _transport;

		/// <summary>
		///		Gets the transport to send response.
		/// </summary>
		/// <value>
		///		The transport to send response. This value will not be <c>null</c>.
		/// </value>
		public ServerTransport Transport
		{
			get { return this._transport; }
		}

		private readonly string _methodName;

		/// <summary>
		///		Gets the invoking method name.
		/// </summary>
		/// <value>
		///		The invoking method name. This value will not be <c>null</c>.
		/// </value>
		public string MethodName
		{
			get { return this._methodName; }
		}

		private readonly MessageType _messageType;

		/// <summary>
		///		Gets the type of message.
		/// </summary>
		/// <value>
		///		The type of message.
		/// </value>
		public MessageType MessageType
		{
			get { return this._messageType; }
		}

		private readonly int? _id;


		/// <summary>
		///		Gets the request ID.
		/// </summary>
		/// <value>
		///		The request ID. This value will be <c>null</c> when <see cref="Type"/> is <see cref="MessageType.Nofitication"/>.
		/// </value>
		public int? Id
		{
			get { return _id; }
		}

		private readonly Unpacker _underlyingUnpacker;

		private bool? _canGetArgumentsUnpacker;

		/// <summary>
		///		Gets the <see cref="Unpacker"/> to enumerate and unpack method arguments.
		/// </summary>
		/// <value>
		///		The <see cref="Unpacker"/> to enumerate and unpack method arguments.
		///		This value will not be <c>null</c>.
		/// </value>
		/// <exception cref="InvalidOperationException">
		///		<see cref="Arguments"/> was already called.
		/// </exception>
		/// <remarks>
		///		Do not refer this property when you use <see cref="Arguments"/> property.
		/// </remarks>
		public Unpacker ArgumentsUnpacker
		{
			get
			{
				if ( !this._canGetArgumentsUnpacker.HasValue )
				{
					this._canGetArgumentsUnpacker = true;
				}
				else if ( !this._canGetArgumentsUnpacker.Value )
				{
					throw new InvalidOperationException( "Arguments was already called." );
				}

				return this._underlyingUnpacker;
			}
		}

		private ReadOnlyCollection<MessagePackObject> _arguments;

		/// <summary>
		///		Gets the unpacked arguments.
		/// </summary>
		/// <value>
		///		The unpacked arguments.
		/// </value>
		/// <exception cref="InvalidOperationException">
		///		<see cref="ArgumentsUnpacker"/> was already called.
		/// </exception>
		/// <remarks>
		///		Do not refer this property when you use <see cref="ArgumentsUnpacker"/> property.
		/// </remarks>
		public ReadOnlyCollection<MessagePackObject> Arguments
		{
			get
			{
				this.UnpackArguments();
				return this._arguments;
			}
		}

		internal RpcMessageReceivedEventArgs( ServerTransport transport, MessageType type, int? id, string methodName, Unpacker underyingUnpackerAtArguments )
		{
			this._transport = transport;
			this._messageType = type;
			this._id = id;
			this._methodName = methodName;
			this._underlyingUnpacker = underyingUnpackerAtArguments;
		}

		private void UnpackArguments()
		{
			if ( !this._canGetArgumentsUnpacker.HasValue )
			{
				this._canGetArgumentsUnpacker = false;
			}
			else if ( this._canGetArgumentsUnpacker.Value )
			{
				throw new InvalidOperationException( "ArgumentsUnpacker was already called." );
			}

			Contract.Assert( this._underlyingUnpacker.IsInStart );

			if ( !this._underlyingUnpacker.Read() )
			{
				this._arguments = new ReadOnlyCollection<MessagePackObject>( new MessagePackObject[ 0 ] );
			}
			else
			{
				var list = new List<MessagePackObject>( checked( ( int )this._underlyingUnpacker.ItemsCount ) );
				while ( this._underlyingUnpacker.Read() )
				{
					list.Add( this._underlyingUnpacker.Data.Value );
				}

				this._arguments = new ReadOnlyCollection<MessagePackObject>( list );
			}
		}
	}
}
