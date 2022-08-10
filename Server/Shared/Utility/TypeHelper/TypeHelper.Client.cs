#if CLIENT
using System;

namespace CustomNetworking.Shared.Utility;

public static partial class TypeHelper
{
	public static Type? GetTypeByName( string name )
	{
		return TypeLibrary.GetTypeByName( name );
	}

	public static T? Create<T>( Type typeToCreate )
	{
		return TypeLibrary.Create<T>( typeToCreate );
	}
	
	public static T? Create<T>( Type typeToCreate, params object[] parameters )
	{
		return TypeLibrary.Create<T>( typeToCreate, parameters );
	}
}
#endif
