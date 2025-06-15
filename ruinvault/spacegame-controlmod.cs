
using System;
using SpaceGame.Pathfinding;
using Translation;
using UnityEngine;

[Feature(DefaultEnabled = true, IngameToggle = true, Description = "Enable autopilot sooner with fewer unsailed path restrictions")]
class SpaceGameControlMod : MonoBehaviour
{
	ShipPatherSettings? orig = null;
	void Update()
	{
		var sm = SpaceGame.Pathfinding.ShipPatherManager.Instance;
		if (sm is null) { return; }
		if (sm.settings is null) { return; }

		orig ??= UnityEngine.Object.Instantiate(sm.settings, sm.transform);

		sm.settings.timeOutsideRiverToBreakPath = 3f;
		sm.settings.skipMaxTimeToReachFromHere = 0f;
		sm.settings.skipMinUntravelledRiverTime = 0f;
	}

	public void Dispose()
	{
		var sm = SpaceGame.Pathfinding.ShipPatherManager.Instance;
		if (sm is null) { return; }
		if (sm.settings is null) { return; }
		if (orig is null) { return; }

		sm.settings.timeOutsideRiverToBreakPath = orig.timeOutsideRiverToBreakPath;
		sm.settings.skipMaxTimeToReachFromHere = orig.skipMaxTimeToReachFromHere;
		sm.settings.skipMinUntravelledRiverTime = orig.skipMinUntravelledRiverTime;
	}

}

[Feature(DefaultEnabled = true, IngameToggle = false, Description = "Spawn ruins more frequently (BROKEN TOGGLE)")]
class SpaceGameMakeMoreFrequentRuins : MonoBehaviour
{
	void Update()
	{
		var srm = SpaceGame.SpaceRuinManager.Instance;
		if (srm != null && srm._settings != null)
		{

			srm._settings.minRuinCreationDistanceRangeOnStart = 100f;
			srm._settings.ruinCreationDistanceRange = new UnityEngine.Vector2(100f, 100f);
		}
	}
}

[Feature(DefaultEnabled = true, IngameToggle = false, Description = "Add positioning widget to space scene (BROKEN TOGGLE)")]
class SpaceGameEnableDebugComponent : MonoBehaviour
{
	void Update()
	{
		var sgi = SpaceGame.SpaceGameMaster.Instance;
		if (sgi != null && sgi.gameObject != null)
		{
			sgi.gameObject.GetOrAddComponent<SpaceGame.SpaceTestPositioner>();
		}
	}
}