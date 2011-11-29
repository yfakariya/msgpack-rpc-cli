using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics.Contracts;
using System.Net;
using MsgPack.Rpc.Serialization;
using MsgPack.Collections;
using MsgPack.Rpc.Protocols.Services;

namespace MsgPack.Rpc.Protocols
{
	public abstract class ConnectionOrientedClientTransport : ClientTransport
	{
		private const int _defaultMinimumConnectionCount = 2;
		private const int _defaultMaximumConnectionCount = 16;
		private static readonly TimeSpan _defaultConnectTimeout = TimeSpan.FromSeconds( 15 );
		private readonly ConnectionPool _connectionPool;
		private readonly TimeSpan? _connectTimeout;

		protected ConnectionOrientedClientTransport( EndPoint remoteEndPoint, RpcTransportProtocol protocol, ClientEventLoop eventLoop, RpcClientOptions options )
			: base( protocol, eventLoop, options )
		{
			if ( remoteEndPoint == null )
			{
				throw new ArgumentNullException( "remoteEndPoint" );
			}

			this._connectionPool =
				new ConnectionPool(
					remoteEndPoint,
					protocol,
					eventLoop,
					options == null ? _defaultMinimumConnectionCount : ( options.MinimumConnectionCount ?? _defaultMinimumConnectionCount ),
					options == null ? _defaultMaximumConnectionCount : ( options.MaximumConnectionCount ?? _defaultMaximumConnectionCount )
				);
			this._connectTimeout =
				options == null
				? _defaultConnectTimeout
				: ( options.ConnectTimeout ?? _defaultConnectTimeout );
		}

		protected override void Dispose( bool disposing )
		{
			// TODO: trace
			this._connectionPool.Dispose();
			base.Dispose( disposing );
		}

		protected override SendingContext CreateNewSendingContext( int? messageId, Action<SendingContext, Exception, bool> onMessageSent )
		{
			// FIXME: from buffer pool?
			var socketContext = this._connectionPool.Borrow( this._connectTimeout, this.EventLoop.CancellationToken );
			return
				new SendingContext(
					new ClientSessionContext( this, this.Options, socketContext ),
					new RpcOutputBuffer( ChunkBuffer.CreateDefault( this.InitialSegmentCount, this.InitialSegmentSize ) ),
					messageId,
					onMessageSent // NOTE: Combine buffer clean up if this uses buffer pooling.
				);
		}

		protected override void OnReceiveCore( ReceivingContext context, ResponseMessage response, RpcErrorMessage error )
		{
			base.OnReceiveCore( context, response, error );
			this._connectionPool.Return( context.SocketContext );
			context.SessionContext.Dispose();
			// FIXME: Return to buffer pool.
		}
	}
}
