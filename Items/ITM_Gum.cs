using System.Collections;
using UnityEngine;

namespace BB_MOD.ExtraItems
{
	// ------ Item Summary ------
	// Traps, and nothing can move for some seconds


	public class ITM_Gum : Item
	{
		void SetupVisual()
		{
			ContentUtilities.AddCollisionToSprite(gameObject, new Vector3(2f, 5f, 2f), Vector3.zero).isTrigger = true; // Creates collider and set it as trigger

			renderer = ContentUtilities.AddVisualToSprite(transform, ContentAssets.GetAsset<Sprite>("gum_ball"), ContentUtilities.DefaultBillBoardMaterial);
			groundSprite = ContentAssets.GetAsset<Sprite>("gum_gummed");
			aud_spit = ContentAssets.GetAsset<SoundObject>("gumSpit");

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

			ItemSoundHolder.CreateSoundHolder(pm.transform, aud_spit, false, 70, 71);

			transform.position = pm.transform.position;
			transform.rotation = pm.transform.rotation;

			return true;
		}

		private void OnTriggerEnter(Collider other)
		{
			if (hasCatched) return;

			if (other.tag == "NPC" && other.isTrigger)
			{
				renderer.sprite = groundSprite;
				hasCatched = true;
				var component = other.GetComponent<ActivityModifier>();

				transform.SetParent(other.transform);
				transform.localPosition = Vector3.down * 3.7f;

				pm.RuleBreak("gumming", 2f);
				
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

		SpriteRenderer renderer;

		SoundObject aud_spit;

		Sprite groundSprite;

		readonly MovementModifier mod = new MovementModifier(Vector3.zero, 0.1f);

		bool hasCatched = false;

		float lifeSpan = 200f;

		[SerializeField]
		const float speed = 25f;
	}
}
