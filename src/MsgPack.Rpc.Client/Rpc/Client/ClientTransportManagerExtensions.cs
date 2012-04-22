using System;
using System.Diagnostics.Contracts;
using System.Net;
using MsgPack.Rpc.Client.Protocols;
using MsgPack.Serialization;

namespace MsgPack.Rpc.Client
{
	/// <summary>
	///		Defines factory extension methods of <see cref="ClientTransportManager"/> which creates <see cref="RpcClient"/>.
	/// </summary>
	public static class ClientTransportManagerExtensions
	{
		/// <summary>
		///		Creates a new <see cref="RpcClient"/> from <see cref="ClientTransportManager"/>.
		/// </summary>
		/// <param name="source">
		///		<see cref="ClientTransportManager"/> which creates binding <see cref="ClientTransport"/>.
		///	</param>
		/// <param name="targetEndPoint">
		///		<see cref="EndPoint"/> which represents destination end point.
		///	</param>
		/// <returns>
		///		A new <see cref="RpcClient"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="source"/> is <c>null</c>.
		/// </exception>
		public static RpcClient CreateClient( this ClientTransportManager source, EndPoint targetEndPoint )
		{
			if ( source == null )
			{
				throw new ArgumentNullException( "source" );
			}

			Contract.Ensures( Contract.Result<RpcClient>() != null );

			return RpcClient.Create( targetEndPoint, source );
		}

		/// <summary>
		///		Creates a new <see cref="RpcClient"/> from <see cref="ClientTransportManager"/>.
		/// </summary>
		/// <param name="source">
		///		<see cref="ClientTransportManager"/> which creates binding <see cref="ClientTransport"/>.
		///	</param>
		/// <param name="targetEndPoint">
		///		<see cref="EndPoint"/> which represents destination end point.
		///	</param>
		/// <param name="serializationContext">
		///		A <see cref="SerializationContext"/> to hold serializers.
		/// </param>
		/// <returns>
		///		A new <see cref="RpcClient"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="source"/> is <c>null</c>.
		/// </exception>
		public static RpcClient CreateClient( this ClientTransportManager source, EndPoint targetEndPoint, SerializationContext serializationContext )
		{
			if ( source == null )
			{
				throw new ArgumentNullException( "source" );
			}

			Contract.Ensures( Contract.Result<RpcClient>() != null );

			return RpcClient.Create( targetEndPoint, source, serializationContext );
		}
	}
}
