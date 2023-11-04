using System.Collections;
using UnityEngine;
using static Patches.Main.StaminaRisingPatch;

namespace BB_MOD.ExtraItems
{
	// ------ Item Summary ------
	// stamina good rising stuff


	public class ITM_BSED : Item
	{
		

		public override bool Use(PlayerManager pm)
		{
			this.pm = pm;
			pm.RuleBreak("Drinking", 2f);
			StartCoroutine(Timer());
			
			return true;
		}

		IEnumerator Timer()
		{
			ItemSoundHolder.CreateSoundHolder(pm.transform, aud_drink, false, 40, 60);

			staminaModifiers.Add(riseToken);
			staminaModifiers.Add(dropToken);
			float timer = maxTime;
			while (timer > 0f)
			{
				timer -= pm.ec.EnvironmentTimeScale * Time.deltaTime;
				yield return null;
			}

			staminaModifiers.Remove(riseToken);
			staminaModifiers.Remove(dropToken);

			Destroy(gameObject);
			yield break;
		}

		readonly SoundObject aud_drink = ContentAssets.GetAsset<SoundObject>("pt_drink");
		readonly StaminaToken riseToken = new StaminaToken(StaminaToken.ModifierType.Rise, 2f);
		readonly StaminaToken dropToken = new StaminaToken(StaminaToken.ModifierType.Drop, 0.5f);
		const float maxTime = 15f;
	}
}
