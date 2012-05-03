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
using System.IO;
using System.Linq;
using MsgPack.Rpc.Server.Protocols;
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc.Server.Dispatch
{
	/// <summary>
	///		Defines helper methods for testing.
	/// </summary>
	internal static class DispatchTestHelper
	{
		/// <summary>
		///		Creates <see cref="ServerRequestContext"/> which contains specified bytes as arguments array.
		/// </summary>
		/// <param name="arguments">The raw MessagePack bytes as arguments array.</param>
		/// <returns><see cref="ServerRequestContext"/> for the unit testing argument.</returns>
		public static ServerRequestContext CreateRequestContext( IEnumerable<byte> arguments )
		{
			return CreateRequestContext( new MemoryStream( arguments.ToArray() ) );
		}

		/// <summary>
		///		Creates <see cref="ServerRequestContext"/> which contains specified objects as arguments array.
		/// </summary>
		/// <param name="arguments">The array of MessagePack objects as arguments array.</param>
		/// <returns><see cref="ServerRequestContext"/> for the unit testing argument.</returns>
		public static ServerRequestContext CreateRequestContext( params MessagePackObject[] arguments )
		{
			return CreateRequestContext( arguments as IEnumerable<MessagePackObject> ?? new MessagePackObject[ 0 ] );
		}

		/// <summary>
		///		Creates <see cref="ServerRequestContext"/> which contains specified objects as arguments array.
		/// </summary>
		/// <param name="arguments">The sequence of MessagePack objects as arguments array.</param>
		/// <returns><see cref="ServerRequestContext"/> for the unit testing argument.</returns>
		public static ServerRequestContext CreateRequestContext( IEnumerable<MessagePackObject> arguments )
		{
			using ( var buffer = new MemoryStream() )
			using ( var packer = Packer.Create( buffer ) )
			{
				packer.Pack( arguments.ToArray() );
				buffer.Position = 0;
				return CreateRequestContext( buffer );
			}
		}

		private static ServerRequestContext CreateRequestContext( MemoryStream arguments )
		{
			var result = new ServerRequestContext();
			arguments.CopyTo( result.ArgumentsBuffer );
			result.ArgumentsBuffer.Position = 0;
			result.ArgumentsUnpacker = Unpacker.Create( result.ArgumentsBuffer, false );
			return result;
		}

		/// <summary>
		///		Creates <see cref="ServerResponseContext"/> which contains specified required states.
		/// </summary>
		/// <param name="transport">The transport to be bound to the context.</param>
		/// <returns><see cref="ServerResponseContext"/> for the unit testing argument.</returns>
		public static ServerResponseContext CreateResponseContext( IContextBoundableTransport transport )
		{
			var result = new ServerResponseContext();
			result.SetTransport( transport );
			return result;
		}
	}
}
