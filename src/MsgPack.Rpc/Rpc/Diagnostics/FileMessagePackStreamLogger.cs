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
using System.Net;
using System.Text.RegularExpressions;

namespace MsgPack.Rpc.Diagnostics
{
	/// <summary>
	///		File based <see cref="MessagePackStreamLogger"/> implementation.
	/// </summary>
	/// <remarks>
	///		The log file path will be <c>{BaseDirectory}\{ProcessName}[-{AppDomainName}]\{ProcessStartTime}-{ProcessId}\{TimeStamp}-{EndPoint}-{ThreadId}.mpac</c>,
	///		so you should specify short path to <see cref="BaseDirectoryPath"/>.
	///		<note>
	///			AppDomainName is omitted in default AppDomain.
	///		</note>
	/// </remarks>
	public class FileMessagePackStreamLogger : MessagePackStreamLogger
	{
		private static readonly Regex _ipAddressEscapingRegex =
			new Regex(
				@"[:\./]",
				RegexOptions.Compiled
				| RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture
			);
		private readonly string _baseDirectoryPath;

		/// <summary>
		///		Gets the base directory path.
		/// </summary>
		/// <value>
		///		The base directory path.
		/// </value>
		public string BaseDirectoryPath
		{
			get { return this._baseDirectoryPath; }
		}

		private readonly string _directoryPath;

		/// <summary>
		///		Gets the calculated directory path which will store logfiles.
		/// </summary>
		/// <value>
		///		The calculated directory path which will store logfiles.
		/// </value>
		public string DirectoryPath
		{
			get { return this._directoryPath; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FileMessagePackStreamLogger"/> class.
		/// </summary>
		/// <param name="baseDirectoryPath">The base directory path.</param>
		public FileMessagePackStreamLogger( string baseDirectoryPath )
		{
			this._baseDirectoryPath = baseDirectoryPath;
			// {BaseDirectory}\{ProcessName}[-{AppDomainName}]\{ProcessStartTime}-{ProcessId}\{TimeStamp}-{EndPoint}-{ThreadId}.mpac
			if ( AppDomain.CurrentDomain.IsDefaultAppDomain() )
			{
				this._directoryPath = Path.Combine( this._baseDirectoryPath, ProcessName, String.Format( CultureInfo.InvariantCulture, "{0:yyyyMMdd_HHmmss}-{1}", ProcessStartTimeUtc, ProcessId ) );
			}
			else
			{
				this._directoryPath = Path.Combine( this._baseDirectoryPath, ProcessName + "-" + AppDomain.CurrentDomain.FriendlyName, String.Format( CultureInfo.InvariantCulture, "{0:yyyyMMdd_HHmmss}-{1}", ProcessStartTimeUtc, ProcessId ) );
			}
		}

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

			string filePath = Path.Combine( this._directoryPath, String.Format( CultureInfo.InvariantCulture, "{0:yyyyMMdd_HHmmss_fff}-{1}-{2}.mpac", sessionStartTime.UtcDateTime, remoteEndPointString, ThreadId ) );

			while ( true )
			{
				if ( !Directory.Exists( this._directoryPath ) )
				{
					Directory.CreateDirectory( this._directoryPath );
				}

				try
				{
					using ( var fileStream = new FileStream( filePath, FileMode.Append, FileAccess.Write, FileShare.Read, 64 * 1024, FileOptions.None ) )
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
