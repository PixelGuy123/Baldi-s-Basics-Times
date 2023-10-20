using System.Collections;
using UnityEngine;

namespace BB_MOD.ExtraItems
{
	// ------ Item Summary ------
	// Traps, and nothing can move for some seconds


	public class ITM_Banana : Item
	{
		void SetupVisual()
		{
			ContentUtilities.AddCollisionToSprite(gameObject, new Vector3(1f, 5f, 1f), Vector3.zero).isTrigger = true; // Creates collider and set it as trigger

			renderer = ContentUtilities.AddVisualToSprite(transform, ContentAssets.GetAsset<Sprite>("banana"), ContentUtilities.DefaultBillBoardMaterial);
			aud_catch = ContentAssets.GetAsset<SoundObject>("bananaSlip");
		}

		public override bool Use(PlayerManager pm)
		{
			SetupVisual();

			this.pm = pm;
			owner = pm.gameObject;
			transform.position = pm.transform.position + Vector3.down * 4.4f;

			pm.RuleBreak("littering", 1f);

			return true;
		}

		private void OnTriggerEnter(Collider other)
		{
			if (hasCatched || other.gameObject == owner) return;

			var component = other.GetComponent<ActivityModifier>();
			if (other.tag == "Player")
			{
				ItemSoundHolder.CreateSoundHolder(transform, aud_catch, false, 70, 80);
				hasCatched = true;
				if (component && !component.moveMods.Contains(mod))
				{
					mod.movementAddend = Singleton<CoreGameManager>.Instance.GetCamera(other.GetComponent<PlayerManager>().playerNumber).transform.forward * speed;
					component.moveMods.Add(mod);
				}
				StartCoroutine(Timer(component));
			}

			if (other.tag == "NPC" && other.isTrigger)
			{
				ItemSoundHolder.CreateSoundHolder(other.transform, aud_catch, true, 70, 80);
				hasCatched = true;

				
				if (component && !component.moveMods.Contains(mod))
				{
					mod.movementAddend = other.transform.forward * speed;
					component.moveMods.Add(mod);
				}
				StartCoroutine(Timer(component));

			}
		}

		private IEnumerator Timer(ActivityModifier target)
		{
			renderer.enabled = false;
			float time = Random.Range(5f, 6f);
			while (time > 0f)
			{
				time -= pm.ec.EnvironmentTimeScale * Time.deltaTime;
				yield return null;
			}

			if (target)
				target.moveMods.Remove(mod);

			Destroy(gameObject);
			

			yield break;
		}

		private void OnTriggerExit(Collider other)
		{
			if (owner == other.gameObject) // Basically a simple failSafe, so the player can get out of the way
			{
				owner = null;
			}
		}

		SoundObject aud_catch;

		SpriteRenderer renderer;

		readonly MovementModifier mod = new MovementModifier(Vector3.zero, 0f);

		bool hasCatched = false;

		GameObject owner;

		[SerializeField]
		const float speed = 50f;
	}
}
