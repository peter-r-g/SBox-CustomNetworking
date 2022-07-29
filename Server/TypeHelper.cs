using System;
using System.Collections.Generic;
using System.Reflection;

namespace CustomNetworking.Server;

public static class TypeHelper
{
	private static readonly Assembly Assembly = Assembly.GetExecutingAssembly();
	private static readonly Dictionary<string, Type> TypeNameCache = new();
	
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
}
