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

namespace MsgPack.Rpc
{
	/// <summary>
	///		Defines extension methods for async-RPC related features.
	/// </summary>
	public static class ExceptionExtensions
	{
		/// <summary>
		///		Gets the <see cref="Exception"/> instance that caused the current exception. 
		/// </summary>
		/// <param name="source">The <see cref="Exception"/>.</param>
		/// <returns>
		///		An instance of <see cref="Exception"/> that describes the error that caused the current exception. 
		///		This method returns the same value as was passed into the constructor, or a <c>null</c> reference ( <c>Nothing</c> in Visual Basic) 
		///		if the inner exception value was not supplied to the constructor.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="source"/> is <c>null</c>.
		/// </exception>
		/// <remarks>
		///		<para>
		///			This method returns 'true' error that cauesed the current exception even if the following conditions is true.
		///		</para>
		///		<para>
		///			For implementative restriction of the runtime,
		///			there are no way to tranfer the exception to other thread with capturing stack trace.
		///			So, MessagePack-RPC uses 'Matrioshka', that is, create wrapper exception which has same message and inner exception.
		///			This strategy works well for catch clause (bacause the type of the exceptions are same) and logging (bacause all stack trace is preserved),
		///			bug its <see cref="P:Exception.InnerException"/> is not works well, because its value will be original 'Matrioshka'-ed exception.
		///		</para>
		///		<para>
		///			By using this method, you can get 'true' inner exception from catched <see cref="Exception"/>.
		///		</para>
		/// </remarks>
		public static Exception GetInnerException( this Exception source )
		{
			if ( source == null )
			{
				throw new ArgumentNullException( "source" );
			}

#if NET_4_5
				// Thanks to ExceptionDispatchInfo, Matrioshika is completely unecessary.
				return source.InnerException;
#else
			var inner = source.InnerException;
			if ( inner == null )
			{
				return null;
			}

			if ( !inner.Data.Contains( ExceptionModifiers.IsMatrioshkaInner ) )
			{
				return inner;
			}

			// inner is matrioshka.
			return GetInnerException( inner );
#endif
		}
	}
}
