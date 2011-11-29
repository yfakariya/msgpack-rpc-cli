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

namespace MsgPack
{
	// This file generated from RpcServerOptions.tt T4Template.
	// Do not modify this file. Edit RpcServerOptions.tt instead.

	/// <summary>
	///		Represents various configuration information of MessagePack-RPC client.
	/// </summary>
	public sealed class RpcServerOptions
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

		private int? _InitialSendBufferSize;

		/// <summary>
		///		Get or set initial buffer size of sending buffer in bytes.
		/// </summary>
		/// <value>
		///		Initial buffer size of sending buffer in bytes.
		/// </value>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public int? InitialSendBufferSize
		{
			get
			{
				return this._InitialSendBufferSize;
			}
			set
			{
				if( this._isFrozen )
				{
					throw new InvalidOperationException( "This instance is frozen." );
				}

				Contract.EndContractBlock();

				this._InitialSendBufferSize = value;
			}
		}

		private int? _ReceiveBufferSize;

		/// <summary>
		///		Get or set receive buffer size in bytes.
		/// </summary>
		/// <value>
		///		Receive buffer size in bytes.
		/// </value>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public int? ReceiveBufferSize
		{
			get
			{
				return this._ReceiveBufferSize;
			}
			set
			{
				if( this._isFrozen )
				{
					throw new InvalidOperationException( "This instance is frozen." );
				}

				Contract.EndContractBlock();

				this._ReceiveBufferSize = value;
			}
		}

		private int? _AcceptConcurrency;

		/// <summary>
		///		Get or set concurrency of 'Accept' operation in <see cref="IOCompletionPortServerEventLoop"/> and <see cref="PollingServerEventLoop"/>.
		/// </summary>
		/// <value>
		///		Concurrency of 'Accept' operation in <see cref="IOCompletionPortServerEventLoop"/> and <see cref="PollingServerEventLoop"/>.
		/// </value>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public int? AcceptConcurrency
		{
			get
			{
				return this._AcceptConcurrency;
			}
			set
			{
				if( this._isFrozen )
				{
					throw new InvalidOperationException( "This instance is frozen." );
				}

				Contract.EndContractBlock();

				this._AcceptConcurrency = value;
			}
		}

		private int? _SendingConcurrency;

		/// <summary>
		///		Get or set concurrency of 'Send' operation in <see cref="PollingServerEventLoop"/>.
		/// </summary>
		/// <value>
		///		Concurrency of 'Send' operation in <see cref="PollingServerEventLoop"/>.
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
		///		Get or set concurrency of 'Receive' operation in <see cref="PollingServerEventLoop"/>.
		/// </summary>
		/// <value>
		///		Concurrency of 'Receive' operation in <see cref="PollingServerEventLoop"/>.
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

		private TimeSpan? _TimeoutWatchPeriod;

		/// <summary>
		///		Get or set period of execution timeout monitoring.
		/// </summary>
		/// <value>
		///		Period of execution timeout monitoring.
		/// </value>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public TimeSpan? TimeoutWatchPeriod
		{
			get
			{
				return this._TimeoutWatchPeriod;
			}
			set
			{
				if( this._isFrozen )
				{
					throw new InvalidOperationException( "This instance is frozen." );
				}

				Contract.EndContractBlock();

				this._TimeoutWatchPeriod = value;
			}
		}

		private TimeSpan? _ExecutionTimeout;

		/// <summary>
		///		Get or set duration to timeout worker thread execution.
		/// </summary>
		/// <value>
		///		Duration to timeout worker thread execution.
		/// </value>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public TimeSpan? ExecutionTimeout
		{
			get
			{
				return this._ExecutionTimeout;
			}
			set
			{
				if( this._isFrozen )
				{
					throw new InvalidOperationException( "This instance is frozen." );
				}

				Contract.EndContractBlock();

				this._ExecutionTimeout = value;
			}
		}

		private int? _SendingQueueLength;

		/// <summary>
		///		Get or set limit of queue of sending message in <see cref="PollingServerEventLoop"/>.
		/// </summary>
		/// <value>
		///		Limit of queue of sending message in <see cref="PollingServerEventLoop"/>.
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
		///		Get or set limit of queue of receiving message in <see cref="PollingServerEventLoop"/>.
		/// </summary>
		/// <value>
		///		Limit of queue of receiving message in <see cref="PollingServerEventLoop"/>.
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

		/// <summary>
		///		Initialize new instance.
		/// </summary>
		public RpcServerOptions()
		{
			// nop.
		}
	}
}