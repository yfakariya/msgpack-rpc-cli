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
using NUnit.Framework;

namespace MsgPack.Rpc
{
	[TestFixture]
	public class ArraySegmentExtensionTests
	{
		[Test]
		public void TestGet()
		{
			var array = new int[] { 0, 1, 2, 3 };
			var target = new ArraySegment<int>( array, 1, 2 );
			Assert.That( target.Get( 0 ), Is.EqualTo( 1 ) );
			Assert.That( target.Get( 1 ), Is.EqualTo( 2 ) );
		}

		[Test]
		[ExpectedException( typeof( ArgumentOutOfRangeException ) )]
		public void TestGetNegative()
		{
			var array = new int[] { 0, 1, 2, 3 };
			var target = new ArraySegment<int>( array, 1, 2 );
			target.Get( -1 );
		}

		[Test]
		[ExpectedException( typeof( ArgumentOutOfRangeException ) )]
		public void TestGetTooBig()
		{
			var array = new int[] { 0, 1, 2, 3 };
			var target = new ArraySegment<int>( array, 1, 2 );
			target.Get( 3 );
		}

		[Test]
		public void TestCopyTo()
		{
			var array = new int[] { 1, 2, 3, 4, 5 };
			var target = new ArraySegment<int>( array, 1, 3 );
			var result = new int[ 5 ];

			// Copy all
			Assert.That( target.CopyTo( 0, result, 0, result.Length ), Is.EqualTo( target.Count ) );
			Assert.That( result, Is.EqualTo( new int[] { 2, 3, 4, 0, 0 } ) );

			// Source offset
			Array.Clear( result, 0, result.Length );
			Assert.That( target.CopyTo( 1, result, 0, result.Length ), Is.EqualTo( target.Count - 1 ) );
			Assert.That( result, Is.EqualTo( new int[] { 3, 4, 0, 0, 0 } ) );

			// Offset
			Array.Clear( result, 0, result.Length );
			Assert.That( target.CopyTo( 0, result, 1, result.Length ), Is.EqualTo( target.Count ) );
			Assert.That( result, Is.EqualTo( new int[] { 0, 2, 3, 4, 0 } ) );

			// Length
			Array.Clear( result, 0, result.Length );
			Assert.That( target.CopyTo( 0, result, 0, result.Length - 1 ), Is.EqualTo( target.Count ) );
			Assert.That( result, Is.EqualTo( new int[] { 2, 3, 4, 0, 0 } ) );

			Array.Clear( result, 0, result.Length );
			Assert.That( target.CopyTo( 0, result, 0, target.Count - 1 ), Is.EqualTo( target.Count - 1 ) );
			Assert.That( result, Is.EqualTo( new int[] { 2, 3, 0, 0, 0 } ) );

			Array.Clear( result, 0, result.Length );
			Assert.That( target.CopyTo( 0, result, 0, 0 ), Is.EqualTo( 0 ) );
			Assert.That( result, Is.EqualTo( new int[] { 0, 0, 0, 0, 0 } ) );

			Array.Clear( result, 0, result.Length );
			Assert.That( target.CopyTo( 0, result, 0, 1 ), Is.EqualTo( 1 ) );
			Assert.That( result, Is.EqualTo( new int[] { 2, 0, 0, 0, 0 } ) );
		}

		[Test]
		public void TestCopyTo_EmptySourceArray()
		{
			var array = new int[ 0 ];
			var target = new ArraySegment<int>( array );
			var result = new int[ 5 ];
			Assert.That( target.CopyTo( 0, result, 0, 1 ), Is.EqualTo( 0 ) );
		}

		[Test]
		public void TestCopyTo_SmallArray()
		{
			var array = new int[] { 1, 2, 3 };
			var target = new ArraySegment<int>( array );
			var result = new int[ 2 ];
			Assert.That( target.CopyTo( 0, result, 0, result.Length ), Is.EqualTo( 2 ) );
			Assert.That( result, Is.EqualTo( new int[] { 1, 2 } ) );
		}

		[Test]
		public void TestCopyTo_EmptyDestinationArray()
		{
			var array = new int[ 0 ];
			var target = new ArraySegment<int>( array );
			var result = new int[ 0 ];
			Assert.That( target.CopyTo( 0, result, 0, 0 ), Is.EqualTo( 0 ) );
		}

		[Test]
		public void TestCopyTo_EmptyArraySegment()
		{
			var array = new int[] { 1 };
			var target = new ArraySegment<int>( array, 0, 0 );
			var result = new int[ 5 ];
			Assert.That( target.CopyTo( 0, result, 0, 1 ), Is.EqualTo( 0 ) );
		}

		[Test]
		[ExpectedException( typeof( ArgumentOutOfRangeException ) )]
		public void TestCopyTo_SourceOffset_Negavie()
		{
			new ArraySegment<int>( new int[] { 1 } ).CopyTo( -1, new int[ 1 ], 0, 1 );
		}

		[Test]
		[ExpectedException( typeof( ArgumentOutOfRangeException ) )]
		public void TestCopyTo_SourceOffset_TooBig()
		{
			new ArraySegment<int>( new int[] { 1 } ).CopyTo( 1, new int[ 1 ], 0, 1 );
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestCopyTo_Array_Null()
		{
			new ArraySegment<int>( new int[] { 1 } ).CopyTo( 0, null, 0, 1 );
		}

		[Test]
		[ExpectedException( typeof( ArgumentOutOfRangeException ) )]
		public void TestCopyTo_ArrayOffset_Negavie()
		{
			new ArraySegment<int>( new int[] { 1 } ).CopyTo( 0, new int[ 1 ], -1, 1 );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestCopyTo_ArrayOffset_TooBig()
		{
			new ArraySegment<int>( new int[] { 1 } ).CopyTo( 0, new int[ 1 ], 1, 1 );
		}

		[Test]
		[ExpectedException( typeof( ArgumentOutOfRangeException ) )]
		public void TestCopyTo_Count_Negavie()
		{
			new ArraySegment<int>( new int[] { 1 } ).CopyTo( 0, new int[ 1 ], 0, -1 );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestCopyTo_Count_TooBig()
		{
			new ArraySegment<int>( new int[] { 1, 2, 3 } ).CopyTo( 0, new int[ 1 ], 0, 2 );
		}
	}
}
