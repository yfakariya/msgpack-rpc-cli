#region -- License Terms --
//
// MessagePack for CLI
//
// Copyright (C) 2010-2013 FUJIWARA, Yusuke
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using MsgPack.Rpc.Server.Dispatch.Reflection;
using MsgPack.Serialization;

namespace MsgPack.Rpc.Server.Dispatch
{
	/// <summary>
	///		Emits skelton of the generated service invoker derived class.
	/// </summary>
	internal sealed class ServiceInvokerEmitter : IDisposable
	{
		private static readonly PropertyInfo _serverRuntimeSerializationContextProperty =
			FromExpression.ToProperty( ( RpcServerRuntime runtime ) => runtime.SerializationContext );
		private static readonly Type[] _constructorParameterTypes = new[] { typeof( RpcServerRuntime ), typeof( ServiceDescription ), typeof( MethodInfo ) };
		private static readonly Type[] _invokeCoreParameterTypes = new[] { typeof( Unpacker ) };
		private static readonly MethodInfo _SerializationContextGetSerializer1_Method =
			typeof( SerializationContext ).GetMethod( "GetSerializer", Type.EmptyTypes );

		/// <summary>
		///		 Gets a value indicating whether this instance is trace enabled.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if the trace enabled; otherwise, <c>false</c>.
		/// </value>
		private static bool IsTraceEnabled
		{
			get { return ( Tracer.Emit.Switch.Level & SourceLevels.Verbose ) == SourceLevels.Verbose; }
		}

		private readonly TextWriter _trace = IsTraceEnabled ? new StringWriter( CultureInfo.InvariantCulture ) : TextWriter.Null;

		/// <summary>
		///		Gets the <see cref="TextWriter"/> for tracing.
		/// </summary>
		/// <value>
		///		The <see cref="TextWriter"/> for tracing.
		///		This value will not be <c>null</c>.
		/// </value>
		private TextWriter Trace { get { return this._trace; } }

		/// <summary>
		///		Flushes the trace.
		/// </summary>
		public void FlushTrace()
		{
			StringWriter writer = this._trace as StringWriter;
			if ( writer != null )
			{
				var buffer = writer.GetStringBuilder();
				var source = Tracer.Emit;
				if ( source != null && 0 < buffer.Length )
				{
					source.TraceData( Tracer.EventType.ILTrace, Tracer.EventId.ILTrace, buffer );
				}

				buffer.Clear();
			}
		}

		private readonly ConstructorBuilder _constructorBuilder;
		private readonly TypeBuilder _typeBuilder;
		private readonly MethodBuilder _invokeCoreMethodBuilder;
		private readonly Dictionary<RuntimeTypeHandle, FieldBuilder> _serializers;
		private readonly bool _isDebuggable;
		private MethodBuilder _privateInvokeMethodBuilder;

		public MethodInfo PrivateInvokeMethod
		{
			get { return this._privateInvokeMethodBuilder; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ServiceInvokerEmitter"/> class.
		/// </summary>
		/// <param name="host">The host <see cref="ModuleBuilder"/>.</param>
		/// <param name="sequence">The sequence number to name new type..</param>
		/// <param name="targetType">Type of the invocation target.</param>
		/// <param name="returnType">The type of operation return value. Specify <see cref="Missing"/> for the void.</param>
		/// <param name="isDebuggable">Set to <c>true</c> when <paramref name="host"/> is debuggable.</param>
		public ServiceInvokerEmitter( ModuleBuilder host, long sequence, Type targetType, Type returnType, bool isDebuggable )
		{
			string typeName =
				String.Join(
				Type.Delimiter.ToString(),
				typeof( ServiceInvokerEmitter ).Namespace,
				"Generated",
				IdentifierUtility.EscapeTypeName( targetType ) + "Serializer" + sequence
			);
			Tracer.Emit.TraceEvent( Tracer.EventType.DefineType, Tracer.EventId.DefineType, "Create {0}", typeName );
			this._typeBuilder =
				host.DefineType(
					typeName,
					TypeAttributes.Sealed | TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.AutoLayout | TypeAttributes.BeforeFieldInit,
					typeof( AsyncServiceInvoker<> ).MakeGenericType( returnType == typeof( void ) ? typeof( Missing ) : returnType )
				);

			this._constructorBuilder = this._typeBuilder.DefineConstructor( MethodAttributes.Public, CallingConventions.Standard, _constructorParameterTypes );

			this._invokeCoreMethodBuilder =
				this._typeBuilder.DefineMethod(
					"InvokeCore",
					MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.Final,
					CallingConventions.HasThis,
					typeof( AsyncInvocationResult ),
					_invokeCoreParameterTypes
				);

			this._typeBuilder.DefineMethodOverride( this._invokeCoreMethodBuilder, this._typeBuilder.BaseType.GetMethod( this._invokeCoreMethodBuilder.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ) );

			this._serializers = new Dictionary<RuntimeTypeHandle, FieldBuilder>();
			this._isDebuggable = isDebuggable;
		}

		public void Dispose()
		{
			if ( this._trace != null )
			{
				this._trace.Dispose();
			}
		}

		/// <summary>
		///		Gets the IL generator to implement <see cref="M:ServiceInvoker.InvokeCore"/> overrides.
		/// </summary>
		/// <returns>
		///		The IL generator to implement <see cref="M:ServiceInvoker.InvokeCore"/> overrides.
		///		This value will not be <c>null</c>.
		/// </returns>
		public TracingILGenerator GetInvokeCoreMethodILGenerator()
		{
			if ( IsTraceEnabled )
			{
				this.Trace.WriteLine( "{0}::{1}", MethodBase.GetCurrentMethod(), this._invokeCoreMethodBuilder );
			}

			return new TracingILGenerator( this._invokeCoreMethodBuilder, this.Trace, this._isDebuggable );
		}

		public TracingILGenerator GetPrivateInvokeMethodILGenerator( Type returnType )
		{
			if ( IsTraceEnabled )
			{
				this.Trace.WriteLine( "{0}::{1}", MethodBase.GetCurrentMethod(), this._privateInvokeMethodBuilder );
			}

			if ( this._privateInvokeMethodBuilder == null )
			{
				this._privateInvokeMethodBuilder =
					this._typeBuilder.DefineMethod(
						"PrivateInvoke",
						MethodAttributes.Private,
						CallingConventions.Standard,
						returnType == typeof( void ) ? null : returnType,
						new Type[] { typeof( object ) }
					);
			}

			return new TracingILGenerator( this._privateInvokeMethodBuilder, this.Trace, this._isDebuggable );
		}

		/// <summary>
		///		Creates the serializer type built now and returns its constructor.
		/// </summary>
		/// <returns>
		///		Newly built <see cref="MessagePackSerializer{T}"/> type constructor.
		///		This value will not be <c>null</c>.
		///	</returns>
		public ConstructorInfo Create()
		{
			if ( !this._typeBuilder.IsCreated() )
			{
				/*
				 *	.ctor( RpcServiceRuntime runtime, ServiceDescription serviceDescription, MethodInfo targetOperation ) 
				 *		: base( runtime, serviceDescription, targetOperation )
				 *	{
				 *		var context = runtime.SerializationContext;
				 *		this._serializer0 = context.GetSerializer<T0>();
				 *		this._serializer1 = context.GetSerializer<T1>();
				 *		this._serializer2 = context.GetSerializer<T2>();
				 *			:
				 *	}
				 */
				var il = this._constructorBuilder.GetILGenerator();
				// : base()
				il.Emit( OpCodes.Ldarg_0 );
				il.Emit( OpCodes.Ldarg_1 );
				il.Emit( OpCodes.Ldarg_2 );
				il.Emit( OpCodes.Ldarg_3 );
				il.Emit( OpCodes.Call, this._typeBuilder.BaseType.GetConstructor( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, _constructorParameterTypes, null ) );

				il.DeclareLocal( typeof( SerializationContext ) );
				il.Emit( OpCodes.Ldarg_1 );
				il.Emit( OpCodes.Callvirt, _serverRuntimeSerializationContextProperty.GetGetMethod() );
				il.Emit( OpCodes.Stloc_0 );

				// this._serializerN = context.GetSerializer<T>();
				foreach ( var entry in this._serializers )
				{
					var targetType = Type.GetTypeFromHandle( entry.Key );
					var getMethod = _SerializationContextGetSerializer1_Method.MakeGenericMethod( targetType );
					il.Emit( OpCodes.Ldarg_0 );
					il.Emit( OpCodes.Ldloc_0 );
					il.Emit( OpCodes.Callvirt, getMethod );
					il.Emit( OpCodes.Stfld, entry.Value );
				}

				il.Emit( OpCodes.Ret );
			}

			return this._typeBuilder.CreateType().GetConstructor( _constructorParameterTypes );
		}

		/// <summary>
		///		Creates the invoke type built now and returns its new instance.
		/// </summary>
		/// <param name="runtime">The <see cref="RpcServerRuntime"/> which provides runtime services.</param>
		/// <param name="serviceDescription">The <see cref="ServiceDescription"/> which holds the service spec.</param>
		/// <param name="targetOperation">The <see cref="MethodInfo"/> which holds the operation method spec.</param>
		/// <returns>
		///		Newly built <see cref="IAsyncServiceInvoker"/> instance.
		///		This value will not be <c>null</c>.
		///	</returns>
		public IAsyncServiceInvoker CreateInstance( RpcServerRuntime runtime, ServiceDescription serviceDescription, MethodInfo targetOperation )
		{
			var runtimeParameter = Expression.Parameter( typeof( RpcServerRuntime ) );
			var serviceDescriptionParameter = Expression.Parameter( typeof( ServiceDescription ) );
			var targetOperationParameter = Expression.Parameter( typeof( MethodInfo ) );
			return
				Expression.Lambda<Func<RpcServerRuntime, ServiceDescription, MethodInfo, IAsyncServiceInvoker>>(
					Expression.New(
						this.Create(),
						runtimeParameter,
						serviceDescriptionParameter,
						targetOperationParameter
					),
					runtimeParameter,
					serviceDescriptionParameter,
					targetOperationParameter
				).Compile()( runtime, serviceDescription, targetOperation );
		}

		/// <summary>
		///		Regisgter using <see cref="MessagePackSerializer{T}"/> target type to the current emitting session.
		/// </summary>
		/// <param name="targetType">Type to be serialized/deserialized.</param>
		/// <returns>
		///		<see cref="FieldInfo"/> to refer the serializer in current building type.
		///		This value will not be <c>null</c>.
		/// </returns>
		public FieldInfo RegisterSerializer( Type targetType )
		{
			if ( this._typeBuilder.IsCreated() )
			{
				throw new InvalidOperationException( "Type is already built." );
			}

			FieldBuilder result;
			if ( !this._serializers.TryGetValue( targetType.TypeHandle, out result ) )
			{
				result = this._typeBuilder.DefineField( "_serializer" + this._serializers.Count, typeof( MessagePackSerializer<> ).MakeGenericType( targetType ), FieldAttributes.Private | FieldAttributes.InitOnly );
				this._serializers.Add( targetType.TypeHandle, result );
			}

			return result;
		}
	}
}
