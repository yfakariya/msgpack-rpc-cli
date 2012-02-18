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
using MsgPack.Rpc.Client.Protocols;
using NUnit.Framework;

namespace MsgPack.Rpc.Client
{
	[TestFixture]
	public class ErrorInterpreterTest
	{
		private static ClientResponseContext CreateContext( RpcErrorMessage message )
		{
			var context = new ClientResponseContext();
			using ( var buffer = new MemoryStream() )
			using ( var packer = Packer.Create( buffer, false ) )
			{
				packer.Pack( message.Error.Identifier );
				context.ErrorBuffer = new ByteArraySegmentStream( new[] { new ArraySegment<byte>( buffer.ToArray() ) } );
				buffer.SetLength( 0 );
				packer.Pack( message.Detail );
				context.ResultBuffer = new ByteArraySegmentStream( new[] { new ArraySegment<byte>( buffer.ToArray() ) } );
			}

			return context;
		}

		[Test]
		public void TestUnpackError_RoundTripped()
		{
			var detail = Guid.NewGuid().ToString();
			var message = new RpcErrorMessage( RpcError.CallError, detail );
			var context = CreateContext( message );
			var result = ErrorInterpreter.UnpackError( context );
			Assert.That( message == result, result.ToString() );
		}
	}
}
