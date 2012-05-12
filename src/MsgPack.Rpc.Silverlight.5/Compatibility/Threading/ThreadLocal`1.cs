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

#if SILVERLIGHT
using System;
using System.Collections.Generic;

namespace MsgPack
{
	internal class ThreadLocal<T>
	{
		[ThreadStatic]
		private static Dictionary<object, T> _storage;

		private readonly Func<T> _factory;

		public T Value
		{
			get
			{
				T value;
				if ( !_storage.TryGetValue( this, out value ) )
				{
					value = this._factory();
					_storage[ this ] = value;
				}

				return value;
			}
		}

		public ThreadLocal( Func<T> factory )
		{
			if ( _storage == null )
			{
				_storage = new Dictionary<object, T>();
			}

			this._factory = factory;
		}
	}
}
#endif