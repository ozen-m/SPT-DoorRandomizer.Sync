using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using Comfort.Common;
using DoorRandomizer.Sync.Models;
using DoorRandomizer.Sync.Patches;
using EFT;
using EFT.Interactive;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using HarmonyLib;

namespace DoorRandomizer.Sync;

[BepInPlugin("com.ozen.doorrandomizer.sync", "DoorRandomizer.Sync", "1.0.1")]
[BepInDependency("com.fika.core", "2.2.3")]
[BepInDependency("xyz.drakia.doorrandomizer", "1.7.0")]
public class DoorRandomizerSync : BaseUnityPlugin
{
    internal static ManualLogSource LogSource;

    internal static readonly AccessTools.FieldRef<World, WorldInteractiveObject[]> InteractiveObjectsField =
        AccessTools.FieldRefAccess<World, WorldInteractiveObject[]>("worldInteractiveObject_0");

    protected void Awake()
    {
        LogSource = Logger;

        FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnFikaNetworkManagerCreatedEvent);

        new AwakePatch().Enable();
    }

    private static void OnFikaNetworkManagerCreatedEvent(FikaNetworkManagerCreatedEvent createNetworkManager)
    {
        switch (createNetworkManager.Manager)
        {
            case FikaClient client:
                client.RegisterPacket<DoorsSyncPacket>(HandleDoorsSyncPacket);
                return;
        }
    }

    private static void HandleDoorsSyncPacket(DoorsSyncPacket packet)
    {
        LogSource.LogInfo($"Received packet to sync {packet.NetIds.Length} doors");

        var changedDoorIds = new HashSet<int>(packet.NetIds);
        foreach (var interactiveObject in InteractiveObjectsField(Singleton<GameWorld>.Instance.World_0))
        {
            if (!changedDoorIds.Contains(interactiveObject.NetId)) continue;

            // Simple door state switch so no need to receive the new door state via packet
            interactiveObject.DoorState =
                interactiveObject.InitialDoorState == EDoorState.Open
                    ? EDoorState.Shut
                    : EDoorState.Open;

            // Trigger "OnEnable" to make sure the properties are set correctly for interaction
            interactiveObject.OnEnable();
        }
    }
}
