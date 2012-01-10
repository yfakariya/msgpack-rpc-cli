﻿
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
using System.Text;
using MsgPack.Rpc.Client.Protocols;

namespace MsgPack.Rpc.Client
{
	// This file generated from RpcClientConfiguration.tt T4Template.
	// Do not modify this file. Edit RpcClientConfiguration.tt instead.

	partial class RpcClientConfiguration
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
				Contract.EndContractBlock();

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
				Contract.EndContractBlock();

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

		private EndPoint _targetEndPoint = null;
		
		/// <summary>
		/// 	Gets or sets the local end point to be bound.
		/// </summary>
		/// <value>
		/// 	The target end point. The default is <c>null</c>. The client will select appropriate version loopback IP address and bind to it with port 10912.
		/// </value>
		public EndPoint TargetEndPoint
		{
			get
			{
				Contract.Ensures( Contract.Result<EndPoint>() != null );

				return this._targetEndPoint;
			}
			set
			{
				if ( !( value != null ) )
				{
					throw new ArgumentNullException( "value" );
				}
				Contract.EndContractBlock();

				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceTargetEndPointValue( ref coerced );
				this._targetEndPoint = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the TargetEndPoint property value.
		/// </summary>
		public void ResetTargetEndPoint()
		{
			this._targetEndPoint = null;
		}
		
		static partial void CoerceTargetEndPointValue( ref EndPoint value );

		private TimeSpan? _connectTimeout = TimeSpan.FromSeconds( 120 );
		
		/// <summary>
		/// 	Gets or sets the timeout value to connect.
		/// </summary>
		/// <value>
		/// 	The timeout value to connect. The default is 120 seconds. <c>null<c> means inifinite timeout.
		/// </value>
		public TimeSpan? ConnectTimeout
		{
			get
			{
				Contract.Ensures( Contract.Result<TimeSpan?>() == null || Contract.Result<TimeSpan?>().Value > default( TimeSpan ) );

				return this._connectTimeout;
			}
			set
			{
				if ( !( value == null || value.Value > default( TimeSpan ) ) )
				{
					throw new ArgumentOutOfRangeException( "value", "Argument must be positive number." );
				}
				Contract.EndContractBlock();

				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceConnectTimeoutValue( ref coerced );
				this._connectTimeout = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the ConnectTimeout property value.
		/// </summary>
		public void ResetConnectTimeout()
		{
			this._connectTimeout = TimeSpan.FromSeconds( 120 );
		}
		
		static partial void CoerceConnectTimeoutValue( ref TimeSpan? value );

		private TimeSpan? _waitTimeout = TimeSpan.FromSeconds( 120 );
		
		/// <summary>
		/// 	Gets or sets the timeout value to wait response.
		/// </summary>
		/// <value>
		/// 	The timeout value to wait response. The default is 120 seconds. <c>null<c> means inifinite timeout.
		/// </value>
		public TimeSpan? WaitTimeout
		{
			get
			{
				Contract.Ensures( Contract.Result<TimeSpan?>() == null || Contract.Result<TimeSpan?>().Value > default( TimeSpan ) );

				return this._waitTimeout;
			}
			set
			{
				if ( !( value == null || value.Value > default( TimeSpan ) ) )
				{
					throw new ArgumentOutOfRangeException( "value", "Argument must be positive number." );
				}
				Contract.EndContractBlock();

				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceWaitTimeoutValue( ref coerced );
				this._waitTimeout = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the WaitTimeout property value.
		/// </summary>
		public void ResetWaitTimeout()
		{
			this._waitTimeout = TimeSpan.FromSeconds( 120 );
		}
		
		static partial void CoerceWaitTimeoutValue( ref TimeSpan? value );

		private Func<RpcClientConfiguration, ClientTransportManager> _transportManagerProvider = ( configuration ) => new TcpClientTransportManager( configuration );
		
		/// <summary>
		/// 	Gets or sets the factory function which creates new <see cref="ClientTransportManager" />.
		/// </summary>
		/// <value>
		/// 	The factory function which creates new <see cref="ClientTransportManager" />. The default is the delegate which creates <see cref="TcpClientTransportManager" /> instance.
		/// </value>
		public Func<RpcClientConfiguration, ClientTransportManager> TransportManagerProvider
		{
			get
			{
				Contract.Ensures( Contract.Result<Func<RpcClientConfiguration, ClientTransportManager>>() != null );

				return this._transportManagerProvider;
			}
			set
			{
				if ( !( value != null ) )
				{
					throw new ArgumentNullException( "value" );
				}
				Contract.EndContractBlock();

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
			this._transportManagerProvider = ( configuration ) => new TcpClientTransportManager( configuration );
		}
		
		static partial void CoerceTransportManagerProviderValue( ref Func<RpcClientConfiguration, ClientTransportManager> value );

		private Func<Func<ClientRequestContext>, ObjectPoolConfiguration, ObjectPool<ClientRequestContext>> _requestContextPoolProvider = ( factory, configuration ) => new StandardObjectPool<ClientRequestContext>( factory, configuration );
		
		/// <summary>
		/// 	Gets or sets the factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="ClientRequestContext" />.
		/// </summary>
		/// <value>
		/// 	The factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="ClientRequestContext" />. The default is the delegate which creates <see cref="StandardObjectPool{T}" /> instance with <c>null</c> configuration.
		/// </value>
		public Func<Func<ClientRequestContext>, ObjectPoolConfiguration, ObjectPool<ClientRequestContext>> RequestContextPoolProvider
		{
			get
			{
				Contract.Ensures( Contract.Result<Func<Func<ClientRequestContext>, ObjectPoolConfiguration, ObjectPool<ClientRequestContext>>>() != null );

				return this._requestContextPoolProvider;
			}
			set
			{
				if ( !( value != null ) )
				{
					throw new ArgumentNullException( "value" );
				}
				Contract.EndContractBlock();

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
			this._requestContextPoolProvider = ( factory, configuration ) => new StandardObjectPool<ClientRequestContext>( factory, configuration );
		}
		
		static partial void CoerceRequestContextPoolProviderValue( ref Func<Func<ClientRequestContext>, ObjectPoolConfiguration, ObjectPool<ClientRequestContext>> value );

		private Func<Func<ClientResponseContext>, ObjectPoolConfiguration, ObjectPool<ClientResponseContext>> _responseContextPoolProvider = ( factory, configuration ) => new StandardObjectPool<ClientResponseContext>( factory, configuration );
		
		/// <summary>
		/// 	Gets or sets the factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="ClientResponseContext" />.
		/// </summary>
		/// <value>
		/// 	The factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="ClientResponseContext" />. The default is the delegate which creates <see cref="StandardObjectPool{T}" /> instance with <c>null</c> configuration.
		/// </value>
		public Func<Func<ClientResponseContext>, ObjectPoolConfiguration, ObjectPool<ClientResponseContext>> ResponseContextPoolProvider
		{
			get
			{
				Contract.Ensures( Contract.Result<Func<Func<ClientResponseContext>, ObjectPoolConfiguration, ObjectPool<ClientResponseContext>>>() != null );

				return this._responseContextPoolProvider;
			}
			set
			{
				if ( !( value != null ) )
				{
					throw new ArgumentNullException( "value" );
				}
				Contract.EndContractBlock();

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
			this._responseContextPoolProvider = ( factory, configuration ) => new StandardObjectPool<ClientResponseContext>( factory, configuration );
		}
		
		static partial void CoerceResponseContextPoolProviderValue( ref Func<Func<ClientResponseContext>, ObjectPoolConfiguration, ObjectPool<ClientResponseContext>> value );

		private Func<Func<TcpClientTransport>, ObjectPoolConfiguration, ObjectPool<TcpClientTransport>> _tcpTransportPoolProvider = ( factory, configuration ) => new StandardObjectPool<TcpClientTransport>( factory, configuration );
		
		/// <summary>
		/// 	Gets or sets the factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="TcpClientTransport" />.
		/// </summary>
		/// <value>
		/// 	The factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="TcpClientTransport" />. The default is the delegate which creates <see cref="StandardObjectPool{T}" /> instance with <c>null</c> configuration.
		/// </value>
		public Func<Func<TcpClientTransport>, ObjectPoolConfiguration, ObjectPool<TcpClientTransport>> TcpTransportPoolProvider
		{
			get
			{
				Contract.Ensures( Contract.Result<Func<Func<TcpClientTransport>, ObjectPoolConfiguration, ObjectPool<TcpClientTransport>>>() != null );

				return this._tcpTransportPoolProvider;
			}
			set
			{
				if ( !( value != null ) )
				{
					throw new ArgumentNullException( "value" );
				}
				Contract.EndContractBlock();

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
			this._tcpTransportPoolProvider = ( factory, configuration ) => new StandardObjectPool<TcpClientTransport>( factory, configuration );
		}
		
		static partial void CoerceTcpTransportPoolProviderValue( ref Func<Func<TcpClientTransport>, ObjectPoolConfiguration, ObjectPool<TcpClientTransport>> value );

		private Func<Func<UdpClientTransport>, ObjectPoolConfiguration, ObjectPool<UdpClientTransport>> _udpTransportPoolProvider = ( factory, configuration ) => new StandardObjectPool<UdpClientTransport>( factory, configuration );
		
		/// <summary>
		/// 	Gets or sets the factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="UdpClientTransport" />.
		/// </summary>
		/// <value>
		/// 	The factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="UdpClientTransport" />. The default is the delegate which creates <see cref="StandardObjectPool{T}" /> instance with <c>null</c> configuration.
		/// </value>
		public Func<Func<UdpClientTransport>, ObjectPoolConfiguration, ObjectPool<UdpClientTransport>> UdpTransportPoolProvider
		{
			get
			{
				Contract.Ensures( Contract.Result<Func<Func<UdpClientTransport>, ObjectPoolConfiguration, ObjectPool<UdpClientTransport>>>() != null );

				return this._udpTransportPoolProvider;
			}
			set
			{
				if ( !( value != null ) )
				{
					throw new ArgumentNullException( "value" );
				}
				Contract.EndContractBlock();

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
			this._udpTransportPoolProvider = ( factory, configuration ) => new StandardObjectPool<UdpClientTransport>( factory, configuration );
		}
		
		static partial void CoerceUdpTransportPoolProviderValue( ref Func<Func<UdpClientTransport>, ObjectPoolConfiguration, ObjectPool<UdpClientTransport>> value );

		/// <summary>
		/// 	Returns a string that represents the current object.
		/// </summary>
		/// <returns>
		/// 	A string that represents the current object.
		/// </returns>
		public sealed override string ToString()
		{
			var buffer = new StringBuilder( 2048 );
			buffer.Append( "{ " );
			buffer.Append( "\"PreferIPv4\" : " );
			ToString( this.PreferIPv4, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"MinimumConcurrentRequest\" : " );
			ToString( this.MinimumConcurrentRequest, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"MaximumConcurrentRequest\" : " );
			ToString( this.MaximumConcurrentRequest, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"TargetEndPoint\" : " );
			ToString( this.TargetEndPoint, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"ConnectTimeout\" : " );
			ToString( this.ConnectTimeout, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"WaitTimeout\" : " );
			ToString( this.WaitTimeout, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"TransportManagerProvider\" : " );
			ToString( this.TransportManagerProvider, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"RequestContextPoolProvider\" : " );
			ToString( this.RequestContextPoolProvider, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"ResponseContextPoolProvider\" : " );
			ToString( this.ResponseContextPoolProvider, buffer );
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
