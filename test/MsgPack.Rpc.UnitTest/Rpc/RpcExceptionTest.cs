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

namespace MsgPack.Rpc
{
	[TestFixture]
	public class RpcExceptionTest : RpcExceptionTestBase<RpcException>
	{
		protected override RpcError DefaultError
		{
			get { return RpcError.RemoteRuntimeError; }
		}

		protected override System.Collections.Generic.IDictionary<string, object> GetTestArguments()
		{
			var result = base.GetTestArguments();
			SetRpcError( result, RpcError.CustomError( "Test.ApplicationError", 1 ) ); // Dummy random
			return result;
		}

		[Test]
		[SetUICulture( "en-US" )]
		public void TestGetExceptionMessage_InnerException_IsDebugMode_WithStackTrace()
		{
			TestGetExceptionMessage_InnerException_IsDebugMode_WithStackTraceCore();
		}

		private static void TestGetExceptionMessage_InnerException_IsDebugMode_WithStackTraceCore()
		{
#if MONO
			Assert.Ignore( "Mono has different timing to capture stack trace." );
#endif
			var message = Guid.NewGuid().ToString();
			var debugInformation = Guid.NewGuid().ToString();
			Exception inner = null;

			try
			{
				throw new Exception();
			}
			catch ( Exception ex )
			{
				inner = ex;
			}

			var target = new RpcException( RpcError.RemoteRuntimeError, message, debugInformation, inner );
			var result = target.GetExceptionMessage( true );
			var deserialized = new RpcException( RpcError.RemoteRuntimeError, result );

			Assert.That( deserialized.ToString(), Is.StringContaining( inner.StackTrace ) );
		}

		[Test]
		public void TestGetExceptionMessage_InnerException_IsNotDebugMode_WithoutStackTrace()
		{
			var message = Guid.NewGuid().ToString();
			var debugInformation = Guid.NewGuid().ToString();
			Exception inner = null;

			try
			{
				throw new Exception( "Inner" );
			}
			catch ( Exception ex )
			{
				inner = ex;
			}

			var target = new RpcException( RpcError.RemoteRuntimeError, message, debugInformation, inner );
			var result = target.GetExceptionMessage( false );
			var deserialized = new RpcException( RpcError.RemoteRuntimeError, result );

			Assert.That( deserialized.ToString(), Is.Not.StringContaining( inner.StackTrace ) );
		}

		[Test]
		[SetUICulture( "en-US" )]
		public void TestGetExceptionMessage_RethrownOnClinetInnerException_IsDebugMode_WithStackTrace()
		{
			TestGetExceptionMessage_RethrownOnClinetInnerException_IsDebugMode_WithStackTraceCore();
		}

		// TODO: Localization test

		private static void TestGetExceptionMessage_RethrownOnClinetInnerException_IsDebugMode_WithStackTraceCore()
		{
#if MONO
			Assert.Ignore( "Mono has different timing to capture stack trace." );
#endif
			var message = Guid.NewGuid().ToString();
			var debugInformation = Guid.NewGuid().ToString();
			Exception inner = null;

			try
			{
				throw new Exception( "Inner" );
			}
			catch ( Exception ex )
			{
				inner = ex;
			}

			var target = new RpcException( RpcError.RemoteRuntimeError, message, debugInformation, inner );
			var asMpo1 = target.GetExceptionMessage( true );
			var deserialized1 = new RpcException( RpcError.RemoteRuntimeError, asMpo1 );
			var asMpo2 = deserialized1.GetExceptionMessage( true );
			var deserialized2 = new RpcException( RpcError.RemoteRuntimeError, asMpo2 );

			Assert.That( deserialized2.ToString(), Is.StringContaining( inner.StackTrace ) );
		}
	}
}
