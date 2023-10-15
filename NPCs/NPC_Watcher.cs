using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BB_MOD.NPCs
{
	// ----------------- WARNING --------------------
	//	Watches player, if player looks for 2 seconds (for quick reflex), it gets to you and switches you with a random character (except himself duh)
				
	public class Watcher : NPC
    {
        private void Start() // This is the Start() method, a normal Monobehavior method, which means, when the npc is enabled, it'll run this method once, you can put your setup here
        {

			// Audio Setup

			audMan = GetComponent<AudioManager>(); // Gets the first (main) audman
			audMan.audioDevice = ContentUtilities.CreateNonPositionalAudio(gameObject);
			audMan.positional = false;

			ambience = ContentAssets.GetAsset<SoundObject>("wch_idle");
			seen = ContentAssets.GetAsset<SoundObject>("wch_see");
			angry = ContentAssets.GetAsset<SoundObject>("wch_angry");
			warp = ContentAssets.GetAsset<SoundObject>("wch_tp");


			ContentUtilities.CreateMusicManager(gameObject, 45, 120, ambience); //music manager

			spots = ec.mainHall.GetTilesOfShape(new List<TileShape>() { TileShape.Corner, TileShape.End }, true).Where(x => !x.containsObject).ToArray();
			Disable();

		}

		private void Enable()
		{
			transform.position = spots[Random.Range(0, spots.Length)].transform.position;
			angryCooldown = maxAngryCooldown;

			StartCoroutine(SpawnSequence());

			IEnumerator SpawnSequence()
			{
				float speed = 0f;
				while (transform.position.y < 5f)
				{
					speed += 0.5f * ec.NpcTimeScale * Time.deltaTime;
					transform.position += Vector3.up * speed;
					yield return null;
				}

				transform.position = new Vector3(transform.position.x, 5f, transform.position.z);

				active = true;
				despawnCoroutine = StartCoroutine(WaitForDespawn());

				yield break;
			}
		}

		private void Disable(bool waitForSpawn = true)
		{
			transform.position = Vector3.zero + Vector3.down * 100f;
			active = false;
			audMan?.FlushQueue(true);
			if (waitForSpawn)
			{
				StartCoroutine(WaitForSpawn());
			}
		}

		private IEnumerator WaitForSpawn()
		{
			waitTime = Random.Range(minWaitTime, maxWaitTime);
			while (waitTime > 0f)
			{
				waitTime -= ec.NpcTimeScale * Time.deltaTime;
				yield return null;
			}
			Enable();

			

			yield break;
		}

		private IEnumerator WaitForDespawn()
		{
			activeTime = Random.Range(minActiveTime, maxActiveTime);
			while (activeTime > 0f)
			{
				activeTime -= ec.NpcTimeScale * Time.deltaTime;
				yield return null;
			}

			while (looker.IsVisible) { yield return null; } // Waits to be unsighted

			Disable();

			

			yield break;
		}

		public override void Initialize() // Basically just the same as Start() method, but it is ran before being enabled into the level, here you could add custom spawn points (just like Crazy Clock does)
		{
			base.Initialize();
			Disable(false);
		}

		private void Update() // On each frame tick, this is called, put here your script to make the npc alive
        {
			if (!active) return;

			if (angered && targetPlayer)
			{
				TargetPlayer(targetPlayer.transform.position);
			}

		}

		private void OnTriggerEnter(Collider other)
		{
			if (!angered || !active)
			{
				return;
			}

			if (other.tag == "Player")
			{
				var player = other.GetComponent<PlayerManager>();
				if (player == targetPlayer)
				{
					// Do the mixup here
					angered = false;
					audMan.FlushQueue(true);
					Disable();
					if (crazyTime != null)
						StopCoroutine(crazyTime);

					MixUp(player);

					targetPlayer = null;
				}
			}
		}

		private void MixUp(PlayerManager player, bool ignoreDistance = false)
		{
			var npcs = new List<NPC>();
			foreach (var npcFound in ec.Npcs)
			{
				if (npcFound != this && npcFound)
				{
					if (!ContentManager.instance.IsNpcStatic(npcFound.Character) && (ignoreDistance || Vector3.Distance(npcFound.transform.position, player.transform.position) > 20f))
						npcs.Add(npcFound);
				}
			}
			if (npcs.Count == 0)
			{
				if (!ignoreDistance)
					MixUp(player, true);
				
				return;
			}

			var npc = npcs[Random.Range(0, npcs.Count)];
			(player.transform.position, npc.transform.position) = (npc.transform.position, player.transform.position); // swap positions

			ec.MakeNoise(player.transform.position, 64); // Also makes noise, so it's not a full advantage on escaping

			audMan.SetLoop(false);
			audMan.QueueAudio(warp);

			EnvironmentExtraVariables.FovModifiers.Remove(crazyToken);


			StartCoroutine(EnvironmentExtraVariables.SmoothFOVSlide(10f, 5, offset: 115f));
		}

		private void GetAngry()
		{

			angered = true; // Get angry phase
			if (despawnCoroutine != null)
				StopCoroutine(despawnCoroutine);

			audMan.FlushQueue(true);

			navigator.maxSpeed = maxSpeed;
			navigator.SetSpeed(0f);
			navigator.accel = speedAccel;

			audMan.SetLoop(true);
			audMan.QueueAudio(angry);

			crazinessRange = smallCrazinessRange;
			
			
		}

		public override void PlayerInSight(PlayerManager player)
		{
			if (!active) return;

			if (looker.IsVisible)
			{
				if (!angered)
				{
					angryCooldown -= ec.NpcTimeScale * Time.deltaTime;
					if (angryCooldown < 0f)
					{
						targetPlayer = player;
						GetAngry();
					}
				}
			}
			
		}

		private IEnumerator CrazyScreen()
		{
			EnvironmentExtraVariables.FovModifiers.Add(crazyToken);
			while (active)
			{
				crazyToken.Offset = Random.Range(-crazinessRange, crazinessRange) * (maxAngryCooldown - angryCooldown);
				yield return null;
			}
			EnvironmentExtraVariables.FovModifiers.Remove(crazyToken);

			yield break;
		}
		

		public override void Sighted() // If the npc is sighted by the player, this method runs once (Looker component required)
		{
			if (active)
			{
				crazyTime = StartCoroutine(CrazyScreen());
				if (!angered)
				{
					crazinessRange = defaultCrazinessRange;
					audMan.QueueAudio(seen);
					audMan.SetLoop(true);
				}
			}
		}

		public override void Unsighted()
		{
			if (active && !angered)
			{
				if (crazyTime != null)
				{
					StopCoroutine(crazyTime);
				}

				EnvironmentExtraVariables.FovModifiers.Remove(crazyToken);
				audMan.FlushQueue(true);
				angryCooldown = maxAngryCooldown;
			}
		}

		private AudioManager audMan;

		private SoundObject ambience;

		private SoundObject seen;

		private SoundObject angry;

		private SoundObject warp;

		private TileController[] spots;

		readonly EnvironmentExtraVariables.FOVToken crazyToken = new EnvironmentExtraVariables.FOVToken(0f, 1);

		bool active = false, angered = false;

		private float waitTime, activeTime, angryCooldown, crazinessRange = defaultCrazinessRange;

		PlayerManager targetPlayer = null;

		Coroutine despawnCoroutine, crazyTime;

		[SerializeField]
        private const float maxSpeed = 150f, speedAccel = 25f, minWaitTime = 25f, maxWaitTime = 35f, minActiveTime = 40f, maxActiveTime = 80f, maxAngryCooldown = 1f, defaultCrazinessRange = 20f, smallCrazinessRange = 5f;
    }
}

