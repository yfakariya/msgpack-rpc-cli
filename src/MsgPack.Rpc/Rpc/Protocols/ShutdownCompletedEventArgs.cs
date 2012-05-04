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

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Contains event data for shutdown completion events both of client and server sides.
	/// </summary>
	public class ShutdownCompletedEventArgs : EventArgs
	{
		private readonly ShutdownSource _source;

		/// <summary>
		///		Gets a <see cref="ShutdownSource"/> value which indicates shutdown source.
		/// </summary>
		/// <value>
		///		A <see cref="ShutdownSource"/> value which indicates shutdown source.
		/// </value>
		public ShutdownSource Source
		{
			get { return this._source; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ShutdownCompletedEventArgs"/> class.
		/// </summary>
		/// <param name="source">A <see cref="ShutdownSource"/> value which indicates shutdown source.</param>
		/// <exception cref="ArgumentOutOfRangeException">
		///		The <paramref name="source"/> is not valid <see cref="ShutdownSource"/> enumeration value.
		/// </exception>
		public ShutdownCompletedEventArgs( ShutdownSource source )
		{
			switch ( source )
			{
				case ShutdownSource.Client:
				case ShutdownSource.Server:
				case ShutdownSource.Unknown:
				case ShutdownSource.Disposing:
				{
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException( "source" );
				}
			}

			Contract.EndContractBlock();

			this._source = source;
		}
	}
}
