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
			get{ return this._preferIPv4; }
			set
			{
				this.VerifyIsNotFrozen();
				ValidatePreferIPv4( value );
				this._preferIPv4 = value;
			}
		}
		
		/// <summary>
		/// 	Resets the PreferIPv4 property value.
		/// </summary>
		public void ResetPreferIPv4()
		{
			this._preferIPv4 = false;
		}
		
		static partial void ValidatePreferIPv4( bool value );

		private int _minimumConcurrentRequest = 2;
		
		/// <summary>
		/// 	Gets or sets the minimum concurrency for the each clients.
		/// </summary>
		/// <value>
		/// 	The minimum concurrency for the each clients. The default is 2.
		/// </value>
		public int MinimumConcurrentRequest
		{
			get{ return this._minimumConcurrentRequest; }
			set
			{
				this.VerifyIsNotFrozen();
				ValidateMinimumConcurrentRequest( value );
				this._minimumConcurrentRequest = value;
			}
		}
		
		/// <summary>
		/// 	Resets the MinimumConcurrentRequest property value.
		/// </summary>
		public void ResetMinimumConcurrentRequest()
		{
			this._minimumConcurrentRequest = 2;
		}
		
		static partial void ValidateMinimumConcurrentRequest( int value );

		private int _maximumConcurrentRequest = 10;
		
		/// <summary>
		/// 	Gets or sets the maximum concurrency for the each clients.
		/// </summary>
		/// <value>
		/// 	The maximum concurrency for the each clients. The default is 10.
		/// </value>
		public int MaximumConcurrentRequest
		{
			get{ return this._maximumConcurrentRequest; }
			set
			{
				this.VerifyIsNotFrozen();
				ValidateMaximumConcurrentRequest( value );
				this._maximumConcurrentRequest = value;
			}
		}
		
		/// <summary>
		/// 	Resets the MaximumConcurrentRequest property value.
		/// </summary>
		public void ResetMaximumConcurrentRequest()
		{
			this._maximumConcurrentRequest = 10;
		}
		
		static partial void ValidateMaximumConcurrentRequest( int value );

		private EndPoint _targetEndPoint = null;
		
		/// <summary>
		/// 	Gets or sets the local end point to be bound.
		/// </summary>
		/// <value>
		/// 	The target end point. The default is <c>null</c>. The client will select appropriate version loopback IP address and bind to it with port 10912.
		/// </value>
		public EndPoint TargetEndPoint
		{
			get{ return this._targetEndPoint; }
			set
			{
				this.VerifyIsNotFrozen();
				ValidateTargetEndPoint( value );
				this._targetEndPoint = value;
			}
		}
		
		/// <summary>
		/// 	Resets the TargetEndPoint property value.
		/// </summary>
		public void ResetTargetEndPoint()
		{
			this._targetEndPoint = null;
		}
		
		static partial void ValidateTargetEndPoint( EndPoint value );

		private TimeSpan? _connectTimeout = TimeSpan.FromSeconds( 120 );
		
		/// <summary>
		/// 	Gets or sets the timeout value to connect.
		/// </summary>
		/// <value>
		/// 	The timeout value to connect. The default is 120 seconds. <c>null<c> means inifinite timeout.
		/// </value>
		public TimeSpan? ConnectTimeout
		{
			get{ return this._connectTimeout; }
			set
			{
				this.VerifyIsNotFrozen();
				ValidateConnectTimeout( value );
				this._connectTimeout = value;
			}
		}
		
		/// <summary>
		/// 	Resets the ConnectTimeout property value.
		/// </summary>
		public void ResetConnectTimeout()
		{
			this._connectTimeout = TimeSpan.FromSeconds( 120 );
		}
		
		static partial void ValidateConnectTimeout( TimeSpan? value );

		private TimeSpan? _waitTimeout = TimeSpan.FromSeconds( 120 );
		
		/// <summary>
		/// 	Gets or sets the timeout value to wait response.
		/// </summary>
		/// <value>
		/// 	The timeout value to wait response. The default is 120 seconds. <c>null<c> means inifinite timeout.
		/// </value>
		public TimeSpan? WaitTimeout
		{
			get{ return this._waitTimeout; }
			set
			{
				this.VerifyIsNotFrozen();
				ValidateWaitTimeout( value );
				this._waitTimeout = value;
			}
		}
		
		/// <summary>
		/// 	Resets the WaitTimeout property value.
		/// </summary>
		public void ResetWaitTimeout()
		{
			this._waitTimeout = TimeSpan.FromSeconds( 120 );
		}
		
		static partial void ValidateWaitTimeout( TimeSpan? value );

		private Func<RpcClientConfiguration, ClientTransportManager> _transportManagerProvider = ( configuration ) => new TcpClientTransportManager( configuration );
		
		/// <summary>
		/// 	Gets or sets the factory function which creates new <see cref="ClientTransportManager" />.
		/// </summary>
		/// <value>
		/// 	The factory function which creates new <see cref="ClientTransportManager" />. The default is the delegate which creates <see cref="TcpClientTransportManager" /> instance.
		/// </value>
		public Func<RpcClientConfiguration, ClientTransportManager> TransportManagerProvider
		{
			get{ return this._transportManagerProvider; }
			set
			{
				this.VerifyIsNotFrozen();
				ValidateTransportManagerProvider( value );
				this._transportManagerProvider = value;
			}
		}
		
		/// <summary>
		/// 	Resets the TransportManagerProvider property value.
		/// </summary>
		public void ResetTransportManagerProvider()
		{
			this._transportManagerProvider = ( configuration ) => new TcpClientTransportManager( configuration );
		}
		
		static partial void ValidateTransportManagerProvider( Func<RpcClientConfiguration, ClientTransportManager> value );

		private Func<Func<ClientRequestContext>, ObjectPoolConfiguration, ObjectPool<ClientRequestContext>> _requestContextPoolProvider = ( factory, configuration ) => new StandardObjectPool<ClientRequestContext>( factory, configuration );
		
		/// <summary>
		/// 	Gets or sets the factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="ClientRequestContext" />.
		/// </summary>
		/// <value>
		/// 	The factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="ClientRequestContext" />. The default is the delegate which creates <see cref="StandardObjectPool{T}" /> instance with <c>null</c> configuration.
		/// </value>
		public Func<Func<ClientRequestContext>, ObjectPoolConfiguration, ObjectPool<ClientRequestContext>> RequestContextPoolProvider
		{
			get{ return this._requestContextPoolProvider; }
			set
			{
				this.VerifyIsNotFrozen();
				ValidateRequestContextPoolProvider( value );
				this._requestContextPoolProvider = value;
			}
		}
		
		/// <summary>
		/// 	Resets the RequestContextPoolProvider property value.
		/// </summary>
		public void ResetRequestContextPoolProvider()
		{
			this._requestContextPoolProvider = ( factory, configuration ) => new StandardObjectPool<ClientRequestContext>( factory, configuration );
		}
		
		static partial void ValidateRequestContextPoolProvider( Func<Func<ClientRequestContext>, ObjectPoolConfiguration, ObjectPool<ClientRequestContext>> value );

		private Func<Func<ClientResponseContext>, ObjectPoolConfiguration, ObjectPool<ClientResponseContext>> _responseContextPoolProvider = ( factory, configuration ) => new StandardObjectPool<ClientResponseContext>( factory, configuration );
		
		/// <summary>
		/// 	Gets or sets the factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="ClientResponseContext" />.
		/// </summary>
		/// <value>
		/// 	The factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="ClientResponseContext" />. The default is the delegate which creates <see cref="StandardObjectPool{T}" /> instance with <c>null</c> configuration.
		/// </value>
		public Func<Func<ClientResponseContext>, ObjectPoolConfiguration, ObjectPool<ClientResponseContext>> ResponseContextPoolProvider
		{
			get{ return this._responseContextPoolProvider; }
			set
			{
				this.VerifyIsNotFrozen();
				ValidateResponseContextPoolProvider( value );
				this._responseContextPoolProvider = value;
			}
		}
		
		/// <summary>
		/// 	Resets the ResponseContextPoolProvider property value.
		/// </summary>
		public void ResetResponseContextPoolProvider()
		{
			this._responseContextPoolProvider = ( factory, configuration ) => new StandardObjectPool<ClientResponseContext>( factory, configuration );
		}
		
		static partial void ValidateResponseContextPoolProvider( Func<Func<ClientResponseContext>, ObjectPoolConfiguration, ObjectPool<ClientResponseContext>> value );

		private Func<Func<TcpClientTransport>, ObjectPoolConfiguration, ObjectPool<TcpClientTransport>> _tcpTransportPoolProvider = ( factory, configuration ) => new StandardObjectPool<TcpClientTransport>( factory, configuration );
		
		/// <summary>
		/// 	Gets or sets the factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="TcpClientTransport" />.
		/// </summary>
		/// <value>
		/// 	The factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="TcpClientTransport" />. The default is the delegate which creates <see cref="StandardObjectPool{T}" /> instance with <c>null</c> configuration.
		/// </value>
		public Func<Func<TcpClientTransport>, ObjectPoolConfiguration, ObjectPool<TcpClientTransport>> TcpTransportPoolProvider
		{
			get{ return this._tcpTransportPoolProvider; }
			set
			{
				this.VerifyIsNotFrozen();
				ValidateTcpTransportPoolProvider( value );
				this._tcpTransportPoolProvider = value;
			}
		}
		
		/// <summary>
		/// 	Resets the TcpTransportPoolProvider property value.
		/// </summary>
		public void ResetTcpTransportPoolProvider()
		{
			this._tcpTransportPoolProvider = ( factory, configuration ) => new StandardObjectPool<TcpClientTransport>( factory, configuration );
		}
		
		static partial void ValidateTcpTransportPoolProvider( Func<Func<TcpClientTransport>, ObjectPoolConfiguration, ObjectPool<TcpClientTransport>> value );

		private Func<Func<UdpClientTransport>, ObjectPoolConfiguration, ObjectPool<UdpClientTransport>> _udpTransportPoolProvider = ( factory, configuration ) => new StandardObjectPool<UdpClientTransport>( factory, configuration );
		
		/// <summary>
		/// 	Gets or sets the factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="UdpClientTransport" />.
		/// </summary>
		/// <value>
		/// 	The factory function which creates new <see cref="ObjectPool{T}" /> of <see cref="UdpClientTransport" />. The default is the delegate which creates <see cref="StandardObjectPool{T}" /> instance with <c>null</c> configuration.
		/// </value>
		public Func<Func<UdpClientTransport>, ObjectPoolConfiguration, ObjectPool<UdpClientTransport>> UdpTransportPoolProvider
		{
			get{ return this._udpTransportPoolProvider; }
			set
			{
				this.VerifyIsNotFrozen();
				ValidateUdpTransportPoolProvider( value );
				this._udpTransportPoolProvider = value;
			}
		}
		
		/// <summary>
		/// 	Resets the UdpTransportPoolProvider property value.
		/// </summary>
		public void ResetUdpTransportPoolProvider()
		{
			this._udpTransportPoolProvider = ( factory, configuration ) => new StandardObjectPool<UdpClientTransport>( factory, configuration );
		}
		
		static partial void ValidateUdpTransportPoolProvider( Func<Func<UdpClientTransport>, ObjectPoolConfiguration, ObjectPool<UdpClientTransport>> value );

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
