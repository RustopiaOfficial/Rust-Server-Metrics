﻿using HarmonyLib;
using Network;

namespace RustServerMetrics.HarmonyPatches
{
    [HarmonyPatch(typeof(NetWrite), nameof(NetWrite.Send))]
    public class NetWrite_Send_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(NetWrite __instance, SendInfo info)
        {
            SingletonComponent<MetricsLogger>.Instance?.OnNetWriteSend(__instance, info);
        }
    }
}
