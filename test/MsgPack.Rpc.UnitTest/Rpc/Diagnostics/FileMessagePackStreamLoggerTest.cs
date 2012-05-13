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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using NUnit.Framework;

namespace MsgPack.Rpc.Diagnostics
{
	[TestFixture]
	public class FileMessagePackStreamLoggerTest
	{
		private readonly IPEndPoint _localhost = new IPEndPoint( IPAddress.Loopback, 0 );

		[Test]
		public void TestWrite_DirectoryExists_WrittenToFile()
		{
			using ( var tempDirectory = TempDirectory.Create() )
			using ( var logger = new FileMessagePackStreamLogger( tempDirectory.Path ) )
			{
				var now = DateTimeOffset.Now;
				Directory.CreateDirectory( logger.DirectoryPath );
				logger.Write( now, _localhost, new byte[] { 1, 2, 3 } );

				var file = AssertFilePath( logger.DirectoryPath, now, _localhost.Address.ToString() );
				Assert.That( File.ReadAllBytes( file ), Is.EqualTo( new byte[] { 1, 2, 3 } ) );
			}
		}

		[Test]
		public void TestWrite_DirectoryDoesNotExist_WrittenToFile()
		{
			using ( var tempDirectory = TempDirectory.Create() )
			using ( var logger = new FileMessagePackStreamLogger( tempDirectory.Path ) )
			{
				var now = DateTimeOffset.Now;
				Assert.That( Directory.Exists( logger.DirectoryPath ), Is.False );
				logger.Write( now, _localhost, new byte[] { 1, 2, 3 } );

				var file = AssertFilePath( logger.DirectoryPath, now, _localhost.Address.ToString() );
				Assert.That( File.ReadAllBytes( file ), Is.EqualTo( new byte[] { 1, 2, 3 } ) );
			}
		}

		[Test]
		public void TestWrite_NotADefaultAppDomain_PathContainsFriendlyName()
		{
			using ( var tempDirectory = TempDirectory.Create() )
			{
				var workderDomain =
					AppDomain.CreateDomain(
						Guid.NewGuid().ToString(),
						null,
						new AppDomainSetup()
						{
							ApplicationBase = AppDomain.CurrentDomain.BaseDirectory
						}
					);
				try
				{
					var proxy = workderDomain.CreateInstanceAndUnwrap( typeof( FileMessagePackStreamLoggerTester ).Assembly.FullName, typeof( FileMessagePackStreamLoggerTester ).FullName ) as FileMessagePackStreamLoggerTester;
					proxy.Test( tempDirectory.Path );
				}
				finally
				{
					AppDomain.Unload( workderDomain );
				}
			}
		}

		[Test]
		public void TestWrite_DnsEndPoint_HostNameIsUsed()
		{
			var dnsEndPoint = new DnsEndPoint( "localhost", 0 );
			using ( var tempDirectory = TempDirectory.Create() )
			using ( var logger = new FileMessagePackStreamLogger( tempDirectory.Path ) )
			{
				var now = DateTimeOffset.Now;
				Directory.CreateDirectory( logger.DirectoryPath );
				logger.Write( now, dnsEndPoint, new byte[] { 1, 2, 3 } );

				var file = AssertFilePath( logger.DirectoryPath, now, dnsEndPoint.Host );
				Assert.That( File.ReadAllBytes( file ), Is.EqualTo( new byte[] { 1, 2, 3 } ) );
			}
		}

		[Test]
		public void TestWrite_EndPointIsNull_HostNameIsUsed()
		{
			using ( var tempDirectory = TempDirectory.Create() )
			using ( var logger = new FileMessagePackStreamLogger( tempDirectory.Path ) )
			{
				var now = DateTimeOffset.Now;
				Directory.CreateDirectory( logger.DirectoryPath );
				logger.Write( now, null, new byte[] { 1, 2, 3 } );

				var file = AssertFilePath( logger.DirectoryPath, now, null );
				Assert.That( File.ReadAllBytes( file ), Is.EqualTo( new byte[] { 1, 2, 3 } ) );
			}
		}

		[Test]
		public void TestWrite_EmptyStream_EmptyFile()
		{
			using ( var tempDirectory = TempDirectory.Create() )
			using ( var logger = new FileMessagePackStreamLogger( tempDirectory.Path ) )
			{
				var now = DateTimeOffset.Now;
				Directory.CreateDirectory( logger.DirectoryPath );
				logger.Write( now, _localhost, new byte[ 0 ] );

				var file = AssertFilePath( logger.DirectoryPath, now, _localhost.Address.ToString() );
				Assert.That( File.ReadAllBytes( file ), Is.Empty );
			}
		}

		[Test]
		public void TestWrite_NullStream_EmptyFile()
		{
			using ( var tempDirectory = TempDirectory.Create() )
			using ( var logger = new FileMessagePackStreamLogger( tempDirectory.Path ) )
			{
				var now = DateTimeOffset.Now;
				Directory.CreateDirectory( logger.DirectoryPath );
				logger.Write( now, _localhost, null );

				var file = AssertFilePath( logger.DirectoryPath, now, _localhost.Address.ToString() );
				Assert.That( File.ReadAllBytes( file ), Is.Empty );
			}
		}

		[Test]
		public void TestWrite_Twise_DifferenceAppended()
		{
			using ( var tempDirectory = TempDirectory.Create() )
			using ( var logger = new FileMessagePackStreamLogger( tempDirectory.Path ) )
			{
				var now = DateTimeOffset.Now;
				Directory.CreateDirectory( logger.DirectoryPath );
				logger.Write( now, _localhost, new byte[] { 1, 2, 3 } );
				logger.Write( now, _localhost, new byte[] { 1, 2, 3, 4, 5 } );

				var file = AssertFilePath( logger.DirectoryPath, now, _localhost.Address.ToString() );
				Assert.That( File.ReadAllBytes( file ), Is.EqualTo( new byte[] { 1, 2, 3, 4, 5 } ) );
			}
		}

		internal static string AssertFilePath( string directory, DateTimeOffset dateTime, string endPoint )
		{
			// {BaseDirectory}\{ProcessName}[-{AppDomainName}]\{ProcessStartTime}-{ProcessId}\{TimeStamp}-{EndPoint}-{ThreadId}.mpac
			using ( var process = Process.GetCurrentProcess() )
			{
				Assert.That( directory, Is.StringContaining( Path.GetFileNameWithoutExtension( process.MainModule.ModuleName ) ) );

				if ( !AppDomain.CurrentDomain.IsDefaultAppDomain() )
				{
					Assert.That( directory, Is.StringContaining( AppDomain.CurrentDomain.FriendlyName ) );
				}

				Assert.That( directory, Is.StringContaining( process.StartTime.ToUniversalTime().ToString( "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture ) ) );
				Assert.That( directory, Is.StringContaining( process.Id.ToString( CultureInfo.InvariantCulture ) ) );

				var files = Directory.GetFiles( directory );
				Assert.That( files, Is.Not.Null.And.Length.EqualTo( 1 ) );
				var file = new FileInfo( files[ 0 ] );
				Assert.That( file.Name, Is.StringContaining( dateTime.UtcDateTime.ToString( "yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture ) ) );

				if ( endPoint != null )
				{
					Assert.That( file.Name, Is.StringContaining( endPoint.Replace( '.', '_' ).Replace( ':', '_' ).Replace( '/', '_' ) ) );
				}

				Assert.That( file.Name, Is.StringContaining( new TraceEventCache().ThreadId ) );
				Assert.That( file.Extension, Is.EqualTo( ".mpac" ) );

				return file.FullName;
			}
		}

		private sealed class TempDirectory : IDisposable
		{
			private readonly string _path;

			public string Path
			{
				get { return this._path; }
			}

			public TempDirectory()
			{
				this._path = System.IO.Path.Combine( System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() );
				Directory.CreateDirectory( this._path );
			}

			public static TempDirectory Create()
			{
				return new TempDirectory();
			}

			public void Dispose()
			{
				if ( Directory.Exists( this._path ) )
				{
					try
					{
						Directory.Delete( this._path, true );
					}
					catch ( DirectoryNotFoundException ) { }
				}
			}
		}
	}
}