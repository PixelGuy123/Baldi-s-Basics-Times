using MTM101BaldAPI;
using UnityEngine;

namespace BB_MOD.ExtraItems
{
	// ------ Item Summary ------
	// Ringing, calls baldi attention!


	public class ITM_Bell : Item
	{

		public override bool Use(PlayerManager pm)
		{
			ItemSoundHolder.CreateSoundHolder(pm.transform.position, ObjectCreatorHandlers.CreateSoundObject(ContentAssets.GetAsset<AudioClip>("bellNoise"), "Vfx_BEL_Ring", SoundType.Voice, new Color(179, 179, 0)), true, maxDistance:95, destructiveParent: transform);

			pm.ec.MakeNoise(pm.transform.position, 112); // alarm clock noise val

			return true;
		}
	}
}
