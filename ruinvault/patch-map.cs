

using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace ruinvault;

[Feature(DefaultEnabled = false, Description = "BROKEN. Enable sailing to anywhere (but if the story blocks you, the game crashes)")]
internal class PatchMapLocationPlottable
{
    [HarmonyPostfix, HarmonyPatch(typeof(Game), nameof(Game.MapStateForLocation))]
    private static void Game_MapStateForLocation(StoryLocation location, ref Game.StoryLocationMapState __result)
    {
        // This makes it possible to attempt a visit, but the story crashes because _validate does not return a knot
        if (__result == Game.StoryLocationMapState.VisibleLocation)
        {
            __result = Game.StoryLocationMapState.VisitableLocation;
        }
    }
}