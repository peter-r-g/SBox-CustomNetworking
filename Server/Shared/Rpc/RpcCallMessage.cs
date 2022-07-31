using System;
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Networkables;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Messages;

public sealed class RpcCallMessage : NetworkMessage
{
	public Guid CallGuid { get; private set; }
	public string ClassName { get; private set; }
	public string MethodName { get; private set; }
	public int EntityId { get; private set; }
	public INetworkable[] Parameters { get; private set; }

	public RpcCallMessage( bool respondable, Type entityType, IEntity? entity, string methodName, params INetworkable[] parameters )
	{
		CallGuid = respondable ? Guid.NewGuid() : Guid.Empty;
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
		CallGuid = Guid.Empty;
		ClassName = string.Empty;
		MethodName = string.Empty;
		EntityId = -1;
		Parameters = Array.Empty<INetworkable>();
	}
	
	public override void Deserialize( NetworkReader reader )
	{
		CallGuid = reader.ReadGuid();
		ClassName = reader.ReadString();
		MethodName = reader.ReadString();
		EntityId = reader.ReadInt32();
		
		Parameters = new INetworkable[reader.ReadInt32()];
		for ( var i = 0; i < Parameters.Length; i++ )
			Parameters[i] = reader.ReadNetworkable();
	}

	public override void Serialize( NetworkWriter writer )
	{
		writer.Write( CallGuid );
		writer.Write( ClassName );
		writer.Write( MethodName );
		writer.Write( EntityId );
		
		writer.Write( Parameters.Length );
		foreach ( var argument in Parameters )
			writer.WriteNetworkable( argument );
	}
}
