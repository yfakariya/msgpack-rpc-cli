namespace MsgPack.Rpc.Server
{
	using System;
	using NUnit.Framework;


	/// <summary>
	///Tests the Locator Based Dispatcher 
	/// </summary>
	[TestFixture()]
	public class LocatorBasedDispatcherTest
	{

		private LocatorBasedDispatcher _testClass;

		/// <summary>
		/// <see cref="NUnit"/> Set Up 
		/// </summary>
		[SetUp()]
		public void SetUp()
		{
			MsgPack.Rpc.Server.RpcServer server = null;
			_testClass = new LocatorBasedDispatcher( server );
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
		/// Tests the Constructor Locator Based Dispatcher 
		/// </summary>
		[Test()]
		public void TestConstructorLocatorBasedDispatcher()
		{
			MsgPack.Rpc.Server.RpcServer server = null;
			LocatorBasedDispatcher testLocatorBasedDispatcher = new LocatorBasedDispatcher( server );
			Assert.IsNotNull( testLocatorBasedDispatcher, "Constructor of type, LocatorBasedDispatcher failed to create instance." );
			Assert.Fail( "Create or modify test(s)." );

		}
	}
}
