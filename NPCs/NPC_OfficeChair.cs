using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetManager;
using System.IO;
using UnityEngine;

namespace BB_MOD.NPCs
{

	
    public class OfficeChair : NPC
    {
        private void Start() 
        {
            navigator.maxSpeed = runSpeed;
            navigator.SetSpeed(runSpeed);

			audMan = GetComponent<AudioManager>();

			audSource = gameObject.AddComponent<AudioSource>();
			audSource.loop = true;
			audSource.clip = aud_ChairRoll.soundClip;

			isMovingToRoom = true;

			aud_ChairRoll = ObjectCreatorHandlers.CreateSoundObject(AssetManager.AudioClipFromFile(Path.Combine(ContentManager.modPath, "Audio", "npc", "ChairRolling.wav")), "Vfx_OFC_Walk", SoundType.Voice, Color.blue);

			Debug.Log("I\'m initialized");

        }
        private void Update()
        {
			if (!audSource.isPlaying && isMovingToRoom)
				audSource.Play();
			
			else
				audSource.Stop();
			

			if (!navigator.HasDestination)
			{
				//navigator.WanderRandom();
			}
        }

		private AudioSource audSource;

		private bool isMovingToRoom = false;

		private TileController targetRoomTile;

        public AudioManager audMan;

        public SoundObject aud_ChairRoll;

        [SerializeField]
        private const float runSpeed = 25f;
    }

}

