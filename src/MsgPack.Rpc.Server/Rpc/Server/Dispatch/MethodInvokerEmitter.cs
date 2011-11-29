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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Globalization;

namespace MsgPack.Rpc.Dispatch
{
	/// <summary>
	///		Emit <see cref="MethodInvoker"/> on the fly using Reflection Emit.
	/// </summary>
	internal sealed class MethodInvokerEmitter
	{
		private static readonly Dictionary<Type, MethodInfo> _conversionOperators =
			typeof( MessagePackObject )
			.GetMethods( BindingFlags.Public | BindingFlags.Instance )
			.Where( method =>
				method.ReturnType != typeof( void )
				&& method.GetParameters().Length == 0
				&& method.Name.StartsWith( "As", StringComparison.Ordinal )
				&& method.Name != "ToString"
			).ToDictionary( method => method.ReturnType );

		#region -- MethoInvoker metadata --

		// public this[int] -- IList<>
		private static readonly MethodInfo _ilistGetItem =
			typeof( IList<> ).GetProperty( "Item", BindingFlags.Public | BindingFlags.Instance, null, typeof( MessagePackObject ), new Type[] { typeof( int ) }, null ).GetGetMethod( false );

		// for emitting ctor.
		private static readonly Type[] _ctorParameterTypes = new Type[] { typeof( Func<object> ) };
		// override Invoke
		private static readonly Type[] _invokeParameterTypes = new Type[] { typeof( IList<MessagePackObject> ) };
		// Parameter types array of protected MethodInvoker(RuntimeMethodHandle,Func<object>)
		private static readonly ConstructorInfo _methodInvokder_ctor =
			typeof( MethodInvoker ).GetConstructor( BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof( RuntimeMethodHandle ), typeof( Func<object> ) }, null );
		// public object GetInstance() -- MethodInvoker
		private static readonly MethodInfo _getInstance =
			typeof( MethodInvoker ).GetMethod( "GetInstance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null );
		// protected MethodInvoker(RuntimeMethodHandle,Func<object>)
		private static readonly MethodInfo _invokeCore =
			typeof( MethodInvoker ).GetMethod( "InvokeCore", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, _invokeParameterTypes, null );
		// protected static object ConvertEnumerable( IEnumerable<MessagePackObject> )  -- MethodInvoker
		private static readonly MethodInfo _convertEnumerable =
			typeof( MethodInvoker ).GetMethod( "ConvertEnumerable", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof( IEnumerable<MessagePackObject> ), typeof( Type ) }, null );
		// protected static object ConvertList( IList<MessagePackObject> )  -- MethodInvoker
		private static readonly MethodInfo _convertList =
			typeof( MethodInvoker ).GetMethod( "ConvertList", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof( IList<MessagePackObject> ), typeof( Type ) }, null );
		// protected static object ConvertDictionary( IDictionary<MessagePackObject, MessagePackObject> )  -- MethodInvoker
		private static readonly MethodInfo _convertDictionary =
			typeof( MethodInvoker ).GetMethod( "ConvertDictionary", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof( IDictionary<MessagePackObject, MessagePackObject> ), typeof( Type ), typeof( Type ) }, null );
		// public static MethodBase GetMethodFromHandle(RuntimeMethodHandle) -- MethodBase
		private static readonly MethodInfo _getMethodFromHandle =
			typeof( MethodBase ).GetMethod( "GetMethodFromHandle", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof( RuntimeMethodHandle ) }, null );
		// public static Type GetTypeFromHandle(RuntimeTypeHandle) -- Type
		private static readonly MethodInfo _getTypeFromHandle =
			typeof( Type ).GetMethod( "GetTypeFromHandle", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof( RuntimeTypeHandle ) }, null );
		// public InvocationResult(Object, Boolean)
		private static readonly ConstructorInfo _invocationResult_ctor_Object_Boolean =
			typeof( InvocationResult ).GetConstructor( BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof( object ), typeof( bool ) }, null );
		// public InvocationResult(Exception, Boolean)
		private static readonly ConstructorInfo _invocationResult_ctor_Exception_Boolean =
			typeof( InvocationResult ).GetConstructor( BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof( Exception ), typeof( bool ) }, null );

		#endregion -- MethoInvoker metadata --

		private static int _sequence;

		private readonly AssemblyBuilder _targetAssembly;
		private readonly ModuleBuilder _targetModule;

		public MethodInvokerEmitter( MethodInvokerEmitterMode mode )
		{
			int sequence = Interlocked.Increment( ref _sequence );
			string name = "MsgPack.Rpc.MethodInvokers." + sequence;
			this._targetAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly( new AssemblyName( name ), ToAssemblyBuilderAccess( mode ) );
			this._targetModule = this._targetAssembly.DefineDynamicModule( name + ".dll" );
		}

		private static AssemblyBuilderAccess ToAssemblyBuilderAccess( MethodInvokerEmitterMode mode )
		{
			switch ( mode )
			{
				case MethodInvokerEmitterMode.Saveable:
				{
					return AssemblyBuilderAccess.RunAndSave;
				}
				case MethodInvokerEmitterMode.Collectable:
				{
					return AssemblyBuilderAccess.RunAndCollect;
				}
				default:
				{
					return AssemblyBuilderAccess.RunAndCollect;
				}
			}
		}

		public Type Emit( MethodInfo targetMethod )
		{
			if ( targetMethod == null )
			{
				throw new ArgumentNullException( "targetMethod" );
			}

			if ( ( targetMethod.CallingConvention & CallingConventions.VarArgs ) == CallingConventions.VarArgs )
			{
				throw new NotSupportedException( "VarArgs method is not supported." );
			}

			Contract.EndContractBlock();

			var typeBuilder = this._targetModule.DefineType( CreateMethodIdentifier( targetMethod ), TypeAttributes.Public | TypeAttributes.Sealed );
			typeBuilder.SetParent( typeof( MethodInvoker ) );

			// ctor
			EmitConstructor( targetMethod, typeBuilder );

			// Invoke
			EmitInvoke( targetMethod, typeBuilder );

			return typeBuilder.CreateType();
		}

		private static string CreateMethodIdentifier( MethodInfo targetMethod )
		{
			StringBuilder buffer = new StringBuilder( "MsgPack.Rpc.Dispatch.EmittedInvokers." );
			buffer.Append( targetMethod.DeclaringType.FullName.Replace( Type.Delimiter, '_' ) );
			buffer.Append( '_' );
			buffer.Append( targetMethod.Name );
			buffer.Append( '_' );
			buffer.Append( targetMethod.MethodHandle.Value.ToString( "x" ) );
			buffer.Append( "_Invoker" );
			return buffer.ToString();
		}

		private void EmitConstructor( MethodInfo targetMethod, TypeBuilder typeBuilder )
		{
			var ctorBuilder = typeBuilder.DefineConstructor( MethodAttributes.Public | MethodAttributes.Final, CallingConventions.HasThis, _ctorParameterTypes );
			ctorBuilder.DefineParameter( 0, ParameterAttributes.None, "instanceFactory" );
			var il = ctorBuilder.GetILGenerator();
			// methodhandleof( Target )
			il.Emit( OpCodes.Ldtoken, this._targetModule.GetMethodToken( targetMethod ).Token );
			// instanceFactory
			il.Emit( OpCodes.Ldarg_2 );
			// : base( , )
			il.Emit( OpCodes.Call, _methodInvokder_ctor );
			il.Emit( OpCodes.Ret );
		}

		private void EmitInvoke( MethodInfo targetMethod, TypeBuilder typeBuilder )
		{
			var methodBuilder =
				typeBuilder.DefineMethod(
					_invokeCore.Name,
					( _invokeCore.Attributes | MethodAttributes.MemberAccessMask ) | MethodAttributes.Final,
					CallingConventions.HasThis,
					_invokeCore.ReturnType,
					_invokeParameterTypes
				);
			typeBuilder.DefineMethodOverride( methodBuilder, _invokeCore );
			var il = methodBuilder.GetILGenerator();
			il.DeclareLocal( typeof( InvocationResult ) );
			il.DeclareLocal( typeof( Exception ) );
			if ( targetMethod.ReturnType != null )
			{
				il.DeclareLocal( typeof( object ) );
			}

			var parameters = targetMethod.GetParameters();
			// pushing conveted real arguments to evaluation stack...
			for ( int i = 0; i < parameters.Length; i++ )
			{
				EmitArgumentConversion( il, parameters[ i ], i );
			}

			// begin try block to catch any exception thrown by target method.
			il.BeginExceptionBlock();
			// call target method.
			if ( targetMethod.IsStatic )
			{
				il.Emit( OpCodes.Call, targetMethod );
			}
			else
			{
				// push target instance.
				il.Emit( OpCodes.Ldarg_0 );
				il.Emit( OpCodes.Call, _getInstance );
				// It is legal to call non-virtual instance method via callvirt.
				// It throws NullReferenceException if target instance is null.
				il.Emit( OpCodes.Callvirt, targetMethod );
			}

			if ( targetMethod.ReturnType != typeof( void ) )
			{
				// l2 = ...
				il.Emit( OpCodes.Stloc_2 );
			}

			// leave/leave.s will be emitted automatically.
			// catch(Exception ex)
			il.BeginCatchBlock( typeof( Exception ) );
			// l1 = ex;
			il.Emit( OpCodes.Stloc_1 );
			// leave/leave.s will be emitted automatically.
			// }
			il.EndExceptionBlock();

			var endIf = il.DefineLabel();
			var @else = il.DefineLabel();

			// if( l1 != null ) { // error
			il.Emit( OpCodes.Ldloc_1 );
			il.Emit( OpCodes.Brfalse_S, @else );

			// l0 = new InvocationResult( l1, b );
			// l1
			il.Emit( OpCodes.Ldloc_1 );
			if ( targetMethod.ReturnType == typeof( void ) )
			{
				// true
				il.Emit( OpCodes.Ldc_I4_1 );
			}
			else
			{
				// false
				il.Emit( OpCodes.Ldc_I4_0 );
			}

			// new InvocationResult
			il.Emit( OpCodes.Newobj, _invocationResult_ctor_Exception_Boolean );
			// l0 = ...
			il.Emit( OpCodes.Stloc_0 );

			// goto endif
			il.Emit( OpCodes.Br_S, endIf );

			// } else {
			il.MarkLabel( @else );

			// l0 = new InvocationResult( l2, b );
			// l1
			il.Emit( OpCodes.Ldloc_2 );
			if ( targetMethod.ReturnType == typeof( void ) )
			{
				// true
				il.Emit( OpCodes.Ldc_I4_1 );
			}
			else
			{
				// false
				il.Emit( OpCodes.Ldc_I4_0 );
			}

			// new InvocationResult
			il.Emit( OpCodes.Newobj, _invocationResult_ctor_Object_Boolean );
			// l0 = ...
			il.Emit( OpCodes.Stloc_0 );

			// }
			il.MarkLabel( endIf );

			// return l0;
			il.Emit( OpCodes.Ldloc_0 );
			il.Emit( OpCodes.Ret );
		}

		private void EmitArgumentConversion( ILGenerator il, ParameterInfo targetParameter, int position )
		{
			// ldarg
			// ldc_i4
			// call get_Item
			// conv

			// push arguments(1st argument)
			il.Emit( OpCodes.Ldarg_1 );

			// push argument index as literal i4(Int32)
			switch ( position )
			{
				case 0:
				{
					il.Emit( OpCodes.Ldc_I4_0 );
					break;
				}
				case 1:
				{
					il.Emit( OpCodes.Ldc_I4_1 );
					break;
				}
				case 2:
				{
					il.Emit( OpCodes.Ldc_I4_2 );
					break;
				}
				case 3:
				{
					il.Emit( OpCodes.Ldc_I4_3 );
					break;
				}
				case 4:
				{
					il.Emit( OpCodes.Ldc_I4_4 );
					break;
				}
				case 5:
				{
					il.Emit( OpCodes.Ldc_I4_5 );
					break;
				}
				case 6:
				{
					il.Emit( OpCodes.Ldc_I4_6 );
					break;
				}
				case 7:
				{
					il.Emit( OpCodes.Ldc_I4_7 );
					break;
				}
				case 8:
				{
					il.Emit( OpCodes.Ldc_I4_8 );
					break;
				}
				default:
				{
					if ( position > Byte.MaxValue + 1 )
					{
						il.Emit( OpCodes.Ldc_I4, position );
					}
					else
					{
						il.Emit( OpCodes.Ldc_I4_S, position );
					}
					break;
				}
			}

			// call indexer
			il.Emit( OpCodes.Call, _ilistGetItem );
			TypeConversionMethod methodology;
			Type conversionKey = ToAppropriateParameterType( targetParameter.ParameterType, out methodology );
			switch ( methodology )
			{
				case TypeConversionMethod.MessagePackObject:
				{
					// arguments[ i ]
					break;
				}
				case TypeConversionMethod.As:
				{
					MethodInfo conversion;
					if ( !_conversionOperators.TryGetValue( conversionKey, out conversion ) )
					{
						// arguments[ i ].AsT
						il.Emit( OpCodes.Call, conversion );
						break;
					}
					else
					{
						goto default;
					}
				}
				case TypeConversionMethod.Enumerable:
				{
					// ConvertEnumerable( arguments[ i ], typeof( T ) ) as IEnumerable<T>;
					il.Emit( OpCodes.Ldtoken, this._targetModule.GetTypeToken( targetParameter.ParameterType.GetGenericArguments()[ 0 ] ).Token );
					il.Emit( OpCodes.Call, _getTypeFromHandle );
					il.Emit( OpCodes.Call, _convertEnumerable );
					il.Emit( OpCodes.Isinst, targetParameter.ParameterType );
					break;
				}
				case TypeConversionMethod.List:
				{
					// ConvertList( arguments[ i ], typeof( T ) ) as IList<T>;
					il.Emit( OpCodes.Ldtoken, this._targetModule.GetTypeToken( targetParameter.ParameterType.GetGenericArguments()[ 0 ] ).Token );
					il.Emit( OpCodes.Call, _getTypeFromHandle );
					il.Emit( OpCodes.Call, _convertList );
					il.Emit( OpCodes.Isinst, targetParameter.ParameterType );
					break;
				}
				case TypeConversionMethod.Dictionary:
				{
					// ConvertDictionary( arguments[ i ], typeof( TKey ), typeof( TValue ) ) as IDictionary<TKey, TValue>;
					il.Emit( OpCodes.Ldtoken, this._targetModule.GetTypeToken( targetParameter.ParameterType.GetGenericArguments()[ 0 ] ).Token );
					il.Emit( OpCodes.Call, _getTypeFromHandle );
					il.Emit( OpCodes.Ldtoken, this._targetModule.GetTypeToken( targetParameter.ParameterType.GetGenericArguments()[ 1 ] ).Token );
					il.Emit( OpCodes.Call, _getTypeFromHandle );
					il.Emit( OpCodes.Call, _convertDictionary );
					il.Emit( OpCodes.Isinst, targetParameter.ParameterType );
					break;
				}
				default:
				{
					throw new NotSupportedException(
							String.Format( CultureInfo.CurrentCulture, "Parameter type '{0}' is not supported.", targetParameter.ParameterType )
						);
				}
			}
		}

		private static Type ToAppropriateParameterType( Type type, out TypeConversionMethod method )
		{
			if ( type == typeof( MessagePackObject ) )
			{
				method = TypeConversionMethod.MessagePackObject;
				return null;
			}

			if ( type.IsPrimitive )
			{
				method = TypeConversionMethod.As;
				return type;
			}

			if ( type == typeof( byte[] ) || type == typeof( String ) )
			{
				method = TypeConversionMethod.As;
				return type;
			}

			if ( type.IsInterface && type.IsGenericType )
			{
				var typeDef = type.GetGenericTypeDefinition();
				if ( typeDef == typeof( IDictionary<,> ) )
				{
					method = TypeConversionMethod.Dictionary;
					return null;
				}

				if ( typeDef == typeof( IList<> ) )
				{
					method = TypeConversionMethod.List;
					return null;
				}

				if ( typeDef == typeof( IEnumerable<> ) )
				{
					method = TypeConversionMethod.Enumerable;
					return null;
				}
			}

			var interfaces = type.GetInterfaces();
			if ( interfaces.Length > 0 )
			{
				var interfaceDefs =
					interfaces
						.Where( item => item.IsGenericType )
						.Select( item => item.GetGenericTypeDefinition() )
						.ToArray();

				if ( interfaceDefs.Any( item => item == typeof( IDictionary<,> ) ) )
				{
					method = TypeConversionMethod.Dictionary;
					return null;
				}

				if ( interfaceDefs.Any( item => item == typeof( IList<> ) ) )
				{
					method = TypeConversionMethod.List;
					return null;
				}

				if ( interfaceDefs.Any( item => item == typeof( IEnumerable<> ) ) )
				{
					method = TypeConversionMethod.Enumerable;
					return null;
				}
			}

			method = TypeConversionMethod.None;
			return null;
		}
		
		private enum TypeConversionMethod
		{
			None = 0,
			As,
			Enumerable,
			List,
			Dictionary,
			MessagePackObject
		}
	}
}
