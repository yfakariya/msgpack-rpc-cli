using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MsgPack.Rpc.Client.Protocols;
using MsgPack.Serialization;

namespace MsgPack.Rpc.Client
{
	public static class ClientTransportManagerExtensions
	{
		public static RpcClient CreateClient( this ClientTransportManager source, EndPoint targetEndPoint )
		{
			if ( source == null )
			{
				throw new ArgumentNullException( "source" );
			}

			Contract.EndContractBlock();

			return RpcClient.Create( targetEndPoint, source );
		}

		public static RpcClient CreateClient( this ClientTransportManager source, EndPoint targetEndPoint, SerializationContext serializationContext )
		{
			if ( source == null )
			{
				throw new ArgumentNullException( "source" );
			}

			Contract.EndContractBlock();

			return RpcClient.Create( targetEndPoint, source, serializationContext );
		}
	}
}
