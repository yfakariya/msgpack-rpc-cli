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
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace MsgPack.Rpc.Server.Dispatch
{
	/// <summary>
	///		Implements MessagePack-RPC method lookup scheame as dictionary like collection.
	/// </summary>
	/// <remarks>
	///		This class supports following method description formats:
	///		<list type="bullet">
	///			<item><c>&lt;method&gt;:&lt;scope&gt;:&lt;version&gt;</c>.</item>
	///			<item>
	///				<c>&lt;method&gt;:&lt;version&gt;</c>. 
	///				A <c>scope</c> is assumed to be omitted, so the empty string is used.
	///			</item>
	///			<item>
	///				<c>&lt;method&gt;:&lt;scope&gt;</c>.
	///				A <c>version</c> is assumed to be omitted, so the <see cref="Int32.MaxValue"/> is used.
	///			</item>
	///			<item>
	///				<c>&lt;method&gt;</c>.
	///				A <c>scope</c> and a <c>version</c> are assumed to be omitted. Inference rules are same as above.
	///			</item>
	///		</list>
	///		<note>
	///			A <c>method</c> and a <c>scope</c> must be compliant with UAX-31(Unicode Identifier and Pattern Syntax)
	///			with allowing to start with underscopre ('_', U+005F).
	///		</note>
	/// </remarks>
	public class VersionedOperationCatalog : OperationCatalog
	{
		private const int _expectedMethodInTheScope = 4;

		private const string MethodGroup = "Method";
		private const string ScopeGroup = "Scope";
		private const string VersionGroup = "Version";
		private const string _idStart = @"\p{L}\p{Nl}_";
		private const string _idContinue = @"\p{L}\p{Nl}\p{Mn}\p{Mc}\p{Nd}\p{Pc}";

		private static readonly Regex _methodDescriptionRegex =
			new Regex(
				@"^(?<" + MethodGroup + @">[" + _idStart + @"][" + _idContinue + @"]*)" +
				@"(\:" +
				@"(" +
				@"(?<" + VersionGroup + @">[0-9]+)" +
				@"|(" +
				@"(?<" + ScopeGroup + @">[" + _idStart + @"][" + _idContinue + @"]*)" +
				@"(\:(?<" + VersionGroup + @">[0-9]+))?" +
				@")" +
				@")" +
				@")?$",
#if !SILVERLGIHT
 RegexOptions.Compiled |
#endif
 RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture
			);

		/// <summary>
		///		Parses method description.
		/// </summary>
		/// <param name="methodDescription">The method description delimited with colon.</param>
		/// <param name="methodName">The method name parts, which is compliant with UAX-31 excepts starting with underscore is allowed.</param>
		/// <param name="scope">The scope, which is compliant with UAX-31 excepts starting with underscore is allowed.</param>
		/// <param name="version">The version, this value is non-negative integer.</param>
		internal static void ParseMethodDescription( string methodDescription, out string methodName, out string scope, out int? version )
		{
			var match = _methodDescriptionRegex.Match( methodDescription );
			if ( !match.Success || !match.Groups[ MethodGroup ].Success )
			{
				throw new ArgumentException( "The format is not valid.", "methodDescription" );
			}

			methodName = match.Groups[ MethodGroup ].Value;
			scope = match.Groups[ ScopeGroup ].Success ? match.Groups[ ScopeGroup ].Value : null;
			version = match.Groups[ VersionGroup ].Success ? Int32.Parse( match.Groups[ VersionGroup ].Value, CultureInfo.InvariantCulture ) : default( int? );
		}

		private readonly Dictionary<string, Dictionary<string, SortedList<int, OperationDescription>>> _catalog;

		/// <summary>
		///		Initializes a new instance of the <see cref="VersionedOperationCatalog"/> class.
		/// </summary>
		public VersionedOperationCatalog()
		{
			this._catalog = new Dictionary<string, Dictionary<string, SortedList<int, OperationDescription>>>();
		}

		/// <summary>
		/// Gets the <see cref="OperationDescription"/> for the specified method description.
		/// </summary>
		/// <param name="methodDescription">The method description.
		/// The format is derived class specific.
		/// This value is not null nor empty.</param>
		/// <returns>
		/// The <see cref="OperationDescription"/> if the item for the specified method description  is found;
		/// <c>null</c>, otherwise.
		/// </returns>
		/// <exception cref="ArgumentException">
		///		<paramref name="methodDescription"/> is invalid.
		/// </exception>
		protected override OperationDescription GetCore( string methodDescription )
		{
			string methodName;
			string scope;
			int? version;

			ParseMethodDescription( methodDescription, out methodName, out scope, out version );

			return this.Get( methodName, scope, version );
		}

		/// <summary>
		///		Gets the <see cref="OperationDescription"/> for the sepcified identifiers.
		/// </summary>
		/// <param name="methodName">
		///		A name of the method. 
		///		This value must be compliant with UAX-31 excepts starting with underscopre ('_', U+005F) is allowed.
		///	</param>
		/// <param name="scope">
		///		The scope. This value can be <c>null</c>. The default is an empty string.
		///		This value must be compliant with UAX-31 excepts starting with underscopre ('_', U+005F) is allowed.
		///	</param>
		/// <param name="version">
		///		The version. When <c>null</c> is specified, it assumed as the latest version.</param>
		/// <returns>
		///		The <see cref="OperationDescription"/> if the item for the specified method description  is found;
		///		<c>null</c>, otherwise.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="methodName"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="methodName"/> is empty or invalid.
		///		Or, <paramref name="scope"/> is invalid.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///		<paramref name="version"/> is negative.
		/// </exception>
		public OperationDescription Get( string methodName, string scope, int? version )
		{
			Validation.ValidateMethodName( methodName, "methodName" );

			if ( !String.IsNullOrEmpty( scope ) )
			{
				Validation.ValidateMethodName( scope, "scope" );
			}

			if ( version != null && version.Value < 0 )
			{
				throw new ArgumentOutOfRangeException( "version" );
			}

			Contract.EndContractBlock();

			return this.GetCore( methodName, scope ?? String.Empty, version );
		}

		private OperationDescription GetCore( string methodName, string scope, int? version )
		{
			var methods = this.PrivateGetMethodsInScope( scope );

			if ( methods == null )
			{
				return null;
			}

			var versionedMethod = PrivateGetVersionedMethods( methods, methodName );

			if ( versionedMethod == null )
			{
				return null;
			}

			if ( version == null )
			{
				return versionedMethod.Last().Value;
			}
			else
			{
				OperationDescription result;
				versionedMethod.TryGetValue( version.Value, out result );
				return result;
			}
		}

		private Dictionary<string, SortedList<int, OperationDescription>> PrivateGetMethodsInScope( string scope )
		{
			Dictionary<string, SortedList<int, OperationDescription>> result;
			this._catalog.TryGetValue( scope ?? String.Empty, out result );
			return result;
		}

		private static SortedList<int, OperationDescription> PrivateGetVersionedMethods( IDictionary<string, SortedList<int, OperationDescription>> methods, string methodName )
		{
			SortedList<int, OperationDescription> result;
			methods.TryGetValue( methodName, out result );
			return result;
		}

		private static void GetKeys( OperationDescription operation, out string methodName, out string scope, out int version )
		{
			methodName = operation.Id;
			scope = operation.Service.Name;
			version = operation.Service.Version;
		}

		/// <summary>
		/// Adds the specified <see cref="OperationDescription"/> to this catalog.
		/// </summary>
		/// <param name="operation">The <see cref="OperationDescription"/> to be added.
		/// This value is not <c>null</c>.</param>
		/// <returns>
		///   <c>true</c> if the <paramref name="operation"/> is added successfully;
		/// <c>false</c>, if it is not added because the operation which has same id already exists.
		/// </returns>
		protected override bool AddCore( OperationDescription operation )
		{
			string methodName;
			string scope;
			int version;
			GetKeys( operation, out methodName, out scope, out version );

			return this.AddCore( methodName, scope, version, operation );
		}

		private bool AddCore( string methodName, string scope, int version, OperationDescription operation )
		{
			bool mayExist = true;
			var methods = this.PrivateGetMethodsInScope( scope );
			if ( methods == null )
			{
				mayExist = false;
				methods = new Dictionary<string, SortedList<int, OperationDescription>>( _expectedMethodInTheScope );
				this._catalog.Add( scope, methods );
			}

			var versions = PrivateGetVersionedMethods( methods, methodName );
			if ( versions == null )
			{
				mayExist = false;
				versions = new SortedList<int, OperationDescription>( 1 );
				methods.Add( methodName, versions );
			}

			// Generally, there are few version to the method, so search twise is OK.
			if ( mayExist && versions.ContainsKey( version ) )
			{
				return false;
			}

			versions.Add( version, operation );

			return true;
		}

		/// <summary>
		/// Removes the specified <see cref="OperationDescription"/> from this catalog.
		/// </summary>
		/// <param name="operation">The <see cref="OperationDescription"/> to be removed.
		/// This value is not <c>null</c>.</param>
		/// <returns>
		///   <c>true</c> if the <paramref name="operation"/> is removed successfully;
		/// <c>false</c>, if it is not removed because the operation does not exist in this catalog.
		/// </returns>
		protected override bool RemoveCore( OperationDescription operation )
		{
			string methodName;
			string scope;
			int version;

			GetKeys( operation, out methodName, out scope, out version );

			return this.RemoveCore( methodName, scope, version );
		}

		/// <summary>
		///		Removes the specified <see cref="OperationDescription"/> with specified identifier from this catalog.
		/// </summary>
		/// <param name="methodName">
		///		A name of the method. 
		///		This value must be compliant with UAX-31 excepts starting with underscopre ('_', U+005F) is allowed.
		///	</param>
		/// <param name="scope">
		///		The scope. This value can be <c>null</c>. The default is an empty string.
		///		This value must be compliant with UAX-31 excepts starting with underscopre ('_', U+005F) is allowed.
		///	</param>
		/// <param name="version">
		///		The version.</param>
		/// <returns>
		///   <c>true</c> if the item is removed successfully;
		/// <c>false</c>, if it is not removed because the operation does not exist in this catalog.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="methodName"/> is <c>null</c>.
		///		Or, <paramref name="scope"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="methodName"/> is empty or invalid.
		///		Or, <paramref name="scope"/> is invalid.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///		<paramref name="version"/> is negative.
		/// </exception>
		public bool Remove( string methodName, string scope, int version )
		{
			Validation.ValidateMethodName( methodName, "methodName" );

			if ( !String.IsNullOrEmpty( scope ) )
			{
				Validation.ValidateMethodName( scope, "scope" );
			}

			if ( version < 0 )
			{
				throw new ArgumentOutOfRangeException( "version" );
			}

			Contract.EndContractBlock();

			return this.RemoveCore( methodName, scope, version );
		}

		private bool RemoveCore( string methodName, string scope, int version )
		{
			var methods = this.PrivateGetMethodsInScope( scope );
			if ( methods == null )
			{
				return false;
			}

			var versions = PrivateGetVersionedMethods( methods, methodName );
			if ( versions == null )
			{
				return false;
			}

			if ( !versions.Remove( version ) )
			{
				return false;
			}

			// Remove empty nodes.
			if ( versions.Count == 0 )
			{
				if ( methods.Remove( methodName ) && methods.Count == 0 )
				{
					this._catalog.Remove( scope );
				}
			}

			return true;
		}

		/// <summary>
		///		Removes all versions <see cref="OperationDescription"/> with specified name and scope from this catalog.
		/// </summary>
		/// <param name="methodName">
		///		A name of the method. 
		///		This value must be compliant with UAX-31 excepts starting with underscopre ('_', U+005F) is allowed.
		///	</param>
		/// <param name="scope">
		///		The scope. This value can be <c>null</c>. The default is an empty string.
		///		This value must be compliant with UAX-31 excepts starting with underscopre ('_', U+005F) is allowed.
		///	</param>
		///	<returns>
		///   <c>true</c> if any items are removed successfully;
		/// <c>false</c>, if it is not removed because no operations do not exist in this catalog.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="methodName"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="methodName"/> is empty or invalid.
		///		Or, <paramref name="scope"/> is invalid.
		/// </exception>
		public bool RemoveMethod( string methodName, string scope )
		{
			Validation.ValidateMethodName( methodName, "methodName" );

			if ( !String.IsNullOrEmpty( scope ) )
			{
				Validation.ValidateMethodName( scope, "scope" );
			}

			Contract.EndContractBlock();

			var methods = this.PrivateGetMethodsInScope( scope );
			if ( methods == null )
			{
				return false;
			}

			if ( !methods.Remove( methodName ) )
			{
				return false;
			}

			// Remove empty node.
			if ( methods.Count == 0 )
			{
				this._catalog.Remove( scope );
			}

			return true;
		}

		/// <summary>
		///		Removes all <see cref="OperationDescription"/> with specified scope from this catalog.
		/// </summary>
		/// <param name="scope">
		///		The scope. This value can be <c>null</c>. The default is an empty string.
		///		This value must be compliant with UAX-31 excepts starting with underscopre ('_', U+005F) is allowed.
		///	</param>
		///	<returns>
		///   <c>true</c> if any items are removed successfully;
		/// <c>false</c>, if it is not removed because no operations do not exist in this catalog.
		/// </returns>
		/// <exception cref="ArgumentException">
		///		<paramref name="scope"/> is invalid.
		/// </exception>
		public bool RemoveScope( string scope )
		{
			if ( !String.IsNullOrEmpty( scope ) )
			{
				Validation.ValidateMethodName( scope, "scope" );
			}

			Contract.EndContractBlock();

			return this._catalog.Remove( scope ?? String.Empty );
		}

		/// <summary>
		///		Clears all operations from this catalog.
		/// </summary>
		public override void Clear()
		{
			this._catalog.Clear();
		}

		/// <summary>
		///		Sets the capacity of this catalog to the actual number of elements it contains, rounded up to a nearby, implementation-specific value.
		/// </summary>
		public void TrimExcess()
		{
			foreach ( var entry in this._catalog )
			{
				foreach ( var versions in entry.Value )
				{
					versions.Value.TrimExcess();
				}
			}
		}

		/// <summary>
		///		Gets the methods for the specified scope.
		/// </summary>
		/// <param name="scope">
		///		The scope. This value can be <c>null</c>. The default is an empty string.
		///		This value must be compliant with UAX-31 excepts starting with underscopre ('_', U+005F) is allowed.
		///	</param>
		/// <returns>
		///		A collection of <see cref="KeyValuePair{TKey,TValue}"/>, which key is method name, and value is versioned <see cref="OperationDescription"/> collections.
		/// </returns>
		/// <exception cref="ArgumentException">
		///		<paramref name="scope"/> is invalid.
		/// </exception>
		[SuppressMessage( "Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "By design." )]
		public IEnumerable<KeyValuePair<string, IEnumerable<OperationDescription>>> GetMethods( string scope )
		{
			if ( !String.IsNullOrEmpty( scope ) )
			{
				Validation.ValidateMethodName( scope, "scope" );
			}

			Contract.EndContractBlock();

			var methods = this.PrivateGetMethodsInScope( scope ?? String.Empty );
			if ( methods != null )
			{
				foreach ( var method in methods )
				{
					yield return new KeyValuePair<string, IEnumerable<OperationDescription>>( method.Key, method.Value.Values );
				}
			}
		}

		/// <summary>
		///		Gets the versioned operations for the specified method name and scope.
		/// </summary>
		/// <param name="methodName">
		///		A name of the method. 
		///		This value must be compliant with UAX-31 excepts starting with underscopre ('_', U+005F) is allowed.
		///	</param>
		/// <param name="scope">
		///		The scope. This value can be <c>null</c>. The default is an empty string.
		///		This value must be compliant with UAX-31 excepts starting with underscopre ('_', U+005F) is allowed.
		///	</param>
		/// <returns>
		///		A collection of <see cref="KeyValuePair{TKey,TValue}"/>, which key is method name, and value is versioned <see cref="OperationDescription"/> collections.
		/// </returns>
		/// <exception cref="ArgumentException">
		///		<paramref name="scope"/> is invalid.
		/// </exception>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="methodName"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="methodName"/> is empty or invalid.
		///		Or, <paramref name="scope"/> is invalid.
		/// </exception>
		public IEnumerable<OperationDescription> GetVersions( string methodName, string scope )
		{
			Validation.ValidateMethodName( methodName, "methodName" );

			if ( !String.IsNullOrEmpty( scope ) )
			{
				Validation.ValidateMethodName( scope, "scope" );
			}

			Contract.EndContractBlock();

			var methods = this.PrivateGetMethodsInScope( scope );
			if ( methods != null )
			{
				var versions = PrivateGetVersionedMethods( methods, methodName );
				if ( versions != null )
				{
					foreach ( var version in versions.Values )
					{
						yield return version;
					}
				}
			}
		}

		/// <summary>
		///		Returns an enumerator to iterate all operations in this catalog.
		/// </summary>
		/// <returns>
		///   <see cref="T:System.Collections.Generic.IEnumerator`1"/> which can be used to interate all operations in this catalog.
		/// </returns>
		public override IEnumerator<OperationDescription> GetEnumerator()
		{
			foreach ( var scope in this._catalog )
			{
				foreach ( var versions in scope.Value )
				{
					foreach ( var method in versions.Value )
					{
						yield return method.Value;
					}
				}
			}
		}
	}
}
