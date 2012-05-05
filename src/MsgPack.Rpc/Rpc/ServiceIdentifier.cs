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
using System.Globalization;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Defines utlities related to ServiceID.
	/// </summary>
	internal static class ServiceIdentifier
	{
		/// <summary>
		///		Creates new service ID.
		/// </summary>
		/// <param name="name">The name (required).</param>
		/// <param name="version">The version.</param>
		/// <returns>The service ID.</returns>
		public static string CreateServiceId( string name, int version )
		{
			Contract.Requires( !String.IsNullOrWhiteSpace( name ) );
			Contract.Ensures( Contract.Result<string>() != null );

			return String.Format( CultureInfo.InvariantCulture, "{0}:{1}", name, version );
		}

		/// <summary>
		///		Truncates the generics suffix from the type name.
		/// </summary>
		/// <param name="typeName">Simple name of the type.</param>
		/// <returns>The name without generics suffix.</returns>
		public static string TruncateGenericsSuffix( string typeName )
		{
			Contract.Requires( typeName != null );
			Contract.Ensures( Contract.Result<string>() != null );

			int positionOfBackQuote = typeName.IndexOf( '`' );
			return positionOfBackQuote < 0 ? typeName : typeName.Remove( positionOfBackQuote );
		}
	}
}
