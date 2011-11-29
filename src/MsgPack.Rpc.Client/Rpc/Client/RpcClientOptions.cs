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
	// This file generated from RpcClientOptions.tt T4Template.
	// Do not modify this file. Edit RpcClientOptions.tt instead.

	/// <summary>
	///		Represents various configuration information of MessagePack-RPC client.
	/// </summary>
	public sealed class RpcClientOptions
	{
		private bool _isFrozen;

		/// <summary>
		///		Get the value which indicates this instance is frozen or not.
		/// </summary>
		/// <value>
		///		If this instance is frozen then true.
		/// </value>
		public bool IsFrozen
		{
			get{ return this._isFrozen; }
		}

		/// <summary>
		///		Freeze this instance.
		/// </summary>
		/// <remarks>
		///		Frozen instance will be immutable.
		/// </remarks>
		public void Freeze()
		{
			this._isFrozen = true;
		}

		private int? _BufferSegmentSize;

		/// <summary>
		///		Get buffer segment size of buffer in bytes.
		/// </summary>
		/// <value>
		///		Buffer chunk size of buffer in bytes.
		/// </value>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public int? BufferSegmentSize
		{
			get
			{
				return this._BufferSegmentSize;
			}
			set
			{
				if( this._isFrozen )
				{
					throw new InvalidOperationException( "This instance is frozen." );
				}

				Contract.EndContractBlock();

				this._BufferSegmentSize = value;
			}
		}

		private int? _BufferSegmentCount;

		/// <summary>
		///		Get buffer segment count of buffer in bytes.
		/// </summary>
		/// <value>
		///		Buffer chunk size of buffer in bytes.
		/// </value>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public int? BufferSegmentCount
		{
			get
			{
				return this._BufferSegmentCount;
			}
			set
			{
				if( this._isFrozen )
				{
					throw new InvalidOperationException( "This instance is frozen." );
				}

				Contract.EndContractBlock();

				this._BufferSegmentCount = value;
			}
		}

		private int? _MinimumConnectionCount;

		/// <summary>
		///		Get minimum count of connection to preserve in pool.
		/// </summary>
		/// <value>
		///		Minimum count of connection to preserve in pool.
		/// </value>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public int? MinimumConnectionCount
		{
			get
			{
				return this._MinimumConnectionCount;
			}
			set
			{
				if( this._isFrozen )
				{
					throw new InvalidOperationException( "This instance is frozen." );
				}

				Contract.EndContractBlock();

				this._MinimumConnectionCount = value;
			}
		}

		private int? _MaximumConnectionCount;

		/// <summary>
		///		Get maximum count of connection to hold in pool.
		/// </summary>
		/// <value>
		///		Maximum count of connection to hold in pool.
		/// </value>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public int? MaximumConnectionCount
		{
			get
			{
				return this._MaximumConnectionCount;
			}
			set
			{
				if( this._isFrozen )
				{
					throw new InvalidOperationException( "This instance is frozen." );
				}

				Contract.EndContractBlock();

				this._MaximumConnectionCount = value;
			}
		}

		private int? _ConnectingConcurrency;

		/// <summary>
		///		Get concurrency of 'Connect' operation in <see cref="PollingClientEventLoop"/>.
		/// </summary>
		/// <value>
		///		Concurrency of 'Connect' operation in <see cref="PollingClientEventLoop"/>.
		/// </value>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public int? ConnectingConcurrency
		{
			get
			{
				return this._ConnectingConcurrency;
			}
			set
			{
				if( this._isFrozen )
				{
					throw new InvalidOperationException( "This instance is frozen." );
				}

				Contract.EndContractBlock();

				this._ConnectingConcurrency = value;
			}
		}

		private int? _SendingConcurrency;

		/// <summary>
		///		Get concurrency of 'Send' operation in <see cref="PollingClientEventLoop"/>.
		/// </summary>
		/// <value>
		///		Concurrency of 'Send' operation in <see cref="PollingClientEventLoop"/>.
		/// </value>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public int? SendingConcurrency
		{
			get
			{
				return this._SendingConcurrency;
			}
			set
			{
				if( this._isFrozen )
				{
					throw new InvalidOperationException( "This instance is frozen." );
				}

				Contract.EndContractBlock();

				this._SendingConcurrency = value;
			}
		}

		private int? _ReceivingConcurrency;

		/// <summary>
		///		Get concurrency of 'Receive' operation in <see cref="PollingClientEventLoop"/>.
		/// </summary>
		/// <value>
		///		Concurrency of 'Receive' operation in <see cref="PollingClientEventLoop"/>.
		/// </value>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public int? ReceivingConcurrency
		{
			get
			{
				return this._ReceivingConcurrency;
			}
			set
			{
				if( this._isFrozen )
				{
					throw new InvalidOperationException( "This instance is frozen." );
				}

				Contract.EndContractBlock();

				this._ReceivingConcurrency = value;
			}
		}

		private int? _ConnectingQueueLength;

		/// <summary>
		///		Get number of connection to pool.
		/// </summary>
		/// <value>
		///		Number of connection to pool
		/// </value>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public int? ConnectingQueueLength
		{
			get
			{
				return this._ConnectingQueueLength;
			}
			set
			{
				if( this._isFrozen )
				{
					throw new InvalidOperationException( "This instance is frozen." );
				}

				Contract.EndContractBlock();

				this._ConnectingQueueLength = value;
			}
		}

		private int? _SendingQueueLength;

		/// <summary>
		///		Get limit of queue of sending message in <see cref="PollingClientEventLoop"/>.
		/// </summary>
		/// <value>
		///		Limit of queue of sending message in <see cref="PollingClientEventLoop"/>.
		/// </value>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public int? SendingQueueLength
		{
			get
			{
				return this._SendingQueueLength;
			}
			set
			{
				if( this._isFrozen )
				{
					throw new InvalidOperationException( "This instance is frozen." );
				}

				Contract.EndContractBlock();

				this._SendingQueueLength = value;
			}
		}

		private int? _ReceivingQueueLength;

		/// <summary>
		///		Get limit of queue of receiving message in <see cref="PollingClientEventLoop"/>.
		/// </summary>
		/// <value>
		///		Limit of queue of receiving message in <see cref="PollingClientEventLoop"/>.
		/// </value>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public int? ReceivingQueueLength
		{
			get
			{
				return this._ReceivingQueueLength;
			}
			set
			{
				if( this._isFrozen )
				{
					throw new InvalidOperationException( "This instance is frozen." );
				}

				Contract.EndContractBlock();

				this._ReceivingQueueLength = value;
			}
		}

		private bool? _UseConnectionPooling;

		/// <summary>
		///		Get whether connection pooling is used.
		/// </summary>
		/// <value>
		///		If connection pooling is used then true.
		/// </value>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public bool? UseConnectionPooling
		{
			get
			{
				return this._UseConnectionPooling;
			}
			set
			{
				if( this._isFrozen )
				{
					throw new InvalidOperationException( "This instance is frozen." );
				}

				Contract.EndContractBlock();

				this._UseConnectionPooling = value;
			}
		}

		private bool? _ForceIPv4;

		/// <summary>
		///		Get whether force using IP v4 even if IP v6 is available.
		/// </summary>
		/// <value>
		///		If IP v4 is forced then true.
		/// </value>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public bool? ForceIPv4
		{
			get
			{
				return this._ForceIPv4;
			}
			set
			{
				if( this._isFrozen )
				{
					throw new InvalidOperationException( "This instance is frozen." );
				}

				Contract.EndContractBlock();

				this._ForceIPv4 = value;
			}
		}

		private int? _MaximumRequestQuota;

		/// <summary>
		///		Get maximum request length in bytes.
		/// </summary>
		/// <value>
		///		Maximum request length in bytes.
		/// </value>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public int? MaximumRequestQuota
		{
			get
			{
				return this._MaximumRequestQuota;
			}
			set
			{
				if( this._isFrozen )
				{
					throw new InvalidOperationException( "This instance is frozen." );
				}

				Contract.EndContractBlock();

				this._MaximumRequestQuota = value;
			}
		}

		private int? _MaximumResponseQuota;

		/// <summary>
		///		Get maximum response length in bytes.
		/// </summary>
		/// <value>
		///		Maximum response length in bytes.
		/// </value>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public int? MaximumResponseQuota
		{
			get
			{
				return this._MaximumResponseQuota;
			}
			set
			{
				if( this._isFrozen )
				{
					throw new InvalidOperationException( "This instance is frozen." );
				}

				Contract.EndContractBlock();

				this._MaximumResponseQuota = value;
			}
		}

		private TimeSpan? _ConnectTimeout;

		/// <summary>
		///		Get socket connect timeout.
		/// </summary>
		/// <value>
		///		Socket connect timeout.
		/// </value>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public TimeSpan? ConnectTimeout
		{
			get
			{
				return this._ConnectTimeout;
			}
			set
			{
				if( this._isFrozen )
				{
					throw new InvalidOperationException( "This instance is frozen." );
				}

				Contract.EndContractBlock();

				this._ConnectTimeout = value;
			}
		}

		private TimeSpan? _DrainTimeout;

		/// <summary>
		///		Get socket drain timeout.
		/// </summary>
		/// <value>
		///		Socket drain timeout.
		/// </value>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public TimeSpan? DrainTimeout
		{
			get
			{
				return this._DrainTimeout;
			}
			set
			{
				if( this._isFrozen )
				{
					throw new InvalidOperationException( "This instance is frozen." );
				}

				Contract.EndContractBlock();

				this._DrainTimeout = value;
			}
		}

		/// <summary>
		///		Initialize new instance.
		/// </summary>
		public RpcClientOptions()
		{
			// nop.
		}
	}
}