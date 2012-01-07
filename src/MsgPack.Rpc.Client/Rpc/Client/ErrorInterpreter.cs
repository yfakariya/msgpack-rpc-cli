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

using System.Diagnostics.Contracts;
using System.Linq;
using MsgPack.Rpc.Client.Protocols;

namespace MsgPack.Rpc.Client
{
	internal static class ErrorInterpreter
	{
		// TODO: Configurable
		private const int _unknownErrorQuota = 1024;

		internal static RpcErrorMessage UnpackError( ClientResponseContext context )
		{
			Contract.Assert( context.ErrorBuffer.Length > 0 );

			MessagePackObject error;
			try
			{
				error = Unpacking.UnpackObject( context.ErrorBuffer );
			}
			catch ( UnpackException )
			{
				error = new MessagePackObject( context.ErrorBuffer.GetBuffer( 0, _unknownErrorQuota ).SelectMany( segment => segment.AsEnumerable() ).ToArray() );
			}

			RpcError errorIdentifier;
			if ( error.IsTypeOf<string>().GetValueOrDefault() )
			{
				errorIdentifier = RpcError.FromIdentifier( error.AsString(), null );
			}
			else if ( error.IsTypeOf<int>().GetValueOrDefault() )
			{
				errorIdentifier = RpcError.FromIdentifier( null, error.AsInt32() );
			}
			else
			{
				errorIdentifier = RpcError.Unexpected;
			}

			MessagePackObject detail;
			if ( context.ResultBuffer.Length == 0 )
			{
				detail = MessagePackObject.Nil;
			}
			else
			{
				try
				{
					detail = Unpacking.UnpackObject( context.ResultBuffer );
				}
				catch ( UnpackException )
				{
					detail = new MessagePackObject( context.ResultBuffer.GetBuffer( 0, _unknownErrorQuota ).SelectMany( segment => segment.AsEnumerable() ).ToArray() );
				}
			}

			return new RpcErrorMessage( errorIdentifier, detail );
		}
	}
}
