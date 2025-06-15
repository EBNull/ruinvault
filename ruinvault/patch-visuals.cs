

using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Microsoft.Win32;

namespace ruinvault;
// FrameSwitcher.DropGhost
[Feature(DefaultEnabled = false)]
internal class PatchNoGhosts
{
    [HarmonyPrefix, HarmonyPatch(typeof(FrameSwitcher), "DropGhost", new[] { typeof(UnityEngine.Vector3), typeof(UnityEngine.Quaternion), typeof(bool) })]
    private static bool FS_DropGhost()
    {
        Tools.MaybeLogInfo(100, "skipping ghost drop");
        return false;
    }
}

// These quality and resolution hacks apply too late for the current session, but they apply
// next time the game opens.

[Feature(DefaultEnabled = true)]
internal class PatchForceHighUnityQuality
{
    public PatchForceHighUnityQuality()
    {
        var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Inkle Ltd\\Heaven's Vault", true);
        key.SetValue("UnityGraphicsQuality_h1669003810", 100, RegistryValueKind.DWord);
    }
}

[Feature(DefaultEnabled = false)]
internal class PatchForceWindowedResolution1440p
{
    public PatchForceWindowedResolution1440p()
    {
        var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Inkle Ltd\\Heaven's Vault", true);
        key.SetValue("Screenmanager Is Fullscreen mode_h3981298716", 0, RegistryValueKind.DWord);
        key.SetValue("Screenmanager Resolution Height_h2627697771", 1440, RegistryValueKind.DWord);
        key.SetValue("Screenmanager Resolution Width_h182942802", 2560, RegistryValueKind.DWord);
    }
}