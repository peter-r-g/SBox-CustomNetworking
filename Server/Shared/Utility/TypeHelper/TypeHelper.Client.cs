#if CLIENT
using System;
using Sandbox;

namespace CustomNetworking.Shared.Utility;

public static partial class TypeHelper
{
	public static Type? GetTypeByName( string name )
	{
		return TypeLibrary.GetDescription( name ).TargetType;
	}

	public static T Create<T>()
	{
		return TypeLibrary.Create<T>( typeof(T) );
	}

	public static T? Create<T>( Type typeToCreate )
	{
		return TypeLibrary.Create<T>( typeToCreate );
	}
	
	public static T? Create<T>( Type typeToCreate, params object[] parameters )
	{
		return TypeLibrary.Create<T>( typeToCreate, parameters );
	}

	public static T? Create<T>( Type baseTypeToCreate, Type[] genericTypes )
	{
		return TypeLibrary.GetDescription( baseTypeToCreate ).CreateGeneric<T>( genericTypes );
	}

	public static Type[] GetGenericArguments( Type type )
	{
		return TypeLibrary.GetDescription( type ).GenericArguments;
	}
	
}
#endif
