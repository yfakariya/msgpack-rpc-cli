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
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Security;
using System.Threading;
using MsgPack.Serialization;

namespace MsgPack.Rpc.Server
{
	/// <summary>
	///		Provides RPC runtime services for the application or extensions.
	/// </summary>
	public sealed class RpcServerRuntime
	{
		private readonly RpcServerConfiguration _configuration;

		/// <summary>
		///		Gets a value indicating whether this server is debug mode.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this server is debug mode; otherwise, <c>false</c>.
		/// </value>
		public bool IsDebugMode
		{
			get { return this._configuration.IsDebugMode; }
		}

		private readonly ObjectPool<RpcApplicationContext> _applicationContextPool;

		private readonly SerializationContext _serializationContext;

		/// <summary>
		///		Gets the <see cref="T:SerializationContext"/> to store serialization context information for this runtime.
		/// </summary>
		/// <value>
		///		The <see cref="T:SerializationContext"/> to store serialization context information for this runtime.
		/// </value>
		public SerializationContext SerializationContext
		{
			get { return this._serializationContext; }
		}

		private readonly MessagePackObject _softTimeoutDetails;

		internal RpcServerRuntime( RpcServerConfiguration configuration, SerializationContext serializationContext )
		{
			this._configuration = configuration;
			this._serializationContext = serializationContext;
			this._applicationContextPool =
				configuration.ApplicationContextPoolProvider(
					() => new RpcApplicationContext(
						configuration.ExecutionTimeout,
						configuration.HardExecutionTimeout
					),
				configuration.CreateApplicationContextPoolConfiguration()
			);
			this._softTimeoutDetails =
				new MessagePackObject(
					new MessagePackObjectDictionary()
					{
						{ RpcException.MessageKeyUtf8, "Execution timeout." },
						{ 
							RpcException.DebugInformationKeyUtf8, 
							String.Format( 
								CultureInfo.InvariantCulture, 
								"{{ \"ExecutionTimeout\" : \"{0}\", \"HardExecutionTimeout\" : \"{1}\" }}",
								configuration.ExecutionTimeout,
								configuration.HardExecutionTimeout
							)
						}
					},
					true
				);
		}

		/// <summary>
		///		Creates new <see cref="RpcServerRuntime"/>.
		/// </summary>
		/// <param name="configuration">The configuration to be used. To use default, specify <c>null</c>.</param>
		/// <param name="serializationContext">The serialization context to be used. To use default, specify <c>null</c>.</param>
		/// <returns>
		///		The <see cref="RpcServerRuntime"/> instance.
		/// </returns>
		/// <remarks>
		///		This method is mainly provided for testing purposes.
		///		You should use <see cref="P:Dispatcher.Runtime"/> property instead.
		/// </remarks>
		public static RpcServerRuntime Create( RpcServerConfiguration configuration, SerializationContext serializationContext )
		{
			Contract.Ensures( Contract.Result<RpcServerRuntime>() != null );

			return new RpcServerRuntime( configuration ?? RpcServerConfiguration.Default, serializationContext ?? new SerializationContext() );
		}

		/// <summary>
		///		Notifies to RPC runtime to begin service operation.
		/// </summary>
		/// <remarks>
		///		Currently, this method performs following:
		///		<list type="bullet">
		///			<item>Sets <see cref="RpcApplicationContext"/>.</item>
		///			<item>Starts execution timeout wathing.</item>
		///		</list>
		/// </remarks>
		public void BeginOperation()
		{
			var context = this._applicationContextPool.Borrow();
			RpcApplicationContext.SetCurrent( context );
			context.StartTimeoutWatch();
		}

		/// <summary>
		///		Notifies to RPC runtime to end service operation.
		/// </summary>
		/// <remarks>
		///		Currently, this method performs following:
		///		<list type="bullet">
		///			<item>Clear <see cref="RpcApplicationContext"/>.</item>
		///			<item>Stop execution timeout wathing.</item>
		///		</list>
		/// </remarks>
		public void EndOperation()
		{
			bool wasSoftTimeout = false;
			var context = RpcApplicationContext.Current;
			try
			{
				if ( context != null )
				{
					context.StopTimeoutWatch();
					wasSoftTimeout = context.IsSoftTimeout;
				}
			}
			finally
			{
				RpcApplicationContext.Clear();

				if ( context != null && !context.IsDisposed )
				{
					this._applicationContextPool.Return( context );
				}
			}

			if ( wasSoftTimeout )
			{
				throw RpcError.ServerError.ToException( this._softTimeoutDetails );
			}
		}

		/// <summary>
		///		Handles a <see cref="ThreadAbortException"/> if it is thrown because hard execution timeout.
		/// </summary>
		/// <param name="mayBeHardTimeoutException">A <see cref="ThreadAbortException"/> if it may be thrown because hard execution timeout.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="mayBeHardTimeoutException"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ThreadStateException">
		///		<paramref name="mayBeHardTimeoutException"/> is not thrown on the current thread.
		/// </exception>
		[SuppressMessage( "Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Logically be instance method." )]
		public void HandleThreadAbortException( ThreadAbortException mayBeHardTimeoutException )
		{
			if ( mayBeHardTimeoutException == null )
			{
				throw new ArgumentNullException( "mayBeHardTimeoutException" );
			}

			Contract.EndContractBlock();

			if ( RpcApplicationContext.HardTimeoutToken.Equals( mayBeHardTimeoutException.ExceptionState ) )
			{
				try
				{
					ResetThreadAbort();
				}
				catch ( SecurityException ) { }
				catch ( MemberAccessException ) { }
			}
		}

		[SecuritySafeCritical]
		private static void ResetThreadAbort()
		{
			Thread.ResetAbort();
		}
	}
}
