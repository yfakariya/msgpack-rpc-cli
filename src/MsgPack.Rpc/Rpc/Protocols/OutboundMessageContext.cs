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
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Defines basic functionality for outbound message contexts.
	/// </summary>
	public abstract class OutboundMessageContext : MessageContext
	{
		/// <summary>
		///		The reusable buffer to pack message ID.
		///		This value will not be <c>null</c>.
		/// </summary>
		private readonly MemoryStream _idBuffer;

		/// <summary>
		///		Gets or sets the buffer lists for sending by socket.
		/// </summary>
		/// <value>
		///		The <see cref="IList{T}"/> of <see cref="ArraySegment{T}"/> of <see cref="Byte"/> which is the buffer lists for sending by socket.
		/// </value>
		[SuppressMessage( "Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Follwing SocketAsyncEventArgs signature." )]
		[SuppressMessage( "Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Follwing SocketAsyncEventArgs signature." )]
		public IList<ArraySegment<byte>> SendingSocketBuffers
		{
			get { return this.SocketContext.BufferList; }
			set { this.SocketContext.BufferList = value; }
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="OutboundMessageContext"/> class.
		/// </summary>
		protected OutboundMessageContext()
			: base()
		{
			this._idBuffer = new MemoryStream( 5 );
		}

		/// <summary>
		///		Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected override void Dispose( bool disposing )
		{
			if ( disposing )
			{
				this._idBuffer.Dispose();
			}

			base.Dispose( disposing );
		}

		internal ArraySegment<byte> GetPackedMessageId()
		{
			Contract.Assert( this._idBuffer.Position == 0 );

			using ( var packer = Packer.Create( this._idBuffer, false ) )
			{
				packer.Pack( this.MessageId );
			}

			return new ArraySegment<byte>( this._idBuffer.GetBuffer(), 0, unchecked( ( int )this._idBuffer.Length ) );
		}

		internal virtual void ClearBuffers()
		{
			this._idBuffer.SetLength( 0 );
		}
	}
}
