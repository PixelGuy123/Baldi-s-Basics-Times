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
			var itemEnum = ContentManager.instance.customEnums.GetItemByName("present"); // Get the Items enum of the item by the name, very useful!




			ItemSoundHolder.CreateSoundHolder(pm.transform, ObjectCreatorHandlers.CreateSoundObject(AssetManager.AudioClipFromFile(Path.Combine(ContentManager.modPath, "Audio", "item", "prs_unbox.wav")), "Vfx_PRS_Unbox", SoundType.Voice, new Color(77, 77, 255))); // Unused caption

			pm.itm.SetItem(WeightedItemObject.RandomSelection(ContentManager.instance.GlobalItems.Where(x => x.selection.itemType != itemEnum && x.selection.itemType != Items.None).ToArray()), pm.itm.selectedItem);

			return false;
		}
	}
}
