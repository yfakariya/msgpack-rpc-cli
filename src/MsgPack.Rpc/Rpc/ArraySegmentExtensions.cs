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

namespace MsgPack.Rpc
{
	internal static class ArraySegmentExtensions
	{
		public static T Get<T>( this ArraySegment<T> source, int index )
		{
			if ( source.Array == null )
			{
				throw new ArgumentNullException( "source" );
			}

			if ( index < 0 || source.Count <= index )
			{
				throw new ArgumentOutOfRangeException( "index" );
			}

			return source.Array[ source.Offset + index ];
		}

		public static int CopyTo<T>( this ArraySegment<T> source, int sourceOffset, T[] array, int arrayOffset, int count )
		{
			if ( array == null )
			{
				throw new ArgumentNullException( "array" );
			}

			if ( source.Count == 0 )
			{
				return 0;
			}

			if ( source.Count <= sourceOffset )
			{
				throw new ArgumentOutOfRangeException( "sourceOffset" );
			}

			int length;
			if ( source.Count - sourceOffset < count )
			{
				length = source.Count - sourceOffset;
			}
			else
			{
				length = count;
			}

			if ( array.Length - arrayOffset < length )
			{
				throw new ArgumentException( "Array is too small.", "array" );
			}

			if ( source.Array == null )
			{
				return 0;
			}

#if !WINDOWS_PHONE
			Array.ConstrainedCopy( source.Array, source.Offset + sourceOffset, array, arrayOffset, length );
#else
			Array.Copy( source.Array, source.Offset + sourceOffset, array, arrayOffset, length );
#endif
			return length;
		}

		public static IEnumerable<T> AsEnumerable<T>( this ArraySegment<T> source )
		{
			if ( source.Array == null )
			{
				yield break;
			}

			for ( int i = 0; i < source.Count; i++ )
			{
				yield return source.Array[ i + source.Offset ];
			}
		}
	}
}
