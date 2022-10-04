#if SERVER
using System;
using System.Collections.Generic;
using System.Reflection;

namespace NetBolt.Shared.Utility;

public static partial class TypeHelper
{
	/// <summary>
	/// The assembly to search for types.
	/// </summary>
	private static readonly Assembly Assembly = Assembly.GetExecutingAssembly();
	/// <summary>
	/// A cache of type names mapped to their C# type.
	/// </summary>
	private static readonly Dictionary<string, Type> TypeNameCache = new();

	/// <summary>
	/// Creates an instance of <see cref="T"/>.
	/// </summary>
	/// <typeparam name="T">The type to create an instance of.</typeparam>
	/// <returns>The created instance of <see cref="T"/>.</returns>
	public static T Create<T>()
	{
		return Activator.CreateInstance<T>();
	}

	/// <summary>
	/// Creates an instance of <see cref="T"/>.
	/// </summary>
	/// <param name="parameters">The parameters to pass to the public constructor.</param>
	/// <typeparam name="T">The type to create an instance of.</typeparam>
	/// <returns>The created instance of <see cref="T"/>.</returns>
	public static T? Create<T>( params object[] parameters )
	{
		return (T?)Activator.CreateInstance( typeof(T), parameters );
	}

	/// <summary>
	/// Creates an instance of <see cref="typeToCreate"/> and casts it to <see cref="T"/>.
	/// </summary>
	/// <param name="typeToCreate">The type to create.</param>
	/// <param name="parameters">The parameters to pass to the public constructor.</param>
	/// <typeparam name="T">The type to cast the created instance to.</typeparam>
	/// <returns>The created instance of <see cref="typeToCreate"/> casted to <see cref="T"/>.</returns>
	public static T? Create<T>( Type typeToCreate, params object[] parameters )
	{
		return (T?)Activator.CreateInstance( typeToCreate, parameters );
	}

	/// <summary>
	/// Creates an instance of <see cref="baseTypeToCreate"/> with <see cref="genericTypes"/> generics and casted to <see cref="T"/>.
	/// </summary>
	/// <param name="baseTypeToCreate">The base type to create.</param>
	/// <param name="genericTypes">The generic arguments of <see cref="baseTypeToCreate"/>.</param>
	/// <typeparam name="T">The type to cast the created instance to.</typeparam>
	/// <returns>The created instance casted to <see cref="T"/>.</returns>
	public static T? Create<T>( Type baseTypeToCreate, Type[] genericTypes )
	{
		return (T?)Activator.CreateInstance( baseTypeToCreate.MakeGenericType( genericTypes ) );
	}

	/// <summary>
	/// Gets the generic arguments of a type.
	/// </summary>
	/// <param name="type">The type to get the generic arguments of.</param>
	/// <returns>The generic arguments on the type.</returns>
	public static Type[] GetGenericArguments( Type type )
	{
		return type.GetGenericArguments();
	}

	/// <summary>
	/// Gets all properties on the type.
	/// </summary>
	/// <param name="type">The type to get the properties of.</param>
	/// <returns>The properties on the type.</returns>
	public static PropertyInfo[] GetAllProperties( Type type )
	{
		return type.GetProperties( BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic );
	}
	
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

	/// <summary>
	/// Returns whether or not a type is a class.
	/// </summary>
	/// <param name="type">The type to check if it is a class.</param>
	/// <returns>Whether or not the type is a class.</returns>
	public static bool IsClass( Type type )
	{
		return type.IsClass;
	}
	
	/// <summary>
	/// Returns whether or not a type is a struct.
	/// </summary>
	/// <param name="type">The type to check if it is a struct.</param>
	/// <returns>Whether or not a type is a struct.</returns>
	public static bool IsStruct( Type type )
	{
		return type.IsValueType && !type.IsEnum;
	}
}
#endif
