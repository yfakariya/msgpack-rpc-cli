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
using System.Collections.Concurrent;
using System.Threading;

namespace MsgPack.Rpc.Protocols
{
	internal sealed class InProcPacket
	{
		public ArraySegment<byte> Data;

		public InProcPacket( byte[] data )
		{
			this.Data = new ArraySegment<byte>( data );
		}

		public static void ProcessReceive( BlockingCollection<byte[]> inboundQueue, ConcurrentQueue<InProcPacket> pendingPackets, MessageContext context, CancellationToken cancellationToken )
		{
			InProcPacket packet;
			if ( !pendingPackets.TryPeek( out packet ) )
			{
				byte[] data = inboundQueue.Take( cancellationToken );
				packet = new InProcPacket( data );
				pendingPackets.Enqueue( packet );
			}

			int copying = Math.Min( context.SocketContext.Count, packet.Data.Count );
			Buffer.BlockCopy( packet.Data.Array, packet.Data.Offset, context.Buffer, context.Offset, copying );
			context.SetBytesTransferred( copying );

			if ( copying == packet.Data.Count )
			{
				InProcPacket dummy;
				pendingPackets.TryDequeue( out dummy );
			}
			else
			{
				var oldData = packet.Data;
				packet.Data = new ArraySegment<byte>( oldData.Array, oldData.Offset + copying, oldData.Count - copying );
			}
		}
	}
}
