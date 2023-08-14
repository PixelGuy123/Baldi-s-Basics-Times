using MTM101BaldAPI.AssetManager;
using MTM101BaldAPI;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BB_MOD.ExtraItems
{
	// ------ Item Summary ------
	// Gives you a random item upon usage
	// Pro: gives you a random item (including op and non-ops)
	// Cons: Rare to find


	public class ITM_Present : Item
	{

		public override bool Use(PlayerManager pm)
		{
			var itemEnum = ContentManager.instance.customItemEnums.GetItemByName("present"); // Get the Items enum of the item by the name, very useful!




			ItemSoundHolder.CreatePositionalSoundHolder(pm.transform, ObjectCreatorHandlers.CreateSoundObject(ContentAssets.GetAsset<AudioClip>("presentUnboxing"), "Vfx_PRS_Unbox", SoundType.Effect, new Color(77, 77, 255)), false, destructiveParent:transform);

			pm.itm.SetItem(WeightedItemObject.RandomSelection(ContentManager.instance.GlobalItems.Where(x => x.selection.itemType != itemEnum && x.selection.itemType != Items.None).ToArray()), pm.itm.selectedItem);

			return false;
		}
	}
}
