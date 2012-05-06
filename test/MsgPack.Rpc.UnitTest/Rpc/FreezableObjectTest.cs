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
	public class FreezableObjectTest
	{
		[Test]
		public void TestIsFrozen_Default_False()
		{
			var target = new Target();

			Assert.That( target.IsFrozen, Is.False );
		}

		[Test]
		public void TestFreeze_IsFrozenToBeTrue()
		{
			IFreezable target = new Target();
			target.Freeze();

			Assert.That( target.IsFrozen, Is.True );
		}

		[Test]
		public void TestFreeze_ReturnsThis()
		{
			IFreezable target = new Target();
			var result = target.Freeze();

			Assert.That( result, Is.SameAs( target ) );
		}

		[Test]
		public void TestAsFrozen_IsFrozenRemainsFalse()
		{
			IFreezable target = new Target();
			target.AsFrozen();

			Assert.That( target.IsFrozen, Is.False );
		}

		[Test]
		public void TestAsFrozen_ReturnsDifferObject()
		{
			IFreezable target = new Target();
			var result = target.AsFrozen();

			Assert.That( result, Is.Not.SameAs( target ) );
		}

		[Test]
		public void TestAsFrozen_ReturnsIsFrozenIsTrue()
		{
			IFreezable target = new Target();
			var result = target.AsFrozen();

			Assert.That( result.IsFrozen, Is.True );
		}

		[Test]
		public void TestAsFrozen_OnFrozenObject_ReturnsThis()
		{
			IFreezable target = new Target();
			target.Freeze();

			var result = target.AsFrozen();

			Assert.That( result, Is.SameAs( target ) );
		}

		[Test]
		[ExpectedException( typeof( InvalidOperationException ) )]
		public void TestVerifyIsNotFrozen_IsFrozen_Fail()
		{
			var target = new Target();
			(target as IFreezable).Freeze();

			target.InvokeVerifyIsNotFrozen();
		}

		[Test]
		public void TestVerifyIsNotFrozen_IsNotFrozen_Harmless()
		{
			var target = new Target();

			target.InvokeVerifyIsNotFrozen();
		}

		[Test]
		public void TestClone_ReturnsDiffer()
		{
			ICloneable target = new Target();
			var result = target.Clone();

			Assert.That( result, Is.Not.SameAs( target ) );
		}

		[Test]
		public void TestClone_IsNotFrozen_ReturnsIsNotFrozen()
		{
			ICloneable target = new Target();
			var result = target.Clone() as Target;

			Assert.That( result.IsFrozen, Is.False );
		}

		[Test]
		public void TestClone_IsFrozen_ReturnsIsNotFrozen()
		{
			ICloneable target = new Target();
			( target as IFreezable ).Freeze();
			var result = target.Clone() as Target;

			Assert.That( result.IsFrozen, Is.False );
		}
		private sealed class Target : FreezableObject
		{
			public void InvokeVerifyIsNotFrozen()
			{
				this.VerifyIsNotFrozen();
			}
		}
	}
}
