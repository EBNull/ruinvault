

using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace ruinvault;
// FrameSwitcher.DropGhost
internal class PatchNoGhosts
{
    [HarmonyPrefix, HarmonyPatch(typeof(FrameSwitcher), "DropGhost", new[] { typeof(UnityEngine.Vector3), typeof(UnityEngine.Quaternion), typeof(bool) })]
    private static bool FS_DropGhost()
    {
        Tools.MaybeLogInfo(100, "skipping ghost drop");
        return false;
    }
}
