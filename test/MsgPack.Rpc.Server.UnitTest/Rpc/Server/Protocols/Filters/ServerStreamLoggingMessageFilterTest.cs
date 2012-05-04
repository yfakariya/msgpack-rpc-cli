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
using System.Linq;
using MsgPack.Rpc.Diagnostics;
using NUnit.Framework;

namespace MsgPack.Rpc.Server.Protocols.Filters
{
	[TestFixture]
	public class ServerStreamLoggingMessageFilterTest
	{
		[Test]
		public void TestApplied()
		{
			using ( var logger = new InProcMessagePackStreamLogger() )
			{
				ServerTransportTest.TestFiltersCore(
					null,
					transport =>
					{
						Assert.That( transport.BeforeDeserializationFilters.Count, Is.EqualTo( 1 ) );
						var filter = transport.BeforeDeserializationFilters[ 0 ] as ServerStreamLoggingMessageFilter;
						Assert.That( filter, Is.Not.Null );
						Assert.That( transport.AfterSerializationFilters, Is.Empty );
					},
					null,
					new ServerStreamLoggingMessageFilterProvider( logger )
				);
			}
		}

		[Test]
		public void TestInvoked_Ok()
		{
			using ( var logger = new InProcMessagePackStreamLogger() )
			{
				ServerTransportTest.TestFiltersCore(
					null,
					null,
					( request, response ) =>
					{
						var entry = logger.Entries.Single();
						Assert.That( entry.Stream, Is.EqualTo( request ) );
					},
					new ServerStreamLoggingMessageFilterProvider( logger )
				);
			}
		}
	}
}
