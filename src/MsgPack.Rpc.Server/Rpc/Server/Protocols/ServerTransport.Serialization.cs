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
using System.Linq;
using System.Text;
using System.IO;

namespace MsgPack.Rpc.Server.Protocols
{
	partial class ServerTransport
	{
		private sealed class SerializationState
		{
			/// <summary>
			///		Constant part of the response header.
			/// </summary>
			private static readonly ArraySegment<byte> _responseHeader =
				new ArraySegment<byte>( new byte[] { 0x94, 0x01 } ); // [FixArray4], [Response:1]

			/// <summary>
			///		The reusable buffer to pack <see cref="Id"/>.
			///		This value will not be <c>null</c>.
			/// </summary>
			public readonly MemoryStream IdBuffer;

			/// <summary>
			///		The reusable buffer to pack error ID.
			///		This value will not be <c>null</c>.
			/// </summary>
			public readonly MemoryStream ErrorDataBuffer;

			/// <summary>
			///		The reusable buffer to pack return value or error detail.
			///		This value will not be <c>null</c>.
			/// </summary>
			public readonly MemoryStream ReturnDataBuffer;

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
			public readonly ArraySegment<byte>[] SendingBuffer;

			public SerializationState()
			{
				this.IdBuffer = new MemoryStream( 5 );
				// TODO: Configurable
				this.ErrorDataBuffer = new MemoryStream( 128 );
				// TODO: Configurable
				this.ReturnDataBuffer = new MemoryStream( 65536 );
				this.SendingBuffer = new ArraySegment<byte>[ 4 ];
				this.SendingBuffer[ 0 ] = _responseHeader;
			}

			internal void Clear()
			{
				this.IdBuffer.SetLength( 0 );
				this.ErrorDataBuffer.SetLength( 0 );
				this.ReturnDataBuffer.SetLength( 0 );
			}
		}
	}
}
