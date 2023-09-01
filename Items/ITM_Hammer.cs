using BB_MOD.ExtraComponents;
using UnityEngine;

namespace BB_MOD.ExtraItems
{
	// ------ Item Summary ------
	// Break window (for now, no idea for this)


	public class ITM_Hammer : Item
	{

		public override bool Use(PlayerManager pm)
		{

			if (!Physics.Raycast(pm.transform.position, Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber).transform.forward, out RaycastHit hit, pm.pc.reach, 2097152, QueryTriggerInteraction.Collide)) // 2097152 >> some layer for windows I guess
			{
				Destroy(gameObject);
				return false;
			}
			Destroy(gameObject);
			if (hit.transform.CompareTag("Window"))
			{
				hit.transform.GetComponent<Window>().Break(true);
				if (hit.transform.GetComponent<WindowExtraFields>().IsBroken)
				{
					pm.RuleBreak("breakproperty", 1f);
					return true;
				}
			}
			return false;
		}
	}
}
