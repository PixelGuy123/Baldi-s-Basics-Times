using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BB_MOD.NPCs
{
	// ----------------- WARNING --------------------
	//	Watches player, if player looks for 4 seconds (for quick reflex), it gets to you and switches you with a random character (except himself duh)
				
	public class Watcher : NPC
    {
        private void Start() // This is the Start() method, a normal Monobehavior method, which means, when the npc is enabled, it'll run this method once, you can put your setup here
        {
            navigator.maxSpeed = normalSpeed;
            navigator.SetSpeed(normalSpeed); // On the start here, also highly important, it sets the speed of your npc using the constant float value (you can always change that and include other constant speed values)

			// Audio Setup

			audMan = GetComponent<AudioManager>(); // Gets the first (main) audman
			hudNoise = ContentUtilities.CreateNonPositionalAudio(gameObject);
			audMan.audioDevice = hudNoise;
			audMan.positional = false;

			ambience = ContentAssets.GetAsset<SoundObject>("wch_idle");
			seen = ContentAssets.GetAsset<SoundObject>("wch_see");

			ContentUtilities.CreateMusicManager(gameObject, 25, 60, ambience); //music manager

			spots = ec.mainHall.GetTilesOfShape(new List<TileShape>() { TileShape.Corner }, false).Where(x => !x.containsObject).ToArray();
			Disable();

		}

		private void Enable()
		{
			transform.position = spots[Random.Range(0, spots.Length)].transform.position + Vector3.up * 5f;
			active = true;
			WaitForDespawn();
		}

		private void Disable(bool waitForSpawn = true)
		{
			transform.position = Vector3.zero + Vector3.down * 100f;
			active = false;
			if (waitForSpawn)
				StartCoroutine(WaitForSpawn());
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


		}

		public override void Sighted() // If the npc is sighted by the player, this method runs once (Looker component required)
		{
			base.Sighted();
		}

		public override void Unsighted()
		{
			base.Unsighted();
		}

		private AudioManager audMan;

		private AudioSource hudNoise;

		private SoundObject ambience;

		private SoundObject seen;

		private TileController[] spots;

		bool active = false, angered = false;

		private float waitTime, activeTime;

		[SerializeField]
        private const float normalSpeed = 15f, minWaitTime = 25f, maxWaitTime = 35f, minActiveTime = 40f, maxActiveTime = 80f;
    }
}

