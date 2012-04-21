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
