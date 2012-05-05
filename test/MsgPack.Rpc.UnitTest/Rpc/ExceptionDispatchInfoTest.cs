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
using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using NUnit.Framework;

namespace MsgPack.Rpc
{
	/// <summary>
	///Tests the Exception Dispatch Info 
	/// </summary>
	[TestFixture()]
	[Serializable]
	public class ExceptionDispatchInfoTest
	{
		private static readonly bool _isTracing = false;

		[Test()]
		public void TestCreateMatroshika()
		{
			var value = new InvalidOperationException( Guid.NewGuid().ToString(), new Exception() );
			var result = ExceptionDispatchInfo.CreateMatroshika( value );
			Assert.That( result, Is.Not.Null );
			Assert.That( result, Is.TypeOf( value.GetType() ) );
			Assert.That( result.Message, Is.EqualTo( value.Message ) );
			Assert.That( result.InnerException, Is.SameAs( value ) );
		}

		[Test()]
		public void TestCaptureAndThrow_NotNull_IsTransferedWithStackTrace()
		{
			Exception value = null;
			try
			{
				CreateOuter();
			}
			catch ( Exception ex )
			{
				value = ex;
			}

			var target = ExceptionDispatchInfo.Capture( value );
			try
			{
				target.Throw();
			}
			catch ( Exception ex )
			{
				Assert.That( ex, Is.TypeOf( value.GetType() ), "Unexpected:{0}{1}", Environment.NewLine, ex );
				Assert.That( ex.Message, Is.EqualTo( "Outer" ), "Unexpected:{0}{1}", Environment.NewLine, ex );
				Assert.That( ex.GetInnerException().Message, Is.EqualTo( "Inner" ), "Unexpected:{0}{1}", Environment.NewLine, ex );
				Assert.That( ex.ToString(), Is.StringContaining( "CreateOuter" ).And.ContainsSubstring( "CreateInner" ), "Unexpected:{0}{1}", Environment.NewLine, ex );
				if ( _isTracing )
				{
					Console.WriteLine( "{1}:{0}{2}", Environment.NewLine, MethodBase.GetCurrentMethod().Name, ex );
				}
				// TODO: Watson info
			}
		}

		[MethodImpl( MethodImplOptions.NoInlining )]
		private static void CreateOuter()
		{
			try
			{
				CreateInner();
			}
			catch ( Exception ex )
			{
				throw new ApplicationException( "Outer", ex );
			}
		}

		[MethodImpl( MethodImplOptions.NoInlining )]
		private static void CreateInner()
		{
			throw new Exception( "Inner" );
		}

		[Test()]
		public void TestCaptureAndThrow_IStackTracePreservable_IsTransferedWithStackTrace()
		{
			Exception value = null;
			try
			{
				CreateOuterStackTracePreservable();
			}
			catch ( Exception ex )
			{
				value = ex;
			}

			var target = ExceptionDispatchInfo.Capture( value );
			try
			{
				target.Throw();
			}
			catch ( Exception ex )
			{
				Assert.That( ex, Is.TypeOf( value.GetType() ), "Unexpected:{0}{1}", Environment.NewLine, ex );
				Assert.That( ex.Message, Is.EqualTo( "Outer" ), "Unexpected:{0}{1}", Environment.NewLine, ex );
				Assert.That( ex.GetInnerException().Message, Is.EqualTo( "Inner" ), "Unexpected:{0}{1}", Environment.NewLine, ex );
				Assert.That( ex.ToString(), Is.StringContaining( "CreateOuter" ).And.ContainsSubstring( "CreateInner" ), "Unexpected:{0}{1}", Environment.NewLine, ex );
				if ( _isTracing )
				{
					Console.WriteLine( "{1}:{0}{2}", Environment.NewLine, MethodBase.GetCurrentMethod().Name, ex );
				}
				// TODO: Watson info
			}
		}

		[MethodImpl( MethodImplOptions.NoInlining )]
		private static void CreateOuterStackTracePreservable()
		{
			try
			{
				CreateInner();
			}
			catch ( Exception ex )
			{
				throw new RpcFaultException( RpcError.RemoteRuntimeError, "Outer", null, ex );
			}
		}

		[Test()]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestCaptureAndThrow_Null()
		{
			ExceptionDispatchInfo.Capture( null );
		}

		[Test]
		public void TestCOMException()
		{
			var value = new COMException( "Message", 12345 );
			try
			{
				ExceptionDispatchInfo.Capture( value ).Throw();
			}
			catch ( COMException ex )
			{
				Assert.That( ex, Is.Not.SameAs( value ) );
				Assert.That( ex.ErrorCode, Is.EqualTo( value.ErrorCode ) );
			}
		}

		[Test]
		public void TestSEHException()
		{
			var value = new SEHException( "Message" );
			try
			{
				ExceptionDispatchInfo.Capture( value ).Throw();
			}
			catch ( SEHException ex )
			{
				Assert.That( ex, Is.Not.SameAs( value ) );
				Assert.That( ex.ErrorCode, Is.EqualTo( value.ErrorCode ) );
			}
		}

		[Test]
		public void TestExternalException()
		{
			var value = new ExternalException( "Message", 12345 );
			try
			{
				ExceptionDispatchInfo.Capture( value ).Throw();
			}
			catch ( ExternalException ex )
			{
				Assert.That( ex, Is.Not.SameAs( value ) );
				Assert.That( ex.ErrorCode, Is.EqualTo( value.ErrorCode ) );
			}
		}

		[Test]
		public void TestSocketException()
		{
			var value = new SocketException( 12345 );
			try
			{
				ExceptionDispatchInfo.Capture( value ).Throw();
			}
			catch ( SocketException ex )
			{
				Assert.That( ex, Is.Not.SameAs( value ) );
				Assert.That( ex.ErrorCode, Is.EqualTo( value.ErrorCode ) );
			}
		}

		[Test]
		public void TestHttpListenerException()
		{
			var value = new HttpListenerException( 12345 );
			try
			{
				ExceptionDispatchInfo.Capture( value ).Throw();
			}
			catch ( HttpListenerException ex )
			{
				Assert.That( ex, Is.Not.SameAs( value ) );
				Assert.That( ex.ErrorCode, Is.EqualTo( value.ErrorCode ) );
			}
		}

		[Test]
		public void TestNetworkInformationException()
		{
			var value = new NetworkInformationException( 12345 );
			try
			{
				ExceptionDispatchInfo.Capture( value ).Throw();
			}
			catch ( NetworkInformationException ex )
			{
				Assert.That( ex, Is.Not.SameAs( value ) );
				Assert.That( ex.ErrorCode, Is.EqualTo( value.ErrorCode ) );
			}
		}

		[Test]
		public void TestWin32Exception()
		{
			var value = new Win32Exception( 12345 );
			try
			{
				ExceptionDispatchInfo.Capture( value ).Throw();
			}
			catch ( Win32Exception ex )
			{
				Assert.That( ex, Is.Not.SameAs( value ) );
				Assert.That( ex.ErrorCode, Is.EqualTo( value.ErrorCode ) );
			}
			catch ( TargetInvocationException ex )
			{
				Assert.That( AppDomain.CurrentDomain.IsFullyTrusted, Is.False, "Unexpedted:{0}{1}", Environment.NewLine, ex );
				Assert.That( ex.InnerException, Is.SameAs( value ) );
			}
		}

		[Test]
		public void TestNotAStandardException()
		{
			var value = new NotAStandardException();
			try
			{
				ExceptionDispatchInfo.Capture( value ).Throw();
			}
			catch ( TargetInvocationException ex )
			{
				Assert.That( ex, Is.Not.SameAs( value ) );
			}
		}

		private static StrongName GetStrongName( Type type )
		{
			var assemblyName = type.Assembly.GetName();
			return new StrongName( new StrongNamePublicKeyBlob( assemblyName.GetPublicKey() ), assemblyName.Name, assemblyName.Version );
		}

		private void DoTestWithPartialTrust( CrossAppDomainDelegate test )
		{
			var appDomainSetUp = new AppDomainSetup() { ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase };
			var evidence = new Evidence();
			evidence.AddHostEvidence( new Zone( SecurityZone.Internet ) );
			var permisions = SecurityManager.GetStandardSandbox( evidence );
			AppDomain workerDomain = AppDomain.CreateDomain( "PartialTrust", evidence, appDomainSetUp, permisions, GetStrongName( this.GetType() ) );
			try
			{
				workerDomain.DoCallBack( test );
			}
			finally
			{
				AppDomain.Unload( workerDomain );
			}
		}


		[Test()]
		public void TestCaptureAndThrow_NotNull_IsTransferedWithStackTrace_PartialTrust()
		{
			this.DoTestWithPartialTrust( TestCaptureAndThrow_NotNull_IsTransferedWithStackTrace );
		}

		[Test()]
		public void TestCaptureAndThrow_IStackTracePreservable_IsTransferedWithStackTrace_PartialTrust()
		{
			this.DoTestWithPartialTrust( TestCaptureAndThrow_IStackTracePreservable_IsTransferedWithStackTrace );
		}

		[Test]
		public void TestCOMException_PartialTrust()
		{
			this.DoTestWithPartialTrust( TestCOMException );
		}

		[Test]
		public void TestSEHException_PartialTrust()
		{
			this.DoTestWithPartialTrust( TestSEHException );
		}

		[Test]
		public void TestExternalException_PartialTrust()
		{
			this.DoTestWithPartialTrust( TestExternalException );
		}

		[Test]
		public void TestSocketException_PartialTrust()
		{
			this.DoTestWithPartialTrust( TestSocketException );
		}

		[Test]
		public void TestHttpListenerException_PartialTrust()
		{
			this.DoTestWithPartialTrust( TestHttpListenerException );
		}

		[Test]
		public void TestNetworkInformationException_PartialTrust()
		{
			this.DoTestWithPartialTrust( TestNetworkInformationException );
		}

		[Test]
		public void TestWin32Exception_PartialTrust()
		{
			this.DoTestWithPartialTrust( TestWin32Exception );
		}

		[Test]
		public void TestNotAStandardException_PartialTrust()
		{
			this.DoTestWithPartialTrust( TestNotAStandardException );
		}

		private sealed class ErrorCodeException : Exception
		{
			public ErrorCodeException( string message, Exception inner ) : base( message, inner ) { }

			public ErrorCodeException( int errorCode )
			{
				this.HResult = errorCode;
			}
		}

		private sealed class NotAStandardException : Exception
		{
			public NotAStandardException() { }
		}
	}
}
