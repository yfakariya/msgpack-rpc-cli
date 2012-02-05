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

namespace MsgPack.Rpc.Client.Protocols
{
	public static class ContextExtensions
	{
		public static byte[] GetTypeHeader( this ClientRequestContext source )
		{
			return source.SendingBuffer[ 0 ].AsEnumerable().ToArray();
		}

		public static byte[] SetTypeHeader( this ClientRequestContext source, byte[] value )
		{
			var old = source.SendingBuffer[ 0 ].AsEnumerable().ToArray();
			source.SendingBuffer[ 0 ] = new ArraySegment<byte>( value );
			return old;
		}

		public static byte[] GetMessageId( this ClientRequestContext source )
		{
			return source.SendingBuffer[ 1 ].AsEnumerable().ToArray();
		}

		public static byte[] SetMessageId( this ClientRequestContext source, byte[] value )
		{
			var old = source.SendingBuffer[ 1 ].AsEnumerable().ToArray();
			source.SendingBuffer[ 1 ] = new ArraySegment<byte>( value );
			return old;
		}

		public static byte[] GetMethodName( this ClientRequestContext source )
		{
			return source.SendingBuffer[ 2 ].AsEnumerable().ToArray();
		}

		public static byte[] SetMethodName( this ClientRequestContext source, byte[] value )
		{
			var old = source.SendingBuffer[ 2 ].AsEnumerable().ToArray();
			source.SendingBuffer[ 2 ] = new ArraySegment<byte>( value );
			return old;
		}

		public static byte[] GetArguments( this ClientRequestContext source )
		{
			return source.SendingBuffer[ 3 ].AsEnumerable().ToArray();
		}

		public static byte[] SetArguments( this ClientRequestContext source, byte[] value )
		{
			var old = source.SendingBuffer[ 3 ].AsEnumerable().ToArray();
			source.SendingBuffer[ 3 ] = new ArraySegment<byte>( value );
			return old;
		}
	}
}
