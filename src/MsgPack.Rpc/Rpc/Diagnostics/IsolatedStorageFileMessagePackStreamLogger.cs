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
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Text.RegularExpressions;

namespace MsgPack.Rpc.Diagnostics
{
	/// <summary>
	///		Isolated storage file based <see cref="MessagePackStreamLogger"/> implementation.
	/// </summary>
	public class IsolatedStorageFileMessagePackStreamLogger : MessagePackStreamLogger
	{
		private static readonly Regex _ipAddressEscapingRegex =
			new Regex(
				@"[:\./]",
#if !SILVERLIGHT
 RegexOptions.Compiled |
#endif
 RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture
			);

		/// <summary>
		/// Initializes a new instance of the <see cref="IsolatedStorageFileMessagePackStreamLogger"/> class.
		/// </summary>
		public IsolatedStorageFileMessagePackStreamLogger() { }

		/// <summary>
		/// Writes the specified data to log sink.
		/// </summary>
		/// <param name="sessionStartTime">The <see cref="DateTimeOffset"/> when session was started.</param>
		/// <param name="remoteEndPoint">The <see cref="EndPoint"/> which is data source of the <paramref name="stream"/>.</param>
		/// <param name="stream">The MessagePack data stream. This value might be corrupted or actually not a MessagePack stream.</param>
		public override void Write( DateTimeOffset sessionStartTime, EndPoint remoteEndPoint, IEnumerable<byte> stream )
		{
			string remoteEndPointString;
			DnsEndPoint dnsEndPoint;
			IPEndPoint ipEndPoint;
			if ( ( dnsEndPoint = remoteEndPoint as DnsEndPoint ) != null )
			{
				remoteEndPointString = _ipAddressEscapingRegex.Replace( dnsEndPoint.Host, "_" );
			}
			else if ( ( ipEndPoint = remoteEndPoint as IPEndPoint ) != null )
			{
				remoteEndPointString = _ipAddressEscapingRegex.Replace( ipEndPoint.Address.ToString(), "_" );
			}
			else
			{
				remoteEndPointString = "(unknown)";
			}

			string fileName = String.Format( CultureInfo.InvariantCulture, "{0:yyyyMMdd_HHmmss_fff}-{1}-{2}.mpac", sessionStartTime.UtcDateTime, remoteEndPointString, ThreadId );

			while ( true )
			{
				try
				{
					using ( var storage = IsolatedStorageFile.GetUserStoreForApplication() )
					using ( var fileStream = storage.OpenFile( fileName, FileMode.Append, FileAccess.Write, FileShare.Read ) )
					{
						if ( stream != null )
						{
							long written = fileStream.Length;
							foreach ( var b in Skip( stream, written ) )
							{
								fileStream.WriteByte( b );
							}
						}
					}

					break;
				}
				catch ( DirectoryNotFoundException ) { }
			}
		}

		private static IEnumerable<T> Skip<T>( IEnumerable<T> source, long count )
		{
			long index = 0;
			foreach ( var item in source )
			{
				if ( index >= count )
				{
					yield return item;
				}

				index++;
			}
		}
	}
}
