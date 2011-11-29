using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;

namespace MsgPack.Rpc.Dispatch
{
	public abstract class DispatchTargetResolver
	{
		public RuntimeMethodHandle ResolveMethod( string methodName )
		{
			if ( methodName == null )
			{
				throw new ArgumentNullException( "methodName" );
			}

			if ( String.IsNullOrWhiteSpace( methodName ) )
			{
				throw new ArgumentException( "'methodName' cannot be empty.", "methodName" );
			}

			Contract.EndContractBlock();

			return this.ResolveMethodCore( methodName );
		}

		protected abstract RuntimeMethodHandle ResolveMethodCore( string methodName );
	}
}
