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
using MsgPack.Serialization;
using NUnit.Framework;

namespace MsgPack.Rpc.Server.Dispatch
{
	[TestFixture()]
	public class OperationDescriptionTest
	{
		[Test()]
		public void TestFromServiceDescription_WithMethods_CreateForPublicAnnotatedMembers()
		{
			RpcServerConfiguration configuration = RpcServerConfiguration.Default;
			SerializationContext serializationContext = new SerializationContext();
			ServiceDescription service = ServiceDescription.FromServiceType( typeof( Service ) );

			var result = OperationDescription.FromServiceDescription( configuration, serializationContext, service ).OrderBy( item=>item.Id).ToArray();

			Assert.That( result, Is.Not.Null.And.Length.EqualTo( 2 ) );
			Assert.That( result[ 0 ].Id, Is.StringEnding( Service.ExpectedOperationId1 ) );
			Assert.That( result[ 0 ].Operation, Is.Not.Null );
			Assert.That( result[ 0 ].Service, Is.EqualTo( service ) );
			Assert.That( result[ 1 ].Id, Is.StringEnding( Service.ExpectedOperationId2 ) );
			Assert.That( result[ 1 ].Operation, Is.Not.Null );
			Assert.That( result[ 1 ].Service, Is.EqualTo( service ) );
		}

		[Test()]
		public void TestFromServiceDescription_WithOutMethods_Empty()
		{
			RpcServerConfiguration configuration = RpcServerConfiguration.Default;
			SerializationContext serializationContext = new SerializationContext();
			ServiceDescription service = ServiceDescription.FromServiceType( typeof( NoMember ) );

			var result = OperationDescription.FromServiceDescription( configuration, serializationContext, service ).ToArray();

			Assert.That( result, Is.Not.Null.And.Empty );
		}

		[Test()]
		[ExpectedException( typeof( NotSupportedException ) )]
		public void TestFromServiceDescription_Overloaded()
		{
			RpcServerConfiguration configuration = RpcServerConfiguration.Default;
			SerializationContext serializationContext = new SerializationContext();
			ServiceDescription service = ServiceDescription.FromServiceType( typeof( Overloaded ) );

			OperationDescription.FromServiceDescription( configuration, serializationContext, service ).ToArray();
		}

		[Test()]
		public void TestFromServiceDescription_NullConfiguration_SucessAtLeast()
		{
			RpcServerConfiguration configuration = null;
			SerializationContext serializationContext = new SerializationContext();
			ServiceDescription service = ServiceDescription.FromServiceType( typeof( Service ) );

			OperationDescription.FromServiceDescription( configuration, serializationContext, service ).ToArray();
		}

		[Test()]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestFromServiceDescription_ContextIsNull()
		{
			RpcServerConfiguration configuration = RpcServerConfiguration.Default;
			SerializationContext serializationContext = null;
			ServiceDescription service = ServiceDescription.FromServiceType( typeof( Overloaded ) );

			OperationDescription.FromServiceDescription( configuration, serializationContext, service ).ToArray();
		}

		[Test()]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestFromServiceDescription_ServiceIsNull()
		{
			RpcServerConfiguration configuration = RpcServerConfiguration.Default;
			SerializationContext serializationContext = new SerializationContext();
			ServiceDescription service = null;

			OperationDescription.FromServiceDescription( configuration, serializationContext, service ).ToArray();
		}

		[MessagePackRpcServiceContract( "Service" )]
		private class Service
		{
			public const string ExpectedOperationId1 = "AnotherPublicMethod";
			public const string ExpectedOperationId2 = "PublicMethod";

			[MessagePackRpcMethod]
			public void PublicMethod() { }

			[MessagePackRpcMethod]
			public int AnotherPublicMethod( string value )
			{
				return 0;
			}

			[MessagePackRpcMethod]
			internal void AssemblyMethod() { }

			[MessagePackRpcMethod]
			protected void FamilyMethod() { }

			public void NotARpcMethod() { }
		}

		[MessagePackRpcServiceContract( "NoMember" )]
		private sealed class NoMember
		{
		}

		[MessagePackRpcServiceContract( "NoRpcMember" )]
		private sealed class NoRpcMember
		{
			public void Foo() { }
		}

		[MessagePackRpcServiceContract( "Overloaded" )]
		private sealed class Overloaded
		{
			[MessagePackRpcMethod]
			public void Foo( object value ) { }

			[MessagePackRpcMethod]
			public void Foo( string value ) { }
		}
	}
}
