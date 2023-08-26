using HarmonyLib;
using MTM101BaldAPI;
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
	// if the player touches it, it'll get a random faculty or office and go for it (in fast speed)
	// you will be dragged within the chair until it stops
	// you can also throw a npc on the chair (use bsoda)
	// It is a force drag, which means you can't leave it unless if you enter a blue locker or use teleporter
	// Pros: while in the chair, you're basically invicible, or you can throw a npc on the chair
	// Cons: The cooldown for use it is REALLY long (around 5 minutes or something)

	public class OfficeChair : NPC
	{



		private void Start()
		{
			navigator.maxSpeed = runSpeed;
			navigator.SetSpeed(runSpeed);

			// Audio Setup

			audMan = GetComponent<AudioManager>();

			aud_ChairRoll = ObjectCreatorHandlers.CreateSoundObject(ContentAssets.GetAsset<AudioClip>("chair_rolling"), "Vfx_OFC_Walk", SoundType.Voice, Color.blue); // Creates audioClip

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
			// Get all corners of every single faculty room
			ec.rooms.Where(x => x.category == RoomCategory.Office || x.category == RoomCategory.Faculty).Do(x => AvailableFacultyTiles.AddRange(x.GetNewTileList().Where(s => s.wallDirections.Length >= 2)));

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
					StartCoroutine(Cooldown(200f));
				beginningPhase = false;
				currentRoomController = targetRoomTile.room;
				targetRoomTile = null;
			}
			if (movingPlayer)
			{
				movingPlayer.Am.moveMods.Remove(moveMod);
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

		public override void Despawn()
		{
			ResetPlayer();
			base.Despawn();
		}

		private void Update()
		{

			if (isMovingToRoom)
			{

				if (!Navigator.HasDestination)
					TargetPosition(targetRoomTile.gameObject.transform.position);
				if (movingPlayer)
				{
					if (movingPlayer.hidden || movingPlayer.plm.addendImmune)
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
			if (!isMovingToRoom && moveCooldown <= 0f && !beginningPhase)
			{
				if (other.tag == "Player")
				{
					PlayerManager player = other.GetComponent<PlayerManager>();


					movingPlayer = player; // Basically makes player invicible until gets into another faculty
					player.invincible = true;
					playerHeight = player.plm.height; // Saves height for later too
					playerLayer = player.gameObject.layer; // Saves last layer, to guarantee nothing goes wrong
					player.gameObject.layer = hideLayer;
					player.plm.height = 8f;
					player.plm.am.moveMods.Add(moveMod);
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

		private readonly MovementModifier moveMod = new MovementModifier(new Vector3(), 0f);

		private TileController targetRoomTile = null;

		private RoomController currentRoomController;

		private readonly List<TileController> AvailableFacultyTiles = new List<TileController>();

		private float moveCooldown = 0f;

		private SpriteRenderer spriteMan;

		private Sprite[] sprites;

		private AudioManager audMan;

		private SoundObject aud_ChairRoll;

		private int playerLayer = 16;

		private float playerHeight;

		[SerializeField]
		private const float runSpeed = 75f;

		[SerializeField]
		private const int hideLayer = 12;


	}

}

