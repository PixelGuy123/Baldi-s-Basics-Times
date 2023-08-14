using MTM101BaldAPI.AssetManager;
using MTM101BaldAPI;
using System.IO;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BB_MOD.ExtraItems
{
	// ------ Item Summary ------
	// tells where characters are (only in yellow color, not exactly which one is it)


	public class ITM_GPS : Item
	{

		public override bool Use(PlayerManager pm)
		{
			if (pm.ec.Npcs.Count == 0)
			{
				Destroy(gameObject);
				return false;
			}

			ItemSoundHolder.CreatePositionalSoundHolder(pm.transform, ObjectCreatorHandlers.CreateSoundObject(ContentAssets.GetAsset<AudioClip>("gpsBeepNoise"), "Vfx_GPS_Beep", SoundType.Effect, new Color(153, 153, 153)), false, 95);

			ec = pm.ec;

			StartCoroutine(Timer(15f));

			return true;
		}

		private IEnumerator Timer(float t)
		{
			CheckForNPCs();
			float timer = t;
			while (timer > 0f)
			{
				timer -= Time.deltaTime * ec.EnvironmentTimeScale;
				if (timer % 7 == 1)
					CheckForNPCs();
				
				yield return null;
			}
			for (int i = 0; i < ec.map.arrowTargets.Count; i++)
			{
				var npc = ec.map.arrowTargets[i];
				if (foundNpcs.Contains(npc.transform))
				{
					ec.map.arrowTargets[i] = null; // Map will remove the arrow by default if only setting null
				}
			}

			Destroy(gameObject);

			yield break;
		}

		private void CheckForNPCs()
		{
			foreach (var npc in ec.Npcs)
			{
				if (!foundNpcs.Contains(npc.transform) && !ContentManager.instance.IsNpcStatic(npc.Character))
				{
					foundNpcs.Add(npc.transform);
					ec.map.AddArrow(npc.transform, new Color(255f, 255f, 0f));
				}
			}
		}

		EnvironmentController ec;

		readonly List<Transform> foundNpcs = new List<Transform>();
	}
}
