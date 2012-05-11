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
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security;

namespace MsgPack.Rpc
{
	internal static class NetworkEnvironment
	{
		internal static EndPoint GetDefaultEndPoint( int port, bool preferIPv4 )
		{
			var iface = SafeGetDefaultEndPoint();
			if ( iface != null )
			{
				bool canUseIPv6;
				try
				{
					iface.GetIPProperties().GetIPv6Properties();
					canUseIPv6 = true;
				}
				catch ( NetworkInformationException )
				{
					canUseIPv6 = false;
				}

				var ipProperties = iface.GetIPProperties();

				if ( ipProperties.UnicastAddresses.Any() )
				{
					UnicastIPAddressInformation address = null;
					if ( canUseIPv6 && !preferIPv4 )
					{
						address = ipProperties.UnicastAddresses.FirstOrDefault( item => item.Address.AddressFamily == AddressFamily.InterNetworkV6 );
					}

					if ( address == null )
					{
						address = ipProperties.UnicastAddresses.FirstOrDefault();
					}

					if ( address != null )
					{
						return new IPEndPoint( address.Address, port );
					}
				}
			}

			return new IPEndPoint( preferIPv4 ? IPAddress.Any : IPAddress.IPv6Any, port );
		}

		private static NetworkInterface SafeGetDefaultEndPoint()
		{
			try
			{
				return NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault();
			}
			catch ( SecurityException ) { }
			catch ( MemberAccessException ) { }

			return null;
		}
	}
}
