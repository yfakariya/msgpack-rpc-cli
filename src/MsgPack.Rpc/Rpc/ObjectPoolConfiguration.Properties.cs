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

namespace MsgPack.Rpc
{
	// This file generated from ObjectPoolConfiguration.tt T4Template.
	// Do not modify this file. Edit ObjectPoolConfiguration.tt instead.

	partial class ObjectPoolConfiguration
	{

		private int _minimumReserved = 1;
		
		/// <summary>
		/// 	Gets or sets the minimum reserved object count in the pool.
		/// </summary>
		/// <value>
		/// 	The minimum reserved object count in the pool. The default is 1.
		/// </value>
		public int MinimumReserved
		{
			get{ return this._minimumReserved; }
			set
			{
				this.VerifyIsNotFrozen();
				
				if( value == null )
				{
					this._minimumReserved = 1;
				}
				else
				{
					ValidateMinimumReserved( value );
					this._minimumReserved = value;
				}
			}
		}
		
		static partial void ValidateMinimumReserved( int value );

		private int? _maximumPooled = null;
		
		/// <summary>
		/// 	Gets or sets the maximum poolable objects count.
		/// </summary>
		/// <value>
		/// 	The maximum poolable objects count. <c>null</c> indicates unlimited pooling. The default is <c>null</c>.
		/// </value>
		public int? MaximumPooled
		{
			get{ return this._maximumPooled; }
			set
			{
				this.VerifyIsNotFrozen();
				
				if( value == null )
				{
					this._maximumPooled = null;
				}
				else
				{
					ValidateMaximumPooled( value );
					this._maximumPooled = value;
				}
			}
		}
		
		static partial void ValidateMaximumPooled( int? value );

		private ExhausionPolicy _exhausionPolicy = ExhausionPolicy.BlockUntilAvailable;
		
		/// <summary>
		/// 	Gets or sets the exhausion policy of the pool.
		/// </summary>
		/// <value>
		/// 	The exhausion policy of the pool. The default is <see cref="ExhausionPolicy.BlockUntilAvailable"/>.
		/// </value>
		public ExhausionPolicy ExhausionPolicy
		{
			get{ return this._exhausionPolicy; }
			set
			{
				this.VerifyIsNotFrozen();
				
				if( value == null )
				{
					this._exhausionPolicy = ExhausionPolicy.BlockUntilAvailable;
				}
				else
				{
					ValidateExhausionPolicy( value );
					this._exhausionPolicy = value;
				}
			}
		}
		
		static partial void ValidateExhausionPolicy( ExhausionPolicy value );

		private TimeSpan? _borrowTimeout = null;
		
		/// <summary>
		/// 	Gets or sets the maximum concurrency for the each clients.
		/// </summary>
		/// <value>
		/// 	The timeout of blocking of the borrowing when the pool is exhausited. <c>null</c> indicates unlimited waiting. The default is <c>null</c>.
		/// </value>
		public TimeSpan? BorrowTimeout
		{
			get{ return this._borrowTimeout; }
			set
			{
				this.VerifyIsNotFrozen();
				
				if( value == null )
				{
					this._borrowTimeout = null;
				}
				else
				{
					ValidateBorrowTimeout( value );
					this._borrowTimeout = value;
				}
			}
		}
		
		static partial void ValidateBorrowTimeout( TimeSpan? value );

		private TimeSpan? _evitionInterval = TimeSpan.FromMinutes( 3 );
		
		/// <summary>
		/// 	Gets or sets the interval to evict extra pooled objects.
		/// </summary>
		/// <value>
		/// 	The interval to evict extra pooled objects. The default is 3 minutes.
		/// </value>
		public TimeSpan? EvitionInterval
		{
			get{ return this._evitionInterval; }
			set
			{
				this.VerifyIsNotFrozen();
				
				if( value == null )
				{
					this._evitionInterval = TimeSpan.FromMinutes( 3 );
				}
				else
				{
					ValidateEvitionInterval( value );
					this._evitionInterval = value;
				}
			}
		}
		
		static partial void ValidateEvitionInterval( TimeSpan? value );
	}
}
