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
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc
{
	/// <summary>
	///Tests the Rpc Error 
	/// </summary>
	[TestFixture()]
	public class RpcErrorTest
	{
		/// <summary>
		/// Tests the To Exception 
		/// </summary>
		[Test()]
		public void TestKnownErrors_Properties_ToException_ToString_GetHashCode_Success()
		{
			foreach ( var prop in typeof( RpcError ).GetProperties( BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic ).Where( item => item.PropertyType == typeof( RpcError ) ) )
			{
				var target = prop.GetValue( null, null ) as RpcError;

				Assert.That( target, Is.Not.Null, prop.Name );
				Assert.That( target.DefaultMessage, Is.Not.Null.And.Not.Empty, prop.Name );
				Assert.That( target.DefaultMessageInvariant, Is.Not.Null.And.Not.Empty, prop.Name );
				Assert.That( target.Identifier, Is.Not.Null.And.Not.Empty, prop.Name );
				Assert.That( target.GetHashCode(), Is.EqualTo( target.ErrorCode ), prop.Name );
				Assert.That(
					target.ToString(),
					Is.Not.Null
					.And.StringContaining( target.Identifier )
					.And.StringContaining( target.ErrorCode.ToString( CultureInfo.CurrentCulture ) )
					.And.StringContaining( target.DefaultMessage ),
					prop.Name
				);
				var message = Guid.NewGuid().ToString();
				var debugInformation = Guid.NewGuid().ToString();
				var detail =
					new MessagePackObject(
						new MessagePackObjectDictionary()
						{
							{ RpcException.MessageKeyUtf8, message },
							{ RpcException.DebugInformationKeyUtf8, debugInformation },
							{ RpcArgumentException.ParameterNameKeyUtf8, "test" },
							{ RpcMethodInvocationException.MethodNameKeyUtf8, "Test" },
							{ RpcTimeoutException.ClientTimeoutKeyUtf8, TimeSpan.FromSeconds( 15 ).Ticks }
						}
					);
				var exception = target.ToException( detail );

				Assert.That( exception, Is.Not.Null, prop.Name );
				Assert.That( exception.DebugInformation, Is.StringContaining( debugInformation ), prop.Name );
				Assert.That( exception.Message, Is.StringContaining( message ), prop.Name );
				Assert.That( exception.RpcError, Is.EqualTo( target ), prop.Name );
			}
		}

		[Test()]
		public void TestEquals_SameObject_True()
		{
			var target = RpcError.NoMethodError;
			Assert.That( target.Equals( target ), Is.True );
#pragma warning disable 1718
			Assert.That( target == target, Is.True );
#pragma warning restore 1718
		}

		[Test()]
		public void TestEquals_SameId_SameCode_True()
		{
			var left = RpcError.CustomError( "ID", 1 );
			var right = RpcError.CustomError( "ID", 1 );
			Assert.That( left, Is.Not.SameAs( right ), "Precondition" );
			Assert.That( left.Equals( right ), Is.True );
			Assert.That( right.Equals( left ), Is.True );
			Assert.That( left == right, Is.True );
			Assert.That( right == left, Is.True );
			Assert.That( left != right, Is.False );
			Assert.That( right != left, Is.False );
		}

		[Test()]
		public void TestEquals_SameId_DifferCode_False()
		{
			var left = RpcError.CustomError( "ID", 1 );
			var right = RpcError.CustomError( "ID", 2 );
			Assert.That( left.Equals( right ), Is.False );
			Assert.That( right.Equals( left ), Is.False );
			Assert.That( left == right, Is.False );
			Assert.That( right == left, Is.False );
			Assert.That( left != right, Is.True );
			Assert.That( right != left, Is.True );
		}

		[Test()]
		public void TestEquals_DifferId_SameCode_False()
		{
			var left = RpcError.CustomError( "ID", 1 );
			var right = RpcError.CustomError( "ID0", 1 );
			Assert.That( left.Equals( right ), Is.False );
			Assert.That( right.Equals( left ), Is.False );
			Assert.That( left == right, Is.False );
			Assert.That( right == left, Is.False );
			Assert.That( left != right, Is.True );
			Assert.That( right != left, Is.True );
		}

		[Test()]
		public void TestEquals_DifferId_DifferCode_False()
		{
			var left = RpcError.CustomError( "ID", 1 );
			var right = RpcError.CustomError( "ID0", 2 );
			Assert.That( left.Equals( right ), Is.False );
			Assert.That( right.Equals( left ), Is.False );
			Assert.That( left == right, Is.False );
			Assert.That( right == left, Is.False );
			Assert.That( left != right, Is.True );
			Assert.That( right != left, Is.True );
		}

		[Test()]
		public void TestEquals_Null_False()
		{
			var left = RpcError.CustomError( "ID", 1 );
			RpcError right = null;
			Assert.That( left.Equals( right ), Is.False );
			Assert.That( left == right, Is.False );
			Assert.That( right == left, Is.False );
			Assert.That( left != right, Is.True );
			Assert.That( right != left, Is.True );
		}

		[Test()]
		public void TestEquals_NotRpcError_False()
		{
			var left = RpcError.CustomError( "ID", 1 );
			var right = "ID";
			Assert.That( left.Equals( right ), Is.False );
		}

		[Test()]
		public void TestCustomError_NonNullId_PositiveErrorCode_AsIs()
		{
			var identifier = Guid.NewGuid().ToString();
			var errorCode = Math.Abs( Environment.TickCount );
			var target = RpcError.CustomError( identifier, errorCode );
			Assert.That( target, Is.Not.Null );
			Assert.That( target.Identifier, Is.EqualTo( identifier ) );
			Assert.That( target.ErrorCode, Is.EqualTo( errorCode ) );
			Assert.That( target.DefaultMessage, Is.Not.Null.And.Not.Empty );
			Assert.That( target.DefaultMessageInvariant, Is.Not.Null.And.Not.Empty );
		}

		[Test()]
		public void TestCustomError_NonNullId_Int32MaxValueErrorCode_AsIs()
		{
			var identifier = Guid.NewGuid().ToString();
			var errorCode = Int32.MaxValue;
			var target = RpcError.CustomError( identifier, errorCode );
			Assert.That( target, Is.Not.Null );
			Assert.That( target.Identifier, Is.EqualTo( identifier ) );
			Assert.That( target.ErrorCode, Is.EqualTo( errorCode ) );
			Assert.That( target.DefaultMessage, Is.Not.Null.And.Not.Empty );
			Assert.That( target.DefaultMessageInvariant, Is.Not.Null.And.Not.Empty );
		}

		[Test()]
		public void TestCustomError_NonNullId_ZeroErrorCode_AsIs()
		{
			var identifier = Guid.NewGuid().ToString();
			var errorCode = 0;
			var target = RpcError.CustomError( identifier, errorCode );
			Assert.That( target, Is.Not.Null );
			Assert.That( target.Identifier, Is.EqualTo( identifier ) );
			Assert.That( target.ErrorCode, Is.EqualTo( errorCode ) );
			Assert.That( target.DefaultMessage, Is.Not.Null.And.Not.Empty );
			Assert.That( target.DefaultMessageInvariant, Is.Not.Null.And.Not.Empty );
		}

		[Test()]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestCustomError_NullId()
		{
			var target = RpcError.CustomError( null, 0 );
		}

		[Test()]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestCustomError_EmptyId()
		{
			var target = RpcError.CustomError( String.Empty, 0 );
		}

		[Test()]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestCustomError_WhiteSpaceId()
		{
			var target = RpcError.CustomError( " ", 0 );
		}

		[Test()]
		[ExpectedException( typeof( ArgumentOutOfRangeException ) )]
		public void TestCustomError_NoNullId_UnasignedNegative()
		{
			var target = RpcError.CustomError( "A", -1 );
		}

		[Test()]
		public void TestFromIdentifier_Known_SameAsKnown()
		{
			foreach ( var prop in typeof( RpcError ).GetProperties( BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic ).Where( item => item.PropertyType == typeof( RpcError ) ) )
			{
				var target = prop.GetValue( null, null ) as RpcError;
				var viaIdentifier = RpcError.FromIdentifier( target.Identifier, null );
				Assert.That( viaIdentifier, Is.SameAs( target ) );
				var viaErrorCode = RpcError.FromIdentifier( null, target.ErrorCode );
				Assert.That( viaErrorCode, Is.SameAs( target ) );
			}
		}

		[Test()]
		public void TestFromIdentifier_Unknown_AsCustom()
		{
			foreach ( var prop in typeof( RpcError ).GetProperties( BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic ).Where( item => item.PropertyType == typeof( RpcError ) ) )
			{
				var target = prop.GetValue( null, null ) as RpcError;
				var viaIdentifier = RpcError.FromIdentifier( target.Identifier, target.ErrorCode * -1 );
				Assert.That( viaIdentifier, Is.Not.Null );
				Assert.That( viaIdentifier, Is.SameAs( target ) );

				var viaErrorCode = RpcError.FromIdentifier( target.Identifier + "A", target.ErrorCode );
				Assert.That( viaErrorCode, Is.Not.Null );
				Assert.That( viaErrorCode, Is.SameAs( target ) );

				var custom = RpcError.FromIdentifier( target.Identifier + "A", Math.Abs( target.ErrorCode ) % 1000 );
				Assert.That( custom, Is.Not.Null );
				Assert.That( custom, Is.Not.SameAs( target ) );
				Assert.That( custom.Identifier, Is.EqualTo( target.Identifier + "A" ) );
				Assert.That( custom.ErrorCode, Is.EqualTo( Math.Abs( target.ErrorCode ) % 1000 ) );
			}
		}
	}
}
