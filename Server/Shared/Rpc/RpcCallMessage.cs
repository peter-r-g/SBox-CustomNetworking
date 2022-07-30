using System;
using CustomNetworking.Shared;
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Networkables;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Server.Shared.Messages;

public class RpcCallMessage : NetworkMessage
{
	public string ClassName { get; private set; }
	public string MethodName { get; private set; }
	public int EntityId { get; private set; }
	public INetworkable[] Parameters { get; private set; }

	public RpcCallMessage( bool respondable, Type entityType, IEntity? entity, string methodName, params INetworkable[] parameters )
	{
		ClassName = entityType.Name;
		if ( entity is null )
			EntityId = -1;
		else
			EntityId = entity.EntityId;

		MethodName = methodName;
		Parameters = parameters;
	}

	public RpcCallMessage()
	{
		ClassName = string.Empty;
		MethodName = string.Empty;
		EntityId = -1;
		Parameters = Array.Empty<INetworkable>();
	}
	
	public override void Deserialize( NetworkReader reader )
	{
		ClassName = reader.ReadString();
		MethodName = reader.ReadString();
		EntityId = reader.ReadInt32();
		
		Parameters = new INetworkable[reader.ReadInt32()];
		for ( var i = 0; i < Parameters.Length; i++ )
			Parameters[i] = reader.ReadNetworkable();
	}

	public override void Serialize( NetworkWriter writer )
	{
		writer.Write( ClassName );
		writer.Write( MethodName );
		writer.Write( EntityId );
		
		writer.Write( Parameters.Length );
		foreach ( var argument in Parameters )
			writer.WriteNetworkable( argument );
	}
}
