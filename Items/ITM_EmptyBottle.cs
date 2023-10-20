using UnityEngine;

namespace BB_MOD.ExtraItems
{
	// ------ Item Summary ------
	// EmptyBottle


	public class ITM_EmptyBottle : Item
	{

		public override bool Use(PlayerManager pm)
		{

			if (Physics.Raycast(pm.transform.position, Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber).transform.forward, out RaycastHit hit, pm.pc.reach, ContentUtilities.defaultClickMask))
			{ 
				if (hit.transform.GetComponent<WaterFountain>())
				{
					pm.itm.SetItem(ContentManager.instance.GetItemByEnum(ContentManager.instance.customItemEnums.GetItemByName("Waterbottle")), pm.itm.selectedItem);
					ItemSoundHolder.CreateSoundHolder(hit.transform, aud_slurp, true, 20, 50);
				}
			}

			Destroy(gameObject);
			return false;
		}

		readonly SoundObject aud_slurp = ContentAssets.GetAsset<SoundObject>("slurp");
	}
}
