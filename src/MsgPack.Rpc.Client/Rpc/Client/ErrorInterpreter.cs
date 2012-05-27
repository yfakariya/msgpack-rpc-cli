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
using System.Diagnostics.Contracts;
using System.Linq;
using MsgPack.Rpc.Client.Protocols;

namespace MsgPack.Rpc.Client
{
	/// <summary>
	///		Interprets error stream.
	/// </summary>
	internal static class ErrorInterpreter
	{
		/// <summary>
		///		Unpacks <see cref="RpcErrorMessage"/> from stream in the specified context.
		/// </summary>
		/// <param name="context"><see cref="ClientResponseContext"/> which stores serialized error.</param>
		/// <returns>An unpacked <see cref="RpcErrorMessage"/>.</returns>
		internal static RpcErrorMessage UnpackError( ClientResponseContext context )
		{
			Contract.Assert( context != null );
			Contract.Assert( context.ErrorBuffer != null );
			Contract.Assert( context.ErrorBuffer.Length > 0 );
			Contract.Assert( context.ResultBuffer != null );
			Contract.Assert( context.ResultBuffer.Length > 0 );

			MessagePackObject error;
			try
			{
				error = Unpacking.UnpackObject( context.ErrorBuffer );
			}
			catch ( UnpackException )
			{
				error = new MessagePackObject( context.ErrorBuffer.GetBuffer().SelectMany( segment => segment.AsEnumerable() ).ToArray() );
			}

			if ( error.IsNil )
			{
				return RpcErrorMessage.Success;
			}

			bool isUnknown = false;
			RpcError errorIdentifier;
			if ( error.IsTypeOf<string>().GetValueOrDefault() )
			{
				var asString = error.AsString();
				errorIdentifier = RpcError.FromIdentifier( asString, null );
				// Check if the error is truely Unexpected error.
				isUnknown = errorIdentifier.ErrorCode == RpcError.Unexpected.ErrorCode && asString != RpcError.Unexpected.Identifier;
			}
			else if ( error.IsTypeOf<int>().GetValueOrDefault() )
			{
				errorIdentifier = RpcError.FromIdentifier( null, error.AsInt32() );
			}
			else
			{
				errorIdentifier = RpcError.Unexpected;
				isUnknown = true;
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
					detail = new MessagePackObject( context.ResultBuffer.GetBuffer().SelectMany( segment => segment.AsEnumerable() ).ToArray() );
				}
			}

			if ( isUnknown )
			{
				// Unknown error, the error should contain original Error field as message.
				if ( detail.IsNil )
				{
					return new RpcErrorMessage( errorIdentifier, error.AsString(), null );
				}
				else
				{
					var details = new MessagePackObjectDictionary( 2 );
					details[ RpcException.MessageKeyUtf8 ] = error;
					details[ RpcException.DebugInformationKeyUtf8 ] = detail;
					return new RpcErrorMessage( errorIdentifier, new MessagePackObject( details, true ) );
				}
			}
			else
			{
				return new RpcErrorMessage( errorIdentifier, detail );
			}
		}
	}
}
