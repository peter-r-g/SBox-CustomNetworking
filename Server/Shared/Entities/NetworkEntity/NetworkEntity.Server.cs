#if SERVER

using System.Collections.Generic;

namespace CustomNetworking.Shared.Entities;

public partial class NetworkEntity
{
	private readonly HashSet<string> _changedProperties = new();
	
	protected virtual void UpdateServer()
	{
	}

	protected void TriggerNetworkingChange( string propertyName )
	{
		_changedProperties.Add( propertyName );
		Changed?.Invoke( this );
	}
}
#endif
