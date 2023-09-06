namespace BB_MOD.ExtraItems
{
	// ------ Item Summary ------
	// Ringing, calls baldi attention!


	public class ITM_Bell : Item
	{

		public override bool Use(PlayerManager pm)
		{
			ItemSoundHolder.CreateSoundHolder(pm.transform.position, ContentAssets.GetAsset<SoundObject>("bellNoise"), true, maxDistance:95, destructiveParent: transform);

			pm.ec.MakeNoise(pm.transform.position, 112); // alarm clock noise val

			return true;
		}
	}
}
