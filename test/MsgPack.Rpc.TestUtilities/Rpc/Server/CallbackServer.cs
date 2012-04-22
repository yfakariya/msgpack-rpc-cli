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
using System.Globalization;
using System.Net;
using MsgPack.Rpc.Server.Dispatch;
using System.Diagnostics.Contracts;

namespace MsgPack.Rpc.Server
{
	/// <summary>
	///		Wraps <see cref="RpcServer"/> with <see cref="CallbackDispatcher"/> and
	///		decouples Server assembly from the caller (typically test code).
	/// </summary>
	public sealed class CallbackServer : IDisposable
	{
		/// <summary>
		///		The default port number.
		/// </summary>
		public const int PortNumber = 57319;

		private readonly RpcServer _server;

		public object Server
		{
			get
			{
				Contract.Ensures( Contract.Result<object>() as RpcServer != null );

				return this._server;
			}
		}

		/// <summary>
		///		Gets the bound end point to the server.
		/// </summary>
		/// <value>
		///		The bound end point to the server.
		/// </value>
		public EndPoint BoundEndPoint
		{
			get { return this._server.Configuration.BindingEndPoint; }
		}

		/// <summary>
		///		Occurs when a error is occurred on this server.
		/// </summary>
		public event EventHandler<CallbackServerErrorEventArgs> Error;

		private void OnClientError( object sender, RpcClientErrorEventArgs e )
		{
			this.OnError( new CallbackServerErrorEventArgs( e.RpcError.ToException(), true ) );
		}

		private void OnServerError( object sender, RpcServerErrorEventArgs e )
		{
			this.OnError( new CallbackServerErrorEventArgs( e.Exception, false ) );
		}

		private void OnError( CallbackServerErrorEventArgs e )
		{
			var handler = this.Error;
			if ( handler != null )
			{
				handler( this, e );
			}
		}

		private CallbackServer( RpcServer server )
		{
			this._server = server;
			server.ClientError += this.OnClientError;
			server.ServerError += this.OnServerError;
			this._server.Start();
		}

		/// <summary>
		///		Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary> 
		public void Dispose()
		{
			this._server.ClientError -= this.OnClientError;
			this._server.ServerError -= this.OnServerError;
			this._server.Dispose();
		}

		/// <summary>
		///		Creates a <see cref="CallbackServer"/> for any IP and <see cref="PortNumber"/>.
		/// </summary>
		/// <param name="callback">The callback without method name.</param>
		/// <param name="isDebugMode"><c>true</c> to enable debug mode;<c>false</c>, otherwise.</param>
		/// <returns>
		///		The <see cref="CallbackServer"/> for any IP and <see cref="PortNumber"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="callback"/> is <c>null</c>.
		/// </exception>
		public static CallbackServer Create( Func<int?, MessagePackObject[], MessagePackObject> callback, bool isDebugMode )
		{
			return Create( callback, new IPEndPoint( IPAddress.Any, PortNumber ), true, isDebugMode );
		}

		/// <summary>
		///		Creates a <see cref="CallbackServer"/> with specified <see cref="EndPoint"/>.
		/// </summary>
		/// <param name="callback">The callback without method name.</param>
		/// <param name="endPoint">
		///		The <see cref="EndPoint"/> to be bound.
		/// </param>
		/// <param name="preferIPv4">
		///		<c>true</c> if use IP v4; otherwise, <c>false</c>.
		/// </param>
		/// <param name="isDebugMode"><c>true</c> to enable debug mode;<c>false</c>, otherwise.</param>
		/// <returns>
		///		The <see cref="CallbackServer"/> with specified <see cref="EndPoint"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="callback"/> is <c>null</c>.
		///		Or, <paramref name="endPoint"/> is <c>null</c>.
		/// </exception>
		public static CallbackServer Create( Func<int?, MessagePackObject[], MessagePackObject> callback, EndPoint endPoint, bool preferIPv4, bool isDebugMode )
		{
			if ( callback == null )
			{
				throw new ArgumentNullException( "callback" );
			}

			if ( endPoint == null )
			{
				throw new ArgumentNullException( "endPoint" );
			}

			Contract.Ensures( Contract.Result<CallbackServer>() != null );

			return
				Create(
					new RpcServerConfiguration()
					{
						PreferIPv4 = preferIPv4,
						BindingEndPoint = endPoint,
						MinimumConcurrentRequest = 1,
						MaximumConcurrentRequest = 10,
						MinimumConnection = 1,
						MaximumConnection = 1,
						DispatcherProvider = server => new CallbackDispatcher( server, callback ),
						IsDebugMode = isDebugMode,
					}
				);
		}

		/// <summary>
		///		Creates a <see cref="CallbackServer"/> for any IP and <see cref="PortNumber"/>.
		/// </summary>
		/// <param name="callback">The callback without method name.</param>
		/// <param name="isDebugMode"><c>true</c> to enable debug mode;<c>false</c>, otherwise.</param>
		/// <returns>
		///		The <see cref="CallbackServer"/> for any IP and <see cref="PortNumber"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="callback"/> is <c>null</c>.
		/// </exception>
		public static CallbackServer Create( Func<string, int?, MessagePackObject[], MessagePackObject> dispatch, bool isDebugMode )
		{
			return Create( dispatch, new IPEndPoint( IPAddress.Any, PortNumber ), true, isDebugMode );
		}

		/// <summary>
		///		Creates a <see cref="CallbackServer"/> with specified <see cref="EndPoint"/>.
		/// </summary>
		/// <param name="dispatch">The callback with method name.</param>
		/// <param name="endPoint">
		///		The <see cref="EndPoint"/> to be bound.
		/// </param>
		/// <param name="preferIPv4">
		///		<c>true</c> if use IP v4; otherwise, <c>false</c>.
		/// </param>
		/// <param name="isDebugMode"><c>true</c> to enable debug mode;<c>false</c>, otherwise.</param>
		/// <returns>
		///		The <see cref="CallbackServer"/> with specified <see cref="EndPoint"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="callback"/> is <c>null</c>.
		///		Or, <paramref name="endPoint"/> is <c>null</c>.
		/// </exception>
		public static CallbackServer Create( Func<string, int?, MessagePackObject[], MessagePackObject> dispatch, EndPoint endPoint, bool preferIPv4, bool isDebugMode )
		{
			if ( dispatch == null )
			{
				throw new ArgumentNullException( "dispatch" );
			}

			if ( endPoint == null )
			{
				throw new ArgumentNullException( "endPoint" );
			}

			Contract.Ensures( Contract.Result<CallbackServer>() != null );

			return
				Create(
					new RpcServerConfiguration()
					{
						PreferIPv4 = preferIPv4,
						BindingEndPoint = endPoint,
						MinimumConcurrentRequest = 1,
						MaximumConcurrentRequest = 10,
						MinimumConnection = 1,
						MaximumConnection = 1,
						DispatcherProvider = server => new CallbackDispatcher( server, dispatch ),
						IsDebugMode = isDebugMode,
					}
				);
		}
		/// <summary>
		///		Creates a <see cref="CallbackServer"/> with specified <see cref="RpcServerConfiguration"/>.
		/// </summary>
		/// <param name="configuration">The <see cref="RpcServerConfiguration"/>.</param>
		/// <returns>
		///		The <see cref="CallbackServer"/> with specified <see cref="RpcServerConfiguration"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="configuration"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="configuration"/> is not a <see cref="RpcServerConfiguration"/>.
		/// </exception>
		public static CallbackServer Create( object configuration )
		{
			if ( configuration == null )
			{
				throw new ArgumentNullException( "configuration" );
			}

			var realConfiguration = configuration as RpcServerConfiguration;

			if ( realConfiguration == null )
			{
				throw new ArgumentException( String.Format( CultureInfo.CurrentCulture, "Configuration is not an '{0}' type.", typeof( RpcServerConfiguration ) ), "configuration" );
			}

			Contract.Ensures( Contract.Result<CallbackServer>() != null );

			return Create( realConfiguration );
		}

		private static CallbackServer Create( RpcServerConfiguration configuration )
		{
			return new CallbackServer( new RpcServer( configuration ) );
		}

	}
}
