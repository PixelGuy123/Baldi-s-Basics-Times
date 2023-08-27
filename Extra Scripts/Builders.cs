using BB_MOD.ExtraComponents;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BB_MOD.Extra
{
	public delegate void ExtraBuilder(LevelBuilder lg, RoomController room, System.Random rng); // Important delegate, don't remove it.
	public static class ReplacementBuilders // This class will store every method that is applied for an existent builder (those methods must use the delegate above)
	{

		public static void CreateWallClocks(LevelBuilder lg, RoomController room, System.Random rng) // Always create your STATIC methods using these 4 parameters, so it is identified as the ExtraBuilder delegate
		{

			var clock = ContentManager.instance.CreatePosterObject("wall_clock.png");
			var tiles = room.GetTilesOfShape(new List<TileShape>() { TileShape.Single, TileShape.Corner }, true).Where(x => x.HasFreeWall);

			if (tiles.Count() > 0)
			{
				var tile = tiles.ElementAt(rng.Next(tiles.Count()));
				lg.Ec.BuildPoster(clock, tile, tile.RandomFreeDirection(rng));
				return;
			}

			Debug.LogWarning("Clock failed to spawn at classroom of pos: " + room.position.x + "," + room.position.z);


		}

		public static void CreateLightBulbs(LevelBuilder lg, RoomController room, System.Random rng)
		{
			if (rng.NextDouble() < 0.5d) return;

			if (!ContentManager.instance.TryGetDecorationTransform(RoomCategory.Faculty, true, "lightBulb", out Transform obj))
			{
				Debug.LogWarning("Failed to spawn light bulbs because the transform hasn\'t been found");
				return;
			}

			var tiles = room.GetTilesOfShape(ContentUtilities.Array(TileShape.Corner).ToList(), true);

			if (tiles.Count > 0)
			{
				foreach (var tile in tiles)
				{
					var newObj = UnityEngine.Object.Instantiate(obj, tile.transform);
					newObj.transform.localPosition = new Vector3(0f, 6.7f, 0f);
					EnvironmentExtraVariables.CreateLighting(tile, Mathf.Clamp(lg.ld.standardLightStrength - 8, 5, lg.ld.standardLightStrength)); // If you want to create a structure that makes lighting, USE THIS METHOD!!
				}
			}

		}

	}
	// Any other class below are actual builders that will be added to weight lists, please make sure to comment your user name on the class of your builder to identify who made it!
	// Those builders must inherate from ObjectBuilder
	public class WallBellBuilder : ObjectBuilder
	{
		public override void Build(EnvironmentController ec, RoomController room, System.Random cRng)
		{
			var tiles = room.GetTilesOfShape(new List<TileShape>() { TileShape.Corner, TileShape.Single }, false).Where(x => x.HasFreeWall);
			if (tiles.Count() == 0)
			{
				Debug.LogWarning("Wall Bell Builder has found no spots for bells to spawn");
				return;
			}
			int amount = (int)Math.Floor((double)tiles.Count() / (Math.Abs(ec.levelSize.x - ec.levelSize.z) + (EnvironmentExtraVariables.lb.seedOffset + 2)));
			for (int i = 0; i < amount; i++)
			{
				var tile = tiles.ElementAt(cRng.Next(tiles.Count()));
				ec.BuildPoster(posterPre, tile, tile.RandomFreeDirection(cRng));
				tile.containsWallObject = true;
				tiles = tiles.RemoveIn(tile);

				if (tiles.Count() == 0)
					break;
			}
		}

		[SerializeField]
		private readonly PosterObject posterPre = ContentManager.instance.CreatePosterObject("wallbell.png");
	}

	// Here are the custom room builders, which can either be a replacement for a existent builder, or a new builder for a new room type (this is planned and will be added soon!)

	public class LossyClassBuilder : RoomBuilder // Basically has messy chair spawns
	{
		public override void Setup(LevelBuilder lg, RoomController room, System.Random rng)
		{
			var decs = AccessTools.Field(typeof(RoomBuilder), "decorations"); // If you want to make an extra builder for an existent room, you have to use this to get it's decorations
																			  // On this line, it gets the field that has the decorations

			decs.SetValue(this, decs.GetValue(ContentUtilities.FindResourceObject<ClassBuilder>())); // then it sets the value of the decorations to the decorations the target builder has (in this case, classbuilder)

			base.Setup(lg, room, rng);
		}

		public override void Build()
		{
			base.Build();
			builder = Builder();
			StartCoroutine(builder);
		}

		private IEnumerator Builder()
		{
			while (!lg.DoorsFinished) { yield return null; }

			var deskSpawner = this.CreateSpawner(new WeightedTransform() { selection = teachDeskPre.transform, weight = 100 }, 1, 1, false, 25f);

			deskSpawner.StartSpawner(lg.Ec, new System.Random(cRNG.Next()));

			var chair = Instantiate(chairPre.transform);
			var itemSpawn = new GameObject("itemspawn");
			itemSpawn.transform.tag = "ItemSpawn";
			itemSpawn.transform.SetParent(chair);
			itemSpawn.transform.localPosition = new Vector3(0f, 1.3f, 0f);

			var spawner = this.CreateSpawner(new WeightedTransform() { selection = chair, weight = 70 }, Math.Min(room.size.x, room.size.z) * 2, Math.Max(room.size.x, room.size.z) * 2, false, 25f);
			spawner.StartSpawner(lg.Ec, new System.Random(cRNG.Next()));




			while (!spawner.Finished || !deskSpawner.Finished)
			{
				yield return null;
			}

			Destroy(chair.gameObject);

			room.AddItemSpawns(spawner.ObjectsSpawned);

			Vector3 deskPos = deskSpawner.ObjectsSpawned[0].transform.localPosition; // Sets a random position for the desk

			Activity activity = this.PlaceObject_RawPos(activityPre, deskPos, room.dir.ToRotation());
			activity.room = room;
			Notebook notebook = this.PlaceObject_RawPos(notebookPre, deskPos, default);
			lg.Ec.AddNotebook(notebook);
			Singleton<BaseGameManager>.Instance.AddNotebookTotal(1);
			activity.SetNotebook(notebook);
			notebook.gameObject.SetActive(false);
			building = false;
			yield break;
		}

		[SerializeField]
		readonly MeshRenderer chairPre = ContentUtilities.FindResourceObjectContainingName<MeshRenderer>("chair");

		[SerializeField]
		readonly private MeshRenderer teachDeskPre = ContentUtilities.FindResourceObjectContainingName<MeshRenderer>("bigdesk");

		[SerializeField]
		readonly private Activity activityPre = ContentUtilities.FindResourceObjectContainingName<NoActivity>("noactivity");

		[SerializeField]
		readonly private Notebook notebookPre = ContentUtilities.FindResourceObjectContainingName<Notebook>("notebook");
	}

	public class BathBuilder : RoomBuilder
	{
		public override void Setup(LevelBuilder lg, RoomController room, System.Random rng)
		{
			base.Setup(lg, room, rng);
		}

		public override void Build()
		{
			base.Build();
			builder = Builder();
			StartCoroutine(builder);
		}

		private IEnumerator Builder()
		{
			while (!lg.DoorsFinished) { yield return null; }
			List<Direction> list = Directions.All();
			if (room.size.x < 4)
			{
				list.Remove(Direction.East);
				list.Remove(Direction.West);
			}
			if (room.size.z < 4)
			{
				list.Remove(Direction.North);
				list.Remove(Direction.South);
			}

			if (list.Count == 0) goto endBuilder;



			Direction dir = list[cRNG.Next(list.Count)];
			TileController reservedCorner = null;

			var tiles = room.GetTilesOfShape(new List<TileShape>() { TileShape.Corner, TileShape.Single }, true).Where(x => x.wallDirections.Contains(dir));

			var corners = tiles.Where(x => x.shape == TileShape.Corner);

			if (corners.Count() == 0) goto onlySinks;


			reservedCorner = corners.ElementAt(cRNG.Next(corners.Count()));
			tiles = tiles.RemoveIn(reservedCorner).Where(x => !x.doorHere && !x.containsWallObject && !x.containsObject); // Filters again but with the right parameters, just because a reserved corner has to be there
			Direction fixatedDir = dir.PerpendicularList().Except(reservedCorner.wallDirections).First().GetOpposite(); // Gets the first direction that isn't from either of the wall directions and is accurately reflecting the right direction opposing the chosen corner
			List<Vector3> spots = new List<Vector3>(tiles.Select(x => x.transform.position));

			var obj = ContentUtilities.CreateBasicCube_WithObstacle(new Vector3(1f, 10f, 9.9f), new Vector3(1f, 5f, 1.3f), "bathToiletWalls.png");
			var doorObj = ContentUtilities.CreateBasicCube_WithObstacle(new Vector3(1f, 10f, 9.9f), new Vector3(1f, 5f, 1.3f), "BathDoor.png");



			foreach (var spot in spots)
			{
				var tile = lg.Ec.TileFromPos(spot);
				if (!tile.wallDirections.Contains(fixatedDir) && !lg.Ec.TileFromPos(tile.position + fixatedDir.ToIntVector2()).containsWallObject)
					this.PlaceObject_RawPos(obj, spot + (fixatedDir.ToVector3() * 5f), dir.GetOpposite().ToRotation());
				if (!tile.wallDirections.Contains(fixatedDir.GetOpposite()) && !lg.Ec.TileFromPos(tile.position + fixatedDir.GetOpposite().ToIntVector2()).containsWallObject)
					this.PlaceObject_RawPos(obj, spot + (fixatedDir.GetOpposite().ToVector3() * 5f), dir.GetOpposite().ToRotation());

				this.PlaceObject_RawPos(doorObj, spot + (dir.GetOpposite().ToVector3() * 4f), fixatedDir.ToRotation(), true, true); // Door

				tile.CoverAllWalls();

				yield return null;
			}

			// Proposital: Last row will always be empty, just to leave a space in case the player wants to hide
			// There will be always a tap there with item spawn set

			Destroy(obj.gameObject);
			Destroy(doorObj.gameObject);

		onlySinks:

			if (ContentManager.instance.TryGetDecorationTransform(room.category, true, "sink", out Transform sink))
			{
				var mirror = ContentManager.instance.CreatePosterObject("mirror.png"); // Create the mirror

				ContentUtilities.AddCollisionToSprite(sink.gameObject, new Vector3(1.5f, 5f, 1.5f), new Vector3(0f, sink.transform.position.y, 0f), new Vector3(2f, 5f, 2f)); // Creates a collider to the sprite

				sink.gameObject.layer = 2; // Ignore Raycast Layer, basically makes it invisible through npcs vision

				dir = dir.GetOpposite(); // Inverts the direction so it can be worked on the opposite side

				if (reservedCorner)
				{
					this.PlaceObject_RawPos(sink, reservedCorner.transform.position + (Vector3.down * sinkOffset), default, true, true);
					room.AddItemSpawn(reservedCorner.transform.position);
				}

				foreach (var spot in room.GetTilesOfShape(new List<TileShape>() { TileShape.Corner, TileShape.Single }, true).Where(x => x.wallDirections.Contains(dir) && !x.doorHere && !x.containsWallObject && !x.containsObject))
				{
					this.PlaceObject_RawPos(sink, spot.transform.position + (Vector3.down * sinkOffset), default, true, true);
					lg.Ec.BuildPoster(mirror, spot, dir);
				}



			}

		endBuilder:

			building = false;
			yield break;
		}

		const float sinkOffset = 3.2f;
	}

	public class AbandonedBuilder : RoomBuilder
	{
		public override void Setup(LevelBuilder lg, RoomController room, System.Random rng)
		{
			base.Setup(lg, room, rng);
		}

		public override void Build()
		{
			base.Build();
			builder = Builder();
			room.CreateRoomFunction<AbandonedRoomFunction>(true);
			StartCoroutine(builder);
		}

		private IEnumerator Builder()
		{
			while (!lg.DoorsFinished) { yield return null; }

			var corners = room.GetTilesOfShape(new List<TileShape>() { TileShape.Corner }, true);

			int amountOfCorners = cRNG.Next(1, corners.Count + 1);
			var allItems = ContentManager.instance.GlobalItems.ToArray();
			for (int i = 0;  i < amountOfCorners; i++)
			{
				lg.Ec.CreateItem(room, WeightedItemObject.ControlledRandomSelection(allItems, cRNG), corners[i].transform.position + Vector3.up * 5f);
			}

			foreach (var door in room.doors)
			{
				door.Lock(true); // Locks those doors
				door.gameObject.AddComponent<StandardDoor_ExtraFunctions>().AssignFuncToUnlock(false);
				yield return null;
			}

			building = false;
			yield break;
		}
	}

	public class AbandonedRoomFunction : RoomFunction
	{
		public override void Initialize(RoomController room)
		{
			base.Initialize(room);
			this.room = room;
			leaveUnlocked = false;
		}
		private void Update()
		{
			if (leaveUnlocked) return;

			if (EnvironmentExtraVariables.IsEndGame || forceUnlock)
			{
				leaveUnlocked = true;
				forceUnlock = false;
				foreach (var door in room.doors)
				{
					if (door.locked)
						door.Unlock();
				}
				return;
			}

			foreach (var door in room.doors)
			{
				if (!door.locked)
					door.Lock(true); // Locks those doors
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			if (other.tag == "Player" && !leaveUnlocked)
			{
				forceUnlock = true;
			}
		}

		RoomController room;

		bool leaveUnlocked = true;

		bool forceUnlock = false;
	}

	// ===================== Special Room Creators ===================================

	public static class CustomSpecialRoom_Extensions
	{
		/// <summary>
		/// If your custom special room has higher ceilings and supports elevators, this should be always ran in AfterUpdatingTIles() method to fix the elevator's ceiling. The <paramref name="ceilingTex"/> MUST BE 256x256
		/// </summary>
		/// <param name="room"></param>
		/// <param name="ceilingTex"></param>
		public static void FixElevatorTiles(this SpecialRoomCreator room, Texture2D ceilingTex)
		{
			try
			{
				var tiles = new List<TileController>();

				foreach (var tilePos in EnvironmentExtraVariables.elevatorTilePositions)
				{
					var tile = room.Room.ec.TileFromPos(tilePos);
					if (ReferenceEquals(tile.room, room.Room))
						tiles.Add(tile); // Adds the tile to the list to replace the ceiling later
					
				}

				if (tiles.Count == 0)
					return;

				var textureAtlas = new Texture2D(512, 512, TextureFormat.RGBA32, false)
				{
					filterMode = FilterMode.Point
				};

				// Makes a new texture atlas for the tiles

				Color[] array;
				array = MaterialModifier.GetColorsForTileTexture(room.Room.floorTex, 256);
				textureAtlas.SetPixels(0, 0, 256, 256, array);
				array = MaterialModifier.GetColorsForTileTexture(room.Room.wallTex, 256);
				textureAtlas.SetPixels(256, 256, 256, 256, array);
				array = MaterialModifier.GetColorsForTileTexture(ceilingTex, 256);
				textureAtlas.SetPixels(0, 256, 256, 256, array);
				textureAtlas.Apply();
				var baseMat = UnityEngine.Object.Instantiate(room.Room.baseMat); // Instantiates the material used by room
				baseMat.SetTexture("_MainTex", textureAtlas);

				foreach (var tile in tiles)
				{
					if (ContentManager.instance.DebugMode)
						Debug.Log("Tiles fixed: " + tile.position.x + "," + tile.position.z);

					tile.SetBase(baseMat); // Sets the base back with new texture atlas
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				Debug.LogWarning(room.name + " failed to fix the elevator ceiling texture");
			}
		}
	}

	public class BasketBallArea : SpecialRoomCreator // Use the BasketBallArea classes as reference for your own special room
	{
		public override void Initialize(EnvironmentController ec)
		{
			base.Initialize(ec);
			lg.AddSpecialRoomToExpand(room);
			
		}
		public override void BeforeUpdatingTiles()
		{
			base.BeforeUpdatingTiles();
			for (int i = 0; i < 4; i++)
			{
				lg.AddRandomDoor(room, doorPre, false, true);
			}

			
		}
		public override void AfterUpdatingTiles()
		{
			base.AfterUpdatingTiles();

			this.FixElevatorTiles(ContentAssets.GetAsset<Texture2D>("defaultSaloonTexture")); // Note, this MUST BE HERE ON "AfterUpdatingTiles()" IF YOUR ROOM HAS HIGHER CEILING, SO ELEVATOR SUPPORTS THE TEXTURE FROM IT

			this.CreateOpenAreaForSpecialRoom(room, ContentAssets.GetAsset<Texture2D>("defaultSaloonTexture"), ContentAssets.GetAsset<Texture2D>("defaultSaloonTexture"), 5); // Creates the beautiful huge walls of the special room (only for higherCeilings!)

			lg.IntegrateRoomBuilder(room, GetComponent<BasketBallBuilder>());

			foreach (var tile in room.GetNewTileList())
			{
				room.ec.GenerateLight(tile, lg.ld.standardLightColor, lg.ld.standardLightStrength);
			}

			room.functionObject.GetComponent<RuleFreeZone>().Initialize(room);
		}

		readonly Door doorPre = ContentUtilities.SwingingDoor;
	}

	public class BasketBallBuilder : RoomBuilder
	{
		public override void Setup(LevelBuilder lg, RoomController room, System.Random rng)
		{
			base.Setup(lg, room, rng);
			if (ContentManager.instance.TryGetDecorationTransform(room.category, true, "basketHoop", out Transform obj))
			{
				basketHoop = obj;
				ContentUtilities.AddCollisionToSprite(obj.gameObject, new Vector3(4f, 15f, 4f), new Vector3(2f, 5f, -1f), new Vector3(4f, 15f, 4f)); // Creates a collider to the sprite
			}

			if (ContentManager.instance.TryGetDecorationTransform(room.category, true, "basketLotsOfBalls", out Transform obj2))
			{
				basketBalls = obj2;
			}

			if (ContentManager.instance.TryGetDecorationTransform(room.category, true, "BaldiBall", out Transform obj3))
			{
				BALDIBBALL = obj3;
			}

		}

		public override void Build()
		{
			base.Build();
			builder = Builder();
			StartCoroutine(builder);
		}

		private IEnumerator Builder()
		{
			if (basketHoop)
			{
				var pos = lg.Ec.RealRoomMid(room);

				var ogPos = pos;
				if (room.size.x > room.size.z)
				{
					pos.x += ((room.size.x / 2) - 3) * 10f;
					_ = this.PlaceObject_RawPos(basketHoop, pos, default);
					pos = ogPos;
					pos.x -= ((room.size.x / 2) - 3) * 10f;
					_ = this.PlaceObject_RawPos(basketHoop, pos, default);
				}
				else
				{
					pos.z += ((room.size.z / 2) - 3) * 10f;
					_ = this.PlaceObject_RawPos(basketHoop, pos, default);
					pos = ogPos;
					pos.z -= ((room.size.z / 2) - 3) * 10f;
					_ = this.PlaceObject_RawPos(basketHoop, pos, default);
				}
			}

			if (basketBalls)
			{
				var collider = basketBalls.gameObject.AddComponent<BoxCollider>(); // Temporary collision for it to avoid spawning too close to walls
				collider.size = new Vector3(3f, 1f, 3f);
				var spawner = this.CreateSpawner(ContentUtilities.Array(
					new WeightedTransform() { selection = basketBalls, weight = 100 },
					new WeightedTransform() { selection = BALDIBBALL, weight = 25 }
					), 5, 7, false);
				spawner.StartSpawner(lg.Ec, cRNG);

				while (!spawner.Finished) { yield return null; }

				foreach (var balls in spawner.ObjectsSpawned)
				{
					Destroy(balls.GetComponent<BoxCollider>());
				}
			}

			building = false;
			yield break;
		}

		Transform basketHoop;

		Transform basketBalls;

		Transform BALDIBBALL;
	}

}
