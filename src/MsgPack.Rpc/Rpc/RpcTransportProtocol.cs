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
using System.Net.Sockets;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Represents underlying transportation protocol of MessagePack-RPC.
	/// </summary>
	public struct RpcTransportProtocol : IEquatable<RpcTransportProtocol>, IFormattable
	{
		/// <summary>
		///		Represents TCP/IP. Version of IP depends on platform.
		/// </summary>
		public static readonly RpcTransportProtocol TcpIp = new RpcTransportProtocol( Socket.OSSupportsIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );

		/// <summary>
		///		Represents TCP/IP. Version of IP is 4.
		/// </summary>
		public static readonly RpcTransportProtocol TcpIpV4 = new RpcTransportProtocol( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );

		/// <summary>
		///		Represents TCP/IP. Version of IP is 6.
		/// </summary>
		public static readonly RpcTransportProtocol TcpIpV6 = new RpcTransportProtocol( AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp );

		/// <summary>
		///		Represents UDP/IP. Version of IP depends on platform.
		/// </summary>
		public static readonly RpcTransportProtocol UdpIp = new RpcTransportProtocol( Socket.OSSupportsIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp );

		/// <summary>
		///		Represents UDP/IP. Version of IP is 4.
		/// </summary>
		public static readonly RpcTransportProtocol UdpIpV4 = new RpcTransportProtocol( AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp );

		/// <summary>
		///		Represents UDP/IP. Version of IP is 6.
		/// </summary>
		public static readonly RpcTransportProtocol UdpIpV6 = new RpcTransportProtocol( AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp );

		private readonly AddressFamily _addressFamily;

		/// <summary>
		///		Get <see cref="AddressFamily"/> of this protocol.
		/// </summary>
		/// <value>
		///		<see cref="AddressFamily"/> of this protocol.
		/// </value>
		public AddressFamily AddressFamily
		{
			get { return this._addressFamily; }
		}

		private readonly SocketType _socketType;

		/// <summary>
		///		Get <see cref="SocketType"/> of this protocol.
		/// </summary>
		/// <value>
		///		<see cref="SocketType"/> of this protocol.
		/// </value>
		public SocketType SocketType
		{
			get { return this._socketType; }
		}

		private readonly ProtocolType _protocolType;

		/// <summary>
		///		Get <see cref="ProtocolType"/> of this protocol.
		/// </summary>
		/// <value>
		///		<see cref="ProtocolType"/> of this protocol.
		/// </value>
		public ProtocolType ProtocolType
		{
			get { return this._protocolType; }
		}

		/// <summary>
		///		Initialize new instance.
		/// </summary>
		/// <param name="addressFamily">
		///		<see cref="AddressFamily"/> of this protocol.
		/// </param>
		/// <param name="socketType">
		///		<see cref="SocketType"/> of this protocol.
		/// </param>
		/// <param name="protocolType">
		///		<see cref="ProtocolType"/> of this protocol.
		///	</param>
		public RpcTransportProtocol( AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType )
		{
			this._addressFamily = addressFamily;
			this._socketType = socketType;
			this._protocolType = protocolType;
		}

		/// <summary>
		///		Get <see cref="RpcTransportProtocol"/> corresponds to specified <see cref="Socket"/>.
		/// </summary>
		/// <param name="socket">
		///		<see cref="Socket"/> for some protocol.
		/// </param>
		/// <returns>
		///		<see cref="RpcTransportProtocol"/> corresponds to specified <see cref="Socket"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="socket"/> is null.
		/// </exception>
		public static RpcTransportProtocol ForSocket( Socket socket )
		{
			if ( socket == null )
			{
				throw new ArgumentNullException( "socket" );
			}

			Contract.EndContractBlock();

			return new RpcTransportProtocol( socket.AddressFamily, socket.SocketType, socket.ProtocolType );
		}

		/// <summary>
		///		Create new <see cref="Socket"/> corresponds to this protocol.
		/// </summary>
		/// <returns>
		///		new <see cref="Socket"/> corresponds to this protocol.
		/// </returns>
		public Socket CreateSocket()
		{
			return new Socket( this._addressFamily, this._socketType, this._protocolType );
		}

		/// <summary>
		///		Returns string representation of this protocol.
		/// </summary>
		/// <returns>
		///		String representation of this protocol.
		/// </returns>
		/// <remarks>
		///		This method is equivalant to <see cref="ToString(string,IFormatProvider)"/> 
		///		with "G" and <see cref="System.Globalization.CultureInfo.CurrentCulture"/>.
		/// </remarks>
		public override string ToString()
		{
			return this.ToString( "G" );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="format">Format of some numeric values.</param>
		/// <returns>
		///		String representation of this protocol.
		/// </returns>
		/// <remarks>
		///		This method is equivalant to <see cref="ToString(string,IFormatProvider)"/> 
		///		with <paramref name="format"/> and <see cref="System.Globalization.CultureInfo.CurrentCulture"/>.
		/// </remarks>
		public string ToString( string format )
		{
			return
				"{AddressFamily=" +
				this._addressFamily.ToString( format ) +
				", SocketType=" +
				this._socketType.ToString( format ) +
				", ProtocolType=" +
				this._protocolType.ToString( format ) +
				"}";
		}

		/// <summary>
		///		Returns string representation of this protocol.
		/// </summary>
		/// <param name="format">Format of some numeric values.</param>
		/// <param name="formatProvider">Ignored.</param>
		/// <returns>
		///		String representation of this protocol.
		/// </returns>
		public string ToString( string format, IFormatProvider formatProvider )
		{
			return this.ToString( format );
		}

		/// <summary>
		///		Returns hash code of this instance.
		/// </summary>
		/// <returns>Hash code of this instance.</returns>
		public override int GetHashCode()
		{
			return this._addressFamily.GetHashCode() ^ this._protocolType.GetHashCode() ^ this._socketType.GetHashCode();
		}

		/// <summary>
		///		Determine that specified object is equal to this instance.
		/// </summary>
		/// <param name="obj">
		///		Object to be compared.
		///	</param>
		/// <returns>
		///		If <paramref name="obj"/> is <see cref="RpcTransportProtocol"/> and the value is equal to this object then true.
		/// </returns>
		public override bool Equals( object obj )
		{
			if ( !( obj is RpcTransportProtocol ) )
			{
				return false;
			}
			else
			{
				return this.Equals( ( RpcTransportProtocol )obj );
			}
		}

		/// <summary>
		///		Determine that specified object is equal to this instance.
		/// </summary>
		/// <param name="other">
		///		<see cref="RpcTransportProtocol"/> to be compared.
		/// </param>
		/// <returns>
		///		If the value is equal to this object then true.
		/// </returns>
		public bool Equals( RpcTransportProtocol other )
		{
			return
				this._addressFamily == other._addressFamily
				&& this._protocolType == other._protocolType
				&& this._socketType == other._socketType;
		}

		/// <summary>
		///		Determine two <see cref="RpcTransportProtocol"/>s are equal.
		/// </summary>
		/// <param name="left"><see cref="RpcTransportProtocol"/>.</param>
		/// <param name="right"><see cref="RpcTransportProtocol"/>.</param>
		/// <returns>
		///		If <paramref name="left"/> is equal to <paramref name="right"/> then true.
		/// </returns>
		public static bool operator ==( RpcTransportProtocol left, RpcTransportProtocol right )
		{
			return left.Equals( right );
		}

		/// <summary>
		///		Determine two <see cref="RpcTransportProtocol"/>s are not equal.
		/// </summary>
		/// <param name="left"><see cref="RpcTransportProtocol"/>.</param>
		/// <param name="right"><see cref="RpcTransportProtocol"/>.</param>
		/// <returns>
		///		If <paramref name="left"/> is not equal to <paramref name="right"/> then true.
		/// </returns>
		public static bool operator !=( RpcTransportProtocol left, RpcTransportProtocol right )
		{
			return !left.Equals( right );
		}
	}
}
