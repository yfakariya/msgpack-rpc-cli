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
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc.Client.Protocols.Filters
{
	[TestFixture]
	public class ClientQuotaMessageFilterTest
	{
		[Test]
		public void TestApplied()
		{
			long quota = Math.Abs( Environment.TickCount );
			ClientTransportTest.TestFiltersCore(
				null,
				transport =>
				{
					Assert.That( transport.BeforeDeserializationFilters.Count, Is.EqualTo( 1 ) );
					var filter = transport.BeforeDeserializationFilters[ 0 ] as ClientQuotaMessageFilter;
					Assert.That( filter, Is.Not.Null );
					Assert.That( filter.Quota, Is.EqualTo( quota ) );
					Assert.That( transport.AfterSerializationFilters, Is.Empty );
				},
				null,
				new ClientQuotaMessageFilterProvider( quota )
			);
		}

		[Test]
		public void TestIsEqualToQuota_Ok()
		{
			long quota = 16;
			ClientTransportTest.TestFiltersCore(
				( method, messasgeId, args ) =>
				{
					// FixedArray + PositiveFixNum + PositiveFixNum + Nil
					const int overHead = 1 + 1 + 1 + 1;
					var remaining = quota - overHead - 1; // header(FixRaw)
					return new MessagePackObject( new String( 'a', ( int )( remaining ) ) );
				},
				null,
				( request, responseData, fatalError, responseError ) =>
				{
					// No error occurred.
					Assert.That( fatalError, Is.Null );
					Assert.That( responseError.IsSuccess, responseError.IsSuccess ? String.Empty : responseError.ToException().ToString() );
				},
				new ClientQuotaMessageFilterProvider( quota )
			);
		}

		[Test]
		public void TestIsGreatorThanQuota_MessagetooLongError()
		{
			long quota = 16;
			ClientTransportTest.TestFiltersCore(
				( method, messasgeId, args ) =>
				{
					// FixedArray + PositiveFixNum + PositiveFixNum + Nil
					const int overHead = 1 + 1 + 1 + 1;
					var remaining = quota - overHead - 1; // header(FixRaw)
					return new MessagePackObject( new String( 'a', ( int )( remaining + 1 ) ) );
				},
				null,
				( request, responseData, fatalError, responseError ) =>
				{
					// Error occurred.
					Assert.That( fatalError, Is.Not.Null.And.InstanceOf<RpcMessageTooLongException>() );
					Assert.That( responseError.IsSuccess, responseError.IsSuccess ? String.Empty : responseError.ToException().ToString() );
				},
				new ClientQuotaMessageFilterProvider( quota )
			);
		}
	}
}
