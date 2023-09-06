using UnityEngine;

namespace BB_MOD.ExtraItems
{
	// ------ Item Summary ------
	// FIXES EVERYTHING


	public class ITM_ScrewDriver : Item
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
						ItemSoundHolder.CreateSoundHolder(hit.transform, audUse, true, 50f, 60f);
						return true;
					}
				}
			}

			Destroy(gameObject);
			return false;
		}

		readonly Items item = ContentManager.instance.customItemEnums.GetItemByName("ScrewDriver");
		readonly SoundObject audUse = ContentAssets.GetAsset<SoundObject>("screwing");
	}
}
