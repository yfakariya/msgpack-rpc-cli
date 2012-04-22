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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.Globalization;
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc.Server.Protocols
{
	/// <summary>
	///		Provides precise control of the <see cref="InProcServerTransportManager"/> to simulate communication and
	///		decouples Server assembly from the caller (typically test code).
	/// </summary>
	public sealed class InProcServerTransportController : IDisposable
	{
		private readonly InProcServerTransport _transport;
		private readonly InProcServerTransportManager _transportManager;
		private readonly bool _ownsTransportManager;

		/// <summary>
		/// Occurs when the <see cref="InProcServerTransportManager"/> sends response.
		/// </summary>
		public event EventHandler<InProcResponseEventArgs> Response;

		private void OnResponse( InProcResponseEventArgs e )
		{
			var handler = this.Response;
			if ( handler != null )
			{
				handler( this, e );
			}
		}

		private InProcServerTransportController( InProcServerTransportManager transportManager, bool ownsTransportManager )
		{
			this._transportManager = transportManager;
			this._transport = transportManager.NewSession();
			this._transportManager.Response += this.OnTransportResponse;
			this._ownsTransportManager = ownsTransportManager;
		}

		public static InProcServerTransportController Create( CallbackServer server, Delegate objectPoolProvider )
		{
			if ( server == null )
			{
				throw new ArgumentNullException( "inProcServerTransportManager" );
			}

			Contract.Ensures( Contract.Result<InProcServerTransportController>() != null );

			return Create( new InProcServerTransportManager( server.Server as RpcServer, objectPoolProvider as Func<InProcServerTransportManager, ObjectPool<InProcServerTransport>> ), true );
		}

		public static InProcServerTransportController Create( object inProcServerTransportManager )
		{
			InProcServerTransportManager asInProcServerTransportManager;

			if ( inProcServerTransportManager == null )
			{
				throw new ArgumentNullException( "inProcServerTransportManager" );
			}

			if ( ( asInProcServerTransportManager = inProcServerTransportManager as InProcServerTransportManager ) == null )
			{
				throw new ArgumentException( String.Format( CultureInfo.CurrentCulture, "TransportManager is not an '{0}' type.", typeof( RpcServerConfiguration ) ), "configuration" );
			}

			Contract.Ensures( Contract.Result<InProcServerTransportController>() != null );

			return Create( asInProcServerTransportManager, false );
		}

		private static InProcServerTransportController Create( InProcServerTransportManager asInProcServerTransportManager, bool ownsTransportManager )
		{
			return new InProcServerTransportController( asInProcServerTransportManager, ownsTransportManager );
		}

		public void Dispose()
		{
			this._transportManager.Response -= this.OnTransportResponse;

			if ( this._ownsTransportManager )
			{
				this._transportManager.Dispose();
			}
		}

		private void OnTransportResponse( object sender, InProcResponseEventArgs e )
		{
			this.OnResponse( e );
		}

		public void FeedReceiveBuffer( byte[] data )
		{
			this._transport.FeedData( data );
		}
	}
}
