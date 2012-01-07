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
using NUnit.Framework;

namespace MsgPack.Rpc
{
	[TestFixture]
	public class ByteArraySegmentStreamTest
	{
		[Test]
		public void TestReadByte()
		{
			using ( var target = new ByteArraySegmentStream( CreateData() ) )
			{
				List<byte> result = new List<byte>();
				for ( int b = target.ReadByte(); 0 < b; b = target.ReadByte() )
				{
					result.Add( ( byte )b );
				}

				Assert.That( result, Is.EqualTo( new byte[] { 12, 13, 14, 22, 23, 24, 32, 33, 34 } ) );
			}
		}

		[Test]
		public void TestRead()
		{
			using ( var target = new ByteArraySegmentStream( CreateData() ) )
			{
				byte[] buffer = new byte[ 2 ];
				Assert.That( target.Read( buffer, 0, buffer.Length ), Is.EqualTo( 2 ) );
				Assert.That( buffer, Is.EqualTo( new byte[] { 12, 13 } ) );
				Assert.That( target.Read( buffer, 0, buffer.Length ), Is.EqualTo( 2 ) );
				Assert.That( buffer, Is.EqualTo( new byte[] { 14, 22 } ) );
				Assert.That( target.Read( buffer, 0, buffer.Length ), Is.EqualTo( 2 ) );
				Assert.That( buffer, Is.EqualTo( new byte[] { 23, 24 } ) );
				Assert.That( target.Read( buffer, 0, buffer.Length ), Is.EqualTo( 2 ) );
				Assert.That( buffer, Is.EqualTo( new byte[] { 32, 33 } ) );
				Array.Clear( buffer, 0, buffer.Length );
				Assert.That( target.Read( buffer, 0, buffer.Length ), Is.EqualTo( 1 ) );
				Assert.That( buffer, Is.EqualTo( new byte[] { 34, 0 } ) );
				Array.Clear( buffer, 0, buffer.Length );
				Assert.That( target.Read( buffer, 0, buffer.Length ), Is.EqualTo( 0 ) );
				Assert.That( buffer, Is.EqualTo( new byte[] { 0, 0 } ) );
			}

			using ( var target = new ByteArraySegmentStream( CreateData() ) )
			{
				byte[] buffer = new byte[ 3 ];
				Assert.That( target.Read( buffer, 0, buffer.Length ), Is.EqualTo( 3 ) );
				Assert.That( buffer, Is.EqualTo( new byte[] { 12, 13, 14 } ) );
				Assert.That( target.Read( buffer, 0, buffer.Length ), Is.EqualTo( 3 ) );
				Assert.That( buffer, Is.EqualTo( new byte[] { 22, 23, 24 } ) );
				Array.Clear( buffer, 0, buffer.Length );
				Assert.That( target.Read( buffer, 0, buffer.Length ), Is.EqualTo( 3 ) );
				Assert.That( buffer, Is.EqualTo( new byte[] { 32, 33, 34 } ) );
				Array.Clear( buffer, 0, buffer.Length );
				Assert.That( target.Read( buffer, 0, buffer.Length ), Is.EqualTo( 0 ) );
				Assert.That( buffer, Is.EqualTo( new byte[] { 0, 0, 0 } ) );
			}

			using ( var target = new ByteArraySegmentStream( CreateData() ) )
			{
				byte[] buffer = new byte[ 4 ];
				Assert.That( target.Read( buffer, 0, buffer.Length ), Is.EqualTo( 4 ) );
				Assert.That( buffer, Is.EqualTo( new byte[] { 12, 13, 14, 22 } ) );
				Assert.That( target.Read( buffer, 0, buffer.Length ), Is.EqualTo( 4 ) );
				Assert.That( buffer, Is.EqualTo( new byte[] { 23, 24, 32, 33 } ) );
				Array.Clear( buffer, 0, buffer.Length );
				Assert.That( target.Read( buffer, 0, buffer.Length ), Is.EqualTo( 1 ) );
				Assert.That( buffer, Is.EqualTo( new byte[] { 34, 0, 0, 0 } ) );
				Array.Clear( buffer, 0, buffer.Length );
				Assert.That( target.Read( buffer, 0, buffer.Length ), Is.EqualTo( 0 ) );
				Assert.That( buffer, Is.EqualTo( new byte[] { 0, 0, 0, 0 } ) );
			}
		}

		[Test]
		public void TestPosition()
		{
			using ( var target = new ByteArraySegmentStream( CreateData() ) )
			{
				Assert.That( target.Position, Is.EqualTo( 0 ) );
				Assert.That( target.ReadByte(), Is.EqualTo( 12 ) );
				target.Position = 0;
				Assert.That( target.Position, Is.EqualTo( 0 ) );
				Assert.That( target.ReadByte(), Is.EqualTo( 12 ) );

				target.Position = target.Length;
				Assert.That( target.ReadByte(), Is.EqualTo( -1 ) );
				Assert.That( target.Position, Is.EqualTo( target.Length ) );

				target.Position = 3;
				Assert.That( target.ReadByte(), Is.EqualTo( 22 ) );
				target.Position = 2;
				Assert.That( target.ReadByte(), Is.EqualTo( 14 ) );

				target.Position = target.Length - 1;
				Assert.That( target.ReadByte(), Is.EqualTo( 34 ) );
				Assert.That( target.Position, Is.EqualTo( target.Length ) );
				Assert.That( target.ReadByte(), Is.EqualTo( -1 ) );
				Assert.That( target.Position, Is.EqualTo( target.Length ) );
			}
		}

		[Test]
		public void TestSeek_Begin()
		{
			using ( var target = new ByteArraySegmentStream( CreateData() ) )
			{
				Assert.That( target.Position, Is.EqualTo( 0 ) );
				Assert.That( target.ReadByte(), Is.EqualTo( 12 ) );
				target.Seek( 0, SeekOrigin.Begin );
				Assert.That( target.Position, Is.EqualTo( 0 ) );
				Assert.That( target.ReadByte(), Is.EqualTo( 12 ) );

				target.Seek( target.Length, SeekOrigin.Begin );
				Assert.That( target.ReadByte(), Is.EqualTo( -1 ) );
				Assert.That( target.Position, Is.EqualTo( target.Length ) );

				target.Seek( 3, SeekOrigin.Begin );
				Assert.That( target.ReadByte(), Is.EqualTo( 22 ) );
				target.Seek( 2, SeekOrigin.Begin );
				Assert.That( target.ReadByte(), Is.EqualTo( 14 ) );

				target.Seek( target.Length - 1, SeekOrigin.Begin );
				Assert.That( target.ReadByte(), Is.EqualTo( 34 ) );
				Assert.That( target.Position, Is.EqualTo( target.Length ) );
				Assert.That( target.ReadByte(), Is.EqualTo( -1 ) );
				Assert.That( target.Position, Is.EqualTo( target.Length ) );
			}
		}


		[Test]
		public void TestSeek_Current()
		{
			using ( var target = new ByteArraySegmentStream( CreateData() ) )
			{
				Assert.That( target.Position, Is.EqualTo( 0 ) );
				Assert.That( target.ReadByte(), Is.EqualTo( 12 ) );
				target.Seek( 0, SeekOrigin.Current );
				Assert.That( target.Position, Is.EqualTo( 1 ) );
				Assert.That( target.ReadByte(), Is.EqualTo( 13 ) );

				target.Seek( target.Length - 2, SeekOrigin.Current );
				Assert.That( target.ReadByte(), Is.EqualTo( -1 ) );
				Assert.That( target.Position, Is.EqualTo( target.Length ) );

				target.Seek( -1, SeekOrigin.Current );
				Assert.That( target.ReadByte(), Is.EqualTo( 34 ) );
				target.Seek( -3, SeekOrigin.Current );
				Assert.That( target.ReadByte(), Is.EqualTo( 32 ) );
				target.Seek( -2, SeekOrigin.Current );
				Assert.That( target.ReadByte(), Is.EqualTo( 24 ) );
				target.Seek( -6, SeekOrigin.Current );
				Assert.That( target.ReadByte(), Is.EqualTo( 12 ) );

				target.Seek( target.Length - 2, SeekOrigin.Current );
				Assert.That( target.ReadByte(), Is.EqualTo( 34 ) );
				Assert.That( target.Position, Is.EqualTo( target.Length ) );
				Assert.That( target.ReadByte(), Is.EqualTo( -1 ) );
				Assert.That( target.Position, Is.EqualTo( target.Length ) );
			}
		}

		[Test]
		public void TestSeek_End()
		{
			using ( var target = new ByteArraySegmentStream( CreateData() ) )
			{
				Assert.That( target.Position, Is.EqualTo( 0 ) );
				Assert.That( target.ReadByte(), Is.EqualTo( 12 ) );
				target.Seek( -target.Length, SeekOrigin.End );
				Assert.That( target.Position, Is.EqualTo( 0 ) );
				Assert.That( target.ReadByte(), Is.EqualTo( 12 ) );

				target.Seek( 0, SeekOrigin.End );
				Assert.That( target.ReadByte(), Is.EqualTo( -1 ) );
				Assert.That( target.Position, Is.EqualTo( target.Length ) );

				target.Seek( -4, SeekOrigin.End );
				Assert.That( target.ReadByte(), Is.EqualTo( 24 ) );
				target.Seek( -3, SeekOrigin.End );
				Assert.That( target.ReadByte(), Is.EqualTo( 32 ) );

				target.Seek( -1, SeekOrigin.End );
				Assert.That( target.ReadByte(), Is.EqualTo( 34 ) );
				Assert.That( target.Position, Is.EqualTo( target.Length ) );
				Assert.That( target.ReadByte(), Is.EqualTo( -1 ) );
				Assert.That( target.Position, Is.EqualTo( target.Length ) );
			}
		}

		[Test]
		public void TestGetBuffer_Int64_Int64_Empty_Empty()
		{
			using ( var target = new ByteArraySegmentStream( new ArraySegment<byte>[ 0 ] ) )
			{
				var result = target.GetBuffer( 0, 1 );
				Assert.That( result, Is.Not.Null.And.Empty );
			}
		}

		[Test]
		public void TestGetBuffer_Int64_Int64_1Byte_AsIs()
		{
			using ( var target = CreateForGetBuffer() )
			{
				var result = target.GetBuffer( 0, 1 );
				Assert.That( result.SelectMany( segment => segment.AsEnumerable() ).ToArray(), Is.EqualTo( new byte[] { 12 } ) );
			}
		}

		[Test]
		public void TestGetBuffer_Int64_Int64_1Segment_Full_AsIs()
		{
			using ( var target = CreateForGetBuffer() )
			{
				var result = target.GetBuffer( 0, 3 );
				Assert.That( result.SelectMany( segment => segment.AsEnumerable() ).ToArray(), Is.EqualTo( new byte[] { 12, 13, 14 } ) );
			}
		}

		[Test]
		public void TestGetBuffer_Int64_Int64_2Segments_Full_AsIs()
		{
			using ( var target = CreateForGetBuffer() )
			{
				var result = target.GetBuffer( 0, 6 );
				Assert.That( result.SelectMany( segment => segment.AsEnumerable() ).ToArray(), Is.EqualTo( new byte[] { 12, 13, 14, 22, 23, 24 } ) );
			}
		}

		[Test]
		public void TestGetBuffer_Int64_Int64_2Of4Segments_Full_AsIs()
		{
			using ( var target = CreateForGetBuffer() )
			{
				var result = target.GetBuffer( 3, 6 );
				Assert.That( result.SelectMany( segment => segment.AsEnumerable() ).ToArray(), Is.EqualTo( new byte[] { 22, 23, 24, 32, 33, 34 } ) );
			}
		}

		[Test]
		public void TestGetBuffer_Int64_Int64_1Segment_HeadToIntermediate_AsIs()
		{
			using ( var target = CreateForGetBuffer() )
			{
				var result = target.GetBuffer( 0, 2 );
				Assert.That( result.SelectMany( segment => segment.AsEnumerable() ).ToArray(), Is.EqualTo( new byte[] { 12, 13 } ) );
			}
		}

		[Test]
		public void TestGetBuffer_Int64_Int64_2Segments_HeadToIntermediate_AsIs()
		{
			using ( var target = CreateForGetBuffer() )
			{
				var result = target.GetBuffer( 0, 5 );
				Assert.That( result.SelectMany( segment => segment.AsEnumerable() ).ToArray(), Is.EqualTo( new byte[] { 12, 13, 14, 22, 23 } ) );
			}
		}

		[Test]
		public void TestGetBuffer_Int64_Int64_2Of4Segments_HeadToIntermediate_AsIs()
		{
			using ( var target = CreateForGetBuffer() )
			{
				var result = target.GetBuffer( 3, 5 );
				Assert.That( result.SelectMany( segment => segment.AsEnumerable() ).ToArray(), Is.EqualTo( new byte[] { 22, 23, 24, 32, 33 } ) );
			}
		}

		[Test]
		public void TestGetBuffer_Int64_Int64_1Segment_IntermediateToTail_AsIs()
		{
			using ( var target = CreateForGetBuffer() )
			{
				var result = target.GetBuffer( 10, 2 );
				Assert.That( result.SelectMany( segment => segment.AsEnumerable() ).ToArray(), Is.EqualTo( new byte[] { 43, 44 } ) );
			}
		}

		[Test]
		public void TestGetBuffer_Int64_Int64_2Segments_IntermediateToTail_AsIs()
		{
			using ( var target = CreateForGetBuffer() )
			{
				var result = target.GetBuffer( 7, 5 );
				Assert.That( result.SelectMany( segment => segment.AsEnumerable() ).ToArray(), Is.EqualTo( new byte[] { 33, 34, 42, 43, 44 } ) );
			}
		}

		[Test]
		public void TestGetBuffer_Int64_Int64_2Of4Segments_IntermediateToTail_AsIs()
		{
			using ( var target = CreateForGetBuffer() )
			{
				var result = target.GetBuffer( 4, 8 );
				Assert.That( result.SelectMany( segment => segment.AsEnumerable() ).ToArray(), Is.EqualTo( new byte[] { 23, 24, 32, 33, 34, 42, 43, 44 } ) );
			}
		}

		[Test]
		public void TestGetBuffer_Int64_Int64_1Segment_IntermediateToIntermediate_AsIs()
		{
			using ( var target = CreateForGetBuffer() )
			{
				var result = target.GetBuffer( 1, 1 );
				Assert.That( result.SelectMany( segment => segment.AsEnumerable() ), Is.EqualTo( new byte[] { 13 } ) );
			}
		}

		[Test]
		public void TestGetBuffer_Int64_Int64_2Segments_IntermediateToIntermediate_AsIs()
		{
			using ( var target = CreateForGetBuffer() )
			{
				var result = target.GetBuffer( 1, 4 );
				Assert.That( result.SelectMany( segment => segment.AsEnumerable() ).ToArray(), Is.EqualTo( new byte[] { 13, 14, 22, 23 } ) );
			}
		}

		[Test]
		public void TestGetBuffer_Int64_Int64_2Of4Segments_IntermediateToIntermediate_AsIs()
		{
			using ( var target = CreateForGetBuffer() )
			{
				var result = target.GetBuffer( 4, 4 );
				Assert.That( result.SelectMany( segment => segment.AsEnumerable() ).ToArray(), Is.EqualTo( new byte[] { 23, 24, 32, 33 } ) );
			}
		}

		[Test]
		[ExpectedException( typeof( ArgumentOutOfRangeException ) )]
		public void TestGetBuffer_Int64_Int64_StartIsNegative()
		{
			using ( var target = CreateForGetBuffer() )
			{
				var dummy = target.GetBuffer( -1, 1 );
			}
		}

		[Test]
		[ExpectedException( typeof( ArgumentOutOfRangeException ) )]
		public void TestGetBuffer_Int64_Int64_LengthIsNegative()
		{
			using ( var target = CreateForGetBuffer() )
			{
				var dummy = target.GetBuffer( 0, -1 );
			}
		}

		[Test]
		public void TestGetBuffer_Int64_Int64_StartIsTooLarge_Empty()
		{
			using ( var target = CreateForGetBuffer() )
			{
				var result = target.GetBuffer( target.Length, 1 );
				Assert.That( result.SelectMany( segment => segment.AsEnumerable() ), Is.Empty );
			}
		}

		[Test]
		public void TestGetBuffer_Int64_Int64_LengthIsZero_Empty()
		{
			using ( var target = CreateForGetBuffer() )
			{
				var result = target.GetBuffer( 0, 0 );
				Assert.That( result.SelectMany( segment => segment.AsEnumerable() ), Is.Empty );
			}
		}

		[Test]
		public void TestGetBuffer_Int64_Int64_LengthIsTooLarge_AsLargeAsPossible()
		{
			using ( var target = CreateForGetBuffer() )
			{
				var result = target.GetBuffer( 0, target.Length + 1 );
				Assert.That( result.SelectMany( segment => segment.AsEnumerable() ).ToArray(), Is.EqualTo( target.ToArray() ) );
			}
		}

		private static ArraySegment<byte>[] CreateData()
		{
			return
				new ArraySegment<byte>[]
				{
					new ArraySegment<byte>( new byte[] { 11, 12, 13, 14, 15 }, 1, 3 ),
					new ArraySegment<byte>( new byte[] { 21, 22, 23, 24, 25 }, 1, 3 ),
					new ArraySegment<byte>( new byte[] { 31, 32, 33, 34, 35 }, 1, 3 ),
				};
		}

		private static ByteArraySegmentStream CreateForGetBuffer()
		{
			return
				new ByteArraySegmentStream(
					new ArraySegment<byte>[]
					{
						new ArraySegment<byte>( new byte[] { 11, 12, 13, 14, 15 }, 1, 3 ),
						new ArraySegment<byte>( new byte[] { 21, 22, 23, 24, 25 }, 1, 3 ),
						new ArraySegment<byte>( new byte[] { 31, 32, 33, 34, 35 }, 1, 3 ),
						new ArraySegment<byte>( new byte[] { 41, 42, 43, 44, 45 }, 1, 3 ),
					}
				);
		}
	}
}
