using BB_MOD.ExtraComponents;
using Patches.Main;
using System.Collections;
using UnityEngine;

namespace BB_MOD.ExtraItems
{
	// ------ Item Summary ------
	// basketball = stunly effect


	public class ITM_Basketball : Item
	{
		void SetupVisual()
		{
			ContentUtilities.AddCollisionToSprite(gameObject, new Vector3(2f, 5f, 2f), Vector3.zero).isTrigger = true; // Creates collider and set it as trigger

			ContentUtilities.AddVisualToSprite(transform, ContentAssets.GetAsset<Sprite>("basketball"), ContentUtilities.DefaultBillBoardMaterial);

		}

		private void Update()
		{
			if (!hasCatched)
			{
				lifeSpan -= pm.ec.EnvironmentTimeScale * Time.deltaTime;
				if (lifeSpan < 0f)
					Destroy(gameObject);

				transform.position += transform.forward * pm.ec.EnvironmentTimeScale * Time.deltaTime * speed;
			}
		}

		public override bool Use(PlayerManager pm)
		{
			SetupVisual();

			this.pm = pm;

			ItemSoundHolder.CreateSoundHolder(pm.transform, aud_throw, false, 70, 71);

			transform.position = pm.transform.position;
			transform.rotation = Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber).transform.rotation; // Must always be by the camera, so backward shooting works aswell

			return true;
		}

		private void OnTriggerEnter(Collider other)
		{
			if (hasCatched) return;

			if (other.tag == "NPC" && other.isTrigger)
			{
				hasCatched = true;
				var component = other.GetComponent<ActivityModifier>();

				transform.position = Vector3.zero;

				pm.RuleBreak("Bullying", 2f);
				ItemSoundHolder.CreateSoundHolder(other.transform, aud_hit, true, 70, 71);

				if (component && !component.moveMods.Contains(mod))
				{
					component.moveMods.Add(mod);
					StartCoroutine(Timer(component, other.GetComponent<Looker>()));
				}
				

			}
		}

		private IEnumerator Timer(ActivityModifier target, Looker looker)
		{
			LookerDistancingPatch.LookerToken token = new LookerDistancingPatch.LookerToken(0f, looker);

			if (looker != null)
			{
				LookerDistancingPatch.lookerModifiers.Add(token);
			}
			var stunningStar = PrefabInstance.SpawnPrefab<StunningStars>(target.transform.position, default, pm.ec, false);
			stunningStar.SetupTarget(target.transform);
			stunningStar.Execute();

			float time = Random.Range(6f, 12f);
			while (time > 0f)
			{
				time -= pm.ec.EnvironmentTimeScale * Time.deltaTime;
				yield return null;
			}
			LookerDistancingPatch.lookerModifiers.Remove(token);
			target.moveMods.Remove(mod);

			stunningStar.Despawn();

			Destroy(gameObject);
			

			yield break;
		}

		readonly SoundObject aud_throw = ContentAssets.GetAsset<SoundObject>("bb_throw"), aud_hit = ContentAssets.GetAsset<SoundObject>("bb_hit");

		readonly MovementModifier mod = new MovementModifier(Vector3.zero, 0.05f);

		bool hasCatched = false;

		float lifeSpan = 200f;

		[SerializeField]
		const float speed = 45f;
	}
}
