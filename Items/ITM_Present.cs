using System.Linq;

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




			ItemSoundHolder.CreateSoundHolder(pm.transform.position, ContentAssets.GetAsset<SoundObject>("presentUnboxing"), false, destructiveParent:transform);

			pm.itm.SetItem(WeightedItemObject.RandomSelection(ContentManager.instance.GlobalItems.Where(x => x.selection.itemType != itemEnum && x.selection.itemType != Items.None).ToArray()), pm.itm.selectedItem);

			return false;
		}
	}
}
