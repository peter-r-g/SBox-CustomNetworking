using CustomNetworking.Shared.Networkables;

namespace CustomNetworking.Shared.Entities;

public abstract class BaseComponent : BaseNetworkable, INetworkable<BaseComponent>
{
	public new event INetworkable<BaseComponent>.ChangedEventHandler? Changed;
	
	/// <summary>
	/// The <see cref="IEntity"/> that this component is a part of.
	/// </summary>
	public IEntity Entity { get; internal set; } = null!;
}
