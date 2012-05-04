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

namespace MsgPack.Rpc.Server.Protocols.Filters
{
	[TestFixture]
	public class ServerQuotaMessageFilterTest
	{
		[Test]
		public void TestApplied()
		{
			long quota = Math.Abs( Environment.TickCount );
			ServerTransportTest.TestFiltersCore(
				null,
				transport =>
				{
					Assert.That( transport.BeforeDeserializationFilters.Count, Is.EqualTo( 1 ) );
					var filter = transport.BeforeDeserializationFilters[ 0 ] as ServerQuotaMessageFilter;
					Assert.That( filter, Is.Not.Null );
					Assert.That( filter.Quota, Is.EqualTo( quota ) );
					Assert.That( transport.AfterSerializationFilters, Is.Empty );
				},
				null,
				new ServerQuotaMessageFilterProvider( quota )
			);
		}

		[Test]
		public void TestIsEqualToQuota_Ok()
		{
			long quota = 16;
			ServerTransportTest.TestFiltersCore(
				( argumentPacker, currentLength ) =>
				{
					var remaining = quota - currentLength - 2; // header(FixArray) + header(FixRaw) = 2 byte
					argumentPacker.PackArrayHeader( 1 );
					argumentPacker.PackRaw( new byte[ remaining ] );
				},
				null,
				( request, response ) =>
				{
					// No error occurred.
					var responseMessage = Unpacking.UnpackArray( response ).Value;
					Assert.That( responseMessage[ 2 ].IsNil, "{0}:{1}", responseMessage[ 2 ], responseMessage[ 3 ] );
				},
				new ServerQuotaMessageFilterProvider( quota )
			);
		}

		[Test]
		public void TestIsGreatorThanQuota_MessagetooLongError()
		{
			long quota = 16;
			ServerTransportTest.TestFiltersCore(
				( argumentPacker, currentLength ) =>
				{
					var remaining = quota - currentLength - 2; // header(FixArray) + header(FixRaw) = 2 byte
					argumentPacker.PackArrayHeader( 1 );
					argumentPacker.PackRaw( new byte[ remaining + 1 ] );
				},
				null,
				( request, response ) =>
				{
					// Error occurred.
					var responseMessage = Unpacking.UnpackArray( response ).Value;
					Assert.That( responseMessage[ 2 ] == RpcError.MessageTooLargeError.Identifier, "{0}:{1}", responseMessage[ 2 ], responseMessage[ 3 ] );
				},
				new ServerQuotaMessageFilterProvider( quota )
			);
		}
	}
}
