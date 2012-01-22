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
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MsgPack.Rpc.Server.Protocols;
using MsgPack.Serialization;

namespace MsgPack.Rpc.Server.Dispatch
{
	/// <summary>
	///		Describes RPC service operation (method).
	/// </summary>
	public sealed class OperationDescription
	{
		private readonly ServiceDescription _service;

		public ServiceDescription Service
		{
			get
			{
				Contract.Ensures( Contract.Result<ServiceDescription>() != null );
				
				return this._service;
			}
		}

		private readonly MethodInfo _method;

		private readonly Func<ServerRequestContext, ServerResponseContext, Task> _operation;

		public Func<ServerRequestContext, ServerResponseContext, Task> Operation
		{
			get
			{
				Contract.Ensures( Contract.Result<Func<ServerRequestContext, ServerResponseContext, Task>>() != null );

				return this._operation;
			}
		}

		private readonly string _id;

		public string Id
		{
			get
			{
				Contract.Ensures( !String.IsNullOrEmpty( Contract.Result<string>() ) );

				return this._id;
			}
		}

		private OperationDescription( ServiceDescription service, MethodInfo method, string id, Func<ServerRequestContext, ServerResponseContext, Task> operation )
		{
			this._service = service;
			this._method = method;
			this._operation = operation;
			this._id = id;
		}

		public static IEnumerable<OperationDescription> FromServiceDescription( RpcServerConfiguration configuration, SerializationContext serializationContext, ServiceDescription service )
		{
			if ( serializationContext == null )
			{
				throw new ArgumentNullException( "serializationContext" );
			}

			if ( service == null )
			{
				throw new ArgumentNullException( "service" );
			}

			Contract.Ensures( Contract.Result<IEnumerable<OperationDescription>>() != null );
			Contract.Ensures( Contract.ForAll( Contract.Result<IEnumerable<OperationDescription>>(), item => item != null ) );

			var generated = new HashSet<string>();
			return
				service.ServiceType.GetMethods()
				.Where( method => method.IsDefined( typeof( MessagePackRpcMethodAttribute ), true ) )
				.Select( operation =>
					{
						if ( !generated.Add( operation.Name ) )
						{
							throw new NotSupportedException(
								String.Format(
									CultureInfo.CurrentCulture,
									"Method '{0}' is overloaded. Method overload is not supported on the MessagePack-RPC.",
									operation.Name
								)
							);
						}

						return FromServiceMethodCore( configuration ?? RpcServerConfiguration.Default, serializationContext, service, operation );
					}
				).ToArray();
		}

		private static OperationDescription FromServiceMethodCore( RpcServerConfiguration configuration, SerializationContext serializationContext, ServiceDescription service, MethodInfo operation )
		{
			Contract.Requires( configuration != null );
			Contract.Ensures( Contract.Result<OperationDescription>() != null );

			var serviceInvoker = ServiceInvokerGenerator.Default.GetServiceInvoker( configuration, serializationContext, service, operation );
			return new OperationDescription( service, operation, serviceInvoker.OperationId, serviceInvoker.InvokeAsync );
		}
	}
}
