using System;
using CustomNetworking.Shared.Entities;
using CustomNetworking.Shared.Networkables;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Messages;

/// <summary>
/// A <see cref="NetworkMessage"/> containing information to call a method from a different realm.
/// </summary>
public sealed class RpcCallMessage : NetworkMessage
{
	/// <summary>
	/// The unique identifier for the <see cref="RpcCallMessage"/>.
	/// </summary>
	public Guid CallGuid { get; private set; }
	/// <summary>
	/// The class name this <see cref="RpcCallMessage"/> came from.
	/// </summary>
	public string ClassName { get; private set; }
	/// <summary>
	/// The name of the method to call in <see cref="ClassName"/>.
	/// </summary>
	public string MethodName { get; private set; }
	/// <summary>
	/// The name of the <see cref="IEntity"/> component that the <see cref="MethodName"/> came from.
	/// </summary>
	public string? ComponentName { get; private set; }
	/// <summary>
	/// The entity instance identifier to call the <see cref="MethodName"/> on.
	/// </summary>
	public int EntityId { get; private set; }
	/// <summary>
	/// The parameters to send to the <see cref="MethodName"/>.
	/// </summary>
	public INetworkable[] Parameters { get; private set; }

	public RpcCallMessage( bool respondable, Type entityType, IEntity? entity, string methodName, string? componentName,
		params INetworkable[] parameters )
	{
		CallGuid = respondable ? Guid.NewGuid() : Guid.Empty;
		ClassName = entityType.Name;
		if ( entity is null )
			EntityId = -1;
		else
			EntityId = entity.EntityId;

		MethodName = methodName;
		ComponentName = componentName;
		Parameters = parameters;
	}

	public RpcCallMessage()
	{
		CallGuid = Guid.Empty;
		ClassName = string.Empty;
		MethodName = string.Empty;
		ComponentName = null;
		EntityId = -1;
		Parameters = Array.Empty<INetworkable>();
	}
	
	public override void Deserialize( NetworkReader reader )
	{
		CallGuid = reader.ReadGuid();
		ClassName = reader.ReadString();
		MethodName = reader.ReadString();
		if ( reader.ReadBoolean() )
			ComponentName = reader.ReadString();
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
		var hasComponentName = ComponentName is not null;
		writer.Write( hasComponentName );
		if ( hasComponentName )
			writer.Write( ComponentName! );
		writer.Write( EntityId );
		
		writer.Write( Parameters.Length );
		foreach ( var argument in Parameters )
			writer.WriteNetworkable( argument );
	}
}
