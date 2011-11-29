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
using System.Net.Sockets;
using System.Threading;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Provides basic feature of all event loop implementations.
	/// </summary>
	/// <remarks>
	///		This class is thread safe, and derived classes should be thread safe.
	/// </remarks>
	public abstract class EventLoop : IDisposable
	{
		private EventHandler<RpcTransportErrorEventArgs> _transportError;

		/// <summary>
		///		Raised when some transport error is occurred.
		/// </summary>
		public event EventHandler<RpcTransportErrorEventArgs> TransportError
		{
			add
			{
				for (
					EventHandler<RpcTransportErrorEventArgs> currentValue = this._transportError, newValue = null, oldValue = null;
					oldValue != currentValue;
					oldValue = currentValue
				)
				{
					oldValue = currentValue;
					newValue = Delegate.Combine( oldValue, value ) as EventHandler<RpcTransportErrorEventArgs>;
					currentValue = Interlocked.CompareExchange( ref this._transportError, newValue, oldValue );
				}
			}
			remove
			{
				for (
					EventHandler<RpcTransportErrorEventArgs> currentValue = this._transportError, newValue = null, oldValue = null;
					oldValue != currentValue;
					oldValue = currentValue
				)
				{
					oldValue = currentValue;
					newValue = Delegate.Remove( oldValue, value ) as EventHandler<RpcTransportErrorEventArgs>;
					currentValue = Interlocked.CompareExchange( ref this._transportError, newValue, oldValue );
				}
			}
		}

		/// <summary>
		///		Raise <see cref="TransportError"/> event.
		/// </summary>
		/// <param name="e">Event information.</param>
		/// <returns>
		///		If error may be handled event handler(s) then true, otherwise false.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="e"/> is null.
		/// </exception>
		/// <remarks>
		///		Caller should cause unhandled exception when this method returns false to prevent unexpected behavior of the program.
		/// </remarks>
		protected virtual bool OnTransportError( RpcTransportErrorEventArgs e )
		{
			if ( e == null )
			{
				throw new ArgumentNullException( "e" );
			}

			EventHandler<RpcTransportErrorEventArgs> handler =
				Interlocked.CompareExchange( ref this._transportError, null, null );
			if ( handler != null )
			{
				handler( this, e );
				return true;
			}

			return false;
		}

		/// <summary>
		///		Initialize new instance.
		/// </summary>
		/// <param name="errorHandler">
		///		Initial event handler of <see cref="TransportError"/>. This handler may be null.
		/// </param>
		protected EventLoop( EventHandler<RpcTransportErrorEventArgs> errorHandler )
		{
			if ( errorHandler != null )
			{
				this.TransportError += errorHandler;
			}
		}

		/// <summary>
		///		Cleanup internal resources.
		/// </summary>
		public void Dispose()
		{
			this.Dispose( true );
			GC.SuppressFinalize( this );
		}

		/// <summary>
		///		Cleanup unamanged resources and optionally managed resources.
		/// </summary>
		/// <param name="disposing">
		///		To cleanup managed resources too, then true.
		/// </param>
		protected virtual void Dispose( bool disposing ) { }

		/// <summary>
		///		Raise error handler for specified socket level error.
		/// </summary>
		/// <param name="operation">Last operation.</param>
		/// <param name="error">Socket error.</param>
		/// <exception cref="SocketException">
		///		There are no event handlers registered.
		/// </exception>
		protected void HandleError( SocketAsyncOperation operation, SocketError error )
		{
			if ( error == System.Net.Sockets.SocketError.Success )
			{
				return;
			}

			if ( !this.OnTransportError( new RpcTransportErrorEventArgs( operation, error ) ) )
			{
				throw new SocketException( ( int )error );
			}
		}

		/// <summary>
		///		Raise error handler for specified socket level error.
		/// </summary>
		/// <param name="operation">Last operation.</param>
		/// <param name="messageId">ID of message.</param>
		/// <param name="error">Socket error.</param>
		/// <exception cref="SocketException">
		///		There are no event handlers registered.
		/// </exception>
		protected void HandleError( SocketAsyncOperation operation, int messageId, SocketError error )
		{
			if ( error == System.Net.Sockets.SocketError.Success )
			{
				return;
			}

			if ( !this.OnTransportError( new RpcTransportErrorEventArgs( operation, messageId, error ) ) )
			{
				throw new SocketException( ( int )error );
			}
		}

		/// <summary>
		///		Raise error handler for specified RPC level error.
		/// </summary>
		/// <param name="operation">Last operation.</param>
		/// <param name="rpcError">RPC error.</param>
		/// <exception cref="RpcException">
		///		There are no event handlers registered.
		/// </exception>
		protected void HandleError( RpcTransportOperation operation, RpcErrorMessage rpcError )
		{
			if ( rpcError.IsSuccess )
			{
				return;
			}

			if ( !this.OnTransportError( new RpcTransportErrorEventArgs( operation, rpcError ) ) )
			{
				throw rpcError.ToException();
			}
		}

		/// <summary>
		///		Raise error handler for specified RPC level error.
		/// </summary>
		/// <param name="operation">Last operation.</param>
		/// <param name="messageId">ID of message.</param>
		/// <param name="rpcError">RPC error.</param>
		/// <exception cref="RpcException">
		///		There are no event handlers registered.
		/// </exception>
		protected void HandleError( RpcTransportOperation operation, int messageId, RpcErrorMessage rpcError )
		{
			if ( rpcError.IsSuccess )
			{
				return;
			}

			if ( !this.OnTransportError( new RpcTransportErrorEventArgs( operation, messageId, rpcError ) ) )
			{
				throw rpcError.ToException();
			}
		}
	}
}
