
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
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using MsgPack.Rpc.Server.Dispatch;
using MsgPack.Rpc.Server.Protocols;

namespace MsgPack.Rpc.Server
{
	// This file generated from RpcServerConfiguration.tt T4Template.
	// Do not modify this file. Edit RpcServerConfiguration.tt instead.

	partial class RpcServerConfiguration
	{
		private bool _preferIPv4 = false;
		
		/// <summary>
		/// 	Gets or sets whether use IP v4 even when IP v6 is supported.
		/// </summary>
		/// <value>
		/// 	<c>true</c>, use IP v4 anyway; otherwise, <c>false</c>. The default is <c>false</c>.
		/// </value>
		public bool PreferIPv4
		{
			get
			{
				return this._preferIPv4;
			}
			set
			{
				this.VerifyIsNotFrozen();
				var coerced = value;
				CoercePreferIPv4Value( ref coerced );
				this._preferIPv4 = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the PreferIPv4 property value.
		/// </summary>
		public void ResetPreferIPv4()
		{
			this._preferIPv4 = false;
		}
		
		static partial void CoercePreferIPv4Value( ref bool value );

		private int _minimumConnection = 2;
		
		/// <summary>
		/// 	Gets or sets the minimum connection to be pool for newly inbound connection.
		/// </summary>
		/// <value>
		/// 	The minimum connection to be pool for newly inbound connection. The default is 2.
		/// </value>
		public int MinimumConnection
		{
			get
			{
				Contract.Ensures( Contract.Result<int>() >= default( int ) );

				return this._minimumConnection;
			}
			set
			{
				if ( !( value >= default( int ) ) )
				{
					throw new ArgumentOutOfRangeException( "value", "Argument cannot be negative number." );
				}

				Contract.Ensures( Contract.Result<int>() >= default( int ) );

				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceMinimumConnectionValue( ref coerced );
				this._minimumConnection = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the MinimumConnection property value.
		/// </summary>
		public void ResetMinimumConnection()
		{
			this._minimumConnection = 2;
		}
		
		static partial void CoerceMinimumConnectionValue( ref int value );

		private int _maximumConnection = 100;
		
		/// <summary>
		/// 	Gets or sets the maximum connection to be handle inbound connection.
		/// </summary>
		/// <value>
		/// 	The minimum connection to be handle inbound connection. The default is 100.
		/// </value>
		public int MaximumConnection
		{
			get
			{
				Contract.Ensures( Contract.Result<int>() > default( int ) );

				return this._maximumConnection;
			}
			set
			{
				if ( !( value > default( int ) ) )
				{
					throw new ArgumentOutOfRangeException( "value", "Argument must be positive number." );
				}

				Contract.Ensures( Contract.Result<int>() > default( int ) );

				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceMaximumConnectionValue( ref coerced );
				this._maximumConnection = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the MaximumConnection property value.
		/// </summary>
		public void ResetMaximumConnection()
		{
			this._maximumConnection = 100;
		}
		
		static partial void CoerceMaximumConnectionValue( ref int value );

		private int _minimumConcurrentRequest = 2;
		
		/// <summary>
		/// 	Gets or sets the minimum concurrency for the each clients.
		/// </summary>
		/// <value>
		/// 	The minimum concurrency for the each clients. The default is 2.
		/// </value>
		public int MinimumConcurrentRequest
		{
			get
			{
				Contract.Ensures( Contract.Result<int>() >= default( int ) );

				return this._minimumConcurrentRequest;
			}
			set
			{
				if ( !( value >= default( int ) ) )
				{
					throw new ArgumentOutOfRangeException( "value", "Argument cannot be negative number." );
				}

				Contract.Ensures( Contract.Result<int>() >= default( int ) );

				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceMinimumConcurrentRequestValue( ref coerced );
				this._minimumConcurrentRequest = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the MinimumConcurrentRequest property value.
		/// </summary>
		public void ResetMinimumConcurrentRequest()
		{
			this._minimumConcurrentRequest = 2;
		}
		
		static partial void CoerceMinimumConcurrentRequestValue( ref int value );

		private int _maximumConcurrentRequest = 10;
		
		/// <summary>
		/// 	Gets or sets the maximum concurrency for the each clients.
		/// </summary>
		/// <value>
		/// 	The maximum concurrency for the each clients. The default is 10.
		/// </value>
		public int MaximumConcurrentRequest
		{
			get
			{
				Contract.Ensures( Contract.Result<int>() > default( int ) );

				return this._maximumConcurrentRequest;
			}
			set
			{
				if ( !( value > default( int ) ) )
				{
					throw new ArgumentOutOfRangeException( "value", "Argument must be positive number." );
				}

				Contract.Ensures( Contract.Result<int>() > default( int ) );

				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceMaximumConcurrentRequestValue( ref coerced );
				this._maximumConcurrentRequest = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the MaximumConcurrentRequest property value.
		/// </summary>
		public void ResetMaximumConcurrentRequest()
		{
			this._maximumConcurrentRequest = 10;
		}
		
		static partial void CoerceMaximumConcurrentRequestValue( ref int value );

		private EndPoint _bindingEndPoint = null;
		
		/// <summary>
		/// 	Gets or sets the local end point to be bound.
		/// </summary>
		/// <value>
		/// 	The local end point to be bound. The default is <c>null</c>. The server will select appropriate version IP and bind to it with port 0.
		/// </value>
		public EndPoint BindingEndPoint
		{
			get
			{
				Contract.Ensures( Contract.Result<EndPoint>() != null );

				return this._bindingEndPoint;
			}
			set
			{
				if ( !( value != null ) )
				{
					throw new ArgumentNullException( "value" );
				}

				Contract.Ensures( Contract.Result<EndPoint>() != null );

				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceBindingEndPointValue( ref coerced );
				this._bindingEndPoint = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the BindingEndPoint property value.
		/// </summary>
		public void ResetBindingEndPoint()
		{
			this._bindingEndPoint = null;
		}
		
		static partial void CoerceBindingEndPointValue( ref EndPoint value );

		private int _listenBackLog = 100;
		
		/// <summary>
		/// 	Gets or sets the listen back log of each sockets.
		/// </summary>
		/// <value>
		/// 	The listen back log of each sockets. The default is 100.
		/// </value>
		public int ListenBackLog
		{
			get
			{
				Contract.Ensures( Contract.Result<int>() >= default( int ) );

				return this._listenBackLog;
			}
			set
			{
				if ( !( value >= default( int ) ) )
				{
					throw new ArgumentOutOfRangeException( "value", "Argument cannot be negative number." );
				}

				Contract.Ensures( Contract.Result<int>() >= default( int ) );

				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceListenBackLogValue( ref coerced );
				this._listenBackLog = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the ListenBackLog property value.
		/// </summary>
		public void ResetListenBackLog()
		{
			this._listenBackLog = 100;
		}
		
		static partial void CoerceListenBackLogValue( ref int value );

		private TimeSpan? _executionTimeout = TimeSpan.FromSeconds( 110 );
		
		/// <summary>
		/// 	Gets or sets the timeout value to execute server thread.
		/// </summary>
		/// <value>
		/// 	The timeout value to execute server thread. The default is 110 seconds. <c>null</c> means inifinite timeout.
		/// </value>
		public TimeSpan? ExecutionTimeout
		{
			get
			{
				Contract.Ensures( Contract.Result<TimeSpan?>() == null || Contract.Result<TimeSpan?>().Value > default( TimeSpan ) );

				return this._executionTimeout;
			}
			set
			{
				if ( !( value == null || value.Value > default( TimeSpan ) ) )
				{
					throw new ArgumentOutOfRangeException( "value", "Argument must be positive number." );
				}

				Contract.Ensures( Contract.Result<TimeSpan?>() == null || Contract.Result<TimeSpan?>().Value > default( TimeSpan ) );

				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceExecutionTimeoutValue( ref coerced );
				this._executionTimeout = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the ExecutionTimeout property value.
		/// </summary>
		public void ResetExecutionTimeout()
		{
			this._executionTimeout = TimeSpan.FromSeconds( 110 );
		}
		
		static partial void CoerceExecutionTimeoutValue( ref TimeSpan? value );

		private TimeSpan? _hardExecutionTimeout = TimeSpan.FromSeconds( 20 );
		
		/// <summary>
		/// 	Gets or sets the timeout value to abort server thread after graceful timeout is occurred.
		/// </summary>
		/// <value>
		/// 	The timeout value to abort server thread after graceful timeout is occurred. The default is 20 seconds. <c>null</c> means inifinite timeout.
		/// </value>
		public TimeSpan? HardExecutionTimeout
		{
			get
			{
				Contract.Ensures( Contract.Result<TimeSpan?>() == null || Contract.Result<TimeSpan?>().Value >= default( TimeSpan ) );

				return this._hardExecutionTimeout;
			}
			set
			{
				if ( !( value == null || value.Value >= default( TimeSpan ) ) )
				{
					throw new ArgumentOutOfRangeException( "value", "Argument cannot be negative number." );
				}

				Contract.Ensures( Contract.Result<TimeSpan?>() == null || Contract.Result<TimeSpan?>().Value >= default( TimeSpan ) );

				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceHardExecutionTimeoutValue( ref coerced );
				this._hardExecutionTimeout = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the HardExecutionTimeout property value.
		/// </summary>
		public void ResetHardExecutionTimeout()
		{
			this._hardExecutionTimeout = TimeSpan.FromSeconds( 20 );
		}
		
		static partial void CoerceHardExecutionTimeoutValue( ref TimeSpan? value );

		private Func<RpcServer, ServerTransportManager> _transportManagerProvider = ( server ) => new TcpServerTransportManager( server );
		
		/// <summary>
		/// 	Gets or sets the factory function which creates new <see cref="ServerTransportManager" />.
		/// </summary>
		/// <value>
		/// 	The factory function which creates new <see cref="ServerTransportManager" />. The default is the delegate which creates <see cref="TcpServerTransportManager" /> instance.
		/// </value>
		public Func<RpcServer, ServerTransportManager> TransportManagerProvider
		{
			get
			{
				Contract.Ensures( Contract.Result<Func<RpcServer, ServerTransportManager>>() != null );

				return this._transportManagerProvider;
			}
			set
			{
				if ( !( value != null ) )
				{
					throw new ArgumentNullException( "value" );
				}

				Contract.Ensures( Contract.Result<Func<RpcServer, ServerTransportManager>>() != null );

				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceTransportManagerProviderValue( ref coerced );
				this._transportManagerProvider = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the TransportManagerProvider property value.
		/// </summary>
		public void ResetTransportManagerProvider()
		{
			this._transportManagerProvider = ( server ) => new TcpServerTransportManager( server );
		}
		
		static partial void CoerceTransportManagerProviderValue( ref Func<RpcServer, ServerTransportManager> value );

		private Func<RpcServer, Dispatcher> _dispatcherProvider = ( server ) => new LocatorBasedDispatcher( server );
		
		/// <summary>
		/// 	Gets or sets the factory function which creates new <see cref="Dispatcher" />.
		/// </summary>
		/// <value>
		/// 	The factory function which creates new <see cref="Dispatcher" />. The default is the delegate which creates <see cref="LocatorBasedDispatcher" /> instance.
		/// </value>
		public Func<RpcServer, Dispatcher> DispatcherProvider
		{
			get
			{
				Contract.Ensures( Contract.Result<Func<RpcServer, Dispatcher>>() != null );

				return this._dispatcherProvider;
			}
			set
			{
				if ( !( value != null ) )
				{
					throw new ArgumentNullException( "value" );
				}

				Contract.Ensures( Contract.Result<Func<RpcServer, Dispatcher>>() != null );

				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceDispatcherProviderValue( ref coerced );
				this._dispatcherProvider = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the DispatcherProvider property value.
		/// </summary>
		public void ResetDispatcherProvider()
		{
			this._dispatcherProvider = ( server ) => new LocatorBasedDispatcher( server );
		}
		
		static partial void CoerceDispatcherProviderValue( ref Func<RpcServer, Dispatcher> value );

		private Func<RpcServerConfiguration, ServiceTypeLocator> _serviceTypeLocatorProvider = ( config ) => new DefaultServiceTypeLocator();
		
		/// <summary>
		/// 	Gets or sets the factory function which creates new <see cref="ServiceTypeLocator" />.
		/// </summary>
		/// <value>
		/// 	The factory function which creates new <see cref="ServiceTypeLocator" />. The default is the delegate which creates <see cref="DefaultServiceTypeLocator" /> instance.
		/// </value>
		public Func<RpcServerConfiguration, ServiceTypeLocator> ServiceTypeLocatorProvider
		{
			get
			{
				Contract.Ensures( Contract.Result<Func<RpcServerConfiguration, ServiceTypeLocator>>() != null );

				return this._serviceTypeLocatorProvider;
			}
			set
			{
				if ( !( value != null ) )
				{
					throw new ArgumentNullException( "value" );
				}

				Contract.Ensures( Contract.Result<Func<RpcServerConfiguration, ServiceTypeLocator>>() != null );

				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceServiceTypeLocatorProviderValue( ref coerced );
				this._serviceTypeLocatorProvider = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the ServiceTypeLocatorProvider property value.
		/// </summary>
		public void ResetServiceTypeLocatorProvider()
		{
			this._serviceTypeLocatorProvider = ( config ) => new DefaultServiceTypeLocator();
		}
		
		static partial void CoerceServiceTypeLocatorProviderValue( ref Func<RpcServerConfiguration, ServiceTypeLocator> value );

		private bool _isDebugMode = false;
		
		/// <summary>
		/// 	Gets or sets whether the server is in debug mode.
		/// </summary>
		/// <value>
		/// 	<c>true</c>, the server is in debug mode; otherwise, <c>false</c>. The default is <c>false</c>.
		/// </value>
		/// <remarks>
		/// 	When the server is in debug mode, the error message contains debugging information includes underlying exception string.
		/// </remarks>
		public bool IsDebugMode
		{
			get
			{
				return this._isDebugMode;
			}
			set
			{
				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceIsDebugModeValue( ref coerced );
				this._isDebugMode = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the IsDebugMode property value.
		/// </summary>
		public void ResetIsDebugMode()
		{
			this._isDebugMode = false;
		}
		
		static partial void CoerceIsDebugModeValue( ref bool value );

		private Func<Func<ServerRequestContext>, ObjectPoolConfiguration, ObjectPool<ServerRequestContext>> _requestContextPoolProvider = ( factory, configuration ) => new StandardObjectPool<ServerRequestContext>( factory, configuration );
		
		/// <summary>
		/// 	Gets or sets the factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="ServerRequestContext" />.
		/// </summary>
		/// <value>
		/// 	The factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="ServerRequestContext" />. The default is the delegate which creates <see cref="StandardObjectPool{T}" /> instance with <c>null</c> configuration.
		/// </value>
		public Func<Func<ServerRequestContext>, ObjectPoolConfiguration, ObjectPool<ServerRequestContext>> RequestContextPoolProvider
		{
			get
			{
				Contract.Ensures( Contract.Result<Func<Func<ServerRequestContext>, ObjectPoolConfiguration, ObjectPool<ServerRequestContext>>>() != null );

				return this._requestContextPoolProvider;
			}
			set
			{
				if ( !( value != null ) )
				{
					throw new ArgumentNullException( "value" );
				}

				Contract.Ensures( Contract.Result<Func<Func<ServerRequestContext>, ObjectPoolConfiguration, ObjectPool<ServerRequestContext>>>() != null );

				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceRequestContextPoolProviderValue( ref coerced );
				this._requestContextPoolProvider = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the RequestContextPoolProvider property value.
		/// </summary>
		public void ResetRequestContextPoolProvider()
		{
			this._requestContextPoolProvider = ( factory, configuration ) => new StandardObjectPool<ServerRequestContext>( factory, configuration );
		}
		
		static partial void CoerceRequestContextPoolProviderValue( ref Func<Func<ServerRequestContext>, ObjectPoolConfiguration, ObjectPool<ServerRequestContext>> value );

		private Func<Func<ServerResponseContext>, ObjectPoolConfiguration, ObjectPool<ServerResponseContext>> _responseContextPoolProvider = ( factory, configuration ) => new StandardObjectPool<ServerResponseContext>( factory, configuration );
		
		/// <summary>
		/// 	Gets or sets the factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="ServerResponseContext" />.
		/// </summary>
		/// <value>
		/// 	The factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="ServerResponseContext" />. The default is the delegate which creates <see cref="StandardObjectPool{T}" /> instance with <c>null</c> configuration.
		/// </value>
		public Func<Func<ServerResponseContext>, ObjectPoolConfiguration, ObjectPool<ServerResponseContext>> ResponseContextPoolProvider
		{
			get
			{
				Contract.Ensures( Contract.Result<Func<Func<ServerResponseContext>, ObjectPoolConfiguration, ObjectPool<ServerResponseContext>>>() != null );

				return this._responseContextPoolProvider;
			}
			set
			{
				if ( !( value != null ) )
				{
					throw new ArgumentNullException( "value" );
				}

				Contract.Ensures( Contract.Result<Func<Func<ServerResponseContext>, ObjectPoolConfiguration, ObjectPool<ServerResponseContext>>>() != null );

				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceResponseContextPoolProviderValue( ref coerced );
				this._responseContextPoolProvider = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the ResponseContextPoolProvider property value.
		/// </summary>
		public void ResetResponseContextPoolProvider()
		{
			this._responseContextPoolProvider = ( factory, configuration ) => new StandardObjectPool<ServerResponseContext>( factory, configuration );
		}
		
		static partial void CoerceResponseContextPoolProviderValue( ref Func<Func<ServerResponseContext>, ObjectPoolConfiguration, ObjectPool<ServerResponseContext>> value );

		private Func<Func<ListeningContext>, ObjectPoolConfiguration, ObjectPool<ListeningContext>> _listeningContextPoolProvider = ( factory, configuration ) => new StandardObjectPool<ListeningContext>( factory, configuration );
		
		/// <summary>
		/// 	Gets or sets the factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="ListeningContext" />.
		/// </summary>
		/// <value>
		/// 	The factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="ListeningContext" />. The default is the delegate which creates <see cref="StandardObjectPool{T}" /> instance with <c>null</c> configuration.
		/// </value>
		public Func<Func<ListeningContext>, ObjectPoolConfiguration, ObjectPool<ListeningContext>> ListeningContextPoolProvider
		{
			get
			{
				Contract.Ensures( Contract.Result<Func<Func<ListeningContext>, ObjectPoolConfiguration, ObjectPool<ListeningContext>>>() != null );

				return this._listeningContextPoolProvider;
			}
			set
			{
				if ( !( value != null ) )
				{
					throw new ArgumentNullException( "value" );
				}

				Contract.Ensures( Contract.Result<Func<Func<ListeningContext>, ObjectPoolConfiguration, ObjectPool<ListeningContext>>>() != null );

				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceListeningContextPoolProviderValue( ref coerced );
				this._listeningContextPoolProvider = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the ListeningContextPoolProvider property value.
		/// </summary>
		public void ResetListeningContextPoolProvider()
		{
			this._listeningContextPoolProvider = ( factory, configuration ) => new StandardObjectPool<ListeningContext>( factory, configuration );
		}
		
		static partial void CoerceListeningContextPoolProviderValue( ref Func<Func<ListeningContext>, ObjectPoolConfiguration, ObjectPool<ListeningContext>> value );

		private Func<Func<TcpServerTransport>, ObjectPoolConfiguration, ObjectPool<TcpServerTransport>> _tcpTransportPoolProvider = ( factory, configuration ) => new StandardObjectPool<TcpServerTransport>( factory, configuration );
		
		/// <summary>
		/// 	Gets or sets the factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="TcpServerTransport" />.
		/// </summary>
		/// <value>
		/// 	The factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="TcpServerTransport" />. The default is the delegate which creates <see cref="StandardObjectPool{T}" /> instance with <c>null</c> configuration.
		/// </value>
		public Func<Func<TcpServerTransport>, ObjectPoolConfiguration, ObjectPool<TcpServerTransport>> TcpTransportPoolProvider
		{
			get
			{
				Contract.Ensures( Contract.Result<Func<Func<TcpServerTransport>, ObjectPoolConfiguration, ObjectPool<TcpServerTransport>>>() != null );

				return this._tcpTransportPoolProvider;
			}
			set
			{
				if ( !( value != null ) )
				{
					throw new ArgumentNullException( "value" );
				}

				Contract.Ensures( Contract.Result<Func<Func<TcpServerTransport>, ObjectPoolConfiguration, ObjectPool<TcpServerTransport>>>() != null );

				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceTcpTransportPoolProviderValue( ref coerced );
				this._tcpTransportPoolProvider = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the TcpTransportPoolProvider property value.
		/// </summary>
		public void ResetTcpTransportPoolProvider()
		{
			this._tcpTransportPoolProvider = ( factory, configuration ) => new StandardObjectPool<TcpServerTransport>( factory, configuration );
		}
		
		static partial void CoerceTcpTransportPoolProviderValue( ref Func<Func<TcpServerTransport>, ObjectPoolConfiguration, ObjectPool<TcpServerTransport>> value );

		private Func<Func<UdpServerTransport>, ObjectPoolConfiguration, ObjectPool<UdpServerTransport>> _udpTransportPoolProvider = ( factory, configuration ) => new StandardObjectPool<UdpServerTransport>( factory, configuration );
		
		/// <summary>
		/// 	Gets or sets the factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="UdpServerTransport" />.
		/// </summary>
		/// <value>
		/// 	The factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="UdpServerTransport" />. The default is the delegate which creates <see cref="StandardObjectPool{T}" /> instance with <c>null</c> configuration.
		/// </value>
		public Func<Func<UdpServerTransport>, ObjectPoolConfiguration, ObjectPool<UdpServerTransport>> UdpTransportPoolProvider
		{
			get
			{
				Contract.Ensures( Contract.Result<Func<Func<UdpServerTransport>, ObjectPoolConfiguration, ObjectPool<UdpServerTransport>>>() != null );

				return this._udpTransportPoolProvider;
			}
			set
			{
				if ( !( value != null ) )
				{
					throw new ArgumentNullException( "value" );
				}

				Contract.Ensures( Contract.Result<Func<Func<UdpServerTransport>, ObjectPoolConfiguration, ObjectPool<UdpServerTransport>>>() != null );

				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceUdpTransportPoolProviderValue( ref coerced );
				this._udpTransportPoolProvider = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the UdpTransportPoolProvider property value.
		/// </summary>
		public void ResetUdpTransportPoolProvider()
		{
			this._udpTransportPoolProvider = ( factory, configuration ) => new StandardObjectPool<UdpServerTransport>( factory, configuration );
		}
		
		static partial void CoerceUdpTransportPoolProviderValue( ref Func<Func<UdpServerTransport>, ObjectPoolConfiguration, ObjectPool<UdpServerTransport>> value );

		/// <summary>
		/// 	Returns a string that represents the current object.
		/// </summary>
		/// <returns>
		/// 	A string that represents the current object.
		/// </returns>
		public sealed override string ToString()
		{
			var buffer = new StringBuilder( 4096 );
			buffer.Append( "{ " );
			buffer.Append( "\"PreferIPv4\" : " );
			ToString( this.PreferIPv4, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"MinimumConnection\" : " );
			ToString( this.MinimumConnection, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"MaximumConnection\" : " );
			ToString( this.MaximumConnection, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"MinimumConcurrentRequest\" : " );
			ToString( this.MinimumConcurrentRequest, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"MaximumConcurrentRequest\" : " );
			ToString( this.MaximumConcurrentRequest, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"BindingEndPoint\" : " );
			ToString( this.BindingEndPoint, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"ListenBackLog\" : " );
			ToString( this.ListenBackLog, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"ExecutionTimeout\" : " );
			ToString( this.ExecutionTimeout, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"HardExecutionTimeout\" : " );
			ToString( this.HardExecutionTimeout, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"TransportManagerProvider\" : " );
			ToString( this.TransportManagerProvider, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"DispatcherProvider\" : " );
			ToString( this.DispatcherProvider, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"ServiceTypeLocatorProvider\" : " );
			ToString( this.ServiceTypeLocatorProvider, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"IsDebugMode\" : " );
			ToString( this.IsDebugMode, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"RequestContextPoolProvider\" : " );
			ToString( this.RequestContextPoolProvider, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"ResponseContextPoolProvider\" : " );
			ToString( this.ResponseContextPoolProvider, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"ListeningContextPoolProvider\" : " );
			ToString( this.ListeningContextPoolProvider, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"TcpTransportPoolProvider\" : " );
			ToString( this.TcpTransportPoolProvider, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"UdpTransportPoolProvider\" : " );
			ToString( this.UdpTransportPoolProvider, buffer );
			buffer.Append( " }" );
			return buffer.ToString();
		}
		
		private static void ToString<T>( T value, StringBuilder buffer )
		{
			if( value == null )
			{
				buffer.Append( "null" );
			}
			
			if( typeof( Delegate ).IsAssignableFrom( typeof( T ) ) )
			{
				var asDelegate = ( Delegate )( object )value;
				buffer.Append( "\"Type='" ).Append( asDelegate.Method.DeclaringType );

				if( asDelegate.Target != null )
				{
					buffer.Append( "', Instance='" ).Append( asDelegate.Target );
				}

				buffer.Append( "', Method='" ).Append( asDelegate.Method ).Append( "'\"" );
				return;
			}

			switch( Type.GetTypeCode( typeof( T ) ) )
			{
				case TypeCode.Boolean:
				{
					buffer.Append( value.ToString().ToLowerInvariant() );
					break;
				}
				case TypeCode.Byte:
				case TypeCode.Double:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.SByte:
				case TypeCode.Single:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				{
					buffer.Append( value.ToString() );
					break;
				}
				default:
				{
					buffer.Append( '"' ).Append( value.ToString() ).Append( '"' );
					break;
				}
			}
		}
	}
}
