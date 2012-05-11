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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MsgPack.Rpc.Server.Protocols;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq.Expressions;

namespace MsgPack.Rpc.Server.Dispatch
{
	[TestFixture]
	public class VersionedOperationCatalogTest
	{
		#region -- ParseMethodDescription --

		[Test]
		public void TestParseMethodDescription_AllSpec_Ok()
		{
			string method;
			string scope;
			int? version;
			VersionedOperationCatalog.ParseMethodDescription( "M:S:1", out method, out scope, out version );

			Assert.That( method, Is.EqualTo( "M" ) );
			Assert.That( scope, Is.EqualTo( "S" ) );
			Assert.That( version, Is.EqualTo( 1 ) );
		}

		[Test]
		public void TestParseMethodDescription_ScopeIsOmitted_Ok()
		{
			string method;
			string scope;
			int? version;
			VersionedOperationCatalog.ParseMethodDescription( "M:1", out method, out scope, out version );

			Assert.That( method, Is.EqualTo( "M" ) );
			Assert.That( scope, Is.Null );
			Assert.That( version, Is.EqualTo( 1 ) );
		}

		[Test]
		public void TestParseMethodDescription_VersionIsOmitted_Ok()
		{
			string method;
			string scope;
			int? version;
			VersionedOperationCatalog.ParseMethodDescription( "M:S", out method, out scope, out version );

			Assert.That( method, Is.EqualTo( "M" ) );
			Assert.That( scope, Is.EqualTo( "S" ) );
			Assert.That( version, Is.Null );
		}

		[Test]
		public void TestParseMethodDescription_ScopeAndVersionIsOmitted_Ok()
		{
			string method;
			string scope;
			int? version;
			VersionedOperationCatalog.ParseMethodDescription( "M", out method, out scope, out version );

			Assert.That( method, Is.EqualTo( "M" ) );
			Assert.That( scope, Is.Null );
			Assert.That( version, Is.Null );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestParseMethodDescription_InvalidMethodName()
		{
			string method;
			string scope;
			int? version;
			VersionedOperationCatalog.ParseMethodDescription( "$:S:1", out method, out scope, out version );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestParseMethodDescription_InvalidScope()
		{
			string method;
			string scope;
			int? version;
			VersionedOperationCatalog.ParseMethodDescription( "M:$:1", out method, out scope, out version );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestParseMethodDescription_NumericScope()
		{
			string method;
			string scope;
			int? version;
			VersionedOperationCatalog.ParseMethodDescription( "M:1:1", out method, out scope, out version );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestParseMethodDescription_NumericMethod()
		{
			string method;
			string scope;
			int? version;
			VersionedOperationCatalog.ParseMethodDescription( "1", out method, out scope, out version );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestParseMethodDescription_InvalidVersion()
		{
			string method;
			string scope;
			int? version;
			VersionedOperationCatalog.ParseMethodDescription( "M:S:$", out method, out scope, out version );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestParseMethodDescription_NegativeVersion()
		{
			string method;
			string scope;
			int? version;
			VersionedOperationCatalog.ParseMethodDescription( "M:S:-1", out method, out scope, out version );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestParseMethodDescription_HexVersion()
		{
			string method;
			string scope;
			int? version;
			VersionedOperationCatalog.ParseMethodDescription( "M:S:-1", out method, out scope, out version );
		}

		#endregion

		#region -- Add(OperationDescription) --

		private static bool TestAddCore( OperationDescription adding, params OperationDescription[] existents )
		{
			var target = new VersionedOperationCatalog();
			foreach ( var existent in existents )
			{
				Assert.That( target.Add( existent ), "Failed to add existent '{0}'.", existent );
			}

			return target.Add( adding );
		}

		[Test]
		public void TestAdd_NotNull_DefaultScope_New_True()
		{
			Assert.That(
				TestAddCore(
					CreateOperation( "M", "", 1 )
				),
				Is.True
			);
		}

		[Test]
		public void TestAdd_NotNull_NonDefaultScope_New_True()
		{
			Assert.That(
				TestAddCore(
					CreateOperation( "M", "S", 1 )
				),
				Is.True
			);
		}

		[Test]
		public void TestAdd_NotNull_NonDefaultScope_NewTwise_True()
		{
			Assert.That(
				TestAddCore(
					CreateOperation( "M", "S1", 1 ),
					CreateOperation( "M", "S2", 1 )
				),
				Is.True
			);
		}

		[Test]
		public void TestAdd_NotNull_NonDefaultScope_AppendMethod_True()
		{
			Assert.That(
				TestAddCore(
					CreateOperation( "M1", "S", 1 ),
					CreateOperation( "M2", "S", 1 )
				),
				Is.True
			);
		}

		[Test]
		public void TestAdd_NotNull_NonDefaultScope_AppendVersion_True()
		{
			Assert.That(
				TestAddCore(
					CreateOperation( "M", "S", 1 ),
					CreateOperation( "M", "S", 2 )
				),
				Is.True
			);
		}

		[Test]
		public void TestAdd_NotNull_NonDefaultScope_SameVersion_False()
		{
			Assert.That(
				TestAddCore(
					CreateOperation( "M", "S", 1 ),
					CreateOperation( "M", "S", 1 )
				),
				Is.False
			);
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestAdd_Null()
		{
			TestAddCore( null );
		}

		#endregion

		#region -- Get(string) --

		// Depends on Add

		private static VersionedOperationCatalog GetTestTarget()
		{
			var result = new VersionedOperationCatalog();
			result.Add( CreateOperation( "Method", String.Empty, 1 ) );
			result.Add( CreateOperation( "Method", String.Empty, 2 ) );
			result.Add( CreateOperation( "Method_", String.Empty, 1 ) );
			result.Add( CreateOperation( "Method_", String.Empty, 2 ) );
			result.Add( CreateOperation( "Method", "Scope_", 1 ) );
			result.Add( CreateOperation( "Method", "Scope_", 2 ) );
			result.Add( CreateOperation( "Method_", "Scope_", 1 ) );
			result.Add( CreateOperation( "Method_", "Scope_", 2 ) );
			result.Add( CreateOperation( "Method", "Scope", 1 ) );
			result.Add( CreateOperation( "Method", "Scope", 2 ) );
			result.Add( CreateOperation( "Method_", "Scope", 1 ) );
			result.Add( CreateOperation( "Method_", "Scope", 2 ) );

			return result;
		}

		[Test]
		public void TestGet_String_Match_NotNull()
		{
			var target = GetTestTarget();
			var result = target.Get( "Method:Scope:1" );
			Assert.That( result, Is.Not.Null );
			Assert.That( result.Id, Is.EqualTo( "Method" ) );
			Assert.That( result.Service.Name, Is.EqualTo( "Scope" ) );
			Assert.That( result.Service.Version, Is.EqualTo( 1 ) );
		}

		[Test]
		public void TestGet_String_MethodDiffer_Null()
		{
			var target = GetTestTarget();
			Assert.That( target.Get( "Method__:Scope:1" ), Is.Null );
		}

		[Test]
		public void TestGet_String_ScopeDiffer_Null()
		{
			var target = GetTestTarget();
			Assert.That( target.Get( "Method:Scope__:1" ), Is.Null );
		}

		[Test]
		public void TestGet_String_ScopeOmitted_Default()
		{
			var target = GetTestTarget();
			var result = target.Get( "Method:1" );
			Assert.That( result, Is.Not.Null );
			Assert.That( result.Id, Is.EqualTo( "Method" ) );
			Assert.That( result.Service.Name, Is.EqualTo( String.Empty ) );
			Assert.That( result.Service.Version, Is.EqualTo( 1 ) );
		}

		[Test]
		public void TestGet_String_VersionDiffer_Null()
		{
			var target = GetTestTarget();
			Assert.That( target.Get( "Method:Scope:3" ), Is.Null );
		}

		[Test]
		public void TestGet_String_VersionOmitted_Latest()
		{
			var target = GetTestTarget();
			var result = target.Get( "Method:Scope" );
			Assert.That( result, Is.Not.Null );
			Assert.That( result.Id, Is.EqualTo( "Method" ) );
			Assert.That( result.Service.Name, Is.EqualTo( "Scope" ) );
			Assert.That( result.Service.Version, Is.EqualTo( 2 ) );
		}

		[Test]
		public void TestGet_String_ScopeAndVersionOmitted_DefaultLatest()
		{
			var target = GetTestTarget();
			var result = target.Get( "Method" );
			Assert.That( result, Is.Not.Null );
			Assert.That( result.Id, Is.EqualTo( "Method" ) );
			Assert.That( result.Service.Name, Is.EqualTo( String.Empty ) );
			Assert.That( result.Service.Version, Is.EqualTo( 2 ) );
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestGet_String_Null()
		{
			new VersionedOperationCatalog().Get( null );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestGet_String_Empty()
		{
			new VersionedOperationCatalog().Get( String.Empty );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestGet_String_Invalid()
		{
			new VersionedOperationCatalog().Get( "$" );
		}

		#endregion

		#region -- Get(string, string, version) --

		[Test]
		public void TestGet_String_String_Int32_Match_NotNull()
		{
			var target = GetTestTarget();
			var result = target.Get( "Method", "Scope", 1 );
			Assert.That( result, Is.Not.Null );
			Assert.That( result.Id, Is.EqualTo( "Method" ) );
			Assert.That( result.Service.Name, Is.EqualTo( "Scope" ) );
			Assert.That( result.Service.Version, Is.EqualTo( 1 ) );
		}

		[Test]
		public void TestGet_String_String_Int32_MethodDiffer_Null()
		{
			var target = GetTestTarget();
			Assert.That( target.Get( "Method__", "Scope", 1 ), Is.Null );
		}

		[Test]
		public void TestGet_String_String_Int32_ScopeDiffer_Null()
		{
			var target = GetTestTarget();
			Assert.That( target.Get( "Method", "Scope__", 1 ), Is.Null );
		}

		[Test]
		public void TestGet_String_String_Int32_ScopeOmitted_Default()
		{
			var target = GetTestTarget();
			var result = target.Get( "Method", null, 1 );
			Assert.That( result, Is.Not.Null );
			Assert.That( result.Id, Is.EqualTo( "Method" ) );
			Assert.That( result.Service.Name, Is.EqualTo( String.Empty ) );
			Assert.That( result.Service.Version, Is.EqualTo( 1 ) );
		}

		[Test]
		public void TestGet_String_String_Int32_VersionDiffer_Null()
		{
			var target = GetTestTarget();
			Assert.That( target.Get( "Method", "Scope", 3 ), Is.Null );
		}

		[Test]
		public void TestGet_String_String_Int32_VersionOmitted_Latest()
		{
			var target = GetTestTarget();
			var result = target.Get( "Method", "Scope", null );
			Assert.That( result, Is.Not.Null );
			Assert.That( result.Id, Is.EqualTo( "Method" ) );
			Assert.That( result.Service.Name, Is.EqualTo( "Scope" ) );
			Assert.That( result.Service.Version, Is.EqualTo( 2 ) );
		}

		[Test]
		public void TestGet_String_String_Int32_ScopeAndVersionOmitted_DefaultLatest()
		{
			var target = GetTestTarget();
			var result = target.Get( "Method", null, null );
			Assert.That( result, Is.Not.Null );
			Assert.That( result.Id, Is.EqualTo( "Method" ) );
			Assert.That( result.Service.Name, Is.EqualTo( String.Empty ) );
			Assert.That( result.Service.Version, Is.EqualTo( 2 ) );
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestGet_String_String_Int32_MethodIsNull()
		{
			new VersionedOperationCatalog().Get( null, null, null );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestGet_String_String_Int32_MethodIsEmpty()
		{
			new VersionedOperationCatalog().Get( String.Empty, null, null );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestGet_String_String_Int32_MethodIsInvalid()
		{
			new VersionedOperationCatalog().Get( "$", null, null );
		}

		#endregion

		#region -- Remove(OperationDescription) --

		// Depends on Add(OperationDescription), Get(string,string,int?), Contains(OperationDescription)

		[Test]
		public void TestRemove_OperationDescription_Exist_True_Removed()
		{
			var target = GetTestTarget();
			var removal = target.Get( "Method", null, null );
			Assert.That( target.Remove( removal ), Is.True );
			Assert.That( target.Contains( removal ), Is.False );
		}

		[Test]
		public void TestRemove_OperationDescription_NotExist_False()
		{
			var target = GetTestTarget();
			var removal = target.Get( "Method__", null, null );
		}

		#endregion

		#region -- Remove(string, string, int) --

		// Depends on Add(OperationDescription), Get(string,string,int?), Contains(OperationDescription)

		[Test]
		public void TestRemove_String_String_Int32_Exist_True_Removed()
		{
			var target = GetTestTarget();
			var removal = target.Get( "Method", "Scope", 1 );
			Assert.That( target.Remove( removal.Id, removal.Service.Name, removal.Service.Version ), Is.True );
			Assert.That( target.Contains( removal ), Is.False );
		}

		[Test]
		public void TestRemove_String_String_Int32_DefaultScope_True()
		{
			var target = GetTestTarget();
			Assert.That( target.Remove( "Method", String.Empty, 1 ), Is.True );
		}

		[Test]
		public void TestRemove_String_String_Int32_MethodNotMatch_False()
		{
			var target = GetTestTarget();
			Assert.That( target.Remove( "Method__", "Scope", 1 ), Is.False );
		}

		[Test]
		public void TestRemove_String_String_Int32_ScopeNotMatch_False()
		{
			var target = GetTestTarget();
			Assert.That( target.Remove( "Method", "Scope__", 1 ), Is.False );
		}

		[Test]
		public void TestRemove_String_String_Int32_VersionNotMatch_False()
		{
			var target = GetTestTarget();
			Assert.That( target.Remove( "Method", "Scope", 3 ), Is.False );
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestRemove_String_String_Int32_MethodIsNull()
		{
			new VersionedOperationCatalog().Remove( null, null, 1 );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestRemove_String_String_Int32_MethodIsEmpty()
		{
			new VersionedOperationCatalog().Remove( String.Empty, null, 1 );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestRemove_String_String_Int32_MethodIsInvalid()
		{
			new VersionedOperationCatalog().Remove( "$", null, 1 );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestRemove_String_String_Int32_ScopeIsInvalid()
		{
			new VersionedOperationCatalog().Remove( "Method", "$", 1 );
		}

		#endregion

		#region -- RemoveMethod --

		// Depends on Add(OperationDescription), Get(string,string,int?), Contains(string)

		[Test]
		public void TestRemoveMethod_Exist_True_Removed()
		{
			var target = GetTestTarget();
			var removal = target.Get( "Method", "Scope", 1 );
			Assert.That( target.RemoveMethod( removal.Id, removal.Service.Name ), Is.True );
			Assert.That( target.Contains( "Method:Scope:1" ), Is.False );
			Assert.That( target.Contains( "Method:Scope:2" ), Is.False );
		}

		[Test]
		public void TestRemoveMethod_DefaultScope_True()
		{
			var target = GetTestTarget();
			Assert.That( target.RemoveMethod( "Method", String.Empty ), Is.True );
			Assert.That( target.Contains( "Method:1" ), Is.False );
			Assert.That( target.Contains( "Method:2" ), Is.False );
		}

		[Test]
		public void TestRemoveMethod_MethodNotMatch_False()
		{
			var target = GetTestTarget();
			Assert.That( target.RemoveMethod( "Method__", "Scope" ), Is.False );
		}

		[Test]
		public void TestRemoveMethod_ScopeNotMatch_False()
		{
			var target = GetTestTarget();
			Assert.That( target.RemoveMethod( "Method", "Scope__" ), Is.False );
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestRemoveMethod_MethodIsNull()
		{
			new VersionedOperationCatalog().RemoveMethod( null, null );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestRemoveMethod_MethodIsEmpty()
		{
			new VersionedOperationCatalog().RemoveMethod( String.Empty, null );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestRemoveMethod_MethodIsInvalid()
		{
			new VersionedOperationCatalog().RemoveMethod( "$", null );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestRemoveMethod_ScopeIsInvalid()
		{
			new VersionedOperationCatalog().RemoveMethod( "Method", "$" );
		}

		#endregion

		#region -- RemoveScope --

		// Depends on Add(OperationDescription), Get(string,string,int?), Contains(string)

		[Test]
		public void TestRemoveScope_Exist_True_Removed()
		{
			var target = GetTestTarget();
			var removal = target.Get( "Method", "Scope", 1 );
			Assert.That( target.RemoveScope( removal.Service.Name ), Is.True );
			Assert.That( target.Contains( "Method:Scope:1" ), Is.False );
			Assert.That( target.Contains( "Method:Scope:2" ), Is.False );
			Assert.That( target.Contains( "Method_:Scope:1" ), Is.False );
			Assert.That( target.Contains( "Method_:Scope:2" ), Is.False );
		}

		[Test]
		public void TestRemoveScope_DefaultScope_True()
		{
			var target = GetTestTarget();
			Assert.That( target.RemoveScope( String.Empty ), Is.True );
			Assert.That( target.Contains( "Method:1" ), Is.False );
			Assert.That( target.Contains( "Method:2" ), Is.False );
			Assert.That( target.Contains( "Method_:1" ), Is.False );
			Assert.That( target.Contains( "Method_:2" ), Is.False );
		}

		[Test]
		public void TestRemoveScope_ScopeNotMatch_False()
		{
			var target = GetTestTarget();
			Assert.That( target.RemoveScope( "Scope__" ), Is.False );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestRemoveScope_ScopeIsInvalid()
		{
			new VersionedOperationCatalog().RemoveScope( "$" );
		}

		#endregion

		#region -- Clear --

		// Depends on Add(OperationDescription), Get(string,string,int?), Contains(string)

		[Test]
		public void TestClear_NotEmpty_ToBeEmpty()
		{
			var target = new VersionedOperationCatalog();
			target.Add( CreateOperation( "M", "S", 1 ) );
			target.Clear();
			Assert.That( target.Contains( "M:S:1" ), Is.False );
		}

		[Test]
		public void TestClear_Empty_Harmless()
		{
			new VersionedOperationCatalog().Clear();
		}

		#endregion

		#region -- GetMethods --

		// Depends on Add(OperationDescription), Get(string,string,int?)

		[Test]
		public void TestGetMethods_Exist_ReturnsAll()
		{
			var target = GetTestTarget();
			var result = target.GetMethods( "Scope" ).ToDictionary( item => item.Key, item => item.Value );
			Assert.That( result.Count, Is.EqualTo( 2 ) );
			Assert.That( result.Sum( m => m.Value.Count() ), Is.EqualTo( 4 ) );
			Assert.That( result[ "Method" ].Contains( target.Get( "Method", "Scope", 1 ) ) );
			Assert.That( result[ "Method" ].Contains( target.Get( "Method", "Scope", 2 ) ) );
			Assert.That( result[ "Method_" ].Contains( target.Get( "Method_", "Scope", 1 ) ) );
			Assert.That( result[ "Method_" ].Contains( target.Get( "Method_", "Scope", 2 ) ) );
		}

		[Test]
		public void TestGetMethods_NotExist_Empty()
		{
			var target = GetTestTarget();
			var result = target.GetMethods( "Scope__" ).ToArray();
			Assert.That( result, Is.Empty );
		}

		#endregion

		#region -- GetVersions --

		// Depends on Add(OperationDescription)

		[Test]
		public void TestGetVersions_Exist_ReturnsAll()
		{
			var target = GetTestTarget();
			var result = target.GetVersions( "Method", "Scope" ).ToArray();
			Assert.That( result.Length, Is.EqualTo( 2 ) );
			Assert.That( result.Contains( target.Get( "Method", "Scope", 1 ) ) );
			Assert.That( result.Contains( target.Get( "Method", "Scope", 2 ) ) );
		}

		[Test]
		public void TestGetVersions_NotExist_Empty()
		{
			var target = GetTestTarget();
			var result = target.GetVersions( "Method", "Scope__" ).ToArray();
			Assert.That( result, Is.Empty );
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestGetVersions_Null()
		{
			new VersionedOperationCatalog().GetVersions( null, null ).ToArray();
		}

		#endregion

		#region -- GetEnumerator --

		// Depends on Add(OperationDescription)

		[Test]
		public void TestGetEnumerator_Exist_ReturnsAll()
		{
			var target = GetTestTarget();
			var result = target.ToArray();
			Assert.That( result.Length, Is.EqualTo( 12 ) );
		}

		[Test]
		public void TestGetEnumerator_Empty_Harmless()
		{
			Assert.That( !new VersionedOperationCatalog().Any() );
		}

		#endregion

		private static readonly Func<ServiceDescription, string, Func<ServerRequestContext, ServerResponseContext, Task>, OperationDescription> _operationDescriptionConstructor =
			GetOperationDescriptionConstructor();

		private static Func<ServiceDescription, string, Func<ServerRequestContext, ServerResponseContext, Task>, OperationDescription> GetOperationDescriptionConstructor()
		{
			var serviceDescription = Expression.Parameter( typeof( ServiceDescription ) );
			var id = Expression.Parameter( typeof( string ) );
			var operation = Expression.Parameter( typeof( Func<ServerRequestContext, ServerResponseContext, Task> ) );

			return
				Expression.Lambda<Func<ServiceDescription, string, Func<ServerRequestContext, ServerResponseContext, Task>, OperationDescription>>(
					Expression.New(
						typeof( OperationDescription ).GetConstructors( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ).Single(),
						serviceDescription,
						id,
						operation
					),
						serviceDescription,
						id,
						operation
				).Compile();
		}

		private static OperationDescription CreateOperation( string methodName, string scope, int version )
		{
			var serviceDescription = new ServiceDescription( scope, () => null );
			serviceDescription.Version = version;
			var id = methodName;
			Func<ServerRequestContext, ServerResponseContext, Task> operation = ( _0, _1 ) => null;
			return _operationDescriptionConstructor( serviceDescription, id, operation );
		}
	}
}
