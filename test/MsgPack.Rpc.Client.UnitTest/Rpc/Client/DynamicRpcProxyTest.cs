namespace MsgPack.Rpc.Client
{
	using System;
	using NUnit.Framework;


	/// <summary>
	///Tests the Dynamic Rpc Proxy 
	/// </summary>
	[TestFixture()]
	public class DynamicRpcProxyTest
	{

		private DynamicRpcProxy _testClass;

		/// <summary>
		/// <see cref="NUnit"/> Set Up 
		/// </summary>
		[SetUp()]
		public void SetUp()
		{
			MsgPack.Rpc.Client.RpcClient client = null;
			_testClass = new DynamicRpcProxy( client );
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
		/// Tests the Constructor Dynamic Rpc Proxy 
		/// </summary>
		[Test()]
		public void TestConstructorDynamicRpcProxy()
		{
			MsgPack.Rpc.Client.RpcClient client = null;
			DynamicRpcProxy testDynamicRpcProxy = new DynamicRpcProxy( client );
			Assert.IsNotNull( testDynamicRpcProxy, "Constructor of type, DynamicRpcProxy failed to create instance." );
			Assert.Fail( "Create or modify test(s)." );

		}

		/// <summary>
		/// Tests the Dispose 
		/// </summary>
		[Test()]
		public void TestDispose()
		{
			_testClass.Dispose();
			Assert.Fail( "Create or modify test(s)." );

		}

		/// <summary>
		/// Tests the Try Invoke Member 
		/// </summary>
		[Test()]
		public void TestTryInvokeMember()
		{
			System.Dynamic.InvokeMemberBinder binder = null;
			object[] args = null;
			object result = null;
			object expectedresult = null;
			bool expectedBoolean = null;
			bool resultBoolean = null;
			resultBoolean = _testClass.TryInvokeMember( binder, args, out result );
			Assert.AreEqual( expectedBoolean, resultBoolean, "TryInvokeMember method returned unexpected result." );
			Assert.IsNotNull( expectedresult, "result out parameter should not be null" );
			Assert.Fail( "Create or modify test(s)." );

		}

		/// <summary>
		/// Tests the Create Target End Point 
		/// </summary>
		[Test()]
		public void TestCreateTargetEndPoint()
		{
			System.Net.EndPoint targetEndPoint = null;
			MsgPack.Rpc.Client.DynamicRpcProxy expectedDynamicRpcProxy = null;
			MsgPack.Rpc.Client.DynamicRpcProxy resultDynamicRpcProxy = null;
			resultDynamicRpcProxy = DynamicRpcProxy.Create( targetEndPoint );
			Assert.AreEqual( expectedDynamicRpcProxy, resultDynamicRpcProxy, "Create method returned unexpected result." );
			Assert.Fail( "Create or modify test(s)." );

		}

		/// <summary>
		/// Tests the Create Target End Point Configuration 
		/// </summary>
		[Test()]
		public void TestCreateTargetEndPointConfiguration()
		{
			System.Net.EndPoint targetEndPoint = null;
			MsgPack.Rpc.Client.RpcClientConfiguration configuration = null;
			MsgPack.Rpc.Client.DynamicRpcProxy expectedDynamicRpcProxy = null;
			MsgPack.Rpc.Client.DynamicRpcProxy resultDynamicRpcProxy = null;
			resultDynamicRpcProxy = DynamicRpcProxy.Create( targetEndPoint, configuration );
			Assert.AreEqual( expectedDynamicRpcProxy, resultDynamicRpcProxy, "Create method returned unexpected result." );
			Assert.Fail( "Create or modify test(s)." );

		}

		/// <summary>
		/// Tests the Create Target End Point Serialization Context 
		/// </summary>
		[Test()]
		public void TestCreateTargetEndPointSerializationContext()
		{
			System.Net.EndPoint targetEndPoint = null;
			MsgPack.Serialization.SerializationContext serializationContext = null;
			MsgPack.Rpc.Client.DynamicRpcProxy expectedDynamicRpcProxy = null;
			MsgPack.Rpc.Client.DynamicRpcProxy resultDynamicRpcProxy = null;
			resultDynamicRpcProxy = DynamicRpcProxy.Create( targetEndPoint, serializationContext );
			Assert.AreEqual( expectedDynamicRpcProxy, resultDynamicRpcProxy, "Create method returned unexpected result." );
			Assert.Fail( "Create or modify test(s)." );

		}

		/// <summary>
		/// Tests the Create Target End Point Configuration Serialization Context 
		/// </summary>
		[Test()]
		public void TestCreateTargetEndPointConfigurationSerializationContext()
		{
			System.Net.EndPoint targetEndPoint = null;
			MsgPack.Rpc.Client.RpcClientConfiguration configuration = null;
			MsgPack.Serialization.SerializationContext serializationContext = null;
			MsgPack.Rpc.Client.DynamicRpcProxy expectedDynamicRpcProxy = null;
			MsgPack.Rpc.Client.DynamicRpcProxy resultDynamicRpcProxy = null;
			resultDynamicRpcProxy = DynamicRpcProxy.Create( targetEndPoint, configuration, serializationContext );
			Assert.AreEqual( expectedDynamicRpcProxy, resultDynamicRpcProxy, "Create method returned unexpected result." );
			Assert.Fail( "Create or modify test(s)." );

		}
	}
}
