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
using System.Threading;
using System.Diagnostics.Contracts;

namespace MsgPack.Rpc.Protocols
{
	// AppDomain local SessionManager.

	/// <summary>
	///		Provide access point of current <see cref="IMessageIdGenerator"/> and default implementation of it.
	///		This implenmentation generates AppDomain local sequence.
	/// </summary>
	public sealed class MessageIdGenerator : IMessageIdGenerator
	{
		private const int _free = 0;
		private const int _used = 1;
		private const int _customized = 2;

		private static int _frozen;
		private static IMessageIdGenerator _current = new MessageIdGenerator();

		public static IMessageIdGenerator Currrent
		{
			get
			{
				Interlocked.Exchange( ref _frozen, _used );
				return _current;
			}
		}

		public static bool SetCurrent( IMessageIdGenerator sessionManager )
		{
			if ( sessionManager == null )
			{
				throw new ArgumentNullException( "sessionManager" );
			}

			Contract.EndContractBlock();

			try
			{
				if ( Interlocked.CompareExchange( ref _frozen, _customized, _free ) != _free )
				{
					return false;
				}
			}
			finally
			{
				Interlocked.Exchange( ref _current, sessionManager );
			}

			return true;
		}

		private readonly bool _allowCycling;
		private int _sequence;

		public MessageIdGenerator() : this( true ) { }

		public MessageIdGenerator( bool allowCycling )
		{
			this._allowCycling = allowCycling;
		}

		public int NextId()
		{
			if ( this._sequence < 0 && !this._allowCycling )
			{
				throw new InvalidOperationException( "Overflow" );
			}

			var result = Interlocked.Increment( ref _sequence );

			if ( result < 0 && !this._allowCycling )
			{
				throw new InvalidOperationException( "Overflow" );
			}

			return result;
		}

		public void ReturnId( int disposal )
		{
			// nop.
		}
	}
}
