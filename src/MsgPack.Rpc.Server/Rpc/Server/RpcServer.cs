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
using System.Net;
using MsgPack.Rpc.Server.Protocols;
using MsgPack.Serialization;
using MsgPack.Rpc.Server.Dispatch;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc.Server
{
	public interface IFreezable
	{
		bool IsFrozen { get; }
		IFreezable Freeze();
		IFreezable AsFrozen();
	}

	public sealed class RpcServerConfiguration : IFreezable, ICloneable
	{
		private static readonly RpcServerConfiguration _default = new RpcServerConfiguration().Freeze();

		public static RpcServerConfiguration Default
		{
			get { return RpcServerConfiguration._default; }
		}

		private bool _isFrozen;

		public bool IsFrozen
		{
			get { return this._isFrozen; }
		}

		private int _minimumConcurrency = Environment.ProcessorCount * 2;

		public int MinimumConcurrency
		{
			get { return this._minimumConcurrency; }
			set
			{
				//FIXME: Validation
				this._minimumConcurrency = value;
			}
		}

		private Func<ObjectPool<ResponseSocketAsyncEventArgs>> _responseContextPoolProvider = () => new StandardObjectPool<ResponseSocketAsyncEventArgs>();

		public Func<ObjectPool<ResponseSocketAsyncEventArgs>> ResponseContextPoolProvider
		{
			get { return this._responseContextPoolProvider; }
			set
			{
				//FIXME: Validation
				this._responseContextPoolProvider = value;
			}
		}

		private Func<ServiceTypeLocator> _serviceTypeLocatorProvider = () => new DefaultServiceTypeLocator();

		public Func<ServiceTypeLocator> ServiceTypeLocatorProvider
		{
			get { return this._serviceTypeLocatorProvider; }
			set
			{
				//FIXME: Validation
				this._serviceTypeLocatorProvider = value;
			}
		}

		public RpcServerConfiguration() { }

		public RpcServerConfiguration Clone()
		{
			return this.MemberwiseClone() as RpcServerConfiguration;
		}

		public RpcServerConfiguration Freeze()
		{
			if ( !this._isFrozen )
			{
				this._isFrozen = true;
				Thread.MemoryBarrier();
			}

			return this;
		}

		public RpcServerConfiguration AsFrozen()
		{
			if ( this.IsFrozen )
			{
				return this;
			}

			return this.Clone().Freeze();
		}

		object ICloneable.Clone()
		{
			return this.Clone();
		}

		IFreezable IFreezable.AsFrozen()
		{
			return this.AsFrozen();
		}

		IFreezable IFreezable.Freeze()
		{
			return this.Freeze();
		}
	}

	/// <summary>
	///		Control stand alone server event loop.
	/// </summary>
	public class RpcServer : IDisposable
	{
		private readonly SerializationContext _serializationContext;

		public SerializationContext SerializationContext
		{
			get { return this._serializationContext; }
		}

		// TODO: auto-scaling
		// _maximumConcurrency, _currentConcurrency, _minimumIdle, _maximumIdle

		private readonly List<ServerTransport> _transports;
		private readonly int _minimumConcurrency;
		private readonly ServiceTypeLocator _locator;
		private readonly Dictionary<string, OperationDescription> _operations;
		private readonly ObjectPool<ResponseSocketAsyncEventArgs> _responseContextPool;

		public RpcServer() : this( null ) { }

		public RpcServer( RpcServerConfiguration configuration )
		{
			var safeConfiguration = ( configuration ?? RpcServerConfiguration.Default ).AsFrozen();
			this._operations = new Dictionary<string, OperationDescription>();
			this._responseContextPool = safeConfiguration.ResponseContextPoolProvider();
			this._minimumConcurrency = safeConfiguration.MinimumConcurrency;
			this._transports = new List<ServerTransport>( safeConfiguration.MinimumConcurrency );
			this._locator = safeConfiguration.ServiceTypeLocatorProvider();
			this._serializationContext = new SerializationContext();

			// FIXME
		}

		public void Dispose()
		{
			this.Stop();
		}

		private ServerTransport CreateTransport( ServerSocketAsyncEventArgs context )
		{
			// TODO: transport factory.
			return new TcpServerTransport( context );
		}

		public void Start( EndPoint bindingEndPoint )
		{
			Tracer.Server.TraceEvent( Tracer.EventType.StartServer, Tracer.EventId.StartServer, "Start server. [\"minimumConcurrency\":{0}]", this._minimumConcurrency );

			this.PopluateOperations();
			// FIXME: Verification
			for ( int i = 0; i < this._minimumConcurrency; i++ )
			{
				var transport = this.CreateTransport( new ServerSocketAsyncEventArgs( this ) );
				transport.Initialize( bindingEndPoint );
				this._transports.Add( transport );
			}
		}

		private void PopluateOperations()
		{
			foreach ( var service in this._locator.FindServices() )
			{
				foreach ( var operation in OperationDescription.FromServiceDescription( this._serializationContext, service ) )
				{
					this._operations.Add( operation.Id, operation );
				}
			}
		}

		public void Stop()
		{
			// FIXME: Verification
			foreach ( var transport in this._transports )
			{
				transport.Dispose();
			}

			this._transports.Clear();
			this._operations.Clear();
		}


		private void OnMessageReceived( object source, RpcMessageReceivedEventArgs e )
		{
			ObjectLease<ResponseSocketAsyncEventArgs> responseContext = null;
			if ( e.MessageType == MessageType.Request )
			{
				responseContext = this._responseContextPool.Borrow();
			}

			OperationDescription operation;
			if ( !this._operations.TryGetValue( e.MethodName, out operation ) )
			{
				var error = new RpcErrorMessage( RpcError.NoMethodError, "Operation does not exist.", null );
				InvocationHelper.TraceInvocationResult<object>(
					e.MessageType,
					e.Id.GetValueOrDefault(),
					e.MethodName,
					error,
					null
				);

				if ( responseContext != null )
				{
					InvocationHelper.SerializeResponse<object>( responseContext.Value.ResponsePacker, e.Id.Value, error, null, null );
				}

				return;
			}

			var task = operation.Operation( e.ArgumentsUnpacker, e.Id.GetValueOrDefault(), responseContext == null ? null : responseContext.Value.ResponsePacker );

#if NET_4_5
			task.ContinueWith( ( previous, state ) =>
				{
					previous.Dispose();
					( state as IDisposable ).Dispose();
				},
				responseContext
			);
#else
			task.ContinueWith( previous =>
				{
					previous.Dispose();
					responseContext.Dispose();
				}
			);
#endif
		}
	}

	public sealed class ResponseSocketAsyncEventArgs : SocketAsyncEventArgs
	{
		private readonly Packer _responsePacker;

		public Packer ResponsePacker
		{
			get { return this._responsePacker; }
		}

	}

	// TODO: Move to core
	public abstract class ObjectPool<T>
		where T : class
	{
		public ObjectLease<T> Borrow()
		{
			return this.BorrowCore();
		}

		protected abstract ObjectLease<T> BorrowCore();

		public void Return( ObjectLease<T> value )
		{
			if ( value == null )
			{
				throw new ArgumentNullException( "value" );
			}

			this.ReturnCore( value );
		}

		protected abstract void ReturnCore( ObjectLease<T> value );
	}

	public abstract class ObjectLease<T> : IDisposable
		where T : class
	{
		private bool _isDisposed;
		private T _value;
		public T Value
		{
			get
			{
				this.VerifyIsNotDisposed();
				return this._value;
			}
			protected set
			{
				this.VerifyIsNotDisposed();
				this._value = value;
			}
		}

		protected ObjectLease( T initialValue )
		{
			this._value = initialValue;
		}

		public void Dispose()
		{
			this.Dispose( true );
			GC.SuppressFinalize( this );
		}

		protected virtual void Dispose( bool disposing )
		{
			this._isDisposed = true;
			Thread.MemoryBarrier();
		}

		private void VerifyIsNotDisposed()
		{
			if ( this._isDisposed )
			{
				throw new ObjectDisposedException( this.GetType().FullName );
			}
		}
	}

	internal sealed class FinalizableObjectLease<T> : ObjectLease<T>
		where T : class
	{
		private Action<T> _returning;

		public FinalizableObjectLease( T initialValue, Action<T> returning )
			: base( null )
		{
			if ( returning == null )
			{
				GC.SuppressFinalize( this );
				throw new ArgumentNullException( "returning" );
			}

			RuntimeHelpers.PrepareConstrainedRegions();
			try { }
			finally
			{
				this._returning = returning;
				this.Value = initialValue;
			}
		}

		~FinalizableObjectLease()
		{
			this.Dispose( false );
		}

		protected sealed override void Dispose( bool disposing )
		{
			try { }
			finally
			{
				var returning = Interlocked.Exchange( ref this._returning, null );
				if ( returning != null )
				{
					var value = this.Value;
					this.Value = null;
					returning( value );
					base.Dispose( disposing );
				}
			}
		}
	}

	internal sealed class ForgettableObjectLease<T> : ObjectLease<T>
		where T : class
	{
		public ForgettableObjectLease( T initialValue ) : base( initialValue ) { }

		protected sealed override void Dispose( bool disposing )
		{
			base.Dispose( disposing );
			// nop.
		}
	}

	internal sealed class StandardObjectPool<T> : ObjectPool<T>
		where T : class
	{
		// TODO: Eviction
		// TODO: MinObjects
		// TODO: MaxObjects
		private readonly Func<T> _factory;

		protected sealed override ObjectLease<T> BorrowCore()
		{
			throw new NotImplementedException();
		}

		protected sealed override void ReturnCore( ObjectLease<T> value )
		{
			throw new NotImplementedException();
		}
	}

	internal sealed class OnTheFlyObjectPool<T> : ObjectPool<T>
		where T : class
	{
		private readonly Func<T> _factory;

		public OnTheFlyObjectPool( Func<T> factory )
		{
			this._factory = factory;
		}

		protected sealed override ObjectLease<T> BorrowCore()
		{
			return new ForgettableObjectLease<T>( this._factory() );
		}

		protected sealed override void ReturnCore( ObjectLease<T> value )
		{
			value.Dispose();
		}
	}
}
