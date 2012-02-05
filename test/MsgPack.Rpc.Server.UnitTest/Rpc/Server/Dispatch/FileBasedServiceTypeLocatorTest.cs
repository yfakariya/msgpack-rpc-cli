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
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using NUnit.Framework;

namespace MsgPack.Rpc.Server.Dispatch
{
	[TestFixture()]
	public class FileBasedServiceTypeLocatorTest : MarshalByRefObject
	{
		public void TestFindServicesCore( string svcDir, bool useBinDir )
		{
			if ( Directory.Exists( svcDir ) )
			{
				Directory.Delete( svcDir, true );
			}

			const string template = "<% @ ServiceHost Service=\"{0}\" %>";
			Directory.CreateDirectory( svcDir );
			if ( useBinDir )
			{
				Directory.CreateDirectory( svcDir + Path.DirectorySeparatorChar + "bin" );
			}

			File.WriteAllText( Path.Combine( svcDir, "1.svc" ), String.Format( CultureInfo.InvariantCulture, template, typeof( TestService1 ).FullName ) );
			File.WriteAllText( Path.Combine( svcDir, "2.svc" ), String.Format( CultureInfo.InvariantCulture, template, typeof( TestService2 ).FullName ) );
			File.Copy(
				typeof( TestService1 ).Assembly.ManifestModule.FullyQualifiedName,
				useBinDir
				? Path.Combine( svcDir, "bin", typeof( TestService1 ).Assembly.ManifestModule.Name )
				: Path.Combine( svcDir, typeof( TestService1 ).Assembly.ManifestModule.Name )
				);
			var target = new FileBasedServiceTypeLocator();
			target.BaseDirectory = svcDir;

			var result = target.FindServices().ToArray();

			Assert.That( result.Any( item => item.ServiceType == typeof( TestService1 ) ) );
			Assert.That( result.Any( item => item.ServiceType == typeof( TestService2 ) ) );
		}

		private void TestFindServicesCoreInWorkerDomain( bool useBinDir )
		{
			var tempDir = "FileBasedServiceTypeLocatorTest";
			try
			{
				var setup = new AppDomainSetup();
				setup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
				setup.PrivateBinPath = tempDir;
				setup.ShadowCopyDirectories = tempDir;
				if ( useBinDir )
				{
					setup.PrivateBinPath += ";" + tempDir + Path.DirectorySeparatorChar + "bin";
					setup.ShadowCopyDirectories += ";" + tempDir + Path.DirectorySeparatorChar + "bin";
				}
				setup.ShadowCopyFiles = "true";
				var workerDomain = AppDomain.CreateDomain( "WorkerDomain", null, setup, new PermissionSet( PermissionState.Unrestricted ) );
				try
				{
					var proxy =
						workerDomain.CreateInstanceAndUnwrap( this.GetType().Assembly.FullName, this.GetType().FullName )
						as FileBasedServiceTypeLocatorTest;
					proxy.TestFindServicesCore( tempDir, useBinDir );
				}
				finally
				{
					AppDomain.Unload( workerDomain );
				}
			}
			finally
			{
				if ( Directory.Exists( tempDir ) )
				{
					Directory.Delete( tempDir, true );
				}
			}
		}

		[Test()]
		public void TestFindServices_FileFound_TypeFound()
		{
			TestFindServicesCoreInWorkerDomain( false );
		}

		[Test()]
		public void TestFindServices_FileFoundInBin_TypeFound()
		{
			TestFindServicesCoreInWorkerDomain( true );
		}

		[Test()]
		[ExpectedException( typeof( InvalidOperationException ) )]
		public void TestFindServices_FileFound_TypeFound_Failed()
		{
			var tempDir = ".\\FileBasedServiceTypeLocatorTest";
			try
			{
				if ( Directory.Exists( tempDir ) )
				{
					Directory.Delete( tempDir, true );
				}

				const string template = "<% @ ServiceHost Service=\"{0}\" %>";
				Directory.CreateDirectory( tempDir );
				File.WriteAllText( Path.Combine( tempDir, "1.svc" ), String.Format( CultureInfo.InvariantCulture, template, "Example.NotExist" ) );
				var target = new FileBasedServiceTypeLocator();
				target.BaseDirectory = tempDir;

				var result = target.FindServices().ToArray();
			}
			finally
			{
				if ( Directory.Exists( tempDir ) )
				{
					Directory.Delete( tempDir, true );
				}
			}
		}

		[Test()]
		[ExpectedException( typeof( InvalidOperationException ) )]
		public void TestFindServices_InvalidSvcFile_Failed()
		{
			var tempDir = ".\\FileBasedServiceTypeLocatorTest";
			try
			{
				if ( Directory.Exists( tempDir ) )
				{
					Directory.Delete( tempDir, true );
				}

				const string template = "<% @ ServiceHost sService=\"{0}\" %>";
				Directory.CreateDirectory( tempDir );
				File.WriteAllText( Path.Combine( tempDir, "1.svc" ), String.Format( CultureInfo.InvariantCulture, template, "Service1" ) );
				var target = new FileBasedServiceTypeLocator();
				target.BaseDirectory = tempDir;

				var result = target.FindServices().ToArray();
			}
			finally
			{
				if ( Directory.Exists( tempDir ) )
				{
					Directory.Delete( tempDir, true );
				}
			}
		}

		[Test()]
		public void TestFindServices_FileNotFound_TypeFound_Ignored()
		{
			var tempDir = ".\\FileBasedServiceTypeLocatorTest";
			try
			{
				if ( Directory.Exists( tempDir ) )
				{
					Directory.Delete( tempDir, true );
				}

				Directory.CreateDirectory( tempDir );
				var target = new FileBasedServiceTypeLocator();
				target.BaseDirectory = tempDir;

				var result = target.FindServices().ToArray();
				Assert.That( result.Any(), Is.False );
			}
			finally
			{
				if ( Directory.Exists( tempDir ) )
				{
					Directory.Delete( tempDir, true );
				}
			}
		}

		[Test]
		public void TestFindServices_NotSvc_Ignored()
		{
			var tempDir = ".\\FileBasedServiceTypeLocatorTest";
			try
			{
				if ( Directory.Exists( tempDir ) )
				{
					Directory.Delete( tempDir, true );
				}

				const string template = "<% @ ServiceHost Service=\"{0}\" %>";
				Directory.CreateDirectory( tempDir );
				File.WriteAllText( Path.Combine( tempDir, "1.svg" ), String.Format( CultureInfo.InvariantCulture, template, typeof( TestService1 ).FullName ) );
				var target = new FileBasedServiceTypeLocator();
				target.BaseDirectory = tempDir;

				var result = target.FindServices().ToArray();

				Assert.That( result.Any(), Is.False );
			}
			finally
			{
				if ( Directory.Exists( tempDir ) )
				{
					Directory.Delete( tempDir, true );
				}
			}
		}

		[Test]
		public void TestFindServices_BaseDirectoryIsNotExist_Ignored()
		{
			var tempDir = ".\\NotExist";
			try
			{
				if ( Directory.Exists( tempDir ) )
				{
					Directory.Delete( tempDir, true );
				}

				var target = new FileBasedServiceTypeLocator();
				target.BaseDirectory = tempDir;

				var result = target.FindServices().ToArray();

				Assert.That( result.Any(), Is.False );
			}
			finally
			{
				if ( Directory.Exists( tempDir ) )
				{
					Directory.Delete( tempDir, true );
				}
			}
		}

		[Test]
		public void TestFindServices_FromAppBase()
		{
			var file = Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "1.svc" );
			try
			{
				const string template = "<% @ ServiceHost Service=\"{0}\" %>";
				File.WriteAllText( file, String.Format( CultureInfo.InvariantCulture, template, typeof( TestService1 ).FullName ) );
				var target = new FileBasedServiceTypeLocator();

				var result = target.FindServices().ToArray();

				Assert.That( result.Any( item => item.ServiceType == typeof( TestService1 ) ) );
			}
			finally
			{
				File.Delete( file );
			}
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestBaseDirectory_ContainsInvalidChar()
		{
			new FileBasedServiceTypeLocator().BaseDirectory = "*";
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestBaseDirectory_ContainsNamedStreamSpecifier()
		{
			new FileBasedServiceTypeLocator().BaseDirectory = ".\\Test:Foo";
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestBaseDirectory_TooLong()
		{
			new FileBasedServiceTypeLocator().BaseDirectory = ".\\" + new String( 'A', 255 ); ;
		}

		[Test]
		public void TestBaseDirectory_Blanks_ResetToNull()
		{
			var target = new FileBasedServiceTypeLocator();
			foreach ( var value in new[] { String.Empty, null, " " } )
			{
				target.BaseDirectory = "A";
				target.BaseDirectory = value;
				Assert.That( target.BaseDirectory, Is.Null, "Set value:\"{0}\"", value );
			}
		}
	}

	[MessagePackRpcServiceContract( Name = "Service1" )]
	public sealed class TestService1
	{
		[MessagePackRpcMethod]
		public void Method() { }
	}

	[MessagePackRpcServiceContract( Name = "Service2" )]
	public sealed class TestService2
	{
		[MessagePackRpcMethod]
		public void Method() { }
	}

}
