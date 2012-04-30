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
	///Tests the Udp Client Transport 
	/// </summary>
	[TestFixture()]
	public class UdpClientTransportTest
	{

		private UdpClientTransport _testClass;

		/// <summary>
		/// <see cref="NUnit"/> Set Up 
		/// </summary>
		[SetUp()]
		public void SetUp()
		{
			MsgPack.Rpc.Client.Protocols.UdpClientTransportManager manager = null;
			_testClass = new UdpClientTransport( manager );
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
		/// Tests the Constructor Udp Client Transport 
		/// </summary>
		[Test()]
		public void TestConstructorUdpClientTransport()
		{
			MsgPack.Rpc.Client.Protocols.UdpClientTransportManager manager = null;
			UdpClientTransport testUdpClientTransport = new UdpClientTransport( manager );
			Assert.IsNotNull( testUdpClientTransport, "Constructor of type, UdpClientTransport failed to create instance." );
			Assert.Fail( "Create or modify test(s)." );

		}

		/// <summary>
		/// Tests the Get Client Request Context 
		/// </summary>
		[Test()]
		public void TestGetClientRequestContext()
		{
			MsgPack.Rpc.Client.Protocols.ClientRequestContext expectedClientRequestContext = null;
			MsgPack.Rpc.Client.Protocols.ClientRequestContext resultClientRequestContext = null;
			resultClientRequestContext = _testClass.GetClientRequestContext();
			Assert.AreEqual( expectedClientRequestContext, resultClientRequestContext, "GetClientRequestContext method returned unexpected result." );
			Assert.Fail( "Create or modify test(s)." );

		}
	}
}
