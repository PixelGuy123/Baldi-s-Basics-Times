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

	// ------ NPC SUMMARY ------
	// If player gets too close, he will not be so happy and will literally pull every character to it's position (chaos basically)
	// Pros: Can teleport literally every npc to the clock location
	// Cons: Can teleport literally every npc to the clock location (lmao)

	public class CrazyClock : NPC
	{



		private void Start()
		{
			Navigator.maxSpeed = 0f; // He doesn't need a navigator, like chalkles
			Navigator.SetSpeed(0f);

			// Audio Setup

			audMan = GetComponent<AudioManager>();

			aud_Scream = ObjectCreatorHandlers.CreateSoundObject(AssetManager.AudioClipFromFile(Path.Combine(ContentManager.modPath, "Audio", "npc", "clock_Scream.wav")), "Vfx_CC_Scream", SoundType.Voice, new Color(230, 46, 0)); // Not so cool scream
			aud_Scream.subDuration -= 3f;
			aud_tick = ObjectCreatorHandlers.CreateSoundObject(AssetManager.AudioClipFromFile(Path.Combine(ContentManager.modPath, "Audio", "npc", "clock_tick.wav")), "Vfx_CC_Tick", SoundType.Voice, new Color(230, 46, 0), 5f);
			aud_tock = ObjectCreatorHandlers.CreateSoundObject(AssetManager.AudioClipFromFile(Path.Combine(ContentManager.modPath, "Audio", "npc", "clock_tack.wav")), "Vfx_CC_Tack", SoundType.Voice, new Color(230, 46, 0), 5f);
			aud_frown = ObjectCreatorHandlers.CreateSoundObject(AssetManager.AudioClipFromFile(Path.Combine(ContentManager.modPath, "Audio", "npc", "clock_frown.wav")), "Vfx_CC_Frown", SoundType.Voice, new Color(230, 46, 0), 7f);

			renderer = GetComponent<CustomNPCData>().spriteObject.GetComponent<SpriteRenderer>();

			
			looker.distance = 90f;
			availableTiles = FindSpawnTiles();
			StartCoroutine(SpawnAtRandomTile());
		}

		public override void Initialize()
		{
			base.Initialize();
			GetComponent<CapsuleCollider>().enabled = false;
			DespawnFromSpot();
			SetupSprites();
		}

		private void SetupSprites()
		{
			availableSpriteArrays.Add(GetComponent<CustomNPCData>().sprites.Take(4).ToArray()); // Normal Sprites
			availableSpriteArrays.Add(GetComponent<CustomNPCData>().sprites.Skip(4).Take(4).ToArray()); // Sight Sprites
			frownSprite = GetComponent<CustomNPCData>().sprites[8];
			availableSpriteArrays.Add(GetComponent<CustomNPCData>().sprites.Skip(9).Take(2).ToArray()); // Scream Sprites
			availableSpriteArrays.Add(GetComponent<CustomNPCData>().sprites.Skip(11).Take(11).ToArray()); // Hide Sprites
			spriteArray = availableSpriteArrays[0];
		}

		// Gets all spawnable tiles
		private List<WeightedSelection<TileController>> FindSpawnTiles()
		{
			List<WeightedSelection<TileController>> weightedTiles = new List<WeightedSelection<TileController>>();
			var tiles = ec.AllTilesNoGarbage(false, false).Where(x => !x.containsObject && !x.containsWallObject && (x.shape == TileShape.Corner || x.shape == TileShape.Single || x.shape == TileShape.End) && (x.room.category == RoomCategory.Hall || x.room.category == RoomCategory.Test || x.room.category == RoomCategory.FieldTrip)
		&& !ec.GetTileNeighbors(x.position).Any(z => z.containsObject || z.containsWallObject));
			foreach (var tile in tiles)
			{
				int weight = tile.room.category == RoomCategory.Hall ? 45 : 25;
				weight += tile.shape == TileShape.Corner || tile.shape == TileShape.End ? 25 : 10;
				weightedTiles.Add(new WeightedSelection<TileController>()
				{
					selection = tile,
					weight = weight
				});
			}
			return weightedTiles;
		}

		private IEnumerator SpawnAtRandomTile()
		{
			TileController tile = WeightedSelection<TileController>.RandomSelection(availableTiles.ToArray());

			transform.position = tile.transform.position + Vector3.up * height;
			spriteBase.transform.position = transform.position + Vector3.down * 10f;
			while (looker.IsVisible || Vector3.Distance(transform.position, Singleton<CoreGameManager>.Instance.GetPlayer(0).transform.position) < 150f)
			{
				yield return null;
			}
			transform.position = tile.transform.position + Vector3.up * height;
			Direction dir = tile.wallDirections[Random.Range(0, tile.wallDirections.Length - 1)];
			spriteBase.transform.eulerAngles = Directions.ToRotation(dir.GetOpposite()).eulerAngles;
			spriteBase.transform.position = transform.position + dir.ToVector3() * tileOffset;
			myTile = tile;
			isActive = true;
			ChangeState(0, true);
			StartCoroutine(ActiveCooldown());
			yield break;
		}

		private void DespawnFromSpot()
		{
			transform.position += Vector3.down * 15f;
			myTile = null;
			isActive = false;
			playTickTack = true;
			stopRender = false;
		}

		public override void PlayerInSight(PlayerManager player)
		{
			if (frownSequence || !isActive) // If it is about to play frown sequence
				return;

			if (Vector3.Distance(transform.position, player.transform.position) < frownDistance)
			{
				StartCoroutine(FrowningSequence()); // Not cool method :(
			}
			else
			{
				ChangeState(1);
			}
		}

		public override void PlayerLost(PlayerManager player)
		{
			if (frownSequence || !isActive)
				return;

			ChangeState(0);
		}
		private IEnumerator ActiveCooldown()
		{
			float timer = Random.Range(60f, 120f);
			while (timer > 0f)
			{
				if (frownSequence || !isActive)
					yield break;
				

				timer -= Time.deltaTime * ec.NpcTimeScale;
				yield return null;
			}
			frownSequence = true;
			StartCoroutine(RespawnSequence());
			yield break;
		}
		// Sequences
		private IEnumerator FrowningSequence()
		{
			float timer = 6f;
			frownSequence = true;
			audMan.FlushQueue(true);
			audMan.PlaySingle(aud_frown);
			ChangeState(2, true); // Frown State
			while (timer > 0f)
			{
				timer -= Time.deltaTime * ec.NpcTimeScale;
				yield return null;
			}
			ChangeState(3);
			PushNPCsToMySpot();
			audMan.PlaySingle(aud_Scream);
			while (audMan.IsPlaying) 
			{
				yield return null;
			}
			
			ChangeState(1);
			StartCoroutine(RespawnSequence());

			yield break;
		}

		private IEnumerator RespawnSequence()
		{
			// Animation to hide here
			isActive = false;
			float animationFrameRate = 0f;
			float holeTimer = 5f;
			Sprite[] animation = availableSpriteArrays[3];
			while (Mathf.FloorToInt(animationFrameRate) < animation.Length)
			{
				if (holeTimer > 0f && Mathf.FloorToInt(animationFrameRate) == 8)
				{
					holeTimer -= Time.deltaTime * ec.NpcTimeScale;
					yield return null;
					continue;
				}
				renderer.sprite = animation[Mathf.FloorToInt(animationFrameRate)];
				animationFrameRate += hideAnimationRate * Time.deltaTime * ec.NpcTimeScale;
				yield return null;
			}
			DespawnFromSpot();
			frownSequence = false;
			float timer = 60f;
			while (timer > 0f)
			{
				timer -= Time.deltaTime * ec.NpcTimeScale;
				yield return null;
			}
			StartCoroutine(SpawnAtRandomTile());
			yield break;
		}
		// End
		private void PushNPCsToMySpot() => ec.Npcs.Where(x => x != this).Do(x => x.transform.position = myTile.transform.position + Vector3.up * 5f);

		private void ChangeState(int state, bool force = false)
		{
			if (this.state == state && !force) // If it is already on state, no reason to change twice
				return;

			forceRender = true;
			this.state = state;
			switch (state)
			{
				case 0:
					spriteArray = availableSpriteArrays[0];
					maxUpdateLimit = 0.9f;
					break;
				case 1:
					spriteArray = availableSpriteArrays[1];
					maxUpdateLimit = 0.6f;
					break;
				case 2:
					spriteArray = null;
					maxUpdateLimit = 0f;
					renderer.sprite = frownSprite;
					stopRender = true;
					return;
				case 3:
					playTickTack = false;
					spriteArray = availableSpriteArrays[2];
					maxUpdateLimit = 0.2f;
					break;
				default:
					spriteArray = availableSpriteArrays[0];
					maxUpdateLimit = 0.9f;
					break;

				
			}
			stopRender = false;
			
		}

		private void FixedUpdate() // Updates sprite each tick
		{
			spriteUpdateCooldown += Time.deltaTime * ec.NpcTimeScale;
			if (isActive && spriteArray != null && !stopRender && (spriteUpdateCooldown > maxUpdateLimit || forceRender))
			{
				spriteUpdateCooldown = 0f;
				forceRender = false;
				currentSprite++;
				if (currentSprite >= spriteArray.Length)
					currentSprite = 0;
				renderer.sprite = spriteArray[currentSprite];
				if (playTickTack)
				{
					audMan.PlaySingle(ticktack ? aud_tick : aud_tock);
					ticktack = !ticktack;
				}
			}
		}


		private AudioManager audMan;

		private SoundObject aud_tick, aud_tock, aud_frown;

		private SoundObject aud_Scream;

		private List<WeightedSelection<TileController>> availableTiles = new List<WeightedSelection<TileController>>();

		private bool isActive = false, forceRender = false, stopRender = false, ticktack = false, playTickTack = true, frownSequence = false;

		private float spriteUpdateCooldown = 0f;

		private int currentSprite, state;

		private float maxUpdateLimit = 0.9f;

		private Sprite[] spriteArray;

		private Sprite frownSprite;

		private SpriteRenderer renderer;

		private readonly List<Sprite[]> availableSpriteArrays = new List<Sprite[]>();

		private TileController myTile;

		[SerializeField]
		private const float height = 5f;

		[SerializeField]
		private const float tileOffset = 4.9f, hideAnimationRate = 10f, frownDistance = 30f;


	}

}

