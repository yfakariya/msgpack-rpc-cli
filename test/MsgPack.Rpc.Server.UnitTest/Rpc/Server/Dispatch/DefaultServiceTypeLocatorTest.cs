namespace MsgPack.Rpc.Server.Dispatch
{
	using System;
	using NUnit.Framework;


	/// <summary>
	///Tests the Default Service Type Locator 
	/// </summary>
	[TestFixture()]
	public class DefaultServiceTypeLocatorTest
	{

		private DefaultServiceTypeLocator _testClass;

		/// <summary>
		/// <see cref="NUnit"/> Set Up 
		/// </summary>
		[SetUp()]
		public void SetUp()
		{
			_testClass = new DefaultServiceTypeLocator();
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
		/// Tests the Constructor Default Service Type Locator 
		/// </summary>
		[Test()]
		public void TestConstructorDefaultServiceTypeLocator()
		{
			DefaultServiceTypeLocator testDefaultServiceTypeLocator = new DefaultServiceTypeLocator();
			Assert.IsNotNull( testDefaultServiceTypeLocator, "Constructor of type, DefaultServiceTypeLocator failed to create instance." );
			Assert.Fail( "Create or modify test(s)." );

		}

		/// <summary>
		/// Tests the Add Service 
		/// </summary>
		[Test()]
		public void TestAddService()
		{
			System.Type serviceType = null;
			bool expectedBoolean = null;
			bool resultBoolean = null;
			resultBoolean = _testClass.AddService( serviceType );
			Assert.AreEqual( expectedBoolean, resultBoolean, "AddService method returned unexpected result." );
			Assert.Fail( "Create or modify test(s)." );

		}

		/// <summary>
		/// Tests the Remove Service 
		/// </summary>
		[Test()]
		public void TestRemoveService()
		{
			System.Type serviceType = null;
			bool expectedBoolean = null;
			bool resultBoolean = null;
			resultBoolean = _testClass.RemoveService( serviceType );
			Assert.AreEqual( expectedBoolean, resultBoolean, "RemoveService method returned unexpected result." );
			Assert.Fail( "Create or modify test(s)." );

		}

		/// <summary>
		/// Tests the Clear Services 
		/// </summary>
		[Test()]
		public void TestClearServices()
		{
			_testClass.ClearServices();
			Assert.Fail( "Create or modify test(s)." );

		}

		/// <summary>
		/// Tests the Enumerate Services 
		/// </summary>
		[Test()]
		public void TestEnumerateServices()
		{
			System.Collections.Generic.IEnumerable<MsgPack.Rpc.Server.Dispatch.ServiceDescription> expectedIEnumerable = null;
			System.Collections.Generic.IEnumerable<MsgPack.Rpc.Server.Dispatch.ServiceDescription> resultIEnumerable = null;
			resultIEnumerable = _testClass.EnumerateServices();
			Assert.AreEqual( expectedIEnumerable, resultIEnumerable, "EnumerateServices method returned unexpected result." );
			Assert.Fail( "Create or modify test(s)." );

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
