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

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Defines interfaces and basic features of tranportation layer.
	/// </summary>
	public abstract class ServerTransport : IDisposable
	{
		public void Dispose()
		{
			this.Dispose( true );
			GC.SuppressFinalize( this );
		}

		protected virtual void Dispose( bool disposing ) { }

		public void OnReceived( RpcServerSession session )
		{
			if ( session == null )
			{
				throw new ArgumentNullException( "session" );
			}

			Contract.EndContractBlock();

			this.OnReceivedCore( session );
		}

		protected abstract void OnReceivedCore( RpcServerSession session );

		public void Send( RpcServerSession session, MessageType type, int messageId, object returnValue, bool isVoid, Exception exception )
		{
			if ( session == null )
			{
				throw new ArgumentNullException( "session" );
			}

			switch ( type )
			{
				case MessageType.Response:
				{
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException( "type", type, "'type' must be 'Response'." );
				}
			}

			if ( isVoid && returnValue != null )
			{
				throw new ArgumentException( "'returnValue' must be null if 'isVoid' is true.", "returnValue" );
			}

			Contract.EndContractBlock();

			this.SendCore( session, type, messageId, returnValue, isVoid, exception );
		}

		protected abstract void SendCore( RpcServerSession session, MessageType type, int messageId, object returnValue, bool isVoid, Exception exception );
	}
}
