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

using MsgPack.Rpc.Protocols;
using MsgPack.Rpc.Serialization;

namespace MsgPack.Rpc.Services
{
	/// <summary>
	///		Define basic features for serializer extensibility.
	/// </summary>
	public abstract class RequestSerializerFactory
	{
		private static readonly RequestSerializerFactory _default = new DefaultRequestSerializerFactory();

		/// <summary>
		///		Get default <see cref="RequestSerializerFactory"/> instance.
		/// </summary>
		/// <value>Default <see cref="RequestSerializerFactory"/> instance.</value>
		public static RequestSerializerFactory Default
		{
			get { return RequestSerializerFactory._default; }
		} 

		/// <summary>
		///		Initialize new instance.
		/// </summary>
		protected RequestSerializerFactory() { }

		/// <summary>
		///		Create <see cref="RequestMessageSerializer"/> for specified protocol and configuration.
		/// </summary>
		/// <param name="protocol">Target protocol.</param>
		/// <param name="options">Option settings. This parameter can be null.</param>
		/// <returns><see cref="RequestMessageSerializer"/> for specified protocol and configuration.</returns>
		public abstract RequestMessageSerializer Create( RpcTransportProtocol protocol, RpcClientOptions options );

		/// <summary>
		///		Default <see cref="RequestSerializerFactory"/> implementation.
		/// </summary>
		private sealed class DefaultRequestSerializerFactory : RequestSerializerFactory
		{
			private const int _defaultRequestQuota = 10 * 1024 * 1024;

			/// <summary>
			///		Initialize new instance.
			/// </summary>
			internal DefaultRequestSerializerFactory() { }

			public override RequestMessageSerializer Create( RpcTransportProtocol protocol, RpcClientOptions options )
			{
				return
					new RequestMessageSerializer(
						null,
						null,
						null,
						null,
						options == null ? _defaultRequestQuota : options.MaximumRequestQuota ?? _defaultRequestQuota
					);
			}
		}
	}
}
