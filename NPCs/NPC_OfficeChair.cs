using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetManager;
using System.IO;
using UnityEngine;

namespace BB_MOD.NPCs
{

	// Use this as a template NPC for your custom NPC

	// ------ NPC SUMMARY ------
	// Office Chair function will be staying in a faculty room
	// if the player touches it, it'll get a random faculty and go for it (in fast speed)
	// you will be dragged within the chair until it stops
	// It is a force drag, which means you can't leave it
	// Pros: while in the chair, you're basically invicible
	// Cons: The cooldown for use it is REALLY long (around 5 minutes or something)
	
    public class OfficeChair : NPC
    {
        private void Start() 
        {
            navigator.maxSpeed = runSpeed;
            navigator.SetSpeed(runSpeed);

			// Audio Setup

			audMan = GetComponent<AudioManager>();
			
			aud_ChairRoll = ObjectCreatorHandlers.CreateSoundObject(AssetManager.AudioClipFromFile(Path.Combine(ContentManager.modPath, "Audio", "npc", "ChairRolling.wav")), "Vfx_OFC_Walk", SoundType.Voice, Color.blue); // Creates audioClip

			// Rest

			isMovingToRoom = true;

			

			Debug.Log("I\'m initialized");

        }
        private void Update()
        {
			if (Input.GetKeyDown(KeyCode.H)) // Debug the audio, press H to play it, and it should play a really not so expected noise
				audMan.PlaySingle(aud_ChairRoll);
				

			if (!Navigator.HasDestination)
				WanderRandom();
			
        }

		public override void DestinationEmpty()
		{
			base.DestinationEmpty();
			if (!returningFromDetour)
			{
				WanderRandom();
			}
		}

		private bool isMovingToRoom = false;

		private TileController targetRoomTile;

        public AudioManager audMan;

        public SoundObject aud_ChairRoll;

        [SerializeField]
        private const float runSpeed = 25f;
    }

}

