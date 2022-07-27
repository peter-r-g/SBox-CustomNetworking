using System;
using System.Collections.Generic;
using CustomNetworking.Shared.Entities;

namespace CustomNetworking.Server;

public class EntityManager
{
	public static readonly List<IEntity> All = new();
	private static int _nextEntityId;

	public void Create<T>() where T : class, IEntity
	{
		var tType = typeof(T);
		var entity = (T?)Activator.CreateInstance( tType, _nextEntityId );
		if ( entity is null )
			throw new Exception( $"Failed to create instance of {tType}" );

		_nextEntityId++;
		All.Add( entity );
	}
}
