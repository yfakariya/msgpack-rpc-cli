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
using MsgPack.Rpc.Server.Dispatch.SvcFileInterop;

namespace MsgPack.Rpc.Server.Dispatch
{
	/// <summary>
	///		Implements <see cref="ServiceTypeLocator"/> which uses WCF compatible *.svc file and the directory.
	/// </summary>
	public class FileBasedServiceTypeLocator : ServiceTypeLocator
	{
		/// <summary>
		///		Gets or sets the base directory.
		/// </summary>
		/// <value>
		///		The base directory to locate *.svc files.
		///		If the value is <c>null</c> or empty, the <see cref="P:AppDomain.BaseDirectory"/> will be used.
		/// </value>
		public string BaseDirectory { get; set; }

		/// <summary>
		/// Find services types with implementation specific way and returns them as <see cref="ServiceDescription"/>.
		/// </summary>
		/// <returns>
		/// The collection of <see cref="ServiceDescription"/>.
		/// </returns>
		public sealed override IEnumerable<ServiceDescription> FindServices()
		{
			var result = new List<ServiceDescription>();
			foreach ( var file in Directory.GetFiles( String.IsNullOrWhiteSpace( this.BaseDirectory ) ? AppDomain.CurrentDomain.BaseDirectory : this.BaseDirectory, "*.svc" ) )
			{
				result.Add( ExtractServiceDescription( file ) );
			}

			return result;
		}

		private static ServiceDescription ExtractServiceDescription( string svcFile )
		{
			ServiceHostDirective directive;
			using ( var stream = new FileStream( svcFile, FileMode.Open, FileAccess.Read, FileShare.Read, 1024, FileOptions.SequentialScan ) )
			using ( var reader = new StreamReader( stream ) )
			{
				directive = new SvcFileParser().Parse( reader );
			}

			var serviceType = Type.GetType( directive.Service, false );
			if ( serviceType == null )
			{
				throw new InvalidOperationException(
					String.Format(
						CultureInfo.CurrentCulture,
						"Cannot load service type '{0}' from directory '{1}'.",
						directive.Service,
						AppDomain.CurrentDomain.BaseDirectory
					)
				);
			}

			return ServiceDescription.FromServiceType( serviceType );
		}
	}
}
