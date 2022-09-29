#if CLIENT
using System;
using Sandbox;

namespace CustomNetworking.Shared.Utility;

public static partial class TypeHelper
{
	/// <summary>
	/// Creates an instance of <see cref="T"/>.
	/// </summary>
	/// <typeparam name="T">The type to create an instance of.</typeparam>
	/// <returns>The created instance of <see cref="T"/>.</returns>
	public static T Create<T>()
	{
		return TypeLibrary.Create<T>( typeof(T) );
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
		return TypeLibrary.Create<T>( typeToCreate, parameters );
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
		return TypeLibrary.GetDescription( baseTypeToCreate ).CreateGeneric<T>( genericTypes );
	}

	/// <summary>
	/// Gets the generic arguments of a type.
	/// </summary>
	/// <param name="type">The type to get the generic arguments of.</param>
	/// <returns>The generic arguments on the type.</returns>
	public static Type[] GetGenericArguments( Type type )
	{
		return TypeLibrary.GetDescription( type ).GenericArguments;
	}
	
	/// <summary>
	/// Gets all public properties on the type.
	/// </summary>
	/// <param name="type">The type to get the public properties of.</param>
	/// <returns>The public properties on the type.</returns>
	public static PropertyDescription[] GetProperties( Type type )
	{
		return TypeLibrary.GetDescription( type ).Properties;
	}
	
	/// <summary>
	/// Gets a C# type by its name.
	/// </summary>
	/// <param name="name">The name of the type.</param>
	/// <returns>The type that was found. Null if none were found.</returns>
	public static Type? GetTypeByName( string name )
	{
		return TypeLibrary.GetDescription( name ).TargetType;
	}

	/// <summary>
	/// Returns whether or not a type is a class.
	/// </summary>
	/// <param name="type">The type to check if it is a class.</param>
	/// <returns>Whether or not the type is a class.</returns>
	public static bool IsClass( Type type )
	{
		return TypeLibrary.GetDescription( type ).IsClass;
	}
}
#endif
