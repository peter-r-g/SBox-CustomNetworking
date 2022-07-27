using System;
using System.Collections.Generic;
using CustomNetworking.Shared.Entities;

namespace CustomNetworking.Server;

public class EntityManager
{
	public static readonly List<IEntity> All = new();
	private static int _nextEntityId;

}
