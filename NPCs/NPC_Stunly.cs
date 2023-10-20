using BB_MOD.ExtraComponents;
using BB_MOD.ExtraItems;
using Patches.Main;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BB_MOD.NPCs
{
	// ----------------- WARNING --------------------
	//	Stuns player/npcs with uglyness

	public class Stunly : NPC
	{
		private void Start()
		{
			navigator.maxSpeed = normalSpeed;
			navigator.SetSpeed(normalSpeed);

			looker.distance = 60f;

			aud_Stun = ContentAssets.GetAsset<SoundObject>("stunly_stun");
		}

		private void Update()
		{
			if (!escaping && !controlOverride && !navigator.HasDestination)
			{
				WanderRandom();
			}
		}

		public override void DestinationEmpty()
		{
			base.DestinationEmpty();
			if (!escaping && !controlOverride && !returningFromDetour)
			{
				WanderRandom();
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			if (!stunningTime || escaping)
				return;


			if (other.tag == "Player")
			{
				WhiteScreenEffect(Singleton<CoreGameManager>.Instance.GetHud(other.GetComponent<PlayerManager>().playerNumber).Canvas());

				ItemSoundHolder.CreateSoundHolder(other.transform, aud_Stun, false, 40, 50);
				StartCoroutine(Escape(other.transform.position));
				StartCoroutine(StunCooldown(other.gameObject));
			}
			else if (other.tag == "NPC" && other.isTrigger)
			{
				var stunningStar = PrefabInstance.SpawnPrefab<StunningStars>(other.transform.position, default, ec, false);
				stunningStar.SetupTarget(other.transform);
				stunningStar.Execute();
				ItemSoundHolder.CreateSoundHolder(other.transform, aud_Stun, true, 40, 70);

				StartCoroutine(Escape(other.transform.position));
				StartCoroutine(StunCooldown(other.gameObject, stunningStar));
			}
		}

		private void WhiteScreenEffect(Canvas canvas)
		{
			stunlyHud = PrefabInstance.SpawnPrefab<StunlyEffect>(Vector3.zero, default, ec, false);
			stunlyHud.SetupHud(canvas);
			stunlyHud.Execute();
		}

		public override void Despawn()
		{
			if (stunlyHud)
				Destroy(stunlyHud.gameObject);
			ClearMods();
			base.Despawn();
		}

		public override void PlayerLost(PlayerManager player) // When the player is lost from the NPC's sight (Looker component required)
		{
			if (aggroed && stunningTime && !escaping)
			{
				Directions.ReverseList(navigator.currentDirs);
				WanderRandom();
				navigator.maxSpeed = normalSpeed;
				navigator.SetSpeed(normalSpeed);
				navigator.ClearDestination();
				aggroed = false;
			}
		}

		public override void PlayerInSight(PlayerManager player) // This is constantly called in case the player is seen by the npc (Looker component required)
		{
			if (stunningTime && !escaping)
			{
				if (!player.Tagged)
				{
					TargetPlayer(player.transform.position);
					aggroed = true;
					navigator.maxSpeed = runSpeed;
					navigator.SetSpeed(runSpeed);
				}
				else if (aggroed)
				{
					Directions.ReverseList(navigator.currentDirs);
					WanderRandom();
					navigator.maxSpeed = normalSpeed;
					navigator.SetSpeed(normalSpeed);
					navigator.ClearDestination();
					aggroed = false;
				}
			}
		}

		private IEnumerator StunCooldown(GameObject target, StunningStars star = null)
		{
			stunningTime = false;
			if (star)
				stars.Add(star);

			var looker = target.GetComponent<Looker>();
			bool enabledLooker = false;

			LookerDistancingPatch.LookerToken token = new LookerDistancingPatch.LookerToken(0f, looker); ;

			if (target.tag == "NPC" && looker)
			{
				if (looker.isActiveAndEnabled)
				{
					LookerDistancingPatch.lookerModifiers.Add(token); // Adds the token
					enabledLooker = true;
				}
			}

			SetGuilt(2f, "uglyStun");

			var mod = target.GetComponent<ActivityModifier>();

			if (!mod.moveMods.Contains(moveMod))
			{
				mod.moveMods.Add(moveMod);
				actMods.Add(mod);
			}
			float time = Random.Range(minStunningCooldown, maxStunningCooldown + 1f);
			while (time > 0f)
			{
				time -= ec.EnvironmentTimeScale * Time.deltaTime;
				yield return null;
			}

			if (enabledLooker)
			{
				LookerDistancingPatch.RemoveLookerFromList(token);
			}

			ClearMods();
			StartCoroutine(NextStunCooldown());

			yield break;
		}

		private IEnumerator NextStunCooldown()
		{
			stunningTime = false;
			wannaStunCooldown = Random.Range(minStunCooldown, maxStunCooldown + 1f);
			while (wannaStunCooldown > 0f)
			{
				wannaStunCooldown -= ec.EnvironmentTimeScale * Time.deltaTime;
				yield return null;
			}
			stunningTime = true;
			yield break;
		}

		private IEnumerator Escape(Vector3 target)
		{
			var tileToBeAwayFrom = ec.TileFromPos(target);
			escaping = true;
			navigator.maxSpeed = escapeSpeed;
			navigator.SetSpeed(escapeSpeed);

			aggroed = true;
			yield return null;

			float time = 10f;
			while (time > 0f)
			{
				time -= ec.EnvironmentTimeScale * Time.deltaTime;

				if (!navigator.HasDestination)
				{

					if (ec.TileFromPos(transform.position) != tileToBeAwayFrom)
						navigator.RunFrom(target);
					else
						WanderRandom();
				}
				yield return null;
			}

			aggroed = false;
			escaping = false;

				navigator.ClearDestination();
				navigator.maxSpeed = normalSpeed;
				navigator.SetSpeed(normalSpeed);
			

			yield break;
		}

		private void ClearMods()
		{
			foreach (var mod in actMods)
			{
				mod.moveMods.Remove(moveMod);
			}

			foreach (var star in stars)
			{
				star.Despawn();
			}

			actMods.Clear();
			stars.Clear();
		}

		private SoundObject aud_Stun;

		private StunlyEffect stunlyHud;

		private readonly List<ActivityModifier> actMods = new List<ActivityModifier>();

		private readonly List<StunningStars> stars = new List<StunningStars>();

		private readonly MovementModifier moveMod = new MovementModifier(Vector3.zero, 0.4f);

		float wannaStunCooldown = 0f;

		bool stunningTime = true;

		bool escaping = false;

		[SerializeField]
		private const float normalSpeed = 13f, runSpeed = 21f, escapeSpeed = 40f, minStunCooldown = 10f, maxStunCooldown = 25f, minStunningCooldown = 10f, maxStunningCooldown = 15f;
	}
}

