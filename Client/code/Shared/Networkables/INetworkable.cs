using System.IO;
using CustomNetworking.Shared.Utility;

namespace CustomNetworking.Shared.Networkables;

public interface INetworkable
{
	bool HasChanged { get; }
	bool CanChangePartially { get; }
	
	void Deserialize( NetworkReader reader );
	void DeserializeChanges( NetworkReader reader );
	void Serialize( NetworkWriter writer );
	void SerializeChanges( NetworkWriter writer );
}
