﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BB_MOD.NPCs
{

	// ------ NPC SUMMARY ------
	// If he sees the player out of the office of a classroom, he will warn Baldi
	// Pros: You can use as advantage to distract Baldi to a specific location
	// Cons: He calls Baldi to your location anyways
	
    public class SuperIntendent : NPC
    {

		

        private void Start() 
        {
            navigator.maxSpeed = normalSpeed;
            navigator.SetSpeed(normalSpeed);

			// Audio Setup

			audMan = GetComponent<AudioManager>();
			audMan.audioDevice.maxDistance = 500; // Sets the max distance the audio of the NPC can reach, that's a very important thing to note, use the audioDevice, not the manager to control volumes!
			
			aud_BaldiComeHere = ContentAssets.GetAsset<SoundObject>("SI_overhere"); // Creates audioClip
		}

		

        private void Update()
        {
			if (!controlOverride && !navigator.HasDestination)
			{
				WanderRounds();
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


		public override void PlayerInSight(PlayerManager player)
		{
			if (called || player.Tagged)
				return;

			var pos = ec.TileFromPos(player.transform.position);
			if (pos != null && !allowedSpots.Contains(pos.room.category))
			{
				StartCoroutine(Cooldown(100f));
				audMan.PlaySingle(aud_BaldiComeHere);
				ec.MakeNoise(transform.position, 127); // 127 = value of an alarm clock
			}
		}

		private IEnumerator Cooldown(float val)
		{
			called = true;
			float cooldown = val;
			while (cooldown > 0f)
			{
				cooldown -= Time.deltaTime * ec.NpcTimeScale;
				yield return null;
			}

			called = false;
			yield break;
		}

		readonly List<RoomCategory> allowedSpots = new List<RoomCategory>() {
			RoomCategory.Class,
			RoomCategory.Office
		};

		private AudioManager audMan;

		private SoundObject aud_BaldiComeHere;

		private bool called = false;

		[SerializeField]
        private const float normalSpeed = 24f;

		
    }

}

