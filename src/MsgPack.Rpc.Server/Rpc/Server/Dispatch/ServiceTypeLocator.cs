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
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Reflection;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;
using MsgPack.Serialization;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using System.Text.RegularExpressions;
using MsgPack.Rpc.Server.Dispatch.SvcFileInterop;

namespace MsgPack.Rpc.Server
{
	internal sealed class ResponseMessage
	{
		public MessagePackObject Error;
		public MessagePackObject ReturnValueOrDetail;
	}

	public sealed class RpcServerSession
	{
		private readonly BlockingCollection<ResponseMessage> _sendQueue = new BlockingCollection<ResponseMessage>();
		private readonly SocketAsyncEventArgs _sendingContext = new SocketAsyncEventArgs();

		internal bool TryTakeResponse( out ResponseMessage response )
		{
			return this._sendQueue.TryTake( out response );
		}

		internal TTuple GetArguments<TTuple>( IList<Type> templates )
		{
			throw new NotImplementedException();
		}

		internal void SetReturn<T>( T value )
		{
			throw new NotImplementedException();
		}

		internal void HandleException( Exception exception )
		{
			throw new NotImplementedException();
		}

		internal void SetError( RpcErrorMessage rpcErrorMessage )
		{
			throw new NotImplementedException();
		}
	}

	[AttributeUsage( AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false, Inherited = true )]
	public sealed class MessagePackRpcServiceContractAttribute : Attribute
	{
		private readonly string _name;

		public string Name { get { return this._name; } }

		public string Application { get; set; }

		public string Version { get; set; }

		public MessagePackRpcServiceContractAttribute( string name )
		{
			this._name = name;
		}
	}

	[AttributeUsage( AttributeTargets.Method, AllowMultiple = false, Inherited = true )]
	public sealed class MessagePackRpcMethodAttribute : Attribute
	{
		public MessagePackRpcMethodAttribute() { }
	}
}

namespace MsgPack.Rpc.Server.Dispatch
{
	/// <summary>
	///		Locates service type as <see cref="ServiceDescription"/>.
	/// </summary>
	public abstract class ServiceTypeLocator
	{
		/// <summary>
		///		Find services types with implementation specific way and returns them as <see cref="ServiceDescription"/>.
		/// </summary>
		/// <returns>
		///		The collection of <see cref="ServiceDescription"/>.
		/// </returns>
		public abstract Collection<ServiceDescription> FindServices();
	}
}