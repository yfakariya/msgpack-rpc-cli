namespace MsgPack.Rpc.Server
{
	using System;
	using NUnit.Framework;


	/// <summary>
	///Tests the Dispatcher 
	/// </summary>
	[TestFixture()]
	public class DispatcherTest
	{

		private Dispatcher _testClass;

		/// <summary>
		/// <see cref="NUnit"/> Set Up 
		/// </summary>
		[SetUp()]
		public void SetUp()
		{
			MsgPack.Rpc.Server.RpcServer server = null;
			_testClass = new Dispatcher( server );
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
		/// Tests the Dispatch Server Transport Request Context 
		/// </summary>
		[Test()]
		public void TestDispatchServerTransportRequestContext()
		{
			MsgPack.Rpc.Server.Protocols.ServerTransport serverTransport = null;
			MsgPack.Rpc.Server.Protocols.ServerRequestContext requestContext = null;
			_testClass.Dispatch( serverTransport, requestContext );
			Assert.Fail( "Create or modify test(s)." );

		}
	}
}
