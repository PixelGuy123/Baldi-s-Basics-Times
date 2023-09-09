using System.Collections;
using UnityEngine;

namespace BB_MOD.ExtraItems
{
	// ------ Item Summary ------
	// Traps, and nothing can move for some seconds


	public class ITM_Trap : Item
	{
		void SetupVisual()
		{
			ContentUtilities.AddCollisionToSprite(gameObject, new Vector3(1f, 5f, 1f), Vector3.zero).isTrigger = true; // Creates collider and set it as trigger

			renderer = ContentUtilities.AddVisualToSprite(transform, ContentAssets.GetAsset<Sprite>("trapOpen"), ContentUtilities.DefaultBillBoardMaterial);
			closedSprite = ContentAssets.GetAsset<Sprite>("trapClosed");
			aud_catch = ContentAssets.GetAsset<SoundObject>("trapCatch");
		}

		public override bool Use(PlayerManager pm)
		{
			SetupVisual();

			this.pm = pm;
			owner = pm.gameObject;
			transform.position = pm.transform.position + Vector3.down * 3.9f;

			return true;
		}

		private void OnTriggerEnter(Collider other)
		{
			if (hasCatched || other.gameObject == owner) return;

			var component = other.GetComponent<ActivityModifier>();
			if (other.tag == "Player")
			{
				ItemSoundHolder.CreateSoundHolder(transform, aud_catch, false, 30, 50);
				renderer.sprite = closedSprite;
				hasCatched = true;
				if (component && !component.moveMods.Contains(mod))
				{
					component.moveMods.Add(mod);
				}
				StartCoroutine(Timer(component));
			}

			if (other.tag == "NPC" && other.isTrigger)
			{
				ItemSoundHolder.CreateSoundHolder(transform, aud_catch, true, 30, 50);
				renderer.sprite = closedSprite;
				hasCatched = true;

				
				if (component && !component.moveMods.Contains(mod))
				{
					component.moveMods.Add(mod);
				}
				StartCoroutine(Timer(component));

			}
		}

		private IEnumerator Timer(ActivityModifier target)
		{
			float time = Random.Range(6f, 12f);
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

		SpriteRenderer renderer;

		Sprite closedSprite;

		SoundObject aud_catch;

		readonly MovementModifier mod = new MovementModifier(Vector3.zero, 0f);

		bool hasCatched = false;

		GameObject owner;
	}
}
