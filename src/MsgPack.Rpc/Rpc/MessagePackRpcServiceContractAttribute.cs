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
using System.Globalization;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Marks the type represents service contract for the MessagePack-RPC.
	/// </summary>
	[AttributeUsage( AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false, Inherited = true )]
	public sealed class MessagePackRpcServiceContractAttribute : Attribute
	{
		private string _name;

		/// <summary>
		///		Gets the name of the RPC procedure.
		/// </summary>
		/// <value>
		///		The name of the RPC procedure.
		///		If the value is <c>null</c>, empty or consisted by whitespace characters only, the qualified type name will be used.
		/// </value>
		public string Name
		{
			get { return this._name; }
			set { this._name = value; }
		}

		/// <summary>
		///		Gets or sets the version of the RPC procedure.
		/// </summary>
		/// <value>
		///		The version of the RPC procedure.
		/// </value>
		public int Version { get; set; }

		/// <summary>
		///		Initializes a new instance of the <see cref="MessagePackRpcServiceContractAttribute"/> class.
		/// </summary>
		public MessagePackRpcServiceContractAttribute() { }

		internal string ToServiceId( Type serviceType )
		{
			return
				ServiceIdentifier.CreateServiceId(
					String.IsNullOrWhiteSpace( this._name ) ? ServiceIdentifier.TruncateGenericsSuffix( serviceType.Name ) : this._name,
					this.Version
				);
		}
	}
}