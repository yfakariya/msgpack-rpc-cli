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
using System.IO;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Defines extension methods for RPC.
	/// </summary>
	internal static class UnpackerExtensions
	{
		/// <summary>
		///		Tries read from <see cref="Unpacker"/> considering fragmented receival.
		/// </summary>
		/// <param name="unpacker">The unpacker.</param>
		/// <param name="underyingStream">The underying stream.</param>
		/// <returns><c>true</c> if data read successfully; otherwise, <c>false</c></returns>
		public static bool TryRead( this Unpacker unpacker, Stream underyingStream )
		{
			long position = underyingStream.Position;
			try
			{
				return unpacker.Read();
			}
			catch( InvalidMessagePackStreamException )
			{
				if( underyingStream.Position == underyingStream.Length )
				{
					// It was fragmented data, so we may be able to read them on next time.
					underyingStream.Position = position;
					return false;
				}

				throw;
			}
		}
	}
}
