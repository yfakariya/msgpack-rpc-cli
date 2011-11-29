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

namespace MsgPack.Rpc
{
	// FIXME: Implement

	/// <summary>
	///		Provide basic features for all RPC proxy classes.
	/// </summary>
	class RpcProxy
	{
		public static T Create<T>()
			where T : class, new()
		{
			throw new NotImplementedException();
		}

		public static T Create<T>( params object[] arguments )
			where T : class ,new()
		{
			throw new NotImplementedException();

			// Use real proxy. It is not so fast.
		}
	}
}
