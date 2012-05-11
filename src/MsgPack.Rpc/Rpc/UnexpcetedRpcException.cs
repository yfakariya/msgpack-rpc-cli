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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Exception in unexpected error.
	/// </summary>
	/// <remarks>
	///		If server returns error but its structure is not compatible with de-facto standard, client library will throw this exception.
	/// </remarks>
#if !SILVERLIGHT
	[Serializable]
#endif
	[SuppressMessage( "Microsoft.Usage", "CA2240:ImplementISerializableCorrectly", Justification = "Using ISafeSerializationData." )]
	[SuppressMessage( "Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Justification = "Using ISafeSerializationData." )]
	public sealed class UnexpcetedRpcException : RpcException
	{
		// NOT readonly for safe deserialization
		private MessagePackObject _error;

		/// <summary>
		///		Get the value of error field of response.
		/// </summary>
		/// <value>
		///		Value of error field of response.
		///		This value is not nil, but its content is arbitary.
		/// </value>
		public MessagePackObject Error
		{
			get { return this._error; }
		}

		// NOT readonly for safe deserialization
		private MessagePackObject _errorDetail;

		/// <summary>
		///		Get the value of return field of response in error.
		/// </summary>
		/// <value>
		///		Value of return field of response in error.
		///		This value may be nil, but server can set any value.
		/// </value>
		public MessagePackObject ErrorDetail
		{
			get { return this._errorDetail; }
		}

		/// <summary>
		///		Initialize new instance.
		/// </summary>
		/// <param name="error">
		///		Value of error field of response.
		///		This value is not nil, but its content is arbitary.
		/// </param>
		/// <param name="errorDetail">
		///		Value of return field of response in error.
		///		This value may be nil, but server can set any value.
		/// </param>
		public UnexpcetedRpcException( MessagePackObject error, MessagePackObject errorDetail )
			: base( RpcError.Unexpected, RpcError.Unexpected.DefaultMessage, null )
		{
			this._error = error;
			this._errorDetail = errorDetail;
		}

#if !SILVERLIGHT
		/// <summary>
		///		When overridden on the derived class, handles <see cref="E:Exception.SerializeObjectState"/> event to add type-specified serialization state.
		/// </summary>
		/// <param name="sender">The <see cref="Exception"/> instance itself.</param>
		/// <param name="e">
		///		The <see cref="System.Runtime.Serialization.SafeSerializationEventArgs"/> instance containing the event data.
		///		The overriding method adds its internal state to this object via <see cref="M:SafeSerializationEventArgs.AddSerializedState"/>.
		///	</param>
		/// <seealso cref="ISafeSerializationData"/>
		protected override void OnSerializeObjectState( object sender, SafeSerializationEventArgs e )
		{
			base.OnSerializeObjectState( sender, e );
			e.AddSerializedState(
				new SerializedState()
				{
					Error = this._error,
					ErrorDetail = this._errorDetail
				}
			);
		}

		[Serializable]
		private sealed class SerializedState : ISafeSerializationData
		{
			public MessagePackObject Error;
			public MessagePackObject ErrorDetail;

			public void CompleteDeserialization( object deserialized )
			{
				var enclosing = deserialized as UnexpcetedRpcException;
				enclosing._error = this.Error;
				enclosing._errorDetail = this.ErrorDetail;
			}
		}
#endif
	}
}
