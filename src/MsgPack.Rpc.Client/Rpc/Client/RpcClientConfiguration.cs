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
using System.Net;
using MsgPack.Rpc.Client.Protocols;

namespace MsgPack.Rpc.Client
{
	public sealed partial class RpcClientConfiguration : FreezableObject
	{
		private static readonly RpcClientConfiguration _default = new RpcClientConfiguration().Freeze();

		/// <summary>
		///		Gets the default frozen instance.
		/// </summary>
		/// <value>
		///		The default frozen instance.
		///		This value will not be <c>null</c>.
		/// </value>
		public static RpcClientConfiguration Default
		{
			get { return RpcClientConfiguration._default; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RpcClientConfiguration"/> class.
		/// </summary>
		public RpcClientConfiguration() { }

		public ObjectPoolConfiguration CreateTcpTransportPoolConfiguration()
		{
			return new ObjectPoolConfiguration() { ExhausionPolicy = ExhausionPolicy.BlockUntilAvailable, MaximumPooled = this.MaximumConcurrentRequest, MinimumReserved = this.MinimumConcurrentRequest };
		}

		public ObjectPoolConfiguration CreateUdpTransportPoolConfiguration()
		{
			return new ObjectPoolConfiguration() { ExhausionPolicy = ExhausionPolicy.BlockUntilAvailable, MaximumPooled = this.MaximumConcurrentRequest, MinimumReserved = this.MinimumConcurrentRequest };
		}

		public ObjectPoolConfiguration CreateRequestContextPoolConfiguration()
		{
			return new ObjectPoolConfiguration() { ExhausionPolicy = ExhausionPolicy.BlockUntilAvailable, MaximumPooled = this.MaximumConcurrentRequest, MinimumReserved = this.MinimumConcurrentRequest };
		}

		public ObjectPoolConfiguration CreateResponseContextPoolConfiguration()
		{
			return new ObjectPoolConfiguration() { ExhausionPolicy = ExhausionPolicy.BlockUntilAvailable, MaximumPooled = this.MaximumConcurrentRequest, MinimumReserved = this.MinimumConcurrentRequest };
		}

		static partial void ValidateRequestContextPoolProvider( Func<Func<ClientRequestContext>, ObjectPoolConfiguration, ObjectPool<ClientRequestContext>> value )
		{
			if ( value == null )
			{
				throw new ArgumentNullException( "value" );
			}
		}

		static partial void ValidateResponseContextPoolProvider( Func<Func<ClientResponseContext>, ObjectPoolConfiguration, ObjectPool<ClientResponseContext>> value )
		{
			if ( value == null )
			{
				throw new ArgumentNullException( "value" );
			}
		}

		static partial void ValidateConnectTimeout( TimeSpan? value )
		{
			if ( value != null && value.Value.Ticks < 0 )
			{
				throw new ArgumentOutOfRangeException( "value" );
			}
		}

		static partial void ValidateWaitTimeout( TimeSpan? value )
		{
			if ( value != null && value.Value.Ticks < 0 )
			{
				throw new ArgumentOutOfRangeException( "value" );
			}
		}

		static partial void ValidateTransportManagerProvider( Func<RpcClientConfiguration, ClientTransportManager> value )
		{
			if ( value == null )
			{
				throw new ArgumentNullException( "value" );
			}
		}

		static partial void ValidateTcpTransportPoolProvider( Func<Func<TcpClientTransport>, ObjectPoolConfiguration, ObjectPool<TcpClientTransport>> value )
		{
			if ( value == null )
			{
				throw new ArgumentNullException( "value" );
			}
		}

		static partial void ValidateUdpTransportPoolProvider( Func<Func<UdpClientTransport>, ObjectPoolConfiguration, ObjectPool<UdpClientTransport>> value )
		{
			if ( value == null )
			{
				throw new ArgumentNullException( "value" );
			}
		}

		static partial void ValidateMaximumConcurrentRequest( int value )
		{
			if ( value < 1 )
			{
				throw new ArgumentOutOfRangeException( "MaximumConcurrentRequest must not be negative nor 0.", "value" );
			}
		}

		static partial void ValidateMinimumConcurrentRequest( int value )
		{
			if ( value < 0 )
			{
				throw new ArgumentOutOfRangeException( "MinimumConcurrentRequest must not be negative.", "value" );
			}
		}

		static partial void ValidateTargetEndPoint( EndPoint value )
		{
			if ( value == null )
			{
				throw new ArgumentNullException( "value" );
			}
		}

		/// <summary>
		///		Clones all of the fields of this instance.
		/// </summary>
		/// <returns>
		///		The shallow copy of this instance.
		/// </returns>
		public RpcClientConfiguration Clone()
		{
			return this.CloneCore() as RpcClientConfiguration;
		}

		/// <summary>
		///		Freezes this instance.
		/// </summary>
		/// <returns>
		///		This instance.
		/// </returns>
		public RpcClientConfiguration Freeze()
		{
			return this.FreezeCore() as RpcClientConfiguration;
		}

		/// <summary>
		/// Gets the frozen copy of this instance.
		/// </summary>
		/// <returns>
		/// This instance if it is already frozen.
		/// Otherwise, frozen copy of this instance.
		/// </returns>
		public RpcClientConfiguration AsFrozen()
		{
			return this.AsFrozenCore() as RpcClientConfiguration;
		}
	}
}
