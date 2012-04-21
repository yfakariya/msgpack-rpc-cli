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
