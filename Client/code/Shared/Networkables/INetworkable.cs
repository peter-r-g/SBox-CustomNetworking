using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Networkables;

public interface INetworkable
{
	delegate void ChangedEventHandler( INetworkable networkable );
	event ChangedEventHandler? Changed;
	
	void Deserialize( NetworkReader reader );
	void DeserializeChanges( NetworkReader reader );
	void Serialize( NetworkWriter writer );
	void SerializeChanges( NetworkWriter writer );
}
