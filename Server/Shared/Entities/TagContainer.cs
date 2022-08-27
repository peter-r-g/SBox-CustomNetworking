using System;
using CustomNetworking.Shared.Networkables;
using CustomNetworking.Shared.Networkables.Builtin;

namespace CustomNetworking.Shared.Entities;

public class TagContainer : BaseNetworkable
{
	public new event EventHandler? Changed;
	
	private NetworkedHashSet<NetworkedString> Tags { get; }

	internal TagContainer()
	{
		Tags = new NetworkedHashSet<NetworkedString>();
		Tags.Changed += OnTagsChanged;
	}

	~TagContainer()
	{
		Tags.Changed -= OnTagsChanged;
	}

	public bool Add( string tag )
	{
		return Tags.Add( tag );
	}

	public bool Remove( string tag )
	{
		return Tags.Remove( tag );
	}

	public bool Has( string tag )
	{
		return Tags.Contains( tag );
	}

	public void Clear()
	{
		Tags.Clear();
	}
	
	private void OnTagsChanged( object? sender, EventArgs args )
	{
		Changed?.Invoke( sender, args );
	}
}
