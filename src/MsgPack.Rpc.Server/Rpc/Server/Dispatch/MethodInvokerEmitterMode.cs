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

namespace MsgPack.Rpc.Dispatch
{
	/// <summary>
	///		Represents code generation mode of <see cref="MethodInvokerEmitter"/>.
	/// </summary>
	internal enum MethodInvokerEmitterMode
	{
		/// <summary>
		///		Determined by emitter implementation.
		/// </summary>
		Default = 0,

		/// <summary>
		///		Gerate code as collctable. This mode used in ad-hoc mode.
		/// </summary>
		Collectable,

		/// <summary>
		///		Gerate code as saveable. This mode used in pre-generate mode.
		/// </summary>
		Saveable
	}
}
