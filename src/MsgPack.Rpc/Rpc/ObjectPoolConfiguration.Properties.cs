
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
using System.Linq;
using System.Text;

namespace MsgPack.Rpc
{
	// This file generated from ObjectPoolConfiguration.tt T4Template.
	// Do not modify this file. Edit ObjectPoolConfiguration.tt instead.

	partial class ObjectPoolConfiguration
	{
		private string _name = null;
		
		/// <summary>
		/// 	Gets or sets the name of the pool for debugging.
		/// </summary>
		/// <value>
		/// 	The name of the pool for debugging. The default is <c>null</c>.
		/// </value>
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceNameValue( ref coerced );
				this._name = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the Name property value.
		/// </summary>
		public void ResetName()
		{
			this._name = null;
		}
		
		static partial void CoerceNameValue( ref string value );

		private int _minimumReserved = 1;
		
		/// <summary>
		/// 	Gets or sets the minimum reserved object count in the pool.
		/// </summary>
		/// <value>
		/// 	The minimum reserved object count in the pool. The default is 1.
		/// </value>
		public int MinimumReserved
		{
			get
			{
				Contract.Ensures( Contract.Result<int>() >= default( int ) );

				return this._minimumReserved;
			}
			set
			{
				if ( !( value >= default( int ) ) )
				{
					throw new ArgumentOutOfRangeException( "value", "Argument cannot be negative number." );
				}

				Contract.Ensures( Contract.Result<int>() >= default( int ) );

				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceMinimumReservedValue( ref coerced );
				this._minimumReserved = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the MinimumReserved property value.
		/// </summary>
		public void ResetMinimumReserved()
		{
			this._minimumReserved = 1;
		}
		
		static partial void CoerceMinimumReservedValue( ref int value );

		private int? _maximumPooled = null;
		
		/// <summary>
		/// 	Gets or sets the maximum poolable objects count.
		/// </summary>
		/// <value>
		/// 	The maximum poolable objects count. <c>null</c> indicates unlimited pooling. The default is <c>null</c>.
		/// </value>
		public int? MaximumPooled
		{
			get
			{
				Contract.Ensures( Contract.Result<int?>() == null || Contract.Result<int?>().Value >= default( int ) );

				return this._maximumPooled;
			}
			set
			{
				if ( !( value == null || value.Value >= default( int ) ) )
				{
					throw new ArgumentOutOfRangeException( "value", "Argument cannot be negative number." );
				}

				Contract.Ensures( Contract.Result<int?>() == null || Contract.Result<int?>().Value >= default( int ) );

				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceMaximumPooledValue( ref coerced );
				this._maximumPooled = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the MaximumPooled property value.
		/// </summary>
		public void ResetMaximumPooled()
		{
			this._maximumPooled = null;
		}
		
		static partial void CoerceMaximumPooledValue( ref int? value );

		private ExhausionPolicy _exhausionPolicy = ExhausionPolicy.BlockUntilAvailable;
		
		/// <summary>
		/// 	Gets or sets the exhausion policy of the pool.
		/// </summary>
		/// <value>
		/// 	The exhausion policy of the pool. The default is <see cref="F:ExhausionPolicy.BlockUntilAvailable"/>.
		/// </value>
		public ExhausionPolicy ExhausionPolicy
		{
			get
			{
				Contract.Ensures( Enum.IsDefined( typeof( ExhausionPolicy ), Contract.Result<ExhausionPolicy>() ) );

				return this._exhausionPolicy;
			}
			set
			{
				if ( !( Enum.IsDefined( typeof( ExhausionPolicy ), value ) ) )
				{
					throw new ArgumentOutOfRangeException( "value", String.Format( CultureInfo.CurrentCulture, "Argument must be valid enum value of '{0}' type.", typeof( ExhausionPolicy ) ) );
				}

				Contract.Ensures( Enum.IsDefined( typeof( ExhausionPolicy ), Contract.Result<ExhausionPolicy>() ) );

				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceExhausionPolicyValue( ref coerced );
				this._exhausionPolicy = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the ExhausionPolicy property value.
		/// </summary>
		public void ResetExhausionPolicy()
		{
			this._exhausionPolicy = ExhausionPolicy.BlockUntilAvailable;
		}
		
		static partial void CoerceExhausionPolicyValue( ref ExhausionPolicy value );

		private TimeSpan? _borrowTimeout = null;
		
		/// <summary>
		/// 	Gets or sets the maximum concurrency for the each clients.
		/// </summary>
		/// <value>
		/// 	The timeout of blocking of the borrowing when the pool is exhausited. <c>null</c> indicates unlimited waiting. The default is <c>null</c>.
		/// </value>
		public TimeSpan? BorrowTimeout
		{
			get
			{
				Contract.Ensures( Contract.Result<TimeSpan?>() == null || Contract.Result<TimeSpan?>().Value > default( TimeSpan ) );

				return this._borrowTimeout;
			}
			set
			{
				if ( !( value == null || value.Value > default( TimeSpan ) ) )
				{
					throw new ArgumentOutOfRangeException( "value", "Argument must be positive number." );
				}

				Contract.Ensures( Contract.Result<TimeSpan?>() == null || Contract.Result<TimeSpan?>().Value > default( TimeSpan ) );

				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceBorrowTimeoutValue( ref coerced );
				this._borrowTimeout = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the BorrowTimeout property value.
		/// </summary>
		public void ResetBorrowTimeout()
		{
			this._borrowTimeout = null;
		}
		
		static partial void CoerceBorrowTimeoutValue( ref TimeSpan? value );

		private TimeSpan? _evitionInterval = TimeSpan.FromMinutes( 3 );
		
		/// <summary>
		/// 	Gets or sets the interval to evict extra pooled objects.
		/// </summary>
		/// <value>
		/// 	The interval to evict extra pooled objects. The default is 3 minutes.
		/// </value>
		public TimeSpan? EvitionInterval
		{
			get
			{
				Contract.Ensures( Contract.Result<TimeSpan?>() == null || Contract.Result<TimeSpan?>().Value > default( TimeSpan ) );

				return this._evitionInterval;
			}
			set
			{
				if ( !( value == null || value.Value > default( TimeSpan ) ) )
				{
					throw new ArgumentOutOfRangeException( "value", "Argument must be positive number." );
				}

				Contract.Ensures( Contract.Result<TimeSpan?>() == null || Contract.Result<TimeSpan?>().Value > default( TimeSpan ) );

				this.VerifyIsNotFrozen();
				var coerced = value;
				CoerceEvitionIntervalValue( ref coerced );
				this._evitionInterval = coerced;
			}
		}
		
		/// <summary>
		/// 	Resets the EvitionInterval property value.
		/// </summary>
		public void ResetEvitionInterval()
		{
			this._evitionInterval = TimeSpan.FromMinutes( 3 );
		}
		
		static partial void CoerceEvitionIntervalValue( ref TimeSpan? value );

		/// <summary>
		/// 	Returns a string that represents the current object.
		/// </summary>
		/// <returns>
		/// 	A string that represents the current object.
		/// </returns>
		public sealed override string ToString()
		{
			var buffer = new StringBuilder( 256 );
			buffer.Append( "{ " );
			buffer.Append( "\"Name\" : " );
			ToString( this.Name, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"MinimumReserved\" : " );
			ToString( this.MinimumReserved, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"MaximumPooled\" : " );
			ToString( this.MaximumPooled, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"ExhausionPolicy\" : " );
			ToString( this.ExhausionPolicy, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"BorrowTimeout\" : " );
			ToString( this.BorrowTimeout, buffer );
			buffer.Append( ", " );
			buffer.Append( "\"EvitionInterval\" : " );
			ToString( this.EvitionInterval, buffer );
			buffer.Append( " }" );
			return buffer.ToString();
		}
		
		private static void ToString<T>( T value, StringBuilder buffer )
		{
			if( value == null )
			{
				buffer.Append( "null" );
			}
			
			if( typeof( Delegate ).IsAssignableFrom( typeof( T ) ) )
			{
				var asDelegate = ( Delegate )( object )value;
				buffer.Append( "\"Type='" ).Append( asDelegate.Method.DeclaringType );

				if( asDelegate.Target != null )
				{
					buffer.Append( "', Instance='" ).Append( asDelegate.Target );
				}

				buffer.Append( "', Method='" ).Append( asDelegate.Method ).Append( "'\"" );
				return;
			}

			switch( Type.GetTypeCode( typeof( T ) ) )
			{
				case TypeCode.Boolean:
				{
					buffer.Append( value.ToString().ToLowerInvariant() );
					break;
				}
				case TypeCode.Byte:
				case TypeCode.Double:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.SByte:
				case TypeCode.Single:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				{
					buffer.Append( value.ToString() );
					break;
				}
				default:
				{
					buffer.Append( '"' ).Append( value.ToString() ).Append( '"' );
					break;
				}
			}
		}
	}
}
