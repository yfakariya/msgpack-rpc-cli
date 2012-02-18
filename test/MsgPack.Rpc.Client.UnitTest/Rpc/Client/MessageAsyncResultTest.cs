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

namespace MsgPack.Rpc.Client
{
	[TestFixture]
	public class MessageAsyncResultTest
	{
		[Test]
		public void TestConstructor_MessageIdIsSet()
		{
			int? messageId = Math.Abs( Environment.TickCount );
			object owner = new object();
			var target =
				new Target(
					owner,
					messageId,
					null,
					null
				);
			Assert.That( target.Owner, Is.SameAs( owner ) );
			Assert.That( target.MessageId, Is.EqualTo( messageId ) );
		}

		[Test]
		public void TestConstructor_MessageIdNull_IsSet()
		{
			int? messageId = null;
			object owner = new object();
			var target =
				new Target(
					owner,
					messageId,
					null,
					null
				);
			Assert.That( target.Owner, Is.SameAs( owner ) );
			Assert.That( target.MessageId, Is.EqualTo( messageId ) );
		}

		private sealed class Target : MessageAsyncResult
		{
			public Target( object owner, int? messageId, AsyncCallback asyncCallback, object asyncState ) 
				: base( owner, messageId, asyncCallback, asyncState ) { }
		}
	}
}
