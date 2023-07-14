using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetManager;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BB_MOD.NPCs
{

	// Use this as a template NPC for your custom NPC

	// ------ NPC SUMMARY ------
	// Office Chair function will be staying in a faculty room
	// if the player touches it, it'll get a random faculty and go for it (in fast speed)
	// you will be dragged within the chair until it stops
	// It is a force drag, which means you can't leave it unless if you enter a blue locker or use teleporter
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
			audMan.volumeModifier = 70f;
			
			aud_ChairRoll = ObjectCreatorHandlers.CreateSoundObject(AssetManager.AudioClipFromFile(Path.Combine(ContentManager.modPath, "Audio", "npc", "ChairRolling.wav")), "Vfx_OFC_Walk", SoundType.Voice, Color.blue); // Creates audioClip

			spriteMan = GetComponent<CustomNPCData>().spriteObject;
			sprites = GetComponent<CustomNPCData>().sprites;

			// Makes chair move to the best corner
			isMovingToRoom = true;
			audMan.SetLoop(true);
			audMan.QueueAudio(aud_ChairRoll);

			var tiles = FindMyFacultyTiles(ec.TileFromPos(gameObject.transform.position).room);
			targetRoomTile = tiles.ElementAt(Random.Range(0, tiles.Count()));

		}
		public override void Initialize()
		{
			base.Initialize();
			AvailableFacultyTiles.AddRange(ec.AllTilesNoGarbage(false, false).Where(t => t.room.category == RoomCategory.Faculty && t.wallDirections.Length >= 2)); 
			// Get all corners of every single faculty room
		}

		private IEnumerable<TileController> CollectFacultyTilesExcept(RoomController currentRoom)
		{
			using (var enumerator = AvailableFacultyTiles.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (!ReferenceEquals(currentRoom, enumerator.Current.room)) // Only return faculty tiles that are not from current Chair Room
						yield return enumerator.Current;
				}
			}
		}

		private IEnumerable<TileController> FindMyFacultyTiles(RoomController currentRoom)
		{
			using (var enumerator = AvailableFacultyTiles.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (ReferenceEquals(currentRoom, enumerator.Current.room)) // Only return faculty tiles that ARE from current Chair Room
						yield return enumerator.Current;
				}
			}
		}

		private void ResetPlayer(bool fullReset = true)
		{
			if (fullReset)
			{
				if (!beginningPhase)
					StartCoroutine(Cooldown(400f));
				beginningPhase = false;
				currentRoomController = targetRoomTile.room;
				targetRoomTile = null;
			}
			if (movingPlayer)
			{
				movingPlayer.invincible = false;
				movingPlayer.plm.height = playerHeight;
				movingPlayer.gameObject.layer = playerLayer;
				movingPlayer = null;
			}
			if (fullReset)
			{
				isMovingToRoom = false;
				audMan.FlushQueue(true);
			}
		}
		

        private void Update()
        {
			
			if (isMovingToRoom)
			{
				
				if (!Navigator.HasDestination)
					TargetPosition(targetRoomTile.gameObject.transform.position);
				if (movingPlayer)
				{
					if (movingPlayer.hidden)
					{
						ResetPlayer(false);
						return;
					}
					movingPlayer.transform.position = gameObject.transform.position;
				}
				
			}
			
        }

		public override void DestinationEmpty()
		{
			base.DestinationEmpty();
			if (targetRoomTile && ReferenceEquals(targetRoomTile.room, ec.TileFromPos(gameObject.transform.position).room)) // If destination reached, reset everything
			{
				ResetPlayer();
			}
		}
		

		private void OnTriggerEnter(Collider other)
		{
			if (other.tag == "Player" && !isMovingToRoom && !movingPlayer && moveCooldown <= 0f)
			{
				PlayerManager player = other.GetComponent<PlayerManager>();
				if (!player.tagged) // An optional IF
				{
					movingPlayer = player; // Basically makes player invicible until gets into another faculty
					player.invincible = true;
					playerHeight = player.plm.height; // Saves height for later too
					playerLayer = player.gameObject.layer; // Saves last layer, to guarantee nothing goes wrong
					player.gameObject.layer = hideLayer;
					player.plm.height = 8f;
					isMovingToRoom = true;
					audMan.SetLoop(true);
					audMan.QueueAudio(aud_ChairRoll);
					var tiles = CollectFacultyTilesExcept(currentRoomController);
					targetRoomTile = tiles.ElementAt(Random.Range(0, tiles.Count()));
				}
			}
		}

		private IEnumerator Cooldown(float val)
		{
			spriteMan.sprite = sprites[1]; // Disabled Sprite
			moveCooldown = val;
			while (moveCooldown > 0f)
			{
				moveCooldown -= Time.deltaTime * ec.NpcTimeScale;
				yield return null;
			}
			spriteMan.sprite = sprites[0]; // Active Sprite
			moveCooldown = 0f;
			yield break;
		}

		private bool isMovingToRoom = false;

		private bool beginningPhase = true;

		private PlayerManager movingPlayer = null;

		private TileController targetRoomTile = null;

		private RoomController currentRoomController;

		private readonly List<TileController> AvailableFacultyTiles = new List<TileController>();

		private float moveCooldown = 0f;

		private SpriteRenderer spriteMan;

		private Sprite[] sprites;

        public AudioManager audMan;

        public SoundObject aud_ChairRoll;

		private int playerLayer = 12;

		private float playerHeight;

		[SerializeField]
        private const float runSpeed = 75f;

		[SerializeField]
		private const int hideLayer = 16;

		
    }

}

