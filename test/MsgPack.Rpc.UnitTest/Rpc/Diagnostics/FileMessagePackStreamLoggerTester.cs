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
using System.IO;
using System.Net;
using NUnit.Framework;

namespace MsgPack.Rpc.Diagnostics
{
	public class FileMessagePackStreamLoggerTester : MarshalByRefObject
	{
		public void Test( string baseDirectoryPath )
		{
			var localhost = new IPEndPoint( IPAddress.Loopback, 0 );
			using ( var logger = new FileMessagePackStreamLogger( baseDirectoryPath ) )
			{
				var now = DateTimeOffset.Now;
				logger.Write( now, localhost, new byte[] { 1, 2, 3 } );

				var file = FileMessagePackStreamLoggerTest.AssertFilePath( logger.DirectoryPath, now, localhost.Address.ToString() );
				Assert.That( File.ReadAllBytes( file ), Is.EqualTo( new byte[] { 1, 2, 3 } ) );
			}
		}
	}
}
