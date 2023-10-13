using BB_MOD.ExtraComponents;
using UnityEngine;

namespace BB_MOD.ExtraItems
{
	// ------ Item Summary ------
	// Break window (for now, no idea for this)


	public class ITM_LockPick: Item
	{

		public override bool Use(PlayerManager pm)
		{

			if (Physics.Raycast(pm.transform.position, Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber).transform.forward, out var hit, pm.pc.reach, pm.pc.ClickLayers))
			{
				foreach (IItemAcceptor itemAcceptor in hit.transform.GetComponents<IItemAcceptor>())
				{
					if (itemAcceptor != null && itemAcceptor.ItemFits(item))
					{
						itemAcceptor.InsertItem(pm, pm.ec);
						Destroy(gameObject);
						return !hit.transform.GetComponent<GreenLocker>();
					}
				}
			}

			Destroy(gameObject);
			return false;
		}

		readonly Items item = ContentManager.instance.customItemEnums.GetItemByName("Lockpick");
	}
}
