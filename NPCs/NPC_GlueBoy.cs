using BB_MOD.ExtraComponents;
using System.Collections;
using UnityEngine;

namespace BB_MOD.NPCs
{
	// ----------------- WARNING --------------------
	//	Glue boy, see player, put glue, haha
				
	public class GlueBoy : NPC
    {
        private void Start() // This is the Start() method, a normal Monobehavior method, which means, when the npc is enabled, it'll run this method once, you can put your setup here
        {
            navigator.maxSpeed = normalSpeed;
            navigator.SetSpeed(normalSpeed); // On the start here, also highly important, it sets the speed of your npc using the constant float value (you can always change that and include other constant speed values)

			// Audio Setup

			audMan = GetComponent<AudioManager>(); // Highly important to set the audMan to the component, so the field refers to it
			audMan.audioDevice.minDistance = 65f;
			audMan.audioDevice.maxDistance = 120f;

			looker.distance = 30f;

		}

		private void Update() // On each frame tick, this is called, put here your script to make the npc alive
        {
			if (!controlOverride && !navigator.HasDestination) // If the npc has no destination and the control isn't being overriden, it'll constantly call this method (this is important so the npc wanders around)
			{
				WanderRandom();
			}

			if (wanderCooldown > 0f)
			{
				wanderCooldown -= ec.NpcTimeScale * Time.deltaTime;
			}
			else
			{
				wanderCooldown = Random.Range(minWanderCooldown, maxWanderCooldown);
				for (int i = 0; i < aud_whatifs.Length; i++)
				{
					audMan.QueueAudio(aud_whatifs[i]);
				}
			}
		}

		public override void DestinationEmpty() // Feature from the npcs, when their destination is empty (which means, when they have reached their destination tile), this is called, here you can set it to wander random with the met conditions
		{
			base.DestinationEmpty();
			if (!controlOverride && !returningFromDetour) // returningFromDetour means if the npc isn't searching for a detour (in other words, the floating objects from the Gravity Chaos event), or if it is searching for the target AFTER getting the floating object
			{
				WanderRandom();
			}
		}

		public override void Despawn() // Runs when the npc is about to despawn, it is really useful to remove an effect that the npc leaves behind, such as gotta sweep with it's sweeping power
		{
			if (currentGlue != null)
				currentGlue.Despawn(true);
			base.Despawn();
		}

		public override void PlayerInSight(PlayerManager player) // Runs once the player is sighted, not constantly (Looker component required)
		{
			if (!wantGlueAgain)
				return;

			if (!player.Tagged)
			{
				SetGuilt(2f, "littering");
				wantGlueAgain = false;
				audMan.FlushQueue(true);
				audMan.QueueAudio(aud_herewego);

				Directions.ReverseList(navigator.currentDirs);
				navigator.WanderRandom();
				navigator.ClearDestination();
				wanderCooldown = Random.Range(minWanderCooldown, maxWanderCooldown);

				currentGlue = PrefabInstance.SpawnPrefab<GroundedGlue>(ec.TileFromPos(transform.position).transform.position, Quaternion.Euler(new Vector3(90f, 0f, 0f)), ec, false);
				currentGlue.SetOwner(gameObject);
				currentGlue.Execute();
				StartCoroutine(GlueCooldown());
			}
		}

		IEnumerator GlueCooldown()
		{
			glueCooldown = maxGlueCooldown;
			while (glueCooldown > 0f)
			{
				glueCooldown -= ec.EnvironmentTimeScale * Time.deltaTime;
				yield return null;
			}

			currentGlue?.Despawn(true);
			wantGlueAgain = true;
			yield break;
		}

		public override void WanderRandom() // Called by the NPC when it wants to wander, there's no clear use of it to be overriden
		{
			base.WanderRandom();
		}

		// It's also recommended to check some of the properties from the NPC class and Looker aswell, they are really useful

		private AudioManager audMan;

		float wanderCooldown = minWanderCooldown, glueCooldown = maxGlueCooldown;

		readonly SoundObject[] aud_whatifs = new SoundObject[]
		{
			ContentAssets.GetAsset<SoundObject>("Gboy_whatif1"),
			ContentAssets.GetAsset<SoundObject>("Gboy_whatif2"),
			ContentAssets.GetAsset<SoundObject>("Gboy_whatif3")
		};
		readonly SoundObject aud_herewego = ContentAssets.GetAsset<SoundObject>("Gboy_herewego");

		GroundedGlue currentGlue = null;

		bool wantGlueAgain = true;

		[SerializeField]
        private const float normalSpeed = 20f, minWanderCooldown = 15f, maxWanderCooldown = 20f, maxGlueCooldown = 30f;
    }
}

