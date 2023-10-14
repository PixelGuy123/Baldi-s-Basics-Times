using System.Collections;
using UnityEngine;

namespace BB_MOD.ExtraItems
{
	// ------ Item Summary ------
	// Me playing roblox be like:
	// (makes you faster duuuuh)


	public class ITM_SpeedPotion : Item
	{

		public override bool Use(PlayerManager pm)
		{
			if (UsedPotions >= potionLimit)
			{
				Destroy(gameObject);
				return false;
			}

			this.pm = pm;
			StartCoroutine(DrinkingPhase());

			return true;
		}

		private IEnumerator DrinkingPhase()
		{
			UsedPotions++;
			ItemSoundHolder holder = ItemSoundHolder.CreateSoundHolder(pm.transform, aud_drink, false, 40, 60);

			StartCoroutine(EnvironmentExtraVariables.SmoothFOVSlide(8f, -20f));

			while (holder.IsPlaying) { yield return null; }

			pm.Am.moveMods.Add(moveMod);
			

			EnvironmentExtraVariables.SetADefaultFOV(speedFOV + fovMultiplier * UsedPotions);
			StartCoroutine(EnvironmentExtraVariables.SmoothFOVSlide(6f, 0f));
			ItemSoundHolder.CreateSoundHolder(pm.transform, aud_speed, false, 40, 60);
			float timer = Random.Range(minTime, maxTime);

			while (timer > 0f)
			{
				timer -= pm.ec.EnvironmentTimeScale * Time.deltaTime;
				yield return null;
			}

			pm.Am.moveMods.Remove(moveMod);

			if (--UsedPotions == 0)
				EnvironmentExtraVariables.SetADefaultFOV(0f);

			StartCoroutine(EnvironmentExtraVariables.SmoothFOVSlide(6f));

			

			while (!EnvironmentExtraVariables.PlayerAdditionalFOV.Compare(EnvironmentExtraVariables.FixedFOV)) { yield return null; }
			

			Destroy(gameObject);

			yield break;

		}

		public static void ResetCount() => UsedPotions = 0;

		readonly SoundObject aud_drink = ContentAssets.GetAsset<SoundObject>("pt_drink");
		readonly SoundObject aud_speed = ContentAssets.GetAsset<SoundObject>("pt_speed");

		readonly MovementModifier moveMod = new MovementModifier(Vector3.zero, 2f);


		const float minTime = 10f, maxTime = 15f, speedFOV = 50f, fovMultiplier = 10f;

		const int potionLimit = 2;

		private static int UsedPotions = 0;
	}
}
