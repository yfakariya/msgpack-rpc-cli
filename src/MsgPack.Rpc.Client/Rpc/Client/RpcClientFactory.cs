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
using System.Net;
using MsgPack.Rpc.Protocols;
using System.Net.Sockets;

namespace MsgPack.Rpc
{
	//public abstract class RpcClientFactory
	//{
	//    private readonly ClientEventLoop _eventLoop;

	//    protected ClientEventLoop EventLoop
	//    {
	//        get { return this._eventLoop; }
	//    }

	//    protected RpcClientFactory( ClientEventLoop eventLoop )
	//    {
	//        if ( eventLoop == null )
	//        {
	//            throw new ArgumentNullException( "eventLoop" );
	//        }

	//        this._eventLoop = eventLoop;
	//    }

	//    public RpcClient Connect( EndPoint endPoint, RpcTransportProtol transportProtocol, RpcClientOptions option )
	//    {
	//        return this.ConnectCore( endPoint, transportProtocol, option );
	//    }

	//    protected virtual RpcClient ConnectCore( EndPoint endPoint, RpcTransportProtol transportProtocol, RpcClientOptions option )
	//    {
	//        return this.EndConnect( this.BeginConnect( endPoint, transportProtocol, option, null, null ) );
	//    }

	//    public IAsyncResult BeginConnect( EndPoint endPoint, RpcTransportProtol transportProtocol, RpcClientOptions options, AsyncCallback asyncCallback, object asyncState )
	//    {
	//        return this.BeginConnectCore( endPoint, transportProtocol, options, asyncCallback, asyncState );
	//    }

	//    protected abstract IAsyncResult BeginConnectCore( EndPoint endPoint, RpcTransportProtol transportProtocol, RpcClientOptions options, AsyncCallback asyncCallback, object asyncState );

	//    public RpcClient EndConnect( IAsyncResult asyncResult )
	//    {
	//        return EndConnectCore( asyncResult );
	//    }

	//    protected abstract RpcClient EndConnectCore( IAsyncResult asyncResult );
	//}

	//public sealed class TcpRpcClientFactory : RpcClientFactory
	//{
	//    public TcpRpcClientFactory( ClientEventLoop eventLoop ) : base( eventLoop ) { }

	//    protected sealed override IAsyncResult BeginConnectCore( EndPoint endPoint, RpcTransportProtol transportProtocol, RpcClientOptions options, AsyncCallback asyncCallback, object asyncState )
	//    {
	//        var transport =
	//            new TcpClientTransport(
	//                transportProtocol.CreateSocket(),
	//                this.EventLoop,
	//                options,
	//                OnConnected,
	//                OnConnectError
	//            );
	//        var ar = new ConnectAsyncResult( this, transport, asyncCallback, asyncState );
	//        var context = new ClientSessionContext( this.EventLoop.CreateAsyncSocketContext(), transport, this.EventLoop.CancellationToken );
	//        transport.Connect( context, ar );
	//        return ar;
	//    }

	//    private static void OnConnected( ClientSessionContext context, object asyncState )
	//    {
	//        ConnectAsyncResult ar = asyncState as ConnectAsyncResult;
	//        Contract.Assume( ar != null );
	//        ar.OnConnected( context.SocketContext, false );
	//        if ( ar.AsyncCallback != null )
	//        {
	//            ar.AsyncCallback( ar );
	//        }
	//    }

	//    private static void OnConnectError( RpcError rpcError, Exception innerException, object asyncState )
	//    {
	//        ConnectAsyncResult ar = asyncState as ConnectAsyncResult;
	//        Contract.Assume( ar != null );
	//        ar.OnError( new RpcTransportException( rpcError, "Failed to connect.", null, innerException ), false );
	//        if ( ar.AsyncCallback != null )
	//        {
	//            ar.AsyncCallback( ar );
	//        }
	//    }

	//    protected sealed override RpcClient EndConnectCore( IAsyncResult asyncResult )
	//    {
	//        var ar = AsyncResult.Verify<ConnectAsyncResult>( asyncResult, this );

	//        if ( !ar.IsCompleted )
	//        {
	//            asyncResult.AsyncWaitHandle.WaitOne();
	//        }
	//        ar.Finish();

	//        if ( ar.Error != null )
	//        {
	//            throw ar.Error;
	//        }

	//        return new RpcClient( ar.Transport );
	//    }

	//    private sealed class ConnectAsyncResult : AsyncResult
	//    {
	//        private ClientSocketAsyncEventArgs _socketContext;

	//        public ClientSocketAsyncEventArgs SocketContext
	//        {
	//            get { return this._socketContext; }
	//        }

	//        private Exception _error;

	//        public Exception Error
	//        {
	//            get { return this._error; }
	//        }

	//        private readonly TcpClientTransport _transport;

	//        public TcpClientTransport Transport
	//        {
	//            get { return this._transport; }
	//        }

	//        public void OnConnected( ClientSocketAsyncEventArgs socketContext, bool completedAsynchronously )
	//        {
	//            this._socketContext = socketContext;
	//            base.Complete( completedAsynchronously );
	//        }

	//        public void OnError( Exception error, bool completedAsynchronously )
	//        {
	//            this._error = error;
	//            base.Complete( completedAsynchronously );
	//        }

	//        public ConnectAsyncResult( object owner, TcpClientTransport transport, AsyncCallback asyncCallback, object asyncState )
	//            : base( owner, asyncCallback, asyncState )
	//        {
	//            Contract.Assume( transport != null );
	//            this._transport = transport;
	//        }

	//    }
	//}

	//public sealed class UdpRpcClientFactory : RpcClientFactory
	//{
	//    public UdpRpcClientFactory( ClientEventLoop eventLoop ) : base( eventLoop ) { }

	//    protected override RpcClient ConnectCore( EndPoint endPoint, RpcTransportProtol transportProtocol, RpcClientOptions option )
	//    {
	//        Contract.Assume( transportProtocol.AddressFamily == AddressFamily.InterNetwork || transportProtocol.AddressFamily == AddressFamily.InterNetworkV6 );
	//        Contract.Assume( transportProtocol.ProtocolType == ProtocolType.Udp );
	//        Contract.Assume( transportProtocol.SocketType == SocketType.Dgram );

	//        // TODO: Socket Pooling
	//        return new RpcClient( new UdpClientTransport( transportProtocol.CreateSocket(), endPoint, this.EventLoop.CreateAsyncSocketContext(), this.EventLoop ) );
	//    }

	//    protected sealed override IAsyncResult BeginConnectCore( EndPoint endPoint, RpcTransportProtol transportProtocol, RpcClientOptions options, AsyncCallback asyncCallback, object asyncState )
	//    {
	//        return new ConstructionAsyncResult( this, endPoint, transportProtocol, options, asyncCallback, asyncState );
	//    }

	//    protected sealed override RpcClient EndConnectCore( IAsyncResult asyncResult )
	//    {
	//        var ar = AsyncResult.Verify<ConstructionAsyncResult>( asyncResult, this );
	//        return this.ConnectCore( ar.RemoteEndPoint, ar.TransportProtocol, ar.Options );
	//    }

	//    private sealed class ConstructionAsyncResult : WrapperAsyncResult
	//    {
	//        private readonly EndPoint _remoteEndPoint;

	//        public EndPoint RemoteEndPoint
	//        {
	//            get { return this._remoteEndPoint; }
	//        }

	//        private readonly RpcTransportProtol _transportProtocol;

	//        public RpcTransportProtol TransportProtocol
	//        {
	//            get { return this._transportProtocol; }
	//        }

	//        private readonly RpcClientOptions _options;

	//        public RpcClientOptions Options
	//        {
	//            get { return this._options; }
	//        }

	//        public ConstructionAsyncResult(
	//            object owner,
	//            EndPoint remoteEndPoint,
	//            RpcTransportProtol transportProtocol,
	//            RpcClientOptions options,
	//            AsyncCallback asyncCallback,
	//            object asyncState
	//        )
	//            : base( owner, asyncCallback, asyncState )
	//        {
	//            Contract.Assert( remoteEndPoint != null );
	//            this._transportProtocol = transportProtocol;
	//            this._options = options;
	//            this._remoteEndPoint = remoteEndPoint;
	//        }
	//    }
	//}

}
