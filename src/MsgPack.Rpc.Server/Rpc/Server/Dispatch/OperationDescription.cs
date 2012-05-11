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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MsgPack.Rpc.Server.Protocols;
using MsgPack.Serialization;

namespace MsgPack.Rpc.Server.Dispatch
{
	/// <summary>
	///		Describes the RPC service operation (method).
	/// </summary>
	public sealed class OperationDescription
	{
		private readonly ServiceDescription _service;

		/// <summary>
		///		Gets the service description.
		/// </summary>
		/// <value>
		///		The service description which describles service and application specification including version.
		///		This value will not be <c>null</c>.
		/// </value>
		public ServiceDescription Service
		{
			get
			{
				Contract.Ensures( Contract.Result<ServiceDescription>() != null );

				return this._service;
			}
		}

		private readonly Func<ServerRequestContext, ServerResponseContext, Task> _operation;

		/// <summary>
		///		Gets the operation to invoke.
		/// </summary>
		/// <value>
		///		The operation to invoke.
		///		This value may <see cref="M:IAsyncInvoker.Invoke"/> implementation.
		///		This value will not be <c>null</c>.
		/// </value>
		public Func<ServerRequestContext, ServerResponseContext, Task> Operation
		{
			get
			{
				Contract.Ensures( Contract.Result<Func<ServerRequestContext, ServerResponseContext, Task>>() != null );

				return this._operation;
			}
		}

		private readonly string _id;

		/// <summary>
		///		Gets the id of the operation.
		/// </summary>
		/// <value>
		///		The id of the operation.
		///		This value will not be <c>null</c> nor empty.
		/// </value>
		public string Id
		{
			get
			{
				Contract.Ensures( !String.IsNullOrEmpty( Contract.Result<string>() ) );

				return this._id;
			}
		}

		private OperationDescription( ServiceDescription service, string id, Func<ServerRequestContext, ServerResponseContext, Task> operation )
		{
			Validation.ValidateMethodName( id, "id" );
			Contract.EndContractBlock();

			this._service = service;
			this._operation = operation;
			this._id = id;
		}

		/// <summary>
		///		Creates the collection of the <see cref="OperationDescription"/> from the service description.
		/// </summary>
		/// <param name="runtime">The <see cref="RpcServerRuntime"/> which provides runtime services.</param>
		/// <param name="service">The target service description.</param>
		/// <returns>
		///		Collection of the <see cref="OperationDescription"/> from the service description.
		///		This value will not be <c>null</c> but might be empty.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="runtime"/> is <c>null</c>.
		///		Or, <paramref name="service"/> is <c>null</c>.
		/// </exception>
		public static IEnumerable<OperationDescription> FromServiceDescription( RpcServerRuntime runtime, ServiceDescription service )
		{
			if ( runtime == null )
			{
				throw new ArgumentNullException( "runtime" );
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

						return FromServiceMethodCore( runtime, service, operation );
					}
				).ToArray();
		}

		private static OperationDescription FromServiceMethodCore( RpcServerRuntime runtime, ServiceDescription service, MethodInfo operation )
		{
			Contract.Requires( runtime != null );
			Contract.Ensures( Contract.Result<OperationDescription>() != null );

			var serviceInvoker = ServiceInvokerGenerator.Default.GetServiceInvoker( runtime, service, operation );
			return new OperationDescription( service, serviceInvoker.OperationId, serviceInvoker.InvokeAsync );
		}
	}
}
