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
using NUnit.Framework;

namespace MsgPack.Rpc.Server.Dispatch
{
	/// <summary>
	///Tests the Invocation Helper 
	/// </summary>
	[TestFixture()]
	public class InvocationHelperTest
	{
		[Test]
		public void TestFields()
		{
			Assert.That( InvocationHelper.HandleArgumentDeserializationExceptionMethod, Is.Not.Null );
		}

		[Test]
		public void TestTrace_SuccessAtLeast()
		{
			var oldLevel = MsgPackRpcServerDispatchTrace.Source.Switch.Level;
			try
			{
				MsgPackRpcServerDispatchTrace.Source.Switch.Level = System.Diagnostics.SourceLevels.All;
				InvocationHelper.TraceInvocationResult<object>( 1, Rpc.Protocols.MessageType.Request, 1, "TracingTest", RpcErrorMessage.Success, null );
				InvocationHelper.TraceInvocationResult<object>( 1, Rpc.Protocols.MessageType.Request, 1, "TracingTest", new RpcErrorMessage( RpcError.RemoteRuntimeError, "Description", "DebugInformation" ), null );
			}
			finally
			{
				MsgPackRpcServerDispatchTrace.Source.Switch.Level = oldLevel;
			}
		}

		[Test()]
		public void TestHandleArgumentDeserializationException_NotNull_IsDebugMode_IncludesFullExceptionInfo()
		{
			var exception = new Exception( Guid.NewGuid().ToString() );
			string parameterName = Guid.NewGuid().ToString();

			var result = InvocationHelper.HandleArgumentDeserializationException( exception, parameterName, true );

			Assert.That( result.IsSuccess, Is.False );
			Assert.That( result.Error, Is.EqualTo( RpcError.ArgumentError ) );
			Assert.That( result.Detail.AsDictionary()[ RpcException.MessageKeyUtf8 ].AsString(), Is.StringContaining( parameterName ) );
			Assert.That( result.Detail.AsDictionary()[ RpcException.DebugInformationKeyUtf8 ].AsString(), Is.StringContaining( exception.Message ) );
		}

		[Test()]
		public void TestHandleArgumentDeserializationException_NotNull_IsNotDebugMode_DoesNotIncludeFullExceptionInfo()
		{
			var exception = new Exception( Guid.NewGuid().ToString() );
			string parameterName = Guid.NewGuid().ToString();

			var result = InvocationHelper.HandleArgumentDeserializationException( exception, parameterName, false );

			Assert.That( result.IsSuccess, Is.False );
			Assert.That( result.Error, Is.EqualTo( RpcError.ArgumentError ) );
			Assert.That( result.Detail.AsDictionary()[ RpcException.MessageKeyUtf8 ].AsString(), Is.StringContaining( parameterName ) );
			Assert.That( result.Detail.AsDictionary().ContainsKey( RpcException.DebugInformationKeyUtf8 ), Is.False );
		}

		[Test()]
		public void TestHandleArgumentDeserializationException_Null_IsDebugMode_DefaultString()
		{
			var result = InvocationHelper.HandleArgumentDeserializationException( null, null, true );

			Assert.That( result.IsSuccess, Is.False );
			Assert.That( result.Error, Is.EqualTo( RpcError.ArgumentError ) );
			Assert.That( result.Detail.AsDictionary()[ RpcException.MessageKeyUtf8 ].AsString(), Is.Not.Null.And.Not.Empty );
			Assert.That( result.Detail.AsDictionary().ContainsKey( RpcException.DebugInformationKeyUtf8 ), Is.False );
		}

		[Test()]
		public void TestHandleArgumentDeserializationException_Null_IsNotDebugMode_DefaultString()
		{
			var result = InvocationHelper.HandleArgumentDeserializationException( null, null, false );

			Assert.That( result.IsSuccess, Is.False );
			Assert.That( result.Error, Is.EqualTo( RpcError.ArgumentError ) );
			Assert.That( result.Detail.AsDictionary()[ RpcException.MessageKeyUtf8 ].AsString(), Is.Not.Null.And.Not.Empty );
			Assert.That( result.Detail.AsDictionary().ContainsKey( RpcException.DebugInformationKeyUtf8 ), Is.False );
		}

		[Test()]
		public void TestHandleInvocationException_NotNull_ArgumentException_IsDebugMode_AsArgumentError()
		{
			var exception = new ArgumentException( Guid.NewGuid().ToString(), Guid.NewGuid().ToString() );

			var result = InvocationHelper.HandleInvocationException( exception, "Method", true );

			Assert.That( result.Error, Is.EqualTo( RpcError.ArgumentError ) );
			Assert.That( result.Detail.AsDictionary()[ RpcException.MessageKeyUtf8 ].AsString(), Is.StringContaining( exception.ParamName ) );
			Assert.That( result.Detail.AsDictionary()[ RpcException.DebugInformationKeyUtf8 ].AsString(), Is.StringContaining( exception.Message ) );
		}

		[Test()]
		public void TestHandleInvocationException_NotNull_ArgumentException_IsNotDebugMode_AsArgumentErrorWithoutDebugInformation()
		{
			var exception = new ArgumentException( Guid.NewGuid().ToString(), Guid.NewGuid().ToString() );

			var result = InvocationHelper.HandleInvocationException( exception, "Method", false );

			Assert.That( result.Error, Is.EqualTo( RpcError.ArgumentError ) );
			Assert.That( result.Detail.AsDictionary()[ RpcException.MessageKeyUtf8 ].AsString(), Is.StringContaining( exception.ParamName ) );
			Assert.That( result.Detail.AsDictionary().ContainsKey( RpcException.DebugInformationKeyUtf8 ), Is.False );
		}

		[Test()]
		public void TestHandleInvocationException_NotNull_RpcException_IsDebugMode_AsCorrespondingError()
		{
			var exception = new RpcException( RpcError.MessageTooLargeError, Guid.NewGuid().ToString(), Guid.NewGuid().ToString() );

			var result = InvocationHelper.HandleInvocationException( exception, "Method", true );

			Assert.That( result.Error, Is.EqualTo( exception.RpcError ) );
			Assert.That( result.Detail.AsDictionary()[ RpcException.MessageKeyUtf8 ].AsString(), Is.StringContaining( exception.Message ) );
			Assert.That( result.Detail.AsDictionary()[ RpcException.DebugInformationKeyUtf8 ].AsString(), Is.StringContaining( exception.DebugInformation ) );
		}

		[Test()]
		public void TestHandleInvocationException_NotNull_RpcException_IsDebugMode_AsCorrespondingErrorWithoutDebugInformation()
		{
			var exception = new RpcException( RpcError.MessageTooLargeError, Guid.NewGuid().ToString(), Guid.NewGuid().ToString() );

			var result = InvocationHelper.HandleInvocationException( exception, "Method", false );

			Assert.That( result.Error, Is.EqualTo( exception.RpcError ) );
			Assert.That( result.Detail.AsDictionary()[ RpcException.MessageKeyUtf8 ].AsString(), Is.Not.Null.And.No.Empty.And.Not.StringContaining( exception.Message ).And.Not.StringContaining( exception.DebugInformation ) );
			Assert.That( result.Detail.AsDictionary().ContainsKey( RpcException.DebugInformationKeyUtf8 ), Is.False );
		}

		[Test()]
		public void TestHandleInvocationException_NotNull_OtherException_IsDebugMode_AsRemoteRuntimeError()
		{
			var exception = new Exception( Guid.NewGuid().ToString() );

			var result = InvocationHelper.HandleInvocationException( exception, "Method", true );

			Assert.That( result.Error, Is.EqualTo( RpcError.CallError ) );
			Assert.That( result.Detail.AsDictionary()[ RpcException.MessageKeyUtf8 ].AsString(), Is.Not.Null.And.Not.Empty.And.StringContaining( exception.Message ) );
			Assert.That( result.Detail.AsDictionary()[ RpcException.DebugInformationKeyUtf8 ].AsString(), Is.StringContaining( exception.Message ) );
		}

		[Test()]
		public void TestHandleInvocationException_NotNull_OtherException_IsNotDebugMode_AsRemoteRuntimeError()
		{
			var exception = new Exception( Guid.NewGuid().ToString() );

			var result = InvocationHelper.HandleInvocationException( exception, "Method", false );

			Assert.That( result.Error, Is.EqualTo( RpcError.CallError ) );
			Assert.That( result.Detail.AsDictionary()[ RpcException.MessageKeyUtf8 ].AsString(), Is.Not.Null.And.Not.Empty.And.No.StringContaining( exception.Message ) );
			Assert.That( result.Detail.AsDictionary().ContainsKey( RpcException.DebugInformationKeyUtf8 ), Is.False );
		}

		[Test()]
		public void TestHandleInvocationException_Null_IsDebugMode_AsRemoteRuntimeError()
		{
			var result = InvocationHelper.HandleInvocationException( null, "Method", false );

			Assert.That( result.Error, Is.EqualTo( RpcError.CallError ) );
			Assert.That( result.Detail.AsDictionary()[ RpcException.MessageKeyUtf8 ].AsString(), Is.Not.Null.And.Not.Empty );
			Assert.That( result.Detail.AsDictionary().ContainsKey( RpcException.DebugInformationKeyUtf8 ), Is.False );
		}

		[Test()]
		public void TestHandleInvocationException_Null_IsNotDebugMode_AsRemoteRuntimeError()
		{
			var result = InvocationHelper.HandleInvocationException( null, "Method", false );

			Assert.That( result.Error, Is.EqualTo( RpcError.CallError ) );
			Assert.That( result.Detail.AsDictionary()[ RpcException.MessageKeyUtf8 ].AsString(), Is.Not.Null.And.Not.Empty );
			Assert.That( result.Detail.AsDictionary().ContainsKey( RpcException.DebugInformationKeyUtf8 ), Is.False );
		}
	}
}
