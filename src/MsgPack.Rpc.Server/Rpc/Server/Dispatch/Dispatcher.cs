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
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;
using System.Security;
using System.Threading;
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc.Dispatch
{
	/// <summary>
	///		Dispatch incoming request or notification to appropriate method.
	/// </summary>
	internal sealed class Dispatcher
	{
		private readonly DispatchTargetResolver _targetResolver;
		private readonly MethodInvokerProvider _invokerProvider;
		private readonly List<IFilterProvider<PreInvocationFilter>> _preInvocationFilters;
		private readonly List<IFilterProvider<PostInvocationFilter>> _postInvocationFilters;
		private readonly InvocationErrorHandler _invocationErrorHandler;

		public Dispatcher(
			DispatchTargetResolver targetResolver,
			MethodInvokerProvider invokerProvider,
			List<IFilterProvider<PreInvocationFilter>> preInvocationFilters,
			List<IFilterProvider<PostInvocationFilter>> postInvocationFilters,
			InvocationErrorHandler invocationErrorHandler
		)
		{
			Contract.Assert( targetResolver != null );
			Contract.Assert( invokerProvider != null );
			Contract.Assert( preInvocationFilters != null );
			Contract.Assert( postInvocationFilters != null );
			Contract.Assert( invocationErrorHandler != null );

			this._targetResolver = targetResolver;
			this._invokerProvider = invokerProvider;
			this._preInvocationFilters = preInvocationFilters;
			this._postInvocationFilters = postInvocationFilters;
			this._invocationErrorHandler = invocationErrorHandler;
		}

		public InvocationResult Dispatch( RpcServerSession session, int messageId, string methodName, IList<MessagePackObject> arguments )
		{
			Contract.Assert( session != null );
			Contract.Assert( !String.IsNullOrWhiteSpace( methodName ) );
			Contract.Assert( arguments != null );

			RuntimeMethodHandle handle = this._targetResolver.ResolveMethod( methodName );
			var targetMethod = MethodBase.GetMethodFromHandle( handle ) as MethodInfo;
			if ( targetMethod == null )
			{
				throw new RpcMissingMethodException( methodName, "Specified member is not method.", MethodBase.GetMethodFromHandle( handle ).ToString() );
			}

			var invoker = this._invokerProvider.GetInvoker( targetMethod );
			var filteredArguments = arguments;
			foreach ( var filter in this._preInvocationFilters )
			{
				filteredArguments = filter.GetFilter().Process( targetMethod, arguments );
			}

			var result = this.Invoke( invoker, targetMethod, arguments );

			foreach ( var filter in this._postInvocationFilters )
			{
				result = filter.GetFilter().Process( targetMethod, result );
			}

			return result;
		}

		private InvocationResult Invoke( MethodInvoker invoker, MethodInfo targetMethod, IList<MessagePackObject> arguments )
		{
			try
			{
				return invoker.Invoke( arguments );
			}
			catch ( Exception ex )
			{
				var filteredResult = this._invocationErrorHandler.Handle( targetMethod, ex );
				return filteredResult ?? new InvocationResult( ex, targetMethod.ReturnType == typeof( void ) );
			}
		}

		private sealed class DefaultDispatchTargetResolver : DispatchTargetResolver
		{
			private readonly Type _serviceType;
			private readonly Dictionary<string, RuntimeMethodHandle> _methodNameResolutionTable = new Dictionary<string, RuntimeMethodHandle>();
			private readonly ReaderWriterLockSlim _methodNameResolutionTableLock = new ReaderWriterLockSlim();

			public DefaultDispatchTargetResolver( Type serviceType )
			{
				this._serviceType = serviceType;
			}

			protected sealed override RuntimeMethodHandle ResolveMethodCore( string methodName )
			{
				RuntimeMethodHandle handle;
				bool lockTaken = false;
				try
				{
					try { }
					finally
					{
						this._methodNameResolutionTableLock.EnterReadLock();
						lockTaken = true;
					}

					if ( this._methodNameResolutionTable.TryGetValue( methodName, out handle ) )
					{
						return handle;
					}
				}
				finally
				{
					if ( lockTaken )
					{
						this._methodNameResolutionTableLock.ExitReadLock();
					}
				}

				try
				{
					handle = this._serviceType.GetMethod( methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance ).MethodHandle;
				}
				catch ( ArgumentException ex )
				{
					throw new RpcMethodInvocationException( RpcError.CallError, methodName, "Invalid method name.", ex.ToString(), ex );
				}
				catch ( MissingMethodException ex )
				{
					throw new RpcMissingMethodException( methodName, RpcError.NoMethodError.DefaultMessage, ex.ToString(), ex );
				}
				catch ( AmbiguousMatchException ex )
				{
					throw new RpcMissingMethodException( methodName, "Failed to find specified method due to overload resolution.", ex.ToString(), ex );
				}
				catch ( SecurityException ex )
				{
					throw new RpcMissingMethodException( methodName, "Invalid method name.", ex.ToString(), ex );
				}

				lockTaken = false;
				try
				{
					try { }
					finally
					{
						this._methodNameResolutionTableLock.EnterWriteLock();
						lockTaken = true;
					}

					// Overwrite is ok.
					this._methodNameResolutionTable[ methodName ] = handle;
				}
				catch ( Exception ex )
				{
					if ( lockTaken )
					{
						Environment.FailFast( "Shutdown to prevent data corruption.", ex );
					}
					else
					{
						throw;
					}
				}
				finally
				{
					if ( lockTaken )
					{
						this._methodNameResolutionTableLock.ExitWriteLock();
					}
				}

				return handle;
			}
		}

		private sealed class DefaultMethodInvokerProvider : MethodInvokerProvider
		{
			private readonly Dictionary<RuntimeMethodHandle, MethodInvoker> _invokerTable = new Dictionary<RuntimeMethodHandle, MethodInvoker>();
			private readonly ReaderWriterLockSlim _invokerTableLock = new ReaderWriterLockSlim();
			private readonly MethodInvokerEmitter _invokerEmitter;

			public DefaultMethodInvokerProvider( MethodInvokerEmitterMode mode )
			{
				this._invokerEmitter = new MethodInvokerEmitter( mode );
			}

			protected sealed override MethodInvoker GetInvokerCore( MethodInfo targetMethod )
			{
				MethodInvoker invoker;
				bool lockTaken = false;
				try
				{
					try { }
					finally
					{
						this._invokerTableLock.EnterReadLock();
						lockTaken = true;
					}

					if ( this._invokerTable.TryGetValue( targetMethod.MethodHandle, out invoker ) )
					{
						return invoker;
					}
				}
				finally
				{
					if ( lockTaken )
					{
						this._invokerTableLock.ExitReadLock();
					}
				}

				try
				{
					invoker =
						( MethodInvoker )Activator.CreateInstance(
							this._invokerEmitter.Emit( targetMethod ),
							Expression.Lambda(
								typeof( Func<> ).MakeGenericType( targetMethod.ReflectedType ),
								Expression.New( targetMethod.ReflectedType.GetConstructor( Type.EmptyTypes ) )
							).Compile()
						);
				}
				catch ( NotSupportedException ex )
				{
					throw new RpcMethodInvocationException( RpcError.CallError, targetMethod.Name, RpcError.CallError.DefaultMessage, ex.ToString() );
				}
				catch ( SecurityException ex )
				{
					throw new RpcMethodInvocationException( RpcError.CallError, targetMethod.Name, RpcError.CallError.DefaultMessage, ex.ToString() );
				}
				catch ( MemberAccessException ex )
				{
					throw new RpcMethodInvocationException( RpcError.CallError, targetMethod.Name, RpcError.CallError.DefaultMessage, ex.ToString() );
				}
				catch ( TargetInvocationException ex )
				{
					throw new RpcMethodInvocationException( RpcError.CallError, targetMethod.Name, RpcError.CallError.DefaultMessage, ex.ToString() );
				}

				lockTaken = false;
				try
				{
					try { }
					finally
					{
						this._invokerTableLock.EnterWriteLock();
						lockTaken = true;
					}

					// Overwrite is ok.
					this._invokerTable[ targetMethod.MethodHandle ] = invoker;
				}
				catch ( Exception ex )
				{
					if ( lockTaken )
					{
						Environment.FailFast( "Shutdown to prevent data corruption.", ex );
					}
					else
					{
						throw;
					}
				}
				finally
				{
					if ( lockTaken )
					{
						this._invokerTableLock.ExitWriteLock();
					}
				}

				return invoker;
			}
		}
	}
}
