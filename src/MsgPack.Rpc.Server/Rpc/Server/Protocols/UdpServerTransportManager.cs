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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MsgPack.Rpc.Server.Protocols
{
	/// <summary>
	///		<see cref="ServerTransportManager{T}"/> implementation for the UDP.
	/// </summary>
	public sealed class UdpServerTransportManager : ServerTransportManager<UdpServerTransport>
	{
		private readonly EndPoint _listeningEndPoint;
		private readonly EndPoint _bindingEndPoint;
		private readonly Thread _listeningThread;
		private readonly Socket _listeningSocket;

		[Conditional( "DEBUG" )]
		internal void GetListeningSocket( ref Socket result )
		{
			result = this._listeningSocket;
		}
	
		private int _isActive;

		private bool IsActive
		{
			get
			{
				return Interlocked.CompareExchange( ref this._isActive, 0, 0 ) != 0;
			}
			set
			{
				Interlocked.Exchange( ref this._isActive, value ? 1 : 0 );
			}
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="UdpServerTransportManager"/> class.
		/// </summary>
		/// <param name="server">The server which will host this instance.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="server"/> is <c>null</c>.
		/// </exception>
		public UdpServerTransportManager( RpcServer server )
			: base( server )
		{
#if !API_SIGNATURE_TEST
			base.SetTransportPool( server.Configuration.UdpTransportPoolProvider( () => new UdpServerTransport( this ), server.Configuration.CreateTransportPoolConfiguration() ) );
#endif

			var addressFamily = ( server.Configuration.PreferIPv4 || !Socket.OSSupportsIPv6 ) ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6;
			var bindingEndPoint = this.Configuration.BindingEndPoint;
#if !API_SIGNATURE_TEST
			if ( bindingEndPoint == null )
			{
				bindingEndPoint =
					new IPEndPoint(
#if MONO
						this.Configuration.PreferIPv4 && Socket.SupportsIPv4
#else
						this.Configuration.PreferIPv4 && Socket.OSSupportsIPv4
#endif
						? IPAddress.Any
						: IPAddress.IPv6Any,
						57319 // arbitrary number
					);
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.DefaultEndPoint,
					"Default end point is selected. {{ \"EndPoint\" : \"{0}\", \"AddressFamily\" : {1}, \"PreferIPv4\" : {2}, \"OSSupportsIPv6\" : {3} }}",
					bindingEndPoint,
					addressFamily,
					server.Configuration.PreferIPv4,
					Socket.OSSupportsIPv6
				);
			}
#endif
			this._bindingEndPoint = bindingEndPoint;

			this._listeningEndPoint = new IPEndPoint( addressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0 );

			this._listeningSocket =
				new Socket(
					this._bindingEndPoint.AddressFamily,
					SocketType.Dgram,
					ProtocolType.Udp
				);

#if !MONO
			if ( !this.Configuration.PreferIPv4
				&& Environment.OSVersion.Platform == PlatformID.Win32NT
				&& Environment.OSVersion.Version.Major >= 6
				&& Socket.OSSupportsIPv6
			)
			{
				// Listen both of IPv4 and IPv6
				this._listeningSocket.SetSocketOption( SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, 0 );
			}
#endif // !MONO

			this._listeningSocket.Bind( this._bindingEndPoint );
			this._listeningThread = new Thread( this.PollArrival ) { IsBackground = true, Name = "UdpListeningThread#" + this.GetHashCode() };
			this.IsActive = true;
			this._listeningThread.Start();
		}

		/// <summary>
		/// When overridden in derived class, releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected override void Dispose( bool disposing )
		{
			try
			{
				this.IsActive = false;
				base.Dispose( disposing );
				this._listeningSocket.Close();
			}
			finally
			{
				if ( this.Configuration.UdpListenerThreadExitTimeout == null )
				{
					this._listeningThread.Join();
				}
				else
				{
					if ( !this._listeningThread.Join( this.Configuration.UdpListenerThreadExitTimeout.Value ) )
					{
						this._listeningThread.Interrupt();
						if ( !this._listeningThread.Join( this.Configuration.UdpListenerThreadExitTimeout.Value ) )
						{
							// Suspend is needed to capture stack trace.
#pragma warning disable 0612,0618
							this._listeningThread.Suspend();
#pragma warning restore 0612,0618

#if !API_SIGNATURE_TEST
							MsgPackRpcServerProtocolsTrace.TraceEvent(
								MsgPackRpcServerProtocolsTrace.FailedToStopUdpServerManagerListenerThread,
								"Failed to stop the UDP server manager listener thread. Stack trace: {0}{1}",
								Environment.NewLine,
								new StackTrace( this._listeningThread, true )
							);
#endif
							// Suspend was needed to capture stack trace.
#pragma warning disable 0612,0618
							this._listeningThread.Resume();
#pragma warning restore 0612,0618
						}
					}
				}
			}
		}

		[SuppressMessage( "Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Transfers the exception." )]
		private void PollArrival()
		{
#if !API_SIGNATURE_TEST
			MsgPackRpcServerProtocolsTrace.TraceEvent(
				MsgPackRpcServerProtocolsTrace.StartListen,
				"Start listen. {{ \"Socket\" : 0x{0:X}, \"EndPoint\" : \"{1}\", \"ListenBackLog\" : {2} }}",
				this._listeningSocket.Handle,
				this._bindingEndPoint,
				this.Server.Configuration.ListenBackLog
			);
#endif
			try
			{
				var transport = this.GetTransport( this._listeningSocket );

				while ( this.IsActive )
				{
					ServerRequestContext context;
					try
					{
						// CancellationToken is useless...
						context = this.GetRequestContext( transport );
					}
					catch ( ThreadInterruptedException )
					{
						this.ReturnTransport( transport );

						if ( this.IsActive )
						{
							throw;
						}
						else
						{
							return;
						}
					}
					catch ( ObjectDisposedException )
					{
						this.ReturnTransport( transport );

						if ( this.IsActive )
						{
							throw;
						}
						else
						{
							return;
						}
					}

					context.RemoteEndPoint = this._listeningEndPoint;

					if ( !this.IsActive )
					{
						this.ReturnRequestContext( context );
						this.ReturnTransport( transport );
						return;
					}

					transport.Receive( context );
				}
			}
#if !API_SIGNATURE_TEST
			catch ( Exception ex )
			{
				MsgPackRpcServerProtocolsTrace.TraceEvent(
					MsgPackRpcServerProtocolsTrace.UdpServerManagerListenerThreadCrash,
					"The exception has been occurred in the UDP server manager listener thread. {0}",
					ex
				);
			}
#else
			catch { }
#endif
		}

		/// <summary>
		///		Gets the transport managed by this instance.
		/// </summary>
		/// <param name="bindingSocket">The <see cref="Socket"/> to be bind the returning transport.</param>
		/// <returns>
		///		The transport managed by this instance.
		///		Note that <see cref="ServerTransport.BoundSocket"/> might be <c>null</c> depends on <see cref="GetTransportCore"/> implementation.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		///		<paramref name="bindingSocket"/> is <c>null</c>.
		/// </exception>
		protected sealed override UdpServerTransport GetTransportCore( Socket bindingSocket )
		{
			if ( bindingSocket == null )
			{
				throw new InvalidOperationException( String.Format( CultureInfo.CurrentCulture, "'bindingSocket' is required in {0}.", this.GetType() ) );
			}

			var transport = base.GetTransportCore( bindingSocket );
			this.BindSocket( transport, bindingSocket );
			return transport;
		}
	}
}
