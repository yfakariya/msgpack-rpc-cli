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
using System.Threading;
using NUnit.Framework;

namespace MsgPack.Rpc
{
	[TestFixture]
	public class OnTheFlyObjectPoolTest
	{
		[Test]
		public void TestBorrow_AlwaysFactoryCalled()
		{
			int count = 0;
			using ( var target = new OnTheFlyObjectPool<object>( conf => Interlocked.Increment( ref count ), new ObjectPoolConfiguration() ) )
			{
				Assert.That( target.Borrow(), Is.EqualTo( 1 ) );
				Assert.That( target.Borrow(), Is.EqualTo( 2 ) );
			}
		}

		[Test]
		public void TestReturn_JustHarmless()
		{
			int count = 0;
			using ( var target = new OnTheFlyObjectPool<object>( conf => Interlocked.Increment( ref count ), new ObjectPoolConfiguration() ) )
			{
				target.Return( new object() );
			}
		}

		[Test]
		public void TestEvictExtraItems_JustHarmless()
		{
			int count = 0;
			using ( var target = new OnTheFlyObjectPool<object>( conf => Interlocked.Increment( ref count ), new ObjectPoolConfiguration() ) )
			{
				target.EvictExtraItems();
			}
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestConstructor_NullFactory()
		{
			new OnTheFlyObjectPool<object>( null, null );
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestReturn_Null()
		{
			int count = 0;
			using ( var target = new OnTheFlyObjectPool<object>( conf => Interlocked.Increment( ref count ), new ObjectPoolConfiguration() ) )
			{
				target.Return( null );
			}
		}
	}
}
