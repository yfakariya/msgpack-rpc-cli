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

namespace MsgPack.Rpc
{
	/// <summary>
	///		Marks the type represents service contract for the MessagePack-RPC.
	/// </summary>
	[AttributeUsage( AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false, Inherited = true )]
	public sealed class MessagePackRpcServiceContractAttribute : Attribute
	{
		private readonly string _name;

		/// <summary>
		///		Gets the name of the RPC procedure.
		/// </summary>
		/// <value>
		///		The name of the RPC procedure.
		/// </value>
		public string Name
		{
			get
			{
				Contract.Ensures( !String.IsNullOrEmpty( Contract.Result<string>() ) );
				return this._name;
			}
		}

		private string _application;

		/// <summary>
		///		Gets or sets the application name of the RPC procedure.
		/// </summary>
		/// <value>
		///		The application name of the RPC procedure.
		/// </value>
		public string Application
		{
			get
			{
				Contract.Ensures( Contract.Result<string>() != null );
				return this._application ?? String.Empty;
			}
			set
			{
				this._application = value;
			}
		}

		private string _version;

		/// <summary>
		///		Gets or sets the version of the RPC procedure.
		/// </summary>
		/// <value>
		///		The version of the RPC procedure.
		/// </value>
		public string Version
		{
			get
			{
				Contract.Ensures( Contract.Result<string>() != null );
				return this._version ?? String.Empty;
			}
			set
			{
				this._version = value;
			}
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="MessagePackRpcServiceContractAttribute"/> class.
		/// </summary>
		/// <param name="name">The name of the RPC procedure.</param>
		public MessagePackRpcServiceContractAttribute( string name )
		{
			if ( name == null )
			{
				throw new ArgumentNullException( "name" );
			}

			if ( name.Length == 0 )
			{
				throw new ArgumentException( "The argument cannot be empty.", "name" );
			}

			Contract.EndContractBlock();

			this._name = name;
		}
	}
}
