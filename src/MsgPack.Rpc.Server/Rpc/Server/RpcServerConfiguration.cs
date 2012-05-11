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
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using MsgPack.Rpc.Protocols.Filters;

[module: SuppressMessage( "Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Scope = "member", Target = "MsgPack.Rpc.Server.RpcServerConfiguration.#RequestContextPoolProvider" )]
[module: SuppressMessage( "Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Scope = "member", Target = "MsgPack.Rpc.Server.RpcServerConfiguration.#TcpTransportPoolProvider" )]
[module: SuppressMessage( "Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Scope = "member", Target = "MsgPack.Rpc.Server.RpcServerConfiguration.#ListeningContextPoolProvider" )]
[module: SuppressMessage( "Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Scope = "member", Target = "MsgPack.Rpc.Server.RpcServerConfiguration.#UdpTransportPoolProvider" )]
[module: SuppressMessage( "Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Scope = "member", Target = "MsgPack.Rpc.Server.RpcServerConfiguration.#ApplicationContextPoolProvider" )]
[module: SuppressMessage( "Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Scope = "member", Target = "MsgPack.Rpc.Server.RpcServerConfiguration.#ResponseContextPoolProvider" )]
[module: SuppressMessage( "Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Scope = "member", Target = "MsgPack.Rpc.Server.RpcServerConfiguration.#ToString`1(!!0,System.Text.StringBuilder)", Justification = "Boolean value should be lower case." )]

namespace MsgPack.Rpc.Server
{
	/// <summary>
	///		Represents server configuration.
	/// </summary>
	public sealed partial class RpcServerConfiguration : FreezableObject
	{
		private static readonly RpcServerConfiguration _default = new RpcServerConfiguration().Freeze();

		/// <summary>
		///		Gets the default frozen instance.
		/// </summary>
		/// <value>
		///		The default frozen instance.
		///		This value will not be <c>null</c>.
		/// </value>
		public static RpcServerConfiguration Default
		{
			get
			{
				Contract.Ensures( Contract.Result<RpcServerConfiguration>() != null ); 

				return RpcServerConfiguration._default;
			}
		}

		private IList<MessageFilterProvider> _filterProviders = new List<MessageFilterProvider>();

		/// <summary>
		///		Gets the filter providers collection.
		/// </summary>
		/// <value>
		///		The filter providers collection. Default is empty.
		/// </value>
		public IList<MessageFilterProvider> FilterProviders
		{
			get { return this._filterProviders; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RpcServerConfiguration"/> class.
		/// </summary>
		public RpcServerConfiguration() { }

		/// <summary>
		///		Creates the <see cref="ObjectPoolConfiguration"/> for the <see cref="MsgPack.Rpc.Server.Protocols.ListeningContext"/> pool corresponds to values of this instance.
		/// </summary>
		/// <returns>
		///		The <see cref="ObjectPoolConfiguration"/> for the <see cref="MsgPack.Rpc.Server.Protocols.ListeningContext"/> pool corresponds to values of this instance.
		///		This value will not be <c>null</c>.
		/// </returns>
		public ObjectPoolConfiguration CreateListeningContextPoolConfiguration()
		{
			Contract.Ensures( Contract.Result<ObjectPoolConfiguration>() != null );

			return new ObjectPoolConfiguration() { ExhausionPolicy = ExhausionPolicy.ThrowException, MaximumPooled = this.MaximumConnection, MinimumReserved = this.MinimumConnection };
		}

		/// <summary>
		///		Creates the <see cref="ObjectPoolConfiguration"/> for the transport pool corresponds to values of this instance.
		/// </summary>
		/// <returns>
		///		The <see cref="ObjectPoolConfiguration"/> for the transport pool corresponds to values of this instance.
		///		This value will not be <c>null</c>.
		/// </returns>
		public ObjectPoolConfiguration CreateTransportPoolConfiguration()
		{
			Contract.Ensures( Contract.Result<ObjectPoolConfiguration>() != null );

			return new ObjectPoolConfiguration() { ExhausionPolicy = ExhausionPolicy.BlockUntilAvailable, MaximumPooled = this.MaximumConcurrentRequest, MinimumReserved = this.MinimumConcurrentRequest };
		}

		/// <summary>
		///		Creates the <see cref="ObjectPoolConfiguration"/> for the RPC application context pool corresponds to values of this instance.
		/// </summary>
		/// <returns>
		///		The <see cref="ObjectPoolConfiguration"/> for the transport pool corresponds to values of this instance.
		///		This value will not be <c>null</c>.
		/// </returns>
		public ObjectPoolConfiguration CreateApplicationContextPoolConfiguration()
		{
			Contract.Ensures( Contract.Result<ObjectPoolConfiguration>() != null );

			return new ObjectPoolConfiguration() { ExhausionPolicy = ExhausionPolicy.BlockUntilAvailable, MaximumPooled = this.MaximumConcurrentRequest, MinimumReserved = this.MinimumConcurrentRequest };
		}

		/// <summary>
		///		Creates the <see cref="ObjectPoolConfiguration"/> for the <see cref="MsgPack.Rpc.Server.Protocols.ServerRequestContext"/> pool corresponds to values of this instance.
		/// </summary>
		/// <returns>
		///		The <see cref="ObjectPoolConfiguration"/> for the <see cref="MsgPack.Rpc.Server.Protocols.ServerRequestContext"/> pool corresponds to values of this instance.
		///		This value will not be <c>null</c>.
		/// </returns>
		public ObjectPoolConfiguration CreateRequestContextPoolConfiguration()
		{
			Contract.Ensures( Contract.Result<ObjectPoolConfiguration>() != null );

			return new ObjectPoolConfiguration() { ExhausionPolicy = ExhausionPolicy.BlockUntilAvailable, MaximumPooled = this.MaximumConcurrentRequest, MinimumReserved = this.MinimumConcurrentRequest };
		}

		/// <summary>
		///		Creates the <see cref="ObjectPoolConfiguration"/> for the <see cref="MsgPack.Rpc.Server.Protocols.ServerResponseContext"/> pool corresponds to values of this instance.
		/// </summary>
		/// <returns>
		///		The <see cref="ObjectPoolConfiguration"/> for the <see cref="MsgPack.Rpc.Server.Protocols.ServerResponseContext"/> pool corresponds to values of this instance.
		///		This value will not be <c>null</c>.
		/// </returns>
		public ObjectPoolConfiguration CreateResponseContextPoolConfiguration()
		{
			Contract.Ensures( Contract.Result<ObjectPoolConfiguration>() != null );

			return new ObjectPoolConfiguration() { ExhausionPolicy = ExhausionPolicy.BlockUntilAvailable, MaximumPooled = this.MaximumConcurrentRequest, MinimumReserved = this.MinimumConcurrentRequest };
		}

		/// <summary>
		///		Clones all of the fields of this instance.
		/// </summary>
		/// <returns>
		///		The shallow copy of this instance.
		/// </returns>
		public RpcServerConfiguration Clone()
		{
			Contract.Ensures( Contract.Result<RpcServerConfiguration>() != null );
			Contract.Ensures( !Object.ReferenceEquals( Contract.Result<RpcServerConfiguration>(), this ) );
			Contract.Ensures( Contract.Result<RpcServerConfiguration>().IsFrozen == this.IsFrozen );

			return this.CloneCore() as RpcServerConfiguration;
		}

		/// <summary>
		///		Freezes this instance.
		/// </summary>
		/// <returns>
		///		This instance.
		/// </returns>
		public RpcServerConfiguration Freeze()
		{
			Contract.Ensures( Object.ReferenceEquals( Contract.Result<RpcServerConfiguration>(), this ) );
			Contract.Ensures( this.IsFrozen );

			return this.FreezeCore() as RpcServerConfiguration;
		}

		/// <summary>
		/// Gets the frozen copy of this instance.
		/// </summary>
		/// <returns>
		/// This instance if it is already frozen.
		/// Otherwise, frozen copy of this instance.
		/// </returns>
		public RpcServerConfiguration AsFrozen()
		{
			Contract.Ensures( Contract.Result<RpcServerConfiguration>() != null );
			Contract.Ensures( !Object.ReferenceEquals( Contract.Result<RpcServerConfiguration>(), this ) );
			Contract.Ensures( Contract.Result<RpcServerConfiguration>().IsFrozen );
			Contract.Ensures( this.IsFrozen == Contract.OldValue( this.IsFrozen ) );

			return this.AsFrozenCore() as RpcServerConfiguration;
		}

		/// <summary>
		///		Clones all of the fields of this instance.
		/// </summary>
		/// <returns>
		///		The shallow copy of this instance. Returned instance always is not frozen.
		/// </returns>
		protected override FreezableObject CloneCore()
		{
			var result = base.CloneCore() as RpcServerConfiguration;
			result._filterProviders = new List<MessageFilterProvider>( result._filterProviders );
			return result;
		}

		/// <summary>
		///		Freezes this instance.
		/// </summary>
		/// <returns>
		///		This instance.
		/// </returns>
		protected override FreezableObject FreezeCore()
		{
			var result = base.FreezeCore() as RpcServerConfiguration;
			result._filterProviders = new ReadOnlyCollection<MessageFilterProvider>( result._filterProviders );
			return result;
		}
	}
}
