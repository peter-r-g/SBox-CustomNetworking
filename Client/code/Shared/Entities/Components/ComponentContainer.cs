using System.Diagnostics.CodeAnalysis;
using CustomNetworking.Shared.Networkables;
using CustomNetworking.Shared.Networkables.Builtin;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Entities;

public class ComponentContainer : BaseNetworkable, INetworkable<ComponentContainer>
{
	public new event INetworkable<ComponentContainer>.ChangedEventHandler? Changed;
	public IEntity Entity { get; internal set; }
	
	private NetworkedHashSet<BaseComponent> Components { get; }

	internal ComponentContainer( IEntity entity )
	{
		Entity = entity;
		Components = new NetworkedHashSet<BaseComponent>();
		Components.Changed += OnComponentsChanged;
	}

	~ComponentContainer()
	{
		Components.Changed -= OnComponentsChanged;
	}

	public void Add<T>() where T : BaseComponent
	{
		if ( Has<T>() )
		{
			Logging.Error( $"{Entity} already has the {typeof(T).Name} component" );
			return;
		}

		var component = TypeHelper.Create<T>();
		component.Entity = Entity;
		Components.Add( component );
	}

	public void Remove<T>() where T : BaseComponent
	{
		if ( !Has<T>() )
		{
			Logging.Error( $"{Entity} does not have the {typeof(T).Name} component" );
			return;
		}

		Components.Remove( Get<T>()! );
	}

	public T? Get<T>() where T : BaseComponent
	{
		var tType = typeof(T);
		foreach ( var component in Components )
		{
			if ( tType.IsEquivalentTo( component.GetType() ) )
				return component as T;
		}

		return default;
	}

	public BaseComponent? Get( string componentName )
	{
		foreach ( var component in Components )
		{
			if ( component.GetType().Name == componentName )
				return component;
		}

		return default;
	}

	public bool TryGet<T>( [NotNullWhen( true )] out T? component ) where T : BaseComponent
	{
		component = Get<T>();
		return component is not null;
	}

	public bool Has<T>() where T : BaseComponent
	{
		return Get<T>() is not null;
	}
	
	private void OnComponentsChanged( NetworkedHashSet<BaseComponent> _, NetworkedHashSet<BaseComponent> newvalue )
	{
		TriggerNetworkingChange( nameof(Components) );
	}
}
