using BB_MOD.ExtraComponents;
using System.Collections;
using UnityEngine;

namespace BB_MOD.ExtraItems
{
	// ------ Item Summary ------
	// Stops leapy effect


	public class ITM_HardHat : Item
	{

		public override bool Use(PlayerManager pm)
		{
			this.pm = pm;
			StartCoroutine(Timer());


			return true;
		}

		IEnumerator Timer()
		{
			var hud = PrefabInstance.SpawnPrefab<HardHatHud>(Vector3.zero, default, pm.ec, false);
			hud.SetupHud(Singleton<CoreGameManager>.Instance.GetHud(pm.playerNumber).Canvas());
			hud.Execute();
			float timer = time;
			pm.GetComponent<CustomPlayerAttributes>().ImmuneTo.Add(token);
			while (timer > 0f)
			{
				timer -= pm.ec.EnvironmentTimeScale * Time.deltaTime;
				yield return null;
			}
			hud.Despawn();
			pm.GetComponent<CustomPlayerAttributes>().ImmuneTo.Remove(token);
			Destroy(gameObject);

			yield break;
		}

		readonly CustomPlayerAttributes.ImmunityToken token = new CustomPlayerAttributes.ImmunityToken("stomped");

		const float time = 20f;
	}
}
