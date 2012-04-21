namespace MsgPack.Rpc.Client.Protocols
{
	using System;
	using NUnit.Framework;


	/// <summary>
	///Tests the Tcp Client Transport Manager 
	/// </summary>
	[TestFixture()]
	public class TcpClientTransportManagerTest
	{

		private TcpClientTransportManager _testClass;

		/// <summary>
		/// <see cref="NUnit"/> Set Up 
		/// </summary>
		[SetUp()]
		public void SetUp()
		{
			MsgPack.Rpc.Client.RpcClientConfiguration configuration = null;
			_testClass = new TcpClientTransportManager( configuration );
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
		/// Tests the Constructor Tcp Client Transport Manager 
		/// </summary>
		[Test()]
		public void TestConstructorTcpClientTransportManager()
		{
			MsgPack.Rpc.Client.RpcClientConfiguration configuration = null;
			TcpClientTransportManager testTcpClientTransportManager = new TcpClientTransportManager( configuration );
			Assert.IsNotNull( testTcpClientTransportManager, "Constructor of type, TcpClientTransportManager failed to create instance." );
			Assert.Fail( "Create or modify test(s)." );

		}
	}
}
