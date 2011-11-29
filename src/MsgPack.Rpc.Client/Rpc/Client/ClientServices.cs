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
using System.IO;
using System.Net.Sockets;
using System.Threading;

using MsgPack.Rpc.Protocols;
using MsgPack.Rpc.Services;

namespace MsgPack.Rpc
{
	// This file generated from ClientServices.tt T4Template.
	// Do not modify this file. Edit ClientServices.tt instead.

	/// <summary>
	///		Represents various configuration information of MessagePack-RPC client.
	/// </summary>
	public static class ClientServices
	{
		/// <summary>
		///		Value which indicates whether I/O complection ports might be available in this runtime.
		/// </summary>
		internal static readonly bool CanUseIOCompletionPort = DetermineCanUseIOCompletionPortOnCli();

		private static bool DetermineCanUseIOCompletionPortOnCli()
		{
			if ( Environment.OSVersion.Platform != PlatformID.Win32NT )
			{
				return false;
			}

			// TODO: silverlight/moonlight path...
			string windir = Environment.GetEnvironmentVariable( "windir" );
			if ( String.IsNullOrEmpty( windir ) )
			{
				return false;
			}

			string clrMSCorLibPath =
				Path.Combine(
					windir,
					"Microsoft.NET",
					"Framework" + ( IntPtr.Size == 8 ? "64" : String.Empty ),
					"v" + Environment.Version.Major + Environment.Version.Minor + Environment.Version.Build,
					"mscorlib.dll"
				);

			return String.Equals( typeof( object ).Assembly.Location, clrMSCorLibPath, StringComparison.OrdinalIgnoreCase );
		}
		
		/// <summary>
		///		Get <see cref="ClientEventLoopFactory"/>.
		/// </summary>
		/// <value>
		/// 	Appropriate <see cref="ClientEventLoopFactory"/>.
		/// </value>
		public static ClientEventLoopFactory EventLoopFactory
		{
			//get { return CanUseIOCompletionPort ? ( ClientEventLoopFactory )new IOCompletionPortClientEventLoopFactory() : new PollingClientEventLoopFactory(); }
			get { return new IOCompletionPortClientEventLoopFactory(); }
		}
		
		private static int _isFrozen;

		/// <summary>
		///		Get the value which indicates this instance is frozen or not.
		/// </summary>
		/// <value>
		///		If this instance is frozen then true.
		/// </value>
		public static bool IsFrozen
		{
			get{ return ClientServices._isFrozen != 0; }
		}

		/// <summary>
		///		Freeze this instance.
		/// </summary>
		/// <remarks>
		///		Frozen instance will be immutable.
		/// </remarks>
		public static void Freeze()
		{
			Interlocked.Exchange( ref ClientServices._isFrozen, 1 );
		}

		private static RequestSerializerFactory _RequestSerializerFactory = RequestSerializerFactory.Default;

		/// <summary>
		///		Get <see cref="RequestSerializerFactory"/> to serialize request and notification message.
		/// </summary>
		/// <value>
		///		<see cref="RequestSerializerFactory"/> to serialize request message.
		/// </value>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public static RequestSerializerFactory RequestSerializerFactory
		{
			get
			{
				return ClientServices._RequestSerializerFactory;
			}
		}
				
		/// <summary>
		///		Set custom <see cref="RequestSerializerFactory"/> to serialize request and notification message.
		/// </summary>
		/// <param name="value">
		///		<see cref="RequestSerializerFactory"/> to serialize request message.
		/// 	If this value is null, reset to default.
		/// </param>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public static void SetRequestSerializerFactory( RequestSerializerFactory value )
		{
			if( ClientServices.IsFrozen )
			{
				throw new InvalidOperationException( "This instance is frozen." );
			}

			Contract.EndContractBlock();

			Interlocked.Exchange( ref ClientServices._RequestSerializerFactory, value ?? RequestSerializerFactory.Default );
		}

		private static ResponseDeserializerFactory _ResponseDeserializerFactory = ResponseDeserializerFactory.Default;

		/// <summary>
		///		Get <see cref="RequestSerializerFactory"/> to deserialize response message.
		/// </summary>
		/// <value>
		///		<see cref="ResponseDeserializerFactory"/> to deserialize response message.
		/// </value>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public static ResponseDeserializerFactory ResponseDeserializerFactory
		{
			get
			{
				return ClientServices._ResponseDeserializerFactory;
			}
		}
				
		/// <summary>
		///		Set custom <see cref="ResponseDeserializerFactory"/> to deserialize response message.
		/// </summary>
		/// <param name="value">
		///		<see cref="ResponseDeserializerFactory"/> to deserialize response message.
		/// 	If this value is null, reset to default.
		/// </param>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public static void SetResponseDeserializerFactory( ResponseDeserializerFactory value )
		{
			if( ClientServices.IsFrozen )
			{
				throw new InvalidOperationException( "This instance is frozen." );
			}

			Contract.EndContractBlock();

			Interlocked.Exchange( ref ClientServices._ResponseDeserializerFactory, value ?? ResponseDeserializerFactory.Default );
		}

		private static Func<Socket, RpcSocket> _SocketFactory = ( socket => new SimpleRpcSocket( socket ) );

		/// <summary>
		///		Get <see cref="Func&lt;T,TResult&gt;">Func&lt;<see cref="Socket"/>, <see cref="RpcSocket"/>&lt;</see> to create <see cref="RpcSocket"/> from <see cref="Socket"/>.
		/// </summary>
		/// <value>
		///		<see cref="Func&lt;T,TResult&gt;">Func&lt;<see cref="Socket"/>, <see cref="RpcSocket"/>&lt;</see> to create <see cref="RpcSocket"/> from <see cref="Socket"/>.
		/// </value>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public static Func<Socket, RpcSocket> SocketFactory
		{
			get
			{
				return ClientServices._SocketFactory;
			}
		}
				
		/// <summary>
		///		Set custom <see cref="Func&lt;T,TResult&gt;">Func&lt;<see cref="Socket"/>, <see cref="RpcSocket"/>&lt;</see> to create <see cref="RpcSocket"/> from <see cref="Socket"/>.
		/// </summary>
		/// <param name="value">
		///		<see cref="Func&lt;T,TResult&gt;">Func&lt;<see cref="Socket"/>, <see cref="RpcSocket"/>&lt;</see> to create <see cref="RpcSocket"/> from <see cref="Socket"/>.
		/// 	If this value is null, reset to default.
		/// </param>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public static void SetSocketFactory( Func<Socket, RpcSocket> value )
		{
			if( ClientServices.IsFrozen )
			{
				throw new InvalidOperationException( "This instance is frozen." );
			}

			Contract.EndContractBlock();

			Interlocked.Exchange( ref ClientServices._SocketFactory, value ?? ( socket => new SimpleRpcSocket( socket ) ) );
		}
	}
}