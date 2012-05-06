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
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace MsgPack.Rpc.Server.Dispatch
{
	/// <summary>
	///Tests the Service Description 
	/// </summary>
	[TestFixture()]
	public partial class ServiceDescriptionTest
	{
		[Test()]
		public void TestConstructor_Normal_PropertiesAreAllSet()
		{
			var name = "Name" + Guid.NewGuid().ToString( "N" );
			Func<Object> initializer = () => new object();
			ServiceDescription target = new ServiceDescription( name, initializer );

			Assert.That( target.Initializer, Is.EqualTo( initializer ) );
			Assert.That( target.Name, Is.EqualTo( name ) );
			Assert.That( target.ServiceType, Is.EqualTo( initializer.Method.DeclaringType ) );
			Assert.That( target.Version, Is.EqualTo( 0 ) );
		}

		[Test()]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestConstructor_NameIsNull()
		{
			string name = null;
			Func<Object> initializer = () => new object();
			ServiceDescription target = new ServiceDescription( name, initializer );
		}

		[Test()]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestConstructor_NameIsEmpty()
		{
			string name = String.Empty;
			Func<Object> initializer = () => new object();
			ServiceDescription target = new ServiceDescription( name, initializer );
		}

		[Test()]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestConstructor_NameIsBlank()
		{
			string name = " ";
			Func<Object> initializer = () => new object();
			ServiceDescription target = new ServiceDescription( name, initializer );
		}

		[Test()]
		public void TestConstructor_NameContainsNonAsciiLetter()
		{
			string name = "\u30C6\u30B9\u30C8";
			Func<Object> initializer = () => new object();
			ServiceDescription target = new ServiceDescription( name, initializer );

			Assert.That( target.Name, Is.EqualTo( name ) );
		}

		[Test()]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestConstructor_NameContainsSymbol()
		{
			// It might be natural in some environemnt but they should not be used.
			// We should follow UAX-31 to maximize interoperability.
			string name = "Test?";
			Func<Object> initializer = () => new object();
			ServiceDescription target = new ServiceDescription( name, initializer );
		}

		[Test()]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestConstructor_InitializerIsNull()
		{
			var name = "Name" + Guid.NewGuid().ToString( "N" );
			Func<Object> initializer = null;
			ServiceDescription target = new ServiceDescription( name, initializer );
		}

		[Test()]
		public void TestFromServiceType_Normal_PropertiesSetAsAttributeValue()
		{
			Type serviceType = typeof( ServiceWithMembers );

			var result = ServiceDescription.FromServiceType( serviceType );

			Assert.That( result.Initializer, Is.Not.Null );
			Assert.That( result.Name, Is.EqualTo( ServiceWithMembers.Name ) );
			Assert.That( result.ServiceType, Is.EqualTo( serviceType ) );
			Assert.That( result.Version, Is.EqualTo( 0 ) );

			Assert.That( result.Initializer(), Is.Not.Null.And.TypeOf( serviceType ) );
		}

		[Test()]
		public void TestFromServiceType_NoMethods_PropertiesSetAsAttributeValue()
		{
			Type serviceType = typeof( ServiceWithoutMembers );

			var result = ServiceDescription.FromServiceType( serviceType );

			Assert.That( result.Initializer, Is.Not.Null );
			Assert.That( result.Name, Is.EqualTo( ServiceWithoutMembers.Name ) );
			Assert.That( result.ServiceType, Is.EqualTo( serviceType ) );
			Assert.That( result.Version, Is.EqualTo( 0 ) );

			Assert.That( result.Initializer(), Is.Not.Null.And.TypeOf( serviceType ) );
		}

		[Test()]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestFromServiceType_Null()
		{
			Type serviceType = null;

			ServiceDescription.FromServiceType( serviceType );
		}

		[Test()]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestFromServiceType_WithoutMessagePackRpcServiceContract()
		{
			Type serviceType = typeof( ServiceWithoutServiceContract );

			ServiceDescription.FromServiceType( serviceType );
		}

		[Test()]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestFromServiceType_ServiceWithoutDefaultPublicConstructor()
		{
			Type serviceType = typeof( ServiceWithoutDefaultPublicConstructor );

			ServiceDescription.FromServiceType( serviceType );
		}

		[Test()]
		public void TestGetHashCode_VersionIsNotSet_Harmless()
		{

			var name = "Name" + Guid.NewGuid().ToString( "N" );
			Func<Object> initializer = () => new object();
			ServiceDescription target = new ServiceDescription( name, initializer );

			// Test it does not throw any exceptions.
			target.GetHashCode();
		}

		[Test()]
		public void TestGetHashCode_VersionIsSet_Harmless()
		{

			var name = "Name" + Guid.NewGuid().ToString( "N" );
			Func<Object> initializer = () => new object();
			ServiceDescription target =
				new ServiceDescription( name, initializer )
				{
					Version = 123
				};

			// Test it does not throw any exceptions.
			target.GetHashCode();
		}

		[Test()]
		public void TestToString_VersionIsSet_Appeared()
		{
			var name = "Name" + Guid.NewGuid().ToString( "N" );
			Func<Object> initializer = () => new object();
			var version = DateTime.UtcNow.Millisecond;
			ServiceDescription target =
				new ServiceDescription( name, initializer )
				{
					Version = version
				};

			var result = target.ToString();

			Assert.That( Regex.Matches( result, Regex.Escape( name ) ), Is.Not.Null.And.Count.EqualTo( 1 ) );
			Assert.That( Regex.Matches( result, Regex.Escape( ":" + version ) ), Is.Not.Null.And.Count.EqualTo( 1 ) );
		}

		[Test()]
		public void TestToString_VersionIsNotSet_Omitted()
		{
			var name = "Name" + Guid.NewGuid().ToString( "N" );
			Func<Object> initializer = () => new object();
			ServiceDescription target = new ServiceDescription( name, initializer );

			var result = target.ToString();

			Assert.That( Regex.Matches( result, Regex.Escape( name ) ), Is.Not.Null.And.Count.EqualTo( 1 ) );
		}


		private sealed class ServiceWithoutServiceContract
		{
		}

		[MessagePackRpcServiceContract( Name = "Name" )]
		private sealed class ServiceWithoutDefaultPublicConstructor
		{
			private ServiceWithoutDefaultPublicConstructor() { }
		}

		[MessagePackRpcServiceContract( Name = ServiceWithoutMembers.Name )]
		private sealed class ServiceWithoutMembers
		{
			public const string Name = "Name2";
		}

		[MessagePackRpcServiceContract( Name = ServiceWithMembers.Name )]
		private sealed class ServiceWithMembers
		{
			public const string Name = "Name3";

			[MessagePackRpcMethod]
			public void Foo() { }
		}
	}
}
