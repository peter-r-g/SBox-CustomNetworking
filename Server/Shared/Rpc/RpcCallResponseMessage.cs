using System;
using CustomNetworking.Shared.Networkables;
using CustomNetworking.Shared.RemoteProcedureCalls;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Messages;

/// <summary>
/// A <see cref="NetworkMessage"/> containing a response to a <see cref="RpcCallMessage"/>.
/// </summary>
public sealed class RpcCallResponseMessage : NetworkMessage
{
	/// <summary>
	/// The unique identifier of the <see cref="RpcCallMessage"/> this message is responding to.
	/// </summary>
	public Guid CallGuid { get; private set; }
	/// <summary>
	/// The state of the executed <see cref="RpcCallMessage"/>.
	/// </summary>
	public RpcCallState State { get; private set; }
	/// <summary>
	/// The return value from the <see cref="RpcCallMessage"/>.
	/// </summary>
	public INetworkable? ReturnValue { get; private set; }

	public RpcCallResponseMessage( Guid callGuid, RpcCallState state, INetworkable? returnValue = null )
	{
		CallGuid = callGuid;
		State = state;
		ReturnValue = returnValue;
	}

	public RpcCallResponseMessage()
	{
		CallGuid = Guid.Empty;
		State = RpcCallState.Failed;
		ReturnValue = null;
	}

	public override void Deserialize( NetworkReader reader )
	{
		CallGuid = reader.ReadGuid();
		State = (RpcCallState)reader.ReadByte();
		ReturnValue = reader.ReadBoolean() ? reader.ReadNetworkable() : null;
	}

	public override void Serialize( NetworkWriter writer )
	{
		writer.Write( CallGuid );
		writer.Write( (byte)State );
		
		var hasReturnValue = ReturnValue is not null;
		writer.Write( hasReturnValue );
		if ( hasReturnValue )
			writer.WriteNetworkable( ReturnValue! );
	}
}
