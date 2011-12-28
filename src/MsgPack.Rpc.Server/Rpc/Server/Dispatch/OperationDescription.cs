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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MsgPack.Serialization;
using MsgPack.Rpc.Server.Protocols;

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
			get { return this._service; }
		}

		private readonly MethodInfo _method;

		private readonly Func<Unpacker, int, ServerResponseSocketAsyncEventArgs, Task> _operation;

		public Func<Unpacker, int, ServerResponseSocketAsyncEventArgs, Task> Operation
		{
			get { return this._operation; }
		}

		private readonly string _id;

		public string Id
		{
			get { return this._id; }
		}

		private OperationDescription( ServiceDescription service, MethodInfo method, string id, Func<Unpacker, int, ServerResponseSocketAsyncEventArgs, Task> operation )
		{
			this._service = service;
			this._method = method;
			this._operation = operation;
			this._id = id;
		}

		public static IEnumerable<OperationDescription> FromServiceDescription( SerializationContext serializationContext, ServiceDescription service )
		{
			if ( serializationContext == null )
			{
				throw new ArgumentNullException( "serializationContext" );
			}

			if ( service == null )
			{
				throw new ArgumentNullException( "service" );
			}

			foreach ( var operation in service.ServiceType.GetMethods().Where( method => method.IsDefined( typeof( MessagePackRpcMethodAttribute ), true ) ) )
			{
				yield return FromServiceMethodCore( serializationContext, service, operation );
			}
		}

		public static OperationDescription FromServiceMethod( SerializationContext serializationContext, ServiceDescription service, MethodInfo operation )
		{
			if ( serializationContext == null )
			{
				throw new ArgumentNullException( "serializationContext" );
			}

			if ( service == null )
			{
				throw new ArgumentNullException( "service" );
			}

			if ( operation == null )
			{
				throw new ArgumentNullException( "operation" );
			}

			if ( !operation.DeclaringType.IsAssignableFrom( service.ServiceType ) )
			{
				throw new ArgumentException(
					String.Format(
						CultureInfo.CurrentCulture,
						"Operation '{0}' is not declared on the service type '{1}' or its ancester types.",
						operation.Name,
						service.ServiceType.AssemblyQualifiedName
					),
					"operation"
				);
			}

			return FromServiceMethodCore( serializationContext, service, operation );
		}

		private static OperationDescription FromServiceMethodCore( SerializationContext serializationContext, ServiceDescription service, MethodInfo operation )
		{
			var serviceInvoker = ServiceInvokerGenerator.Default.GetServiceInvoker( serializationContext, service, operation );
			return new OperationDescription( service, operation, serviceInvoker.OperationId, serviceInvoker.InvokeAsync );
		}
	}
}
