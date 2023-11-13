namespace BB_MOD.ExtraItems
{
	// ------ Item Summary ------
	// Refills stamina


	public class ITM_WaterBottle : Item
	{

		public override bool Use(PlayerManager pm)
		{
			pm.plm.stamina = pm.plm.staminaMax + extraStamina;
			pm.itm.SetItem(ContentManager.instance.GetItemByEnum(ContentManager.instance.customItemEnums.GetItemByName("Emptybottle")), pm.itm.selectedItem); // Just replaces with empty bottle
			pm.RuleBreak("Drinking", 2f);
			ItemSoundHolder.CreateSoundHolder(pm.transform, aud_drink, false, 40, 60);

			Destroy(gameObject);

			return false;
		}

		readonly SoundObject aud_drink = ContentAssets.GetAsset<SoundObject>("pt_drink");

		float extraStamina = 0f;
	}
}
