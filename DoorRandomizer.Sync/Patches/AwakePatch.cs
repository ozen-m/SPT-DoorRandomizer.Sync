using System.Collections.Generic;
using System.Reflection;
using Comfort.Common;
using DoorRandomizer.Sync.Models;
using EFT;
using EFT.Interactive;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace DoorRandomizer.Sync.Patches;

public class AwakePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        var doorRandomizerComponentType = AccessTools.TypeByName("DrakiaXYZ.DoorRandomizer.DoorRandomizerComponent");
        return AccessTools.Method(doorRandomizerComponentType, "Awake");
    }

    [PatchPrefix]
    protected static bool Prefix(ref Dictionary<Door, EDoorState> __state)
    {
        if (!FikaBackendUtils.IsServer) return false;

        // Cache initial door states
        __state = [];
        var interactiveObjects = DoorRandomizerSync.InteractiveObjectsField(Singleton<GameWorld>.Instance.World_0);
        foreach (var interactiveObject in interactiveObjects)
        {
            if (interactiveObject is not Door door) continue;

            __state.TryAdd(door, door.InitialDoorState);
        }

        return true;
    }

    [PatchPostfix]
    protected static void Postfix(ref Dictionary<Door, EDoorState> __state)
    {
        if (!FikaBackendUtils.IsServer) return;

        // Compare new states with cached initial states
        // "OnEnable" changes the door's initial state so we have to cache
        var changedDoors = new List<int>();
        foreach (var (door, initialState) in __state)
        {
            if (door.DoorState == initialState) continue;

            changedDoors.Add(door.NetId);
        }

        DoorRandomizerSync.LogSource.LogInfo($"Sending packet to sync {changedDoors.Count} doors");
        var syncPacket = new DoorsSyncPacket([.. changedDoors]);
        Singleton<FikaServer>.Instance.SendData(ref syncPacket, DeliveryMethod.ReliableUnordered);
    }
}
