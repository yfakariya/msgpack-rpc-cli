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

namespace MsgPack.Rpc
{
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
}
