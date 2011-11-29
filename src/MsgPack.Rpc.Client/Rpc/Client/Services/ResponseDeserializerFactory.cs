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
	///		Define basic features for deserializer extensibility.
	/// </summary>
	public abstract class ResponseDeserializerFactory
	{
		private static readonly ResponseDeserializerFactory _default = new DefaultResponseDeserializerFactory();

		/// <summary>
		///		Get default <see cref="ResponseDeserializerFactory"/> instance.
		/// </summary>
		/// <value>Default <see cref="ResponseDeserializerFactory"/> instance.</value>
		public static ResponseDeserializerFactory Default
		{
			get { return ResponseDeserializerFactory._default; }
		} 

		/// <summary>
		///		Initialize new instance.
		/// </summary>
		protected ResponseDeserializerFactory() { }

		/// <summary>
		///		Create <see cref="RequestMessageSerializer"/> for specified protocol and configuration.
		/// </summary>
		/// <param name="protocol">Target protocol.</param>
		/// <param name="options">Option settings. This parameter can be null.</param>
		/// <returns><see cref="RequestMessageSerializer"/> for specified protocol and configuration.</returns>
		public abstract ResponseMessageSerializer Create( RpcTransportProtocol protocol, RpcClientOptions options );
	}

	public sealed class DefaultResponseDeserializerFactory : ResponseDeserializerFactory
	{
		private const int _defaultResponseQuota = 10 * 1024 * 1024;

		public DefaultResponseDeserializerFactory() { }

		public override ResponseMessageSerializer Create( RpcTransportProtocol protocol, RpcClientOptions options )
		{
			return 
				new ResponseMessageSerializer( 
					null, 
					null, 
					null, 
					null,
					options == null ? _defaultResponseQuota : options.MaximumRequestQuota ?? _defaultResponseQuota
				);
		}
	}

}
