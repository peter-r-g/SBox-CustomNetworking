#if SERVER

using System.Collections.Generic;

namespace CustomNetworking.Shared.Entities;

public partial class NetworkEntity
{
	private readonly HashSet<string> _changedProperties = new();
	
	/// <summary>
	/// <see cref="Update"/> but for the server realm.
	/// </summary>
	protected virtual void UpdateServer()
	{
	}

	/// <summary>
	/// Marks a property as changed and invokes the <see cref="Changed"/> event.
	/// </summary>
	/// <param name="propertyName">The name of the property that changed.</param>
	protected void TriggerNetworkingChange( string propertyName )
	{
		_changedProperties.Add( propertyName );
		Changed?.Invoke( this );
	}
}
#endif
