using BB_MOD.ExtraComponents;

namespace BB_MOD.ExtraItems
{
	// ------ Item Summary ------
	// Stops stunly's effect


	public class ITM_HeadachePill : Item
	{

		public override bool Use(PlayerManager pm)
		{
			var att = pm.GetComponent<CustomPlayerAttributes>();
			if (att.TryGetAttribute("stunly_stun", out _)) 
			{
				att.ImmuneTo.Add(token);
				ItemSoundHolder.CreateSoundHolder(pm.transform, aud_swallow, false, 40, 60);
				Destroy(gameObject);
				return true;
			}

			Destroy(gameObject);
			return false;
		}

		readonly CustomPlayerAttributes.ImmunityToken token = new CustomPlayerAttributes.ImmunityToken("stun");

		readonly SoundObject aud_swallow = ContentAssets.GetAsset<SoundObject>("hdp_swallow");
	}
}
