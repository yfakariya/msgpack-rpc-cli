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
	/// <summary>
	///		Interface of providers which provide custom filter instance.
	/// </summary>
	/// <typeparam name="T">Type of filter.</typeparam>
	public interface IFilterProvider<T>
	{
		/// <summary>
		///		Get priority of the filter in filter chain.
		/// </summary>
		/// <remarks>
		///		The filter which has higher priority will be up to other filters.
		///		Conceptually, there are application codes on the top of the filter chain,
		///		and there are network device abstractions (e.g. Berkley Sockets) on the bottom of the filter chain.
		/// </remarks>
		int Priority { get; }

		/// <summary>
		///		Get filter instance.
		/// </summary>
		/// <returns>Filter instance.</returns>
		/// <remarks>
		///		Consumer of filters will call this method for each invocation.
		///		This method typically returns cached instance which is immutable (or stateful) object.
		///		When it has considerable condition, you create brand-new instance for each calling.
		/// </remarks>
		T GetFilter();
	}
}
