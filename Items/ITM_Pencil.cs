using MTM101BaldAPI;
using UnityEngine;
using System.Collections;

namespace BB_MOD.ExtraItems
{
	// ------ Item Summary ------
	// Stabs someone, freeze it!


	public class ITM_Pencil : Item
	{

		public override bool Use(PlayerManager pm)
		{
			if (!Physics.Raycast(pm.transform.position, Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber).transform.forward, out RaycastHit hit, pm.pc.reach, 2326529, QueryTriggerInteraction.Ignore) || (!hit.transform.CompareTag("NPC") || ContentManager.instance.IsNpcStatic(hit.transform.GetComponent<NPC>().Character))) // 2326529 < npc layer basically
			{
				Destroy(gameObject);
				return false;
			}

			StartCoroutine(FreezeNPC(hit.transform.GetComponent<NPC>(), pm));

			if (pm.jumpropes.Count > 0 && hit.transform.GetComponent<NPC>().Character == Character.Playtime)
			{
				while (pm.jumpropes.Count > 0)
				{
					pm.jumpropes[0].End(false);
				}
			}

			pm.RuleBreak("stabbing", 2f);
			ItemSoundHolder.CreatePositionalSoundHolder(hit.transform, ObjectCreatorHandlers.CreateSoundObject(ContentAssets.GetAsset<AudioClip>("pb_stab"), "Vfx_PC_stab", SoundType.Voice, new Color(179, 179, 0)), true, 95);
			return true;
		}

		private IEnumerator FreezeNPC(NPC npc, PlayerManager pm)
		{
			float time = 10f;
			ActivityModifier component = npc.GetComponent<ActivityModifier>();
			MovementModifier mod = new MovementModifier(Vector3.zero, 0f); // Creates a modifier with 0 multiplier (will make the character unable to move)

			if (!component.moveMods.Contains(mod)) component.moveMods.Add(mod);
			foreach (var trigger in npc.baseTrigger) // Makes the character unable to touch the player
			{
				Physics.IgnoreCollision(trigger, pm.plm.cc, true);
			}
			while (time > 0f)
			{
				time -= Time.deltaTime * pm.ec.EnvironmentTimeScale;
				yield return null;
			}

			component.moveMods.Remove(mod); // Removes the mod afterwards

			foreach (var trigger in npc.baseTrigger)
			{
				Physics.IgnoreCollision(trigger, pm.plm.cc, false);
			}

			Destroy(gameObject);

			yield break;
		}
	}
}
