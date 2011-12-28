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
using MsgPack.Rpc.Server.Dispatch;
using MsgPack.Rpc.Server.Protocols;

namespace MsgPack.Rpc.Server
{
	// This file generated from RpcServerConfiguration.tt T4Template.
	// Do not modify this file. Edit RpcServerConfiguration.tt instead.

	partial class RpcServerConfiguration
	{

		private int? _minimumConnection = 2;
		
		/// <summary>
		/// 	Gets or sets the minimum connection to be pool for newly inbound connection.
		/// </summary>
		/// <value>
		/// 	The minimum connection to be pool for newly inbound connection. The default is 2.
		/// </value>
		public int? MinimumConnection
		{
			get{ return this._minimumConnection; }
			set
			{
				this.VerifyIsNotFrozen();
				
				if( value == null )
				{
					this._minimumConnection = 2;
				}
				else
				{
					ValidateMinimumConnection( value );
					this._minimumConnection = value;
				}
			}
		}
		
		static partial void ValidateMinimumConnection( int? value );

		private int? _maximumConnection = 100;
		
		/// <summary>
		/// 	Gets or sets the maximum connection to be handle inbound connection.
		/// </summary>
		/// <value>
		/// 	The minimum connection to be handle inbound connection. The default is 100.
		/// </value>
		public int? MaximumConnection
		{
			get{ return this._maximumConnection; }
			set
			{
				this.VerifyIsNotFrozen();
				
				if( value == null )
				{
					this._maximumConnection = 100;
				}
				else
				{
					ValidateMaximumConnection( value );
					this._maximumConnection = value;
				}
			}
		}
		
		static partial void ValidateMaximumConnection( int? value );

		private int? _minimumConcurrentRequest = 2;
		
		/// <summary>
		/// 	Gets or sets the minimum concurrency for the each clients.
		/// </summary>
		/// <value>
		/// 	The minimum concurrency for the each clients. The default is 2.
		/// </value>
		public int? MinimumConcurrentRequest
		{
			get{ return this._minimumConcurrentRequest; }
			set
			{
				this.VerifyIsNotFrozen();
				
				if( value == null )
				{
					this._minimumConcurrentRequest = 2;
				}
				else
				{
					ValidateMinimumConcurrentRequest( value );
					this._minimumConcurrentRequest = value;
				}
			}
		}
		
		static partial void ValidateMinimumConcurrentRequest( int? value );

		private int? _maximumConcurrentRequest = 10;
		
		/// <summary>
		/// 	Gets or sets the maximum concurrency for the each clients.
		/// </summary>
		/// <value>
		/// 	The maximum concurrency for the each clients. The default is 10.
		/// </value>
		public int? MaximumConcurrentRequest
		{
			get{ return this._maximumConcurrentRequest; }
			set
			{
				this.VerifyIsNotFrozen();
				
				if( value == null )
				{
					this._maximumConcurrentRequest = 10;
				}
				else
				{
					ValidateMaximumConcurrentRequest( value );
					this._maximumConcurrentRequest = value;
				}
			}
		}
		
		static partial void ValidateMaximumConcurrentRequest( int? value );

		private EndPoint _bindingEndPoint = null;
		
		/// <summary>
		/// 	Gets or sets the local end point to be bound.
		/// </summary>
		/// <value>
		/// 	The local end point to be bound. The default is <c>null</c>. The server will select appropriate version IP and bind to it with port 0.
		/// </value>
		public EndPoint BindingEndPoint
		{
			get{ return this._bindingEndPoint; }
			set
			{
				this.VerifyIsNotFrozen();
				
				if( value == null )
				{
					this._bindingEndPoint = null;
				}
				else
				{
					ValidateBindingEndPoint( value );
					this._bindingEndPoint = value;
				}
			}
		}
		
		static partial void ValidateBindingEndPoint( EndPoint value );

		private int? _listenBackLog = 100;
		
		/// <summary>
		/// 	Gets or sets the listen back log of each sockets.
		/// </summary>
		/// <value>
		/// 	The listen back log of each sockets. The default is 100.
		/// </value>
		public int? ListenBackLog
		{
			get{ return this._listenBackLog; }
			set
			{
				this.VerifyIsNotFrozen();
				
				if( value == null )
				{
					this._listenBackLog = 100;
				}
				else
				{
					ValidateListenBackLog( value );
					this._listenBackLog = value;
				}
			}
		}
		
		static partial void ValidateListenBackLog( int? value );

		private int? _portNumber = 10912;
		
		/// <summary>
		/// 	Gets or sets the listening port number.
		/// </summary>
		/// <value>
		/// 	The listening port number. The default is 10912.
		/// </value>
		public int? PortNumber
		{
			get{ return this._portNumber; }
			set
			{
				this.VerifyIsNotFrozen();
				
				if( value == null )
				{
					this._portNumber = 10912;
				}
				else
				{
					ValidatePortNumber( value );
					this._portNumber = value;
				}
			}
		}
		
		static partial void ValidatePortNumber( int? value );

		private Func<ServiceTypeLocator> _serviceTypeLocatorProvider = () => new DefaultServiceTypeLocator();
		
		/// <summary>
		/// 	Gets or sets the factory function which creates new <see cref="ServiceTypeLocator" />.
		/// </summary>
		/// <value>
		/// 	The factory function which creates new <see cref="ServiceTypeLocator" />. The default is the delegate which creates <see cref="DefaultServiceTypeLocator" /> instance.
		/// </value>
		public Func<ServiceTypeLocator> ServiceTypeLocatorProvider
		{
			get{ return this._serviceTypeLocatorProvider; }
			set
			{
				this.VerifyIsNotFrozen();
				
				if( value == null )
				{
					this._serviceTypeLocatorProvider = () => new DefaultServiceTypeLocator();
				}
				else
				{
					ValidateServiceTypeLocatorProvider( value );
					this._serviceTypeLocatorProvider = value;
				}
			}
		}
		
		static partial void ValidateServiceTypeLocatorProvider( Func<ServiceTypeLocator> value );

		private Func<Func<ServerRequestSocketAsyncEventArgs>, ObjectPool<ServerRequestSocketAsyncEventArgs>> _requestContextPoolProvider = ( factory ) => new StandardObjectPool<ServerRequestSocketAsyncEventArgs>( factory, null );
		
		/// <summary>
		/// 	Gets or sets the factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="ServerRequestSocketAsyncEventArgs" />.
		/// </summary>
		/// <value>
		/// 	The factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="ServerRequestSocketAsyncEventArgs" />. The default is the delegate which creates <see cref="StandardObjectPool{T}" /> instance with <c>null</c> configuration.
		/// </value>
		public Func<Func<ServerRequestSocketAsyncEventArgs>, ObjectPool<ServerRequestSocketAsyncEventArgs>> RequestContextPoolProvider
		{
			get{ return this._requestContextPoolProvider; }
			set
			{
				this.VerifyIsNotFrozen();
				
				if( value == null )
				{
					this._requestContextPoolProvider = ( factory ) => new StandardObjectPool<ServerRequestSocketAsyncEventArgs>( factory, null );
				}
				else
				{
					ValidateRequestContextPoolProvider( value );
					this._requestContextPoolProvider = value;
				}
			}
		}
		
		static partial void ValidateRequestContextPoolProvider( Func<Func<ServerRequestSocketAsyncEventArgs>, ObjectPool<ServerRequestSocketAsyncEventArgs>> value );

		private Func<Func<ServerResponseSocketAsyncEventArgs>, ObjectPool<ServerResponseSocketAsyncEventArgs>> _responseContextPoolProvider = ( factory ) => new StandardObjectPool<ServerResponseSocketAsyncEventArgs>( factory, null );
		
		/// <summary>
		/// 	Gets or sets the factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="ServerResponseSocketAsyncEventArgs" />.
		/// </summary>
		/// <value>
		/// 	The factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="ServerResponseSocketAsyncEventArgs" />. The default is the delegate which creates <see cref="StandardObjectPool{T}" /> instance with <c>null</c> configuration.
		/// </value>
		public Func<Func<ServerResponseSocketAsyncEventArgs>, ObjectPool<ServerResponseSocketAsyncEventArgs>> ResponseContextPoolProvider
		{
			get{ return this._responseContextPoolProvider; }
			set
			{
				this.VerifyIsNotFrozen();
				
				if( value == null )
				{
					this._responseContextPoolProvider = ( factory ) => new StandardObjectPool<ServerResponseSocketAsyncEventArgs>( factory, null );
				}
				else
				{
					ValidateResponseContextPoolProvider( value );
					this._responseContextPoolProvider = value;
				}
			}
		}
		
		static partial void ValidateResponseContextPoolProvider( Func<Func<ServerResponseSocketAsyncEventArgs>, ObjectPool<ServerResponseSocketAsyncEventArgs>> value );

		private Func<Func<ListeningContext>, ObjectPool<ListeningContext>> _listeningContextPoolProvider = ( factory ) => new StandardObjectPool<ListeningContext>( factory, null );
		
		/// <summary>
		/// 	Gets or sets the factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="ListeningContext" />.
		/// </summary>
		/// <value>
		/// 	The factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="ListeningContext" />. The default is the delegate which creates <see cref="StandardObjectPool{T}" /> instance with <c>null</c> configuration.
		/// </value>
		public Func<Func<ListeningContext>, ObjectPool<ListeningContext>> ListeningContextPoolProvider
		{
			get{ return this._listeningContextPoolProvider; }
			set
			{
				this.VerifyIsNotFrozen();
				
				if( value == null )
				{
					this._listeningContextPoolProvider = ( factory ) => new StandardObjectPool<ListeningContext>( factory, null );
				}
				else
				{
					ValidateListeningContextPoolProvider( value );
					this._listeningContextPoolProvider = value;
				}
			}
		}
		
		static partial void ValidateListeningContextPoolProvider( Func<Func<ListeningContext>, ObjectPool<ListeningContext>> value );

		private Func<Func<TcpServerTransport>, ObjectPool<TcpServerTransport>> _tcpTransportPoolProvider = ( factory ) => new StandardObjectPool<TcpServerTransport>( factory, null );
		
		/// <summary>
		/// 	Gets or sets the factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="TcpServerTransport" />.
		/// </summary>
		/// <value>
		/// 	The factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="TcpServerTransport" />. The default is the delegate which creates <see cref="StandardObjectPool{T}" /> instance with <c>null</c> configuration.
		/// </value>
		public Func<Func<TcpServerTransport>, ObjectPool<TcpServerTransport>> TcpTransportPoolProvider
		{
			get{ return this._tcpTransportPoolProvider; }
			set
			{
				this.VerifyIsNotFrozen();
				
				if( value == null )
				{
					this._tcpTransportPoolProvider = ( factory ) => new StandardObjectPool<TcpServerTransport>( factory, null );
				}
				else
				{
					ValidateTcpTransportPoolProvider( value );
					this._tcpTransportPoolProvider = value;
				}
			}
		}
		
		static partial void ValidateTcpTransportPoolProvider( Func<Func<TcpServerTransport>, ObjectPool<TcpServerTransport>> value );

		private Func<Func<UdpServerTransport>, ObjectPool<UdpServerTransport>> _udpTransportPoolProvider = ( factory ) => new StandardObjectPool<UdpServerTransport>( factory, null );
		
		/// <summary>
		/// 	Gets or sets the factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="UdpServerTransport" />.
		/// </summary>
		/// <value>
		/// 	The factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="UdpServerTransport" />. The default is the delegate which creates <see cref="StandardObjectPool{T}" /> instance with <c>null</c> configuration.
		/// </value>
		public Func<Func<UdpServerTransport>, ObjectPool<UdpServerTransport>> UdpTransportPoolProvider
		{
			get{ return this._udpTransportPoolProvider; }
			set
			{
				this.VerifyIsNotFrozen();
				
				if( value == null )
				{
					this._udpTransportPoolProvider = ( factory ) => new StandardObjectPool<UdpServerTransport>( factory, null );
				}
				else
				{
					ValidateUdpTransportPoolProvider( value );
					this._udpTransportPoolProvider = value;
				}
			}
		}
		
		static partial void ValidateUdpTransportPoolProvider( Func<Func<UdpServerTransport>, ObjectPool<UdpServerTransport>> value );
	}
}
