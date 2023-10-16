using System.Linq;

namespace BB_MOD.ExtraItems
{
	// ------ Item Summary ------
	// Golden quarter gives 3 quarters if it can do it


	public class ITM_GQuarter : Item
	{

		public override bool Use(PlayerManager pm)
		{
			int availableSlots = 0;
			for (int i = 0; i <= pm.itm.maxItem; i++)
			{
				if (pm.itm.items[i].itemType == Items.None)
					availableSlots++;
			}


			if (availableSlots >= neededSlots)
			{
				pm.itm.SetItem(myItem, pm.itm.selectedItem);
				for (int i = 0; i < neededSlots; i++)
				{
					pm.itm.AddItem(myItem);
				}
			}

			return false;
		}

		const int neededSlots = 2;

		readonly ItemObject myItem = ContentManager.instance.GlobalItems.First(x => x.selection.itemType == Items.Quarter).selection;

	}
}
