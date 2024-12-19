
using UnityEngine;

[Feature(DefaultEnabled = true)]
class SpaceGameControlMod : MonoBehaviour
{
	private void Update()
	{
		var sm = SpaceGame.Pathfinding.ShipPatherManager.Instance;
		if (sm != null && sm.settings != null)
		{
			sm.settings.timeOutsideRiverToBreakPath = 3f;
			sm.settings.skipMaxTimeToReachFromHere = 0f;
			sm.settings.skipMinUntravelledRiverTime = 0f;
		}
	}
}

[Feature(DefaultEnabled = true)]
class SpaceGameMakeMoreFrequentRuins : MonoBehaviour
{
	private void Update()
	{
		var srm = SpaceGame.SpaceRuinManager.Instance;
		if (srm != null && srm._settings != null)
		{

			srm._settings.minRuinCreationDistanceRangeOnStart = 100f;
			srm._settings.ruinCreationDistanceRange = new UnityEngine.Vector2(100f, 100f);
		}
	}
}

[Feature(DefaultEnabled = true)]
class SpaceGameEnableDebugComponent : MonoBehaviour
{
	private void Update()
	{
		var sgi = SpaceGame.SpaceGameMaster.Instance;
		if (sgi != null && sgi.gameObject != null)
		{
			sgi.gameObject.GetOrAddComponent<SpaceGame.SpaceTestPositioner>();
		}
	}
}