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
using System.Linq;
using System.Reflection;
using System.Security;
using MsgPack.Rpc.Server.Dispatch.SvcFileInterop;

namespace MsgPack.Rpc.Server.Dispatch
{
	/// <summary>
	///		Implements <see cref="ServiceTypeLocator"/> which uses WCF compatible *.svc file and the directory.
	/// </summary>
	public class FileBasedServiceTypeLocator : ServiceTypeLocator
	{
		private string _baseDirectory;

		/// <summary>
		///		Gets or sets the base directory.
		/// </summary>
		/// <value>
		///		The base directory to locate *.svc files.
		///		If the value is <c>null</c> or empty, the <see cref="P:AppDomain.BaseDirectory"/> will be used.
		/// </value>
		public string BaseDirectory
		{
			get { return this._baseDirectory; }
			set
			{
				if ( String.IsNullOrWhiteSpace( value ) )
				{
					this._baseDirectory = null;
				}
				else
				{
					try
					{
						this._baseDirectory = Path.GetFullPath( value );
					}
					catch ( ArgumentException ex )
					{
						throw new ArgumentException( "Invalid directory path.", "value", ex );
					}
					catch ( NotSupportedException ex )
					{
						throw new ArgumentException( "Invalid directory path.", "value", ex );
					}
					catch ( PathTooLongException ex )
					{
						throw new ArgumentException( "Invalid directory path.", "value", ex );
					}
				}
			}
		}

		/// <summary>
		/// Find services types with implementation specific way and returns them as <see cref="ServiceDescription"/>.
		/// </summary>
		/// <returns>
		/// The collection of <see cref="ServiceDescription"/>.
		/// </returns>
		public sealed override IEnumerable<ServiceDescription> FindServices()
		{
			var result = new List<ServiceDescription>();
			var targetDirectory = String.IsNullOrWhiteSpace( this.BaseDirectory ) ? AppDomain.CurrentDomain.BaseDirectory : this.BaseDirectory;

			if ( !Directory.Exists( targetDirectory ) )
			{
				return result;
			}

			// DirectoryNotFoundException might be occured here due to race condition,
			// but it must be unexpected failure of the server environment, so it should not be catch.

			var binDirectory = Path.Combine( targetDirectory, "bin" );

			// AssemblyQualifiedName
			var typeCatalog = new List<string>();
			LoadAssemblies( targetDirectory, binDirectory, typeCatalog );

			var catalog =
				typeCatalog
				.Select( typeQualifiedName => Type.GetType( typeQualifiedName ) )
				.ToDictionary( item => item.FullName );

			foreach ( var file in Directory.GetFiles( targetDirectory, "*.svc" ) )
			{
				var description = ExtractServiceDescription( file, targetDirectory, catalog );
				if ( description != null )
				{
					result.Add( description );
				}
			}

			return result;
		}

		[SecuritySafeCritical]
		private static void LoadAssemblies( string targetDirectory, string binDirectory, List<string> typeCatalog )
		{
			AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ReflectionOnlyAssemblyResolve;
			try
			{
				foreach ( var catalogEntry
					in Directory.GetFiles( targetDirectory, "*.dll" )
					.Concat( Directory.Exists( binDirectory ) ? Directory.GetFiles( binDirectory, "*.dll" ) : Enumerable.Empty<string>() )
					.SelectMany( file =>
						Assembly.ReflectionOnlyLoadFrom( file ).GetTypes()
					).Select( type =>
						new
						{
							Type = type,
							CustomAttribute =
								type.GetCustomAttributesData().SingleOrDefault( attribute =>
									attribute.Constructor.DeclaringType.AssemblyQualifiedName == typeof( MessagePackRpcServiceContractAttribute ).AssemblyQualifiedName
								)
						}
					).Where( item => item.CustomAttribute != null )
					.GroupBy( item => item.Type.Assembly.FullName )
				)
				{
					Assembly.Load( catalogEntry.Key );
					foreach ( var item in catalogEntry )
					{
						typeCatalog.Add( item.Type.AssemblyQualifiedName );
					}
				}
			}
			finally
			{
				AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= ReflectionOnlyAssemblyResolve;
			}
		}

		[SecuritySafeCritical]
		private static Assembly ReflectionOnlyAssemblyResolve( object sender, ResolveEventArgs args )
		{
			return Assembly.ReflectionOnlyLoad( args.Name );
		}

		private static ServiceDescription ExtractServiceDescription( string svcFile, string directory, IDictionary<string, Type> serviceTypeCatalog )
		{
			ServiceHostDirective directive;
			using ( var stream = new FileStream( svcFile, FileMode.Open, FileAccess.Read, FileShare.Read, 1024, FileOptions.SequentialScan ) )
			using ( var reader = new StreamReader( stream ) )
			{
				directive = SvcFileParser.Parse( reader );
			}

			if ( directive.Service == null )
			{
				throw new InvalidOperationException(
					String.Format(
						CultureInfo.CurrentCulture,
						"'Service' attribute is not found in '{0}' file.",
						svcFile
					)
				);
			}

			Type serviceType;
			if ( !serviceTypeCatalog.TryGetValue( directive.Service, out serviceType ) )
			{
				throw new InvalidOperationException(
					String.Format(
						CultureInfo.CurrentCulture,
						"Cannot load the service type for name '{0}' from directory '{1}'. Note that service type name is case sensitive.",
						directive.Service,
						directory
					)
				);
			}

			return ServiceDescription.FromServiceType( serviceType );
		}
	}
}
