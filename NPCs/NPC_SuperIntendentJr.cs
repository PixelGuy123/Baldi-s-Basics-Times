using Rewired;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BB_MOD.NPCs
{
	// ----------------- WARNING --------------------
	//	Warns and warps the principal to the location when see rule breaker
				
	public class SuperIntendentJr : NPC
    {
        private void Start() // This is the Start() method, a normal Monobehavior method, which means, when the npc is enabled, it'll run this method once, you can put your setup here
        {
            navigator.maxSpeed = normalSpeed;
            navigator.SetSpeed(normalSpeed);


			audMan = GetComponent<AudioManager>(); // Highly important to set the audMan to the component, so the field refers to it
			audMan.audioDevice.maxDistance = 120f;
			audMan.audioDevice.minDistance = 67f;

			audMan_step = gameObject.AddComponent<AudioManager>();
			audMan_step.audioDevice = GetComponent<AudioSource>();
			data = GetComponent<CustomNPCData>();
		
			wanderCooldown = Random.Range(wanderCooldownMin, wanderCooldownMax);
		}

		private void Update() // On each frame tick, this is called, put here your script to make the npc alive
        {
			if (!controlOverride && !navigator.HasDestination) // If the npc has no destination and the control isn't being overriden, it'll constantly call this method (this is important so the npc wanders around)
			{
				WanderRandom();
			}

			if (!isScreaming)
			{
				wanderCooldown -= ec.NpcTimeScale * Time.deltaTime;
				if (wanderCooldown < 0f)
				{
					wanderCooldown = Random.Range(wanderCooldownMin, wanderCooldownMax);
					audMan.QueueAudio(aud_wander);
				}
			}

			if (!wantToCheckBreaker && !isScreaming)
			{
				if (checkBreakerCooldown > 0f)
				{
					checkBreakerCooldown -= ec.NpcTimeScale * Time.deltaTime;
				}
				else
				{
					wantToCheckBreaker = true;
				}
			}
			stepCooldown -= ec.NpcTimeScale * Time.deltaTime * navigator.Velocity.magnitude;
			if (stepCooldown < 0f)
			{
				int thing = (stepTurn ? 0 : 1) + (isScreaming ? 2 : 0);
				data.spriteObject.sprite = data.sprites[thing];
				stepCooldown = maxStepCool;
				audMan_step.PlaySingle(stepTurn ? aud_step1 : aud_step2);

				stepTurn = !stepTurn;
			}
		}

		public override void DestinationEmpty()
		{
			base.DestinationEmpty();
			if (!controlOverride && !returningFromDetour)
			{
				WanderRounds();
			}
		}

		public override void PlayerInSight(PlayerManager player) // This is constantly called in case the player is seen by the npc (Looker component required)
		{
			if (!wantToCheckBreaker) return;

			if (player.Disobeying && !player.Tagged)
			{
				if (failsafe > 0f)
				{
					failsafe -= ec.NpcTimeScale * Time.deltaTime;
					return;
				}
				player.RuleBreak(player.ruleBreak, 4f);
				navigator.RunFrom(player.transform.position);
				navigator.ClearDestination();
				PullUpAPrincipal();
				StartCoroutine(ScreamTimer());
			}
		}

		public override void PlayerLost(PlayerManager player)
		{
			base.PlayerLost(player);
			failsafe = failSafeMax; // Just resets the failSafe
		}

		IEnumerator ScreamTimer()
		{
			audMan.FlushQueue(true);
			audMan.PlaySingle(aud_spot);
			isScreaming = true;
			wantToCheckBreaker = false;
			stepCooldown = -1f;
			while (audMan.IsPlaying) { yield return null; }

			checkBreakerCooldown = checkBreakMaxCooldown;
			isScreaming = false;
			stepCooldown = -1f;

			yield break;
		}

		private void PullUpAPrincipal()
		{
			List<NPC> npcs = new List<NPC>();
			foreach (var npc in ec.Npcs)
			{
				if (npc.Character == Character.Principal || (npc.GetComponent<CustomNPCData>()?.isReplacing == Character.Principal))
				{
					npcs.Add(npc);
				}
			}

			if (npcs.Count == 0) return;
			npcs[Random.Range(0, npcs.Count)].transform.position = transform.position;
		}

		// It's also recommended to check some of the properties from the NPC class and Looker aswell, they are really useful

		private AudioManager audMan;

		private AudioManager audMan_step;

		float wanderCooldown = 0f, checkBreakerCooldown = 0f, stepCooldown = 1f, failsafe = failSafeMax;

		bool wantToCheckBreaker = true, stepTurn = false, isScreaming = false;

		readonly SoundObject aud_step1 = ContentAssets.GetAsset<SoundObject>("spj_step1"), aud_step2 = ContentAssets.GetAsset<SoundObject>("spj_step2"), aud_wander = ContentAssets.GetAsset<SoundObject>("spj_wonder"), aud_spot = ContentAssets.GetAsset<SoundObject>("spj_principal");

		CustomNPCData data = null;

		[SerializeField]
        private const float normalSpeed = 19f, wanderCooldownMin = 10f, wanderCooldownMax = 20f, checkBreakMaxCooldown = 15f, maxStepCool = 0.2f, failSafeMax = 0.4f;
    }
}

