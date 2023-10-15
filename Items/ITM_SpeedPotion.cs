using System.Collections;
using System.Transactions;
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

			var token = new EnvironmentExtraVariables.FOVToken(0f, 1, false);
			ItemSoundHolder holder = ItemSoundHolder.CreateSoundHolder(pm.transform, aud_drink, false, 40, 60);

			StartCoroutine(EnvironmentExtraVariables.SmoothFOVSlide(8f, token, -20f));

			while (holder.IsPlaying) { yield return null; }

			pm.Am.moveMods.Add(moveMod);

			token.CanSum = true;
			StartCoroutine(EnvironmentExtraVariables.SmoothFOVSlide(6f, token, 30f));

			ItemSoundHolder.CreateSoundHolder(pm.transform, aud_speed, false, 40, 60);
			float timer = Random.Range(minTime, maxTime);

			while (timer > 0f)
			{
				timer -= pm.ec.EnvironmentTimeScale * Time.deltaTime;
				yield return null;
			}

			pm.Am.moveMods.Remove(moveMod);

			token.CanSum = false;
			StartCoroutine(EnvironmentExtraVariables.SmoothFOVSlide(6f, token));

			

			while (!token.DoneSlide) { yield return null; }

			EnvironmentExtraVariables.FovModifiers.Remove(token);

			UsedPotions--;
			

			Destroy(gameObject);

			yield break;

		}

		public static void ResetCount() => UsedPotions = 0;

		readonly SoundObject aud_drink = ContentAssets.GetAsset<SoundObject>("pt_drink");
		readonly SoundObject aud_speed = ContentAssets.GetAsset<SoundObject>("pt_speed");

		readonly MovementModifier moveMod = new MovementModifier(Vector3.zero, 2f);


		const float minTime = 10f, maxTime = 15f;

		const int potionLimit = 2;

		private static int UsedPotions = 0;
	}
}
