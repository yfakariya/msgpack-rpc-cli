namespace MsgPack.Rpc.Server.Dispatch
{
	using System;
	using NUnit.Framework;


	/// <summary>
	///Tests the File Based Service Type Locator 
	/// </summary>
	[TestFixture()]
	public class FileBasedServiceTypeLocatorTest
	{

		private FileBasedServiceTypeLocator _testClass;

		/// <summary>
		/// <see cref="NUnit"/> Set Up 
		/// </summary>
		[SetUp()]
		public void SetUp()
		{
			_testClass = new FileBasedServiceTypeLocator();
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
		/// Tests the Find Services 
		/// </summary>
		[Test()]
		public void TestFindServices()
		{
			System.Collections.Generic.IEnumerable<MsgPack.Rpc.Server.Dispatch.ServiceDescription> expectedIEnumerable = null;
			System.Collections.Generic.IEnumerable<MsgPack.Rpc.Server.Dispatch.ServiceDescription> resultIEnumerable = null;
			resultIEnumerable = _testClass.FindServices();
			Assert.AreEqual( expectedIEnumerable, resultIEnumerable, "FindServices method returned unexpected result." );
			Assert.Fail( "Create or modify test(s)." );

		}
	}
}
