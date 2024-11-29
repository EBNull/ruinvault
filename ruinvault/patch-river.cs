
using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MonoMod.Utils;
using SpaceGame;

namespace ruinvault;

internal class PatchShipSpeed
{
	[HarmonyPrefix, HarmonyPatch(typeof(SpaceGame.Ship.ShipMotorModule.RiverForceModule), "FixedUpdate")]
	private static void RFM_FixedUpdate(ref SpaceGame.Ship.ShipMotorModule.RiverForceModule __instance)
	{
		var self = __instance;
		self.settings.minRiverSpeed = 30f;
		self.settings.strength = 3f;
		Tools.MaybeLogInfo(3, $"RiverForce bankStrength={self.bankStrength} minRiverSpeed={self.settings.minRiverSpeed} strength={self.settings.strength}");
	}

	[HarmonyPostfix, HarmonyPatch(typeof(SpaceGame.Ship.ShipMotorModule.SwimModule), "FixedUpdate")]
	private static void SMM_FixedUpdate(ref SpaceGame.Ship.ShipMotorModule.SwimModule __instance)
	{
		var sm = __instance;
		Tools.MaybeLogInfo(3, $"SwimModule coasting={sm.coasting} sweepingBack={sm.sweepingBack} sailsPosition={sm.sailsPosition} sailsPositionChangeSpeed={sm.sailsPositionChangeSpeed}, delayBeforeRetract={sm.settings.delayBeforeRetract}");
		//__instance.sailsPositionChangeSpeed = 1f;
		//__instance.sailsPosition = 1f;
		//__instance.settings.retractDeadZone = 0f;
		//Math.Min(__instance.sailsPositionChangeSpeed, 10f);
		sm.settings.delayBeforeRetract = 0.01f;
		sm.coasting = false;
	}
}

internal class PatchRiverAutoReset
{
	[HarmonyPostfix, HarmonyPatch(typeof(SpaceGame.Pathfinding.ShipPather), "TrySetCachedResetPoint")]
	private static void ResetOnPathLost()
	{
		Tools.MaybeLogInfo(-1, "Autoresetting ship back on path after wrong turn");
		SpaceGame.SpaceGameMaster.Instance.ship.ResetToBeforeBranch();
	}
}
