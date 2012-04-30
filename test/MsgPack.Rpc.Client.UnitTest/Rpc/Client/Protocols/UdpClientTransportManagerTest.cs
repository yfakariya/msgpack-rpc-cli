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

namespace MsgPack.Rpc.Client.Protocols
{
	using System;
	using NUnit.Framework;


	/// <summary>
	///Tests the Udp Client Transport Manager 
	/// </summary>
	[TestFixture()]
	public class UdpClientTransportManagerTest
	{

		private UdpClientTransportManager _testClass;

		/// <summary>
		/// <see cref="NUnit"/> Set Up 
		/// </summary>
		[SetUp()]
		public void SetUp()
		{
			MsgPack.Rpc.Client.RpcClientConfiguration configuration = null;
			_testClass = new UdpClientTransportManager( configuration );
		}

		/// <summary>
		/// <see cref="NUnit"/> Tear Down 
		/// </summary>
		[TearDown()]
		public void TearDown()
		{
			_testClass = null;
		}

		/// <summary>
		/// Tests the Constructor Udp Client Transport Manager 
		/// </summary>
		[Test()]
		public void TestConstructorUdpClientTransportManager()
		{
			MsgPack.Rpc.Client.RpcClientConfiguration configuration = null;
			UdpClientTransportManager testUdpClientTransportManager = new UdpClientTransportManager( configuration );
			Assert.IsNotNull( testUdpClientTransportManager, "Constructor of type, UdpClientTransportManager failed to create instance." );
			Assert.Fail( "Create or modify test(s)." );

		}
	}
}
