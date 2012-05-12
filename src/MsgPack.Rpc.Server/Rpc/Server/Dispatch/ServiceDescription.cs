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
using System.Linq.Expressions;
using System.Reflection;
using MsgPack.Rpc.Server.Dispatch.Reflection;
using MsgPack.Serialization;

namespace MsgPack.Rpc.Server.Dispatch
{
	/// <summary>
	///		Defines service specification description.
	/// </summary>
	public sealed class ServiceDescription
	{
		internal static readonly PropertyInfo InitializerProperty =
			FromExpression.ToProperty( ( ServiceDescription @this ) => @this.Initializer );

		private readonly string _name;

		/// <summary>
		///		Gets the name of the service.
		/// </summary>
		/// <value>
		///		The name of the service.
		///		This value is non-null valid identifier.
		/// </value>
		public string Name
		{
			get
			{
				Contract.Ensures( !String.IsNullOrWhiteSpace( Contract.Result<string>() ) );
				return this._name;
			}
		}

		private readonly Func<object> _initializer;

		/// <summary>
		///		Gets the initializer routine to instantiate the service.
		/// </summary>
		/// <value>
		///		The initializer routine to instantiate the service.
		///		This value will not be <c>null</c>.
		/// </value>
		public Func<object> Initializer
		{
			get
			{
				Contract.Ensures( Contract.Result<Func<object>>() != null );
				return this._initializer;
			}
		}

		private Type _serviceType;

		/// <summary>
		///		Gets the type of the service.
		/// </summary>
		/// <value>
		///		The type of the service.
		///		This value will not be <c>null</c>.
		/// </value>
		public Type ServiceType
		{
			get
			{
				Contract.Ensures( Contract.Result<Type>() != null );
				return this._serviceType ?? this._initializer.Method.DeclaringType;
			}
		}

		private int _version;

		/// <summary>
		///		Gets or sets the version number of this service.
		/// </summary>
		/// <value>
		///		The version number. This value may not be negative.
		///		Default is 0.
		/// </value>
		/// <exception cref="ArgumentOutOfRangeException">
		///		The setter is invoked by negative value.
		/// </exception>
		public int Version
		{
			get
			{
				Contract.Ensures( Contract.Result<int>() >= 0 );
				return this._version;
			}
			set
			{
				if ( value < 0 )
				{
					throw new ArgumentOutOfRangeException( "value" );
				}

				this._version = value;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ServiceDescription"/> class.
		/// </summary>
		/// <param name="name">The name of the service.</param>
		/// <param name="initializer">The initializer of the service instance.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="initializer"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="name"/> is not valid identifier.
		/// </exception>
		public ServiceDescription( string name, Func<object> initializer )
		{
			if ( initializer == null )
			{
				throw new ArgumentNullException( "initializer" );
			}

			Contract.EndContractBlock();

			this._name = String.IsNullOrEmpty( name ) ? String.Empty : RpcIdentifierUtility.EnsureValidIdentifier( name, "name" );
			this._initializer = initializer;
		}

		/// <summary>
		///		Creates new instance from the specified service type.
		/// </summary>
		/// <param name="serviceType">The concrete type which implements given service contract.</param>
		/// <returns>
		///		<see cref="ServiceDescription"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="serviceType"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="serviceType"/> is abstract class or interface.
		///		Or, <paramref name="serviceType"/> does not have service contract, that is, it is not marked by <see cref="MessagePackRpcServiceContractAttribute"/>.
		///		Or, <paramref name="serviceType"/> does not have publicly visible default constructor.
		///		Or, any <see cref="MessagePackRpcServiceContractAttribute"/> property of <paramref name="serviceType"/> is invalid.
		/// </exception>
		public static ServiceDescription FromServiceType( Type serviceType )
		{
			if ( serviceType == null )
			{
				throw new ArgumentNullException( "serviceType" );
			}

			if ( serviceType.IsAbstract )
			{
				throw new ArgumentException(
					String.Format(
						CultureInfo.CurrentCulture,
						"Service type '{0}' is not concrete type.",
						serviceType.AssemblyQualifiedName
					),
					"serviceType"
				);
			}

			Contract.EndContractBlock();

			var serviceContract = Attribute.GetCustomAttribute( serviceType, typeof( MessagePackRpcServiceContractAttribute ), true ) as MessagePackRpcServiceContractAttribute;
			if ( serviceContract == null )
			{
				throw new ArgumentException(
					String.Format(
						CultureInfo.CurrentCulture,
						"Service type '{0}' does not have service contract.",
						serviceType.AssemblyQualifiedName
					),
					"serviceType"
				);
			}

			var serviceName = String.IsNullOrWhiteSpace( serviceContract.Name ) ? ServiceIdentifier.TruncateGenericsSuffix( serviceType.Name ) : serviceContract.Name;

			var ctor = serviceType.GetConstructor( Type.EmptyTypes );
			if ( ctor == null )
			{
				throw new ArgumentException(
					String.Format(
						CultureInfo.CurrentCulture,
						"Service type '{0}' does not have public default constructor.",
						serviceType.AssemblyQualifiedName
					),
					"serviceType"
				);
			}

			return
				new ServiceDescription( serviceName, Expression.Lambda<Func<object>>( Expression.New( ctor ) ).Compile() )
				{
					Version = serviceContract.Version,
					_serviceType = serviceType
				};
		}

		/// <summary>
		///		Determines whether the specified <see cref="System.Object"/> is equal to this instance.
		/// </summary>
		/// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
		/// <returns>
		///		<c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
		/// </returns>
		public sealed override bool Equals( object obj )
		{
			if ( Object.ReferenceEquals( this, obj ) )
			{
				return true;
			}

			var other = obj as ServiceDescription;
			if ( Object.ReferenceEquals( obj, null ) )
			{
				return false;
			}

			return this._name == other._name && this._version == other._version && this.ServiceType == other.ServiceType;
		}

		/// <summary>
		///		Returns a hash code for this instance.
		/// </summary>
		/// <returns>
		///		A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
		/// </returns>
		public sealed override int GetHashCode()
		{
			return this._name.GetHashCode() ^ this._version.GetHashCode() ^ this.ServiceType.GetHashCode();
		}

		/// <summary>
		///		Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		///		A <see cref="System.String"/> that represents this instance.
		/// </returns>
		public sealed override string ToString()
		{
			return ServiceIdentifier.CreateServiceId( this._name, this._version );
		}

		/// <summary>
		///		Determines whether specified <see cref="ServiceDescription"/>s are identical.
		/// </summary>
		/// <param name="left">The <see cref="ServiceDescription"/>.</param>
		/// <param name="right">The <see cref="ServiceDescription"/>.</param>
		/// <returns>
		///		<c>true</c> if the specified <see cref="ServiceDescription"/> are equal; otherwise, <c>false</c>.
		/// </returns>
		public static bool operator ==( ServiceDescription left, ServiceDescription right )
		{
			if ( Object.ReferenceEquals( left, null ) )
			{
				return Object.ReferenceEquals( right, null );
			}
			else
			{
				return left.Equals( right );
			}
		}

		/// <summary>
		///		Determines whether specified <see cref="ServiceDescription"/>s are not identical.
		/// </summary>
		/// <param name="left">The <see cref="ServiceDescription"/>.</param>
		/// <param name="right">The <see cref="ServiceDescription"/>.</param>
		/// <returns>
		///		<c>true</c> if the specified <see cref="ServiceDescription"/> are not equal; otherwise, <c>false</c>.
		/// </returns>
		public static bool operator !=( ServiceDescription left, ServiceDescription right )
		{
			return !( left == right );
		}
	}
}
