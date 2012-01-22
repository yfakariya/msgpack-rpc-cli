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

using System;
using System.Linq;
using NUnit.Framework;

namespace MsgPack.Rpc.Server.Dispatch
{

	/// <summary>
	///Tests the Default Service Type Locator 
	/// </summary>
	[TestFixture()]
	public class DefaultServiceTypeLocatorTest
	{
		[Test()]
		public void TestAddService_Once_Sucess()
		{
			var serviceType = typeof( Service );

			var target = new DefaultServiceTypeLocator();

			var result = target.AddService( serviceType );

			Assert.That( result, Is.True );
			Assert.That( target.EnumerateServices().Count(), Is.EqualTo( 1 ) );
		}

		[Test()]
		public void TestAddService_Twise_FirstIsSucceededAndSecondIsFailed()
		{
			var serviceType = typeof( Service );

			var target = new DefaultServiceTypeLocator();

			var result1 = target.AddService( serviceType );
			var result2 = target.AddService( serviceType );

			Assert.That( result1, Is.True );
			Assert.That( result2, Is.False );
			Assert.That( target.EnumerateServices().Count(), Is.EqualTo( 1 ) );
		}

		[Test()]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestAddService_Null()
		{
			Type serviceType = null;

			var target = new DefaultServiceTypeLocator();

			target.AddService( serviceType );
		}

		[Test()]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestAddService_IsNotMarkedWithRpcServiceAttribute()
		{
			Type serviceType = typeof( string );

			var target = new DefaultServiceTypeLocator();

			target.AddService( serviceType );
		}

		[Test()]
		[Description( "Depends on AddService" )]
		public void TestRemoveService_Registerd_Success()
		{
			var serviceType = typeof( Service );

			var target = new DefaultServiceTypeLocator();

			target.AddService( serviceType );
			var result = target.RemoveService( serviceType );

			Assert.That( result, Is.True );
			Assert.That( target.EnumerateServices().Count(), Is.EqualTo( 0 ) );
		}

		[Test()]
		[Description( "Depends on AddService" )]
		public void TestRemoveService_NotRegisterdNonEmpty_Fail()
		{
			var serviceType = typeof( Service );

			var target = new DefaultServiceTypeLocator();

			target.AddService( serviceType );
			var result = target.RemoveService( typeof( Service2 ) );

			Assert.That( result, Is.False );
			Assert.That( target.EnumerateServices().Count(), Is.EqualTo( 1 ) );
		}

		[Test()]
		[Description( "Depends on AddService" )]
		public void TestRemoveService_SpecifyNonServiceType_JustFail()
		{
			var serviceType = typeof( Service );

			var target = new DefaultServiceTypeLocator();

			target.AddService( serviceType );
			var result = target.RemoveService( typeof( object ) );

			Assert.That( result, Is.False );
			Assert.That( target.EnumerateServices().Count(), Is.EqualTo( 1 ) );
		}

		[Test()]
		[Description( "Depends on AddService" )]
		public void TestRemoveService_Empty_Fail()
		{
			var serviceType = typeof( Service );

			var target = new DefaultServiceTypeLocator();

			var result = target.RemoveService( serviceType );

			Assert.That( result, Is.False );
			Assert.That( target.EnumerateServices().Count(), Is.EqualTo( 0 ) );
		}

		[Test()]
		public void TestRemoveService_Null_JustFail()
		{
			Type serviceType = null;

			var target = new DefaultServiceTypeLocator();

			var result = target.RemoveService( serviceType );
			
			Assert.That( result, Is.False );
		}

		[Test()]
		public void TestClearServices_NonEmpty_Cleared()
		{
			var target = new DefaultServiceTypeLocator();

			target.AddService( typeof( Service ) );
			target.AddService( typeof( Service2 ) );

			target.ClearServices();

			Assert.That( target.EnumerateServices().Count(), Is.EqualTo( 0 ) );
		}

		[Test()]
		public void TestClearServices_Empty_Halmless()
		{
			var target = new DefaultServiceTypeLocator();

			target.ClearServices();

			Assert.That( target.EnumerateServices().Count(), Is.EqualTo( 0 ) );
		}

		[Test()]
		public void TestEnumerateServices_Initial_Empty()
		{
			var target = new DefaultServiceTypeLocator();

			Assert.That( target.EnumerateServices().Any(), Is.False );
		}

		[Test()]
		public void TestFindServices_EquiavalantToEnumerateServices()
		{
			var target = new DefaultServiceTypeLocator();
			target.AddService( typeof( Service ) );
			target.AddService( typeof( Service2 ) );

			Assert.That( target.FindServices(), Is.EqualTo( target.EnumerateServices() ) );
		}

		[MessagePackRpcServiceContract( "Svc1" )]
		private sealed class Service
		{
			[MessagePackRpcMethod]
			public void Action( string value )
			{
			}

			[MessagePackRpcMethod]
			public int Func( string value )
			{
				return 0;
			}
		}

		[MessagePackRpcServiceContract( "Svc2" )]
		private sealed class Service2
		{
		}
	}
}
