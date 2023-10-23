using BB_MOD.ExtraComponents;
using HarmonyLib;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Patches.Main;

namespace BB_MOD.Builders
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

			WeightedTransform[] lights = new WeightedTransform[2];

			if (!ContentManager.instance.TryGetDecorationTransform(RoomCategory.Faculty, true, "lightBulb", out Transform obj))
			{
				Debug.LogWarning("Failed to spawn light bulbs because the transform hasn\'t been found");
				return;
			}

			lights[0] = ContentUtilities.GetWeightedTransform(obj, 50);

			if (!ContentManager.instance.TryGetDecorationTransform(RoomCategory.Faculty, true, "lamp", out Transform obj2))
			{
				Debug.LogWarning("Failed to spawn lamps because the transform hasn\'t been found");
				return;
			}

			lights[1] = ContentUtilities.GetWeightedTransform(obj2, 50);

			var tiles = room.GetTilesOfShape(ContentUtilities.Array(TileShape.Corner).ToList(), true);

			var lamp = WeightedTransform.ControlledRandomSelection(lights, rng);

			if (tiles.Count > 0)
			{
				foreach (var tile in tiles)
				{
					var newObj = UnityEngine.Object.Instantiate(lamp, tile.transform);
					ContentUtilities.AddCollisionToSprite(newObj.gameObject, new Vector3(2.5f, 10f, 2.5f), Vector3.zero, new Vector3(2.5f, 10f, 2.5f));
					EnvironmentExtraVariables.CreateLighting(tile, Mathf.Max(5, lg.ld.standardLightStrength - 8)); // If you want to create a structure that makes lighting, USE THIS METHOD!!
					tile.containsObject = true;
				}
			}

		}

		public static void CreateTrashCans(LevelBuilder lg, RoomController room, System.Random rng)
		{
			if (rng.NextDouble() < 0.7d) return; // Chance to spawn

			var tiles = room.GetTilesOfShape(new List<TileShape>() { TileShape.Corner }, true).Where(x => !x.containsObject);
			int tileCount = tiles.Count();

			if (tileCount == 0) return; // If there's available corner for it

			var pos = tiles.ElementAt(rng.Next(0, tileCount));
			PrefabInstance.SpawnPrefab<TrashCan>(pos, lg.Ec);

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

	public class BananaTreeBuilder : ObjectBuilder
	{
		public override void Build(EnvironmentController ec, RoomController room, System.Random cRng)
		{
			List<RoomController> targetRooms = new List<RoomController>();
			var targetObstacle = ContentManager.instance.customObstacleEnums.GetObstacleByName("ForestArea");
			foreach (var specialRoom in FindObjectsOfType<SpecialRoomCreator>())
			{
				if (specialRoom.obstacle == targetObstacle)
				{
					targetRooms.Add(specialRoom.Room);
				}
			}

			targetRooms.AddRange(FindObjectsOfType<TripEntrance>().Select(x => x.Room));

			if (targetRooms.Count == 0) return;

			foreach (var target in targetRooms)
			{
				var tiles = target.GetTilesOfShape(new List<TileShape> { TileShape.Corner }, true);
				if (tiles.Count == 0) continue; // Skips if no corner is found

				int amount = WeightedSelection<int>.ControlledRandomSelection(amounts, cRng);
				for (int i = 0; i < amount; i++)
				{
					int index = cRng.Next(tiles.Count);
					PrefabInstance.SpawnPrefab<BananaTree>(tiles[index], ec);
					tiles.RemoveAt(index);
					if (tiles.Count == 0) break;
				}
			}

		}

		readonly WeightedSelection<int>[] amounts = new WeightedSelection<int>[3] {
			new WeightedSelection<int>() {selection = 1, weight = 100},
			new WeightedSelection<int>() {selection = 2, weight = 75},
			new WeightedSelection<int>() {selection = 3, weight = 45}
		};
	}

	public class TrapDoorBuilder : ObjectBuilder
	{
		public override void Build(EnvironmentController ec, RoomController room, System.Random cRng)
		{
			var tiles = room.GetTilesOfShape(new List<TileShape>() { TileShape.Corner, TileShape.End }, false).Where(x => !x.containsObject).ToList();
			if (tiles.Count == 0) return;
			var currentFloor = EnvironmentExtraVariables.currentFloor;

			int amount = cRng.Next(trapdoorAmounts[currentFloor][0], trapdoorAmounts[currentFloor][1] + 1);
			int allowedLinkeds = trapdoorAmounts[currentFloor][2];
			int allowedRandoms = trapdoorAmounts[currentFloor][3];
			for (int i = 0; i < amount; i++)
			{
				if (tiles.Count == 0 || (allowedLinkeds <= 0 && allowedRandoms <= 0)) break;
				int idx = cRng.Next(tiles.Count);
				if (allowedLinkeds > 0 && tiles.Count > 1 && cRng.NextDouble() >= 0.5f)
				{ // Linked trapdoor
					
					var firstTrapdoor = PrefabInstance.SpawnPrefab<Trapdoor>(tiles[idx], ec, false);
					tiles[idx].containsObject = true;
					tiles.RemoveAt(idx);
					idx = cRng.Next(tiles.Count);
					var secondTrapdoor = PrefabInstance.SpawnPrefab<Trapdoor>(tiles[idx], ec, false);
					tiles[idx].containsObject = true;
					tiles.RemoveAt(idx);

					firstTrapdoor.SetTrapdoorLink(secondTrapdoor);
					secondTrapdoor.SetTrapdoorLink(firstTrapdoor);

					firstTrapdoor.transform.position += Vector3.down * 5f;
					secondTrapdoor.transform.position += Vector3.down * 5f;

					firstTrapdoor.SetAlreadyActive();
					secondTrapdoor.SetAlreadyActive();

					firstTrapdoor.Execute();
					secondTrapdoor.Execute();

					allowedLinkeds--;
				}
				else if (allowedRandoms > 0)
				{ // Random Trap door
					var firstTrapdoor = PrefabInstance.SpawnPrefab<Trapdoor>(tiles[idx], ec, false);
					firstTrapdoor.transform.position += Vector3.down * 5f;
					tiles[idx].containsObject = true;
					tiles.RemoveAt(idx);

					firstTrapdoor.SetAlreadyActive();

					firstTrapdoor.Execute();

					allowedRandoms--;
				}
			}
		}

		readonly Dictionary<Floors, int[]> trapdoorAmounts = new Dictionary<Floors, int[]>()
		{
			{Floors.F1, ContentUtilities.Array(0, 0, 0, 0)  }, // first 2 numbers: how many trapdoors. third number: how many linked trapdoors allowed. fourth number: how many random trapdoors allowed
			{Floors.F2, ContentUtilities.Array(1, 2, 2, 0)  },
			{Floors.F3, ContentUtilities.Array(1, 3, 1, 3)  },
			{Floors.END, ContentUtilities.Array(2, 3, 3, 1)  }
		};
	}

	// Here are the custom room builders, which can either be a replacement for a existent builder, or a new builder for a new room type (this is planned and will be added soon!)

	public class LossyClassBuilder : RoomBuilder // Basically has messy chair spawns, don't judge the "lossy" name lol
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

				var door = this.PlaceObject_RawPos(doorObj, spot + (dir.GetOpposite().ToVector3() * 4f), fixatedDir.ToRotation(), true, true); // Door

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

	public class ComputerRoomBuilder : RoomBuilder
	{
		public override void Setup(LevelBuilder lg, RoomController room, System.Random rng)
		{
			base.Setup(lg, room, rng);
			var buffer = ContentUtilities.CreateBasicCube(new Vector3(9.9f, 1f, 9.9f), "computerTableTex.png");
			buffer.gameObject.layer = ContentUtilities.defaultIgnoreRaycastLayer;
			desk = new GameObject("TheLongTable").transform;

			ContentUtilities.AddNavigationCollisionToObject(desk.gameObject, new Vector3(11f, 10f, 11f));

			buffer.SetParent(desk);
			buffer.localPosition = Vector3.up * 3f;

			var legs = new Transform[] { ContentUtilities.CreateBasicCube(new Vector3(1f, 3f, 1f), "black.png"), 
				ContentUtilities.CreateBasicCube(new Vector3(1f, 3f, 1f), "black.png"), 
				ContentUtilities.CreateBasicCube(new Vector3(1f, 3f, 1f), "black.png"), 
				ContentUtilities.CreateBasicCube(new Vector3(1f, 3f, 1f), "black.png") };

			float multiplier = 3f;

			legs.Do(x => x.SetParent(desk));
			legs[0].localPosition = new Vector3(desk.localScale.x * multiplier, 1f, desk.localScale.z * multiplier);
			legs[1].localPosition = new Vector3(-desk.localScale.x * multiplier, 1f, -desk.localScale.z * multiplier);
			legs[2].localPosition = new Vector3(-desk.localScale.x * multiplier, 1f, desk.localScale.z * multiplier);
			legs[3].localPosition = new Vector3(desk.localScale.x * multiplier, 1f, -desk.localScale.z * multiplier); // Sets the legs for a corner

			if (ContentManager.instance.TryGetDecorationTransform(room.category, true, "computer", out var comp))
			{
				computer = comp;
			}

			compPoster = ContentManager.instance.CreatePosterObject("ComputerPoster.png");


			room.acceptsPosters = false; // Only the computer poster is acceptable!!
		}

		public override void Build()
		{
			base.Build();
			builder = Builder();
			StartCoroutine(builder);
		}

		private IEnumerator Builder()
		{
			if (!desk)
			{
				Debug.LogWarning("Computer room failed to get desk prefab");
				building = false;
				yield break;
			}

			while (!lg.DoorsFinished) { yield return null; }

			var startDirs = Directions.All();

			if (room.size.x > room.size.z)
			{
				startDirs.Remove(Direction.North);
				startDirs.Remove(Direction.South);
			}
			else if (room.size.z > room.size.x)
			{
				startDirs.Remove(Direction.East);
				startDirs.Remove(Direction.West);
			}
			TileController corner = null;
			Direction dir = Direction.Null;

			while (corner == null && startDirs.Count > 0)
			{
				int index = cRNG.Next(startDirs.Count);
				dir = startDirs[index];

				startDirs.RemoveAt(index);

				var tiles = room.GetTilesOfShape(new List<TileShape>() { TileShape.Corner }, true);

				foreach (var tile in tiles)
				{
					if (tile.wallDirections.Contains(dir.GetOpposite()))
					{
						corner = tile;
						break;
					}
				}
			}

			if (corner == null || dir == Direction.Null)
			{
				Debug.LogWarning("Computer room failed to find a suitable corner");
				building = false;
				yield break;
			}

			var rightDirs = dir.PerpendicularList();

			var rightDir = Direction.Null;

			for (int i = 0; i < rightDirs.Count; i++) 
			{
				if (corner.wallDirections.Contains(rightDirs[i]))
				{
					rightDir = rightDirs[i].GetOpposite();
					break;
				}
			}

			if (ContentManager.instance.DebugMode)
			{
				Debug.Log("COMPUTER ROOM: Current Direction: " + dir);
				Debug.Log("COMPUTER ROOM: Side Direction: " + rightDir);
			}
			

			List<TileController> startingTiles = new List<TileController>();
			IntVector2 curTile = corner.position;

			while (room.ec.ContainsCoordinates(curTile) && room.ec.TileFromPos(curTile) != null && room.ec.TileFromPos(curTile).TileMatches(room))
			{
				
				startingTiles.Add(room.ec.TileFromPos(curTile + dir.ToIntVector2()));
				curTile += rightDir.ToIntVector2() * 2;
			}

			if (ContentManager.instance.DebugMode)
				startingTiles.ForEach(x => Debug.Log("COMPUTER ROOM: TileStartingPos: " + x.position.GetString()));

			foreach (var tile in startingTiles)
			{
				curTile = tile.position;
				while (room.ec.ContainsCoordinates(curTile) && room.ec.TileFromPos(curTile) != null && room.ec.TileFromPos(curTile).TileMatches(room) && !room.ec.TileFromPos(curTile).wallDirections.Contains(dir)) // Make a 1 tile gap between every table specifically for that
				{

					if (!room.ec.TileFromPos(curTile).doorHere)
					{
						var table = this.PlaceObject_RawPos(desk, room.ec.TileFromPos(curTile).transform.position, dir.ToRotation(), true, forceYPosition: true);
						if (computer)
						{
							var comp = Instantiate(computer);
							comp.SetParent(table);
							comp.localPosition = Vector3.up * 5.2f;
						}
					}

					curTile += dir.ToIntVector2();
					yield return null;
				}
			}

			var walls = room.GetTilesOfShape(new List<TileShape>() { TileShape.Single, TileShape.Corner }, true).Where(x => x.HasFreeWall).ToList();
			var posterTile = walls[cRNG.Next(walls.Count)];
			room.ec.BuildPoster(compPoster, posterTile, posterTile.wallDirections[cRNG.Next(posterTile.wallDirections.Length)]); // Creates a computer poster on a random spot

			List<TileController> machineSpots = new List<TileController>();

			foreach (var tile in room.GetNewTileList())
			{
				if (!tile.doorHere && tile.wallDirections.Length > 0 && !tile.containsObject)
				{
					machineSpots.Add(tile);
				}
			}

			if (machineSpots.Count > 0)
			{
				var spot = machineSpots[cRNG.Next(machineSpots.Count)];
				dir = spot.wallDirections[cRNG.Next(spot.wallDirections.Length)];
				PrefabInstance.SpawnPrefab<FogMachine>(spot.transform.position + Vector3.up * 5f + dir.ToVector3() * ContentUtilities.TileOffset, dir.ToRotation(), room.ec, false);
				spot.CoverWall(dir, true);
			}

			yield return null;

			building = false;
			Destroy(desk.gameObject);
			Destroy(compPoster);
			yield break;
		}

		private Transform desk;

		private Transform computer;

		private PosterObject compPoster;
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
					var tile = room.Room.ec.TileFromPos(tilePos.Key);
					if (tile.TileMatches(room.Room))
						tiles.Add(tile); // Adds the tile to the list to replace the ceiling later
					
				}

				if (tiles.Count == 0)
					return;

				var textureAtlas = new Texture2D(512, 512, TextureFormat.RGBA32, false)
				{
					filterMode = FilterMode.Point
				};

				// Makes a new texture atlas for the tiles

				textureAtlas.SetPixels(0, 0, 256, 256, MaterialModifier.GetColorsForTileTexture(room.Room.floorTex, 256));
				textureAtlas.SetPixels(256, 256, 256, 256, MaterialModifier.GetColorsForTileTexture(room.Room.wallTex, 256));
				textureAtlas.SetPixels(0, 256, 256, 256, MaterialModifier.GetColorsForTileTexture(ceilingTex, 256));
				textureAtlas.Apply();

				foreach (var tile in tiles)
				{
					if (ContentManager.instance.DebugMode)
						Debug.Log("Tiles fixed: " + tile.position.x + "," + tile.position.z);

					tile.mesh.material.mainTexture = textureAtlas; // Only sets the texture, not the material itself (must fix now this issue)
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
			AfterGen.QueueElevatorFix(this, ContentAssets.GetAsset<Texture2D>("defaultSaloonTexture")); // Put this in Initialize() if your special room has higher ceilings (if you want your elevator to have ceiling aswell)
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

			this.CreateOpenAreaForSpecialRoom(room, ContentAssets.GetAsset<Texture2D>("defaultSaloonTexture"), ContentAssets.GetAsset<Texture2D>("defaultSaloonTexture"), 5); // Creates the beautiful huge walls of the special room (only for higherCeilings!)

			lg.IntegrateRoomBuilder(room, GetComponent<BasketBallBuilder>());

			foreach (var tile in room.GetNewTileList())
			{
				room.ec.GenerateLight(tile, lg.ld.standardLightColor, lg.ld.standardLightStrength);
			}


			room.functionObject.GetComponent<RuleFreeZone>().Initialize(room);

			var tiles = room.GetTilesOfShape(new List<TileShape>() { TileShape.Open }, true).Where(x => !x.containsObject);
			room.ec.CreateItem(room, 
				ContentManager.instance.GetItemByEnum(ContentManager.instance.customItemEnums.GetItemByName("Basketball")), 
				tiles.ElementAt(cRNG.Next(tiles.Count())).transform.position + Vector3.up * 5f); // At least just one basketball per special room


		}

		readonly Door doorPre = ContentUtilities.SwingingDoor;
	}

	public class ForestArea : SpecialRoomCreator // Use the BasketBallArea classes as reference for your own special room
	{
		public override void Initialize(EnvironmentController ec)
		{
			base.Initialize(ec);
			lg.AddSpecialRoomToExpand(room);

		}
		public override void BeforeUpdatingTiles()
		{
			base.BeforeUpdatingTiles();
			SetDoorsForOppositeSides(doorPre);
			for (int i = 0; i < 2; i++)
			{
				lg.AddRandomDoor(room, doorPre, false, true);
			}

			room.acceptsPosters = false; // Nuh uh

		}
		public override void AfterUpdatingTiles()
		{
			base.AfterUpdatingTiles();

			this.CreateOpenAreaForSpecialRoom(room, ContentAssets.GetAsset<Texture2D>("nightSky"), ContentAssets.GetAsset<Texture2D>("nightSky"), 6); // Creates the beautiful huge walls of the special room (only for higherCeilings!)

			lg.IntegrateRoomBuilder(room, GetComponent<ForestAreaBuilder>());

			room.functionObject.GetComponent<ForestAreaFunction>().Initialize(room);

			var box = room.functionObject.AddComponent<VA_Box>();
			box.BoxCollider = room.functionObject.GetComponent<BoxCollider>();
			this.CreateMoveableAudioSource(box, out var audio, 30f, 50f);
			audio.QueueAudio(ContentAssets.GetAsset<SoundObject>("cricketsAmbience"));
			audio.SetLoop(true);
			

		}

		readonly Door doorPre = ContentUtilities.SwingingDoor;
	}

	public class ForestAreaBuilder : RoomBuilder
	{
		public override void Setup(LevelBuilder lg, RoomController room, System.Random rng)
		{
			base.Setup(lg, room, rng);
			GameObject buffer;
			if (ContentManager.instance.TryGetDecorationTransform(room.category, true, "forestTree", out var obj))
			{
				buffer = ContentUtilities.AddBasicBuffer(obj);
				ContentUtilities.AddCollisionToSprite(buffer, new Vector3(2.8f, 10f, 2.8f), Vector3.zero, new Vector3(3.7f, 10f, 3.7f));
				forestTrees[0] = new WeightedTransform() { selection = obj, weight = 100 };
			}

			if (ContentManager.instance.TryGetDecorationTransform(room.category, true, "forestTreeEasterEgg", out var obj2))
			{
				buffer = ContentUtilities.AddBasicBuffer(obj2);
				ContentUtilities.AddCollisionToSprite(buffer, new Vector3(2.8f, 10f, 2.8f), Vector3.zero, new Vector3(3.7f, 10f, 3.7f));
				forestTrees[1] = new WeightedTransform() { selection = obj2, weight = 1};
			}

			if (ContentManager.instance.TryGetDecorationTransform(room.category, true, "FireStatic", out var fire))
			{
				ContentUtilities.AddCollisionToSprite(fire.gameObject, new Vector3(3.5f, 10f, 3.5f), Vector3.zero, new Vector3(4.5f, 10f, 4.5f));
				this.fire = fire;
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
			if (forestTrees.Any(x => !x.selection))
			{
				building = false;
				Debug.LogWarning("Forest Tree failed to spawn trees because they don\'t exist");
				yield break;
			}

			List<TileController> reservedTiles = new List<TileController>();

			Vector3 firePos = room.ec.RealRoomMid(room);
			TileController fireTile = room.ec.TileFromPos(firePos);

			reservedTiles.Add(fireTile);

			var fireObj = this.PlaceObject_RawPos(fire, firePos, default, true, false, true);
			ContentUtilities.CreateMusicManager(fireObj.gameObject, 20f, 40f, ContentAssets.GetAsset<SoundObject>("fireNoises"));

			room.ec.GenerateLight(fireTile, new Color(0.9960f, 0.5f, 0f), Mathf.Max(3, lg.ld.standardLightStrength / 2)); // Orange Color

			int max = Mathf.Max(room.size.x, room.size.z) * 3;

			var spawner = this.CreateSpawner(forestTrees, Mathf.Max(1, max - 1), max, false, 0);

			spawner.StartSpawner(room.ec, cRNG); // Always remember of putting this, bruuuh

			while (!spawner.Finished) { yield return null; }

			DarkRoom(reservedTiles);

			building = false;
			yield break;
		}

		private void DarkRoom(List<TileController> reservedTiles)
		{
			foreach (var tile in room.GetNewTileList())
			{
				if (!reservedTiles.Contains(tile))
					room.ec.GenerateLight(tile, lg.ld.standardLightColor, 0); // No light, just a dark room
			}
		}

		readonly WeightedTransform[] forestTrees = new WeightedTransform[2];
		Transform fire;
	}

	public class ForestAreaFunction : RoomFunction
	{
		public override void Initialize(RoomController room)
		{
			base.Initialize(room);
			overlay = ContentUtilities.GumOverlay; // Gets the gum overlay
			overlay.name = "Forest_DarkOverlay";
			overlay.transform.SetParent(transform);
			overlay.transform.Find("Image").GetComponent<Image>().sprite = ContentAssets.GetAsset<Sprite>("darkOverlay");
		}

		private void OnTriggerEnter(Collider other)
		{
			if (other.tag == "NPC" && other.isTrigger)
			{
				var looker = other.GetComponent<Looker>();
				if (looker)
				{
					var token = new LookerDistancingPatch.LookerToken(20f, looker);
					LookerDistancingPatch.lookerModifiers.Add(token);
					lookers.Add(token);
				}
			}
			if (other.tag == "Player")
			{
				StartCoroutine(EnvironmentExtraVariables.SmoothFOVSlide(4f, token, -30f));
				overlay.SetActive(true);
			}
		}

		private void OnTriggerExit(Collider other)
		{
			if (other.tag == "NPC" && other.isTrigger)
			{
				var looker = other.GetComponent<Looker>();
				
				if (looker)
				{
					int index = lookers.FindIndex(x => ReferenceEquals(x.Target, looker));
					if (index >= 0)
					{
						LookerDistancingPatch.lookerModifiers.Remove(lookers[index]);
						lookers.RemoveAt(index);
					}
				}
			}
			if (other.tag == "Player")
			{
				StartCoroutine(EnvironmentExtraVariables.SmoothFOVSlide(4f, token, removeAfter:true));
				overlay.SetActive(false);
			}
		}

		readonly FOVToken token = new FOVToken(0f, 10);

		GameObject overlay;

		readonly List<LookerDistancingPatch.LookerToken> lookers = new List<LookerDistancingPatch.LookerToken>();
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
			GameObject buffer;
			if (ContentManager.instance.TryGetDecorationTransform(room.category, true, "basketLotsOfBalls", out Transform obj2))
			{
				buffer = ContentUtilities.AddBasicBuffer(obj2);
				ContentUtilities.AddCollisionToSprite(buffer, new Vector3(3f, 5f, 3f), Vector3.zero);
				basketBalls = obj2;
			}

			if (ContentManager.instance.TryGetDecorationTransform(room.category, true, "BaldiBall", out Transform obj3))
			{
				buffer = ContentUtilities.AddBasicBuffer(obj3);
				ContentUtilities.AddCollisionToSprite(buffer, new Vector3(3f, 5f, 3f), Vector3.zero);
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
					this.PlaceObject_RawPos(basketHoop, pos, default);
					pos = ogPos;
					pos.x -= ((room.size.x / 2) - 3) * 10f;
					this.PlaceObject_RawPos(basketHoop, pos, default);
				}
				else
				{
					pos.z += ((room.size.z / 2) - 3) * 10f;
					this.PlaceObject_RawPos(basketHoop, pos, default);
					pos = ogPos;
					pos.z -= ((room.size.z / 2) - 3) * 10f;
					this.PlaceObject_RawPos(basketHoop, pos, default);
				}
			}

			if (basketBalls)
			{
				var spawner = this.CreateSpawner(ContentUtilities.Array(
					new WeightedTransform() { selection = basketBalls, weight = 100 },
					new WeightedTransform() { selection = BALDIBBALL, weight = 25 }
					), 5, 7, false, 0f);
				spawner.StartSpawner(lg.Ec, cRNG);

				while (!spawner.Finished) { yield return null; }

				foreach (var balls in spawner.ObjectsSpawned)
				{
					Destroy(balls.transform.Find("Buffer").gameObject);
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
