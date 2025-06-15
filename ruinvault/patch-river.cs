
using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MonoMod.Utils;
using SpaceGame;
using SpaceGame.Ship;
using UnityEngine;

namespace ruinvault;

[Feature(DefaultEnabled = false, IngameToggle =true, Description = "Rivers are stronger and your ship moves more quickly")]
internal class PatchShipSpeed : MonoBehaviour
{
	static RiverForceModuleSettings? rfm_orig = null;
	static SwimModuleSettings? sm_orig = null;

	static RiverForceModuleSettings? rfm_cur = null;
	static SwimModuleSettings? sm_cur = null;

	[HarmonyPrefix, HarmonyPatch(typeof(SpaceGame.Ship.ShipMotorModule.RiverForceModule), "FixedUpdate")]
	private static void RFM_FixedUpdate(ref SpaceGame.Ship.ShipMotorModule.RiverForceModule __instance)
	{
		var self = __instance;
		if (self.settings is null) { return; }
		rfm_orig ??= UnityEngine.Object.Instantiate(self.settings);
		rfm_cur = self.settings;

		self.settings.minRiverSpeed = 20f;
		self.settings.strength = 2f;
		Tools.MaybeLogInfo(3, $"RiverForce bankStrength={self.bankStrength} minRiverSpeed={self.settings.minRiverSpeed} strength={self.settings.strength}");
	}

	[HarmonyPostfix, HarmonyPatch(typeof(SpaceGame.Ship.ShipMotorModule.SwimModule), "FixedUpdate")]
	private static void SMM_FixedUpdate(ref SpaceGame.Ship.ShipMotorModule.SwimModule __instance)
	{
		var sm = __instance;
		if (sm.settings is null) { return; }
		sm_orig ??= UnityEngine.Object.Instantiate(sm.settings);
		sm_cur = sm.settings;
		Tools.MaybeLogInfo(3, $"SwimModule coasting={sm.coasting} sweepingBack={sm.sweepingBack} sailsPosition={sm.sailsPosition} sailsPositionChangeSpeed={sm.sailsPositionChangeSpeed}, delayBeforeRetract={sm.settings.delayBeforeRetract}");
		//__instance.sailsPositionChangeSpeed = 1f;
		//__instance.sailsPosition = 1f;
		//__instance.settings.retractDeadZone = 0f;
		//Math.Min(__instance.sailsPositionChangeSpeed, 10f);
		sm.settings.delayBeforeRetract = 0.01f;
		sm.coasting = false;
	}

	public void Dispose()
	{
		if (rfm_orig is not null && rfm_cur is not null)
		{
			rfm_cur.minRiverSpeed = rfm_orig.minRiverSpeed;
			rfm_cur.strength = rfm_orig.strength;
		}
		if (sm_orig is not null && sm_cur is not null)
		{
			sm_cur.delayBeforeRetract = sm_orig.delayBeforeRetract;
		}
	}
}

[Feature(DefaultEnabled = false, IngameToggle = true, Description = "Automatically reset your ship's travel when taking a wrong turn")]
internal class PatchRiverAutoReset
{
	[HarmonyPostfix, HarmonyPatch(typeof(SpaceGame.Pathfinding.ShipPather), "TrySetCachedResetPoint")]
	private static void ResetOnPathLost()
	{
		Tools.MaybeLogInfo(-1, "Autoresetting ship back on path after wrong turn");
		SpaceGame.SpaceGameMaster.Instance.ship.ResetToBeforeBranch();
	}
}
