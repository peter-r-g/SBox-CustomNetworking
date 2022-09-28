#if SERVER
using System;
using System.Collections.Generic;
using System.Reflection;

namespace CustomNetworking.Shared.Utility;

public static partial class TypeHelper
{
	private static readonly Assembly Assembly = Assembly.GetExecutingAssembly();
	private static readonly Dictionary<string, Type> TypeNameCache = new();
	
	/// <summary>
	/// Gets a C# type by its name.
	/// </summary>
	/// <param name="name">The name of the type.</param>
	/// <returns>The type that was found. Null if none were found.</returns>
	public static Type? GetTypeByName( string name )
	{
		if ( TypeNameCache.TryGetValue( name, out var cachedType ) )
			return cachedType;
		
		foreach ( var type in Assembly.DefinedTypes )
		{
			if ( type.Name != name )
				continue;
			
			TypeNameCache.Add( name, type );
			return type;
		}
		
		return null;
	}

	public static T Create<T>()
	{
		return Activator.CreateInstance<T>();
	}

	public static T? Create<T>( Type typeToCreate, params object[] parameters )
	{
		return (T?)Activator.CreateInstance( typeToCreate, parameters );
	}

	public static T? Create<T>( Type baseTypeToCreate, Type[] genericTypes )
	{
		return (T?)Activator.CreateInstance( baseTypeToCreate.MakeGenericType( genericTypes ) );
	}

}
#endif
