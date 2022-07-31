using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Networkables;

/// <summary>
/// Contract to define something that can be networked.
/// </summary>
public interface INetworkable
{
	/// <summary>
	/// The event handler for <see cref="INetworkable"/>.<see cref="INetworkable.Changed"/>.
	/// </summary>
	delegate void ChangedEventHandler( INetworkable networkable );
	/// <summary>
	/// Called when something in the <see cref="INetworkable"/> has changed.
	/// </summary>
	event ChangedEventHandler? Changed;
	
	/// <summary>
	/// Deserializes all information relating to the <see cref="INetworkable"/>.
	/// </summary>
	/// <param name="reader">The reader to read from.</param>
	void Deserialize( NetworkReader reader );
	/// <summary>
	/// Deserializes all changes relating to the <see cref="INetworkable"/>.
	/// </summary>
	/// <param name="reader">The reader to read from.</param>
	void DeserializeChanges( NetworkReader reader );
	/// <summary>
	/// Serializes all information relating to the <see cref="INetworkable"/>.
	/// </summary>
	/// <param name="writer">The writer to write to.</param>
	void Serialize( NetworkWriter writer );
	/// <summary>
	/// Serializes all changes relating to the <see cref="INetworkable"/>.
	/// </summary>
	/// <param name="writer">The writer to write to.</param>
	void SerializeChanges( NetworkWriter writer );
}
