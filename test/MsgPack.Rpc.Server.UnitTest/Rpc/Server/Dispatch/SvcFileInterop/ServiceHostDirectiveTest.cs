namespace MsgPack.Rpc.Server.Dispatch.SvcFileInterop
{
	using System;
	using NUnit.Framework;


	/// <summary>
	///Tests the Service Host Directive 
	/// </summary>
	[TestFixture()]
	public class ServiceHostDirectiveTest
	{

		private ServiceHostDirective _testClass;

		/// <summary>
		/// <see cref="NUnit"/> Set Up 
		/// </summary>
		[SetUp()]
		public void SetUp()
		{
			_testClass = new ServiceHostDirective();
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
		/// Tests the Constructor Service Host Directive 
		/// </summary>
		[Test()]
		public void TestConstructorServiceHostDirective()
		{
			ServiceHostDirective testServiceHostDirective = new ServiceHostDirective();
			Assert.IsNotNull( testServiceHostDirective, "Constructor of type, ServiceHostDirective failed to create instance." );
			Assert.Fail( "Create or modify test(s)." );

		}
	}
}
