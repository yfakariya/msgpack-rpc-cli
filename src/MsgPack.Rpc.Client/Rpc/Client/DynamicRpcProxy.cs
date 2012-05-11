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
using System.Dynamic;
using System.Net;
using MsgPack.Serialization;

namespace MsgPack.Rpc.Client
{
	/// <summary>
	///		Acts as dynamic MessagePack RPC proxy object.
	/// </summary>
	/// <remarks>
	///		This object takes naming convention to interpret target method as following:
	///		<list type="bullet">
	///			<item>
	///				<para>
	///					If the method name starts with "Begin" 
	///					and its arguments contains <see cref="AsyncCallback"/>( or <see cref="Action{T}"/> of <see cref="IAsyncResult"/>) at just before tail,
	///					invoking method is considered as "Begin" call of the target method,
	///					and the rest of method name is actual neme of the remote method.
	///				</para>
	///				<para>
	///					For example, if you type <c>d.BeginFoo(arg1, callback, state)</c> then <c>Foo</c> with 1 argument (<c>arg1</c>) will be invoked,
	///					and <see cref="IAsyncResult"/> will be returned.
	///				</para>
	///				<para>
	///					Note that the method name is equal to "Begin",
	///					or arguments signature is not match,
	///					this will be considered as synchronous method invocation for the remote method whose name is "Begin".
	///				</para>
	///			</item>
	///			<item>
	///				<para>
	///					If the method name starts with "End" 
	///					and its arguments contains <see cref="AsyncCallback"/>( or <see cref="Action{T}"/> of <see cref="IAsyncResult"/>) at just before tail,
	///					invoking method is considered as "End" call of the target method,
	///					and the rest of method name is just ignored.
	///				</para>
	///				<para>
	///					For example, if you type <c>d.EndFoo(ar)</c> then the return value will be returned.
	///				</para>
	///				<para>
	///					Note that the method name is equal to "End",
	///					or arguments signature is not match,
	///					this will be considered as synchronous method invocation for the remote method whose name is "End".
	///				</para>
	///			</item>
	///			<item>
	///				<para>
	///					If the method name ends with "Async" 
	///					and its arguments are not empty
	///					invoking method is considered as "Async" call of the target method and the last argument is considered as <c>asyncState</c>,
	///					and the rest of method name is actual neme of the remote method.
	///				</para>
	///				<para>
	///					For example, if you type <c>d.FooAsync(arg1, state)</c> then <c>Foo</c> with 1 argument (<c>arg1</c>) will be invoked,
	///					and <see cref="System.Threading.Tasks.Task{T}"/> of <see cref="MessagePackObject"/> will be returned.
	///				</para>
	///				<para>
	///					Note that the method name is equal to "Async",
	///					or arguments signature is not match,
	///					this will be considered as synchronous method invocation for the remote method whose name is "Async".
	///				</para>
	///			</item>
	///			<item>
	///				Else, the remote method is invoke as specified name.
	///			</item>
	///		</list>
	///		<note>
	///			Every method always called as "Request" message, not "Notification".
	///			When you have to use "Notification" message, use <see cref="RpcClient"/> directly.
	///		</note>
	/// </remarks>
	public sealed class DynamicRpcProxy : DynamicObject, IDisposable
	{
		private readonly RpcClient _client;

		/// <summary>
		///		Initializes a new instance of the <see cref="DynamicRpcProxy"/> class.
		/// </summary>
		/// <param name="client">An underlying <see cref="RpcClient"/>.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="client"/> is <c>null</c>.
		/// </exception>
		public DynamicRpcProxy( RpcClient client )
		{
			if ( client == null )
			{
				throw new ArgumentNullException( "client" );
			}

			Contract.EndContractBlock();

			this._client = client;
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="DynamicRpcProxy"/> class.
		/// </summary>
		/// <param name="targetEndPoint">
		///		<see cref="EndPoint"/> for the target.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="targetEndPoint"/> is <c>null</c>.
		/// </exception>
		public DynamicRpcProxy( EndPoint targetEndPoint )
			: this( new RpcClient( targetEndPoint ) ) { }

		/// <summary>
		///		Initializes a new instance of the <see cref="DynamicRpcProxy"/> class.
		/// </summary>
		/// <param name="targetEndPoint">
		///		<see cref="EndPoint"/> for the target.
		/// </param>
		/// <param name="configuration">
		///		A <see cref="RpcClientConfiguration"/> which contains protocol information etc.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="targetEndPoint"/> is <c>null</c>.
		/// </exception>
		public DynamicRpcProxy( EndPoint targetEndPoint, RpcClientConfiguration configuration )
			: this( new RpcClient( targetEndPoint, configuration ) ) { }

		/// <summary>
		///		Initializes a new instance of the <see cref="DynamicRpcProxy"/> class.
		/// </summary>
		/// <param name="targetEndPoint">
		///		<see cref="EndPoint"/> for the target.
		/// </param>
		/// <param name="serializationContext">
		///		A <see cref="SerializationContext"/> to hold serializers.
		///	</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="targetEndPoint"/> is <c>null</c>.
		/// </exception>
		public DynamicRpcProxy( EndPoint targetEndPoint, SerializationContext serializationContext )
			: this( new RpcClient( targetEndPoint, serializationContext ) ) { }

		/// <summary>
		///		Initializes a new instance of the <see cref="DynamicRpcProxy"/> class.
		/// </summary>
		/// <param name="targetEndPoint">
		///		<see cref="EndPoint"/> for the target.
		/// </param>
		/// <param name="configuration">
		///		A <see cref="RpcClientConfiguration"/> which contains protocol information etc.
		/// </param>
		/// <param name="serializationContext">
		///		A <see cref="SerializationContext"/> to hold serializers.
		///	</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="targetEndPoint"/> is <c>null</c>.
		/// </exception>
		public DynamicRpcProxy( EndPoint targetEndPoint, RpcClientConfiguration configuration, SerializationContext serializationContext )
			: this( new RpcClient( targetEndPoint, configuration, serializationContext ) ) { }

		/// <summary>
		///		Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			this._client.Dispose();
		}

		/// <summary>
		///		Provides the implementation for operations that invoke a member. 
		/// </summary>
		/// <param name="binder">
		///		Provides information about the dynamic operation. 
		/// </param>
		/// <param name="args">
		///		The arguments that are passed to the object member during the invoke operation.
		/// </param>
		/// <param name="result">
		///		The result of the member invocation.
		/// </param>
		/// <returns>
		///		This method always returns <c>true</c> because client cannot determine whether the remote server actually implements specified method or not ultimately.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="binder"/> is <c>null</c>.
		/// </exception>
		/// <remarks>
		///		For detailed information, see the <strong>remarks</strong> section of the type description.
		/// </remarks>
		public override bool TryInvokeMember( InvokeMemberBinder binder, object[] args, out object result )
		{
			if ( binder == null )
			{
				throw new ArgumentNullException( "binder" );
			}

			if ( binder.Name.StartsWith( "Begin", binder.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal )
				&& binder.Name.Length > "Begin".Length
				&& args.Length >= 2 )
			{
				var asAsyncCallback = args[ args.Length - 2 ] as AsyncCallback;
				var asAction = args[ args.Length - 2 ] as Action<IAsyncResult>;
				if ( args[ args.Length - 2 ] == null || asAsyncCallback != null || asAsyncCallback != null )
				{
					var realArgs = new object[ args.Length - 2 ];
					Array.ConstrainedCopy( args, 0, realArgs, 0, args.Length - 2 );
					if ( asAsyncCallback == null && asAction != null )
					{
						asAsyncCallback = ar => asAction( ar );
					}
					result = this._client.BeginCall( binder.Name.Substring( "Begin".Length ), realArgs, asAsyncCallback, args[ args.Length - 1 ] );
					return true;
				}
			}
			else if ( binder.Name.StartsWith( "End", binder.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal )
				&& binder.Name.Length > "End".Length
				&& args.Length == 1 )
			{
				var ar = args[ 0 ] as IAsyncResult;
				if ( ar != null )
				{
					result = this._client.EndCall( ar );
					return true;
				}
			}
			else if ( binder.Name.EndsWith( "Async", binder.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal )
				&& binder.Name.Length > "Async".Length
				&& args.Length >= 1 )
			{
				var realArgs = new object[ args.Length - 1 ];
				Array.ConstrainedCopy( args, 0, realArgs, 0, args.Length - 1 );
				result = this._client.CallAsync( binder.Name.Remove( binder.Name.Length - "Async".Length ), realArgs, args[ args.Length - 1 ] );
				return true;
			}

			result = this._client.Call( binder.Name, args );
			return true;
		}
	}
}
