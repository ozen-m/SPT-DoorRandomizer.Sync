using Fika.Core.Networking.LiteNetLib.Utils;
using JetBrains.Annotations;

namespace DoorRandomizer.Sync.Models;

public struct DoorsSyncPacket(int[] netIds) : INetSerializable
{
    [UsedImplicitly]
    public int[] NetIds = netIds;

    public void Deserialize(NetDataReader reader)
    {
        NetIds = reader.GetIntArray();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutArray(NetIds);
    }
}
