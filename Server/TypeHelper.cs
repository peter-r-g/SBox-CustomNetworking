using System;
using System.Collections.Generic;
using System.Reflection;

namespace CustomNetworking.Server;

/// <summary>
/// A collection of helper methods to get types from the current assembly.
/// </summary>
public static class TypeHelper
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
}
