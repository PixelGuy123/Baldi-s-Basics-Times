using BB_MOD.Events;
using BB_MOD.Builders;
using BB_MOD.ExtraComponents;
using BB_MOD.ExtraItems;
using BB_MOD.NPCs;
using BB_MOD;
using HarmonyLib;
using MonoMod.Utils;
using MTM101BaldAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Patches.Main
{

	[HarmonyPatch(typeof(NameManager), "Awake")]
	internal class ThisisWhereAllBegins // Yup, most important patch by now
	{
		private static void Prefix()
		{
			if (HasDone) return;

			HasDone = true;
			// Add custom posters
			var ogLds = new LevelObject[3];
			try
			{


				ogLds[0] = ContentUtilities.FindResourceObjectWithName<LevelObject>("Main1");
				ogLds[1] = ContentUtilities.FindResourceObjectWithName<LevelObject>("Main2");
				ogLds[2] = ContentUtilities.FindResourceObjectWithName<LevelObject>("Main3");

				foreach (var ld in ogLds)
				{
					ContentManager.instance.AddLevelObject(UnityEngine.Object.Instantiate(ld));
					ld.previousLevels = ContentManager.instance.GetLevelObjectCopy(ld); 
					// Basically replaces the previousLevels variable that is used on the npcs spawn, so npcs from specific floor aren't includes on unexpected ones
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				Debug.LogWarning("Failed to initialize mod, please report this to the developer");
				return;
			}

			var lds = new List<LevelObject>(ogLds)
			{
				ContentUtilities.FindResourceObjectWithName<LevelObject>("Endless1") // Includes endless this time
			};

			// Setting Special Room

			ContentManager.Prefabs.specialRoomPre = ContentUtilities.FindResourceObject<CafeteriaCreator>();

			ContentManager.Prefabs.windowPre = ContentUtilities.FindResourceObjectContainingName<WindowObject>("wood");

			ContentManager.Prefabs.iconPre = ContentUtilities.FindResourceObject<Notebook>().iconPre;

			bool canDoIt = true;

			if (!ContentManager.Prefabs.posterPre)
			{
				try
				{
					ContentManager.Prefabs.posterPre = lds[0].posters[0].selection;
					ContentManager.instance.SetupPosterWeights();
				}
				catch
				{
					Debug.LogWarning("Unable to grab a poster object for instancing, custom posters won\'t be added to the level");
					canDoIt = false;
				}
			}

			if (canDoIt)
				ContentManager.Prefabs.decorationPre = ((WeightedTransform[])AccessTools.Field(typeof(RoomBuilder), "decorations").GetValue(lds[0].facultyBuilders[0].selection))[0].selection; // Get decoration
			canDoIt = true;

			try
			{
				ContentManager.Prefabs.beans = Character.Beans.GetFirstInstance().gameObject; // Finds the first Beans instance to be used
			}
			catch
			{
				Debug.LogWarning("Beans somehow doesn\'t exist on the npc list, the mod won\'t spawn new npcs");
				canDoIt = false;
			}

			if (canDoIt)
				ContentManager.instance.SetupWeightNPCValues(); // Npc Setup

			ContentManager.instance.SetupObjectBuilders(); // Object Builder Setup

			ContentManager.instance.SetupItemWeights(); // Item Setup

			ContentManager.instance.SetupEventWeights(); // Event setup

			ContentManager.instance.SetupSchoolTextWeights(); // Custom Textures

			var sweep = Resources.FindObjectsOfTypeAll<GottaSweep>()[0];
			ContentManager.instance.sweepPoster = UnityEngine.Object.Instantiate(sweep.Poster.baseTexture);
			ContentManager.instance.sweepSprite = UnityEngine.Object.Instantiate(sweep.spriteBase.transform.Find("Sprite").GetComponent<SpriteRenderer>().sprite); // Specifically for classic sweep

			ContentManager.instance.SetupExtraContent(); // Extra Content like PrefabInstances





			// Actually doing changes to Level Objects below here

			var newMat = ScriptableObject.CreateInstance<StandardDoorMats>(); // Principal's Office Door Setup
			newMat.open = new Material(lds[0].classDoorMat.open) { mainTexture = ContentAssets.GetAsset<Texture2D>("officeDoorOpen") };
			newMat.shut = new Material(lds[0].classDoorMat.shut) { mainTexture = ContentAssets.GetAsset<Texture2D>("officeDoorClosed") };
			newMat.name = "OfficeDoor_Mat";
			foreach (var ld in lds)
			{
				// Setup constant variables
				Floors currentFloor = ld.name.ToFloorIdentifier(); // Support for level object now

				ld.posters = ld.posters.AddRangeToArray(ContentManager.instance.AllPosters(false).ToArray()); // Add posters

				ld.chalkBoards = ld.chalkBoards.AddRangeToArray(ContentManager.instance.AllPosters(true).ToArray()); // Add chalkboards

				ld.OfficeDoorMat = newMat; // Changing the office material to a custom one

				ld.potentialNPCs.AddRange(ContentManager.instance.GetNPCs(currentFloor)); // Add Npcs

				// Hall Builders
				ld.standardHallBuilders = ld.standardHallBuilders.AddRangeToArray(ContentManager.instance.StandardHallBuilders.ToArray());
				ld.forcedSpecialHallBuilders = ld.forcedSpecialHallBuilders.AddRangeToArray(ContentManager.instance.GetForcedHallBuilders(currentFloor).ToArray());
				ld.specialHallBuilders = ld.specialHallBuilders.AddRangeToArray(ContentManager.instance.GetObjectBuilders(currentFloor).ToArray());

				// Room Builders
				ld.classBuilders = ld.classBuilders.AddRangeToArray(ContentManager.instance.GetNewRoomBuilders(RoomCategory.Class).ToArray());
				ld.facultyBuilders = ld.facultyBuilders.AddRangeToArray(ContentManager.instance.GetNewRoomBuilders(RoomCategory.Faculty).ToArray());
				ld.officeBuilders = ld.officeBuilders.AddRangeToArray(ContentManager.instance.GetNewRoomBuilders(RoomCategory.Office).ToArray());

				ld.items = ld.items.AddRangeToArray(ContentManager.instance.GetItems(currentFloor).ToArray()); // Add Items

				ld.shopItems = ld.shopItems.AddRangeToArray(ContentManager.instance.GetShoppingItems(currentFloor).ToArray()); // Add shopping items

				if (ld.fieldTrip)
					ld.fieldTripItems.AddRange(ContentManager.instance.FieldTripItems); // Add field trip items

				ld.randomEvents.AddRange(ContentManager.instance.GetEvents(currentFloor)); // Add queued events for the floor



				switch (currentFloor)
				{
					case Floors.F1:
						ld.maxClassRooms = 5;
						ld.maxSize += new IntVector2(6, 6);
						ld.maxPlots += 1;
						ld.maxFacultyRooms += 1;
						ld.additionalNPCs += 1;
						break;
					case Floors.F2:
						ld.minClassRooms = 6;
						ld.maxClassRooms = 8;
						ld.minSize += new IntVector2(5, 6);
						ld.maxSize += new IntVector2(10, 10);
						ld.maxPlots += 2;
						ld.exitCount += 1;
						ld.minHallsToRemove += 1;
						ld.maxHallsToRemove += 2;
						ld.maxReplacementHalls += 1;
						ld.minFacultyRooms += 1;
						ld.maxFacultyRooms += 2;
						ld.additionalNPCs += 2;
						ld.maxLightDistance = 10;
						ld.standardLightChance = 25;
						ld.maxSpecialBuilders += 1;
						ld.maxOffices = 2;
						break;
					case Floors.END:
						ld.minClassRooms = 6;
						ld.maxClassRooms = 8;
						ld.minSize += new IntVector2(5, 6);
						ld.maxSize += new IntVector2(10, 10);
						ld.maxPlots += 2;
						ld.minHallsToRemove += 1;
						ld.maxHallsToRemove += 2;
						ld.maxReplacementHalls += 2;
						ld.maxFacultyRooms += 3;
						ld.additionalNPCs += 3;
						ld.maxLightDistance = 12;
						ld.standardLightChance = 25;
						ld.maxSpecialBuilders += 2;
						ld.maxOffices = 2;
						break;
					case Floors.F3:
						ld.maxClassRooms = 12;
						ld.minSize += new IntVector2(8, 10);
						ld.maxSize += new IntVector2(20, 20);
						ld.minPlots += 6;
						ld.maxPlots += 9;
						ld.minHallsToRemove += 4;
						ld.maxHallsToRemove += 5;
						ld.minReplacementHalls += 1;
						ld.maxReplacementHalls += 3;
						ld.minFacultyRooms += 3;
						ld.maxFacultyRooms += 2;
						ld.additionalNPCs += 5;
						ld.maxLightDistance = 15;
						ld.maxSpecialBuilders += 2;
						ld.minOffices = 2;
						ld.maxOffices = 3;
						break;
				}


				// Changes classroom stuff

				ld.classCeilingTexs = ld.classCeilingTexs.AddRangeToArray(ContentManager.instance.GetSchoolText(currentFloor, SchoolTextType.Ceiling, 0).ToArray());
				ld.classWallTexs = ld.classWallTexs.AddRangeToArray(ContentManager.instance.GetSchoolText(currentFloor, SchoolTextType.Wall, 0).ToArray());
				ld.classFloorTexs = ld.classFloorTexs.AddRangeToArray(ContentManager.instance.GetSchoolText(currentFloor, SchoolTextType.Floor, 0).ToArray());

				// Changes faculty stuff
				ld.facultyCeilingTexs = ld.facultyCeilingTexs.AddRangeToArray(ContentManager.instance.GetSchoolText(currentFloor, SchoolTextType.Ceiling, 1).ToArray());
				ld.facultyWallTexs = ld.facultyWallTexs.AddRangeToArray(ContentManager.instance.GetSchoolText(currentFloor, SchoolTextType.Wall, 1).ToArray());
				ld.facultyFloorTexs = ld.facultyFloorTexs.AddRangeToArray(ContentManager.instance.GetSchoolText(currentFloor, SchoolTextType.Floor, 1).ToArray());

				// Changes school stuff
				ld.hallCeilingTexs = ld.hallCeilingTexs.AddRangeToArray(ContentManager.instance.GetSchoolText(currentFloor, SchoolTextType.Ceiling).ToArray());
				ld.hallFloorTexs = ld.hallFloorTexs.AddRangeToArray(ContentManager.instance.GetSchoolText(currentFloor, SchoolTextType.Floor).ToArray());
				ld.hallWallTexs = ld.hallWallTexs.AddRangeToArray(ContentManager.instance.GetSchoolText(currentFloor, SchoolTextType.Wall).ToArray());


				ld.specialRooms = ld.specialRooms.AddRangeToArray(ContentManager.instance.GetSpecialRooms(currentFloor));

				foreach (var locker in Resources.FindObjectsOfTypeAll<MeshRenderer>().Where(x => x.name.EndsWith("Locker")))
				{
					if (locker.GetComponent<PlaceholderComponent>() == null)
						locker.gameObject.AddComponent<PlaceholderComponent>();
				}




			}
		}

		static bool HasDone = false;
	}

	[HarmonyPatch(typeof(LevelGenerator), "StartGenerate")]
	public class SetupExtraContent // Setup items, npcs, events, etc.
	{
		private static void Prefix(LevelGenerator __instance)
		{
			EnvironmentExtraVariables.currentFloor = Singleton<CoreGameManager>.Instance.sceneObject.levelTitle.ToFloorIdentifier();
			EnvironmentExtraVariables.ec = __instance.Ec;
			EnvironmentExtraVariables.lb = __instance;

			EnvironmentExtraVariables.SetVariables();

			ogPotentialNpcs = new List<WeightedNPC>(__instance.ld.potentialNPCs);

			System.Random rng = new System.Random(Singleton<CoreGameManager>.Instance.Seed());

			// Extra Stuff

			int index2 = __instance.ld.potentialNPCs.FindIndex(x => x.selection.Character == Character.Sweep); // If gotta sweep exist, then it has a chance of changing to his old texture, else change back
			if (index2 >= 0)
			{
				if (rng.Next(0, 3) == 0)
				{


					__instance.ld.potentialNPCs[index2].selection.spriteBase.transform.Find("Sprite").GetComponent<SpriteRenderer>().sprite = ContentAssets.GetAsset<Sprite>("oldSweepSprite");
					__instance.ld.potentialNPCs[index2].selection.Poster.baseTexture = ContentAssets.GetAsset<Texture2D>("oldSweepPoster");
					AccessTools.Field(typeof(GottaSweep), "speed").SetValue(__instance.ld.potentialNPCs[index2].selection, 70f);
					AccessTools.Field(typeof(GottaSweep), "moveModMultiplier").SetValue(__instance.ld.potentialNPCs[index2].selection, 1f);
				}
				else
				{

					__instance.ld.potentialNPCs[index2].selection.spriteBase.transform.Find("Sprite").GetComponent<SpriteRenderer>().sprite = ContentManager.instance.sweepSprite;
					__instance.ld.potentialNPCs[index2].selection.Poster.baseTexture = ContentManager.instance.sweepPoster;
					AccessTools.Field(typeof(GottaSweep), "speed").SetValue(__instance.ld.potentialNPCs[index2].selection, 40f);
					AccessTools.Field(typeof(GottaSweep), "moveModMultiplier").SetValue(__instance.ld.potentialNPCs[index2].selection, 0.9f);
				}
			}

			AddReplacementNpcs();

			ContentManager.instance.TurnDecorations(true); // Turn PrefabInstances on for... instancing



			void AddReplacementNpcs()
			{
				// Npc Replacement here

				var replacementNPCs = ContentManager.instance.GetNPCs(Singleton<CoreGameManager>.Instance.sceneObject.levelTitle.ToFloorIdentifier(), true, true);

				if (replacementNPCs.Count == 0) // In case no replacement NPCs exist
					return;

				int maximumReplacements = 1;
				switch (EnvironmentExtraVariables.currentFloor)
				{
					case Floors.F1:
					case Floors.F2:
						maximumReplacements = 1;
						break;
					default:
						maximumReplacements = 2;
						break;
				}

				for (int i = 0; i < maximumReplacements; i++)
				{
					bool success = false;
					NPC rNpc = null;

					if (replacementNPCs.Count > 1)
						rNpc = WeightedNPC.ControlledRandomSelectionList(WeightedNPC.Convert(replacementNPCs), rng);
					else if (rng.Next(0, replacementNPCs[0].weight) >= replacementNPCs[0].weight / 2)
						rNpc = replacementNPCs[0].selection;

					if (!rNpc) // In case it still keeps as a null
						break;

					var characters = rNpc.gameObject.GetComponent<CustomNPCData>().replacementCharacters;



					Dictionary<WeightedSelection<NPC>, int> npcsToChoose = new Dictionary<WeightedSelection<NPC>, int>(); // Creates another weight selection to choose the npc that will be replaced
					foreach (var targetChar in characters)
					{
						int index = __instance.ld.potentialNPCs.FindIndex(x => x.selection.Character == targetChar);
						if (index >= 0)
						{
							npcsToChoose.Add(new WeightedSelection<NPC>()
							{
								selection = __instance.ld.potentialNPCs[index].selection,
								weight = 50
							}, index);
						}
					}
					if (npcsToChoose.Count > 0)
					{
						var target = WeightedNPC.ControlledRandomSelection(npcsToChoose.Select(x => x.Key).ToArray(), rng);
						var tarOrigin = npcsToChoose.First(x => x.Key.selection == target);

						rNpc.spawnableRooms = new List<RoomCategory>(target.spawnableRooms);
						rNpc.GetComponent<CustomNPCData>().isReplacing = target.Character;


						success = true;

						__instance.ld.potentialNPCs.Replace(tarOrigin.Value, new WeightedNPC()
						{
							selection = rNpc,
							weight = tarOrigin.Key.weight
						});
					}


					for (int z = 0; z < replacementNPCs.Count; z++) // Removes npc from selection
					{
						if (replacementNPCs[z].selection == rNpc)
						{
							replacementNPCs.RemoveAt(z);
							z--;
						}
					}

					if (replacementNPCs.Count == 0)
						break;

					if (!success) i--; // If replacement wasn't a success, repeat for loop until a replacement NPC work
				}
			}

		}

		public static List<WeightedNPC> ogPotentialNpcs = new List<WeightedNPC>();
	}

	[HarmonyPatch(typeof(GameInitializer), "Initialize")]
	internal class ResettingVariables
	{
		private static void Prefix()
		{
			EnvironmentExtraVariables.ResetVariables();
		}
	}

	[HarmonyPatch(typeof(LevelGenerator), "Generate", MethodType.Enumerator)]
	internal class ChangeSomeGenParameters // Second Main Patch that does some special changes inside the generator
	{
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> codeInstructions = new List<CodeInstruction>(instructions);

			int i = codeInstructions.IndexAt(x => x.opcode == OpCodes.Stfld && x.operand.ToString().ToLower().Contains("<roomcount>")) - 4; // finds the room count and goes 4 instructions back to put the count
			codeInstructions.Insert(i, new CodeInstruction(OpCodes.Add)); // This is inverted btw
			codeInstructions.Insert(i, Transpilers.EmitDelegate<Func<int>>(() => ContentManager.instance.RoomCount)); // Adds the room count here

			i = codeInstructions.IndexAt(x => x.opcode == OpCodes.Stfld && x.operand.ToString().ToLower().Contains("potentialclassrooms"));
			codeInstructions.Insert(i, Transpilers.EmitDelegate<Action>(ContentManager.instance.AssignCustomRooms));

			i = codeInstructions.IndexAt(x => x.opcode == OpCodes.Stsfld && ReferenceEquals(AccessTools.Field(typeof(CoreGameManager), "lightMapPaused"), x.operand));
			codeInstructions.Insert(i, Transpilers.EmitDelegate<Action>(() => EnvironmentExtraVariables.allowHangingLights = false));
			codeInstructions.Insert(++i, Transpilers.EmitDelegate<Action>(() =>
			{
				foreach (var item in EnvironmentExtraVariables.tilesForLighting)
				{
					EnvironmentExtraVariables.ec.GenerateLight(item.Tile, item.MapColor, item.Strength);
				}
			}));

			i = codeInstructions.LastIndexAt(x => x.opcode == OpCodes.Stsfld && ReferenceEquals(AccessTools.Field(typeof(CoreGameManager), "lightMapPaused"), x.operand)) + 1; // Gets index on where it pauses the light map (perfect spot)
			codeInstructions.Insert(i, Transpilers.EmitDelegate<Action>(() =>
			{


				EnvironmentExtraVariables.allowHangingLights = true;
				foreach (var light in ContentUtilities.FindObjectsContainingName<Transform>("hanginglight")) // If hanging lights exists in dark rooms, remove them
				{
					var tile = EnvironmentExtraVariables.ec.TileFromPos(light.position);
					if (tile != null && ContentManager.instance.TryGetRoom(tile.room.category, out ContentManager.RoomData data))
					{
						if (data.IsDarkRoom) UnityEngine.Object.Destroy(light.gameObject);
					}
				}

			}));

			i = codeInstructions.IndexAt(x => x.opcode == OpCodes.Stfld && ReferenceEquals(AccessTools.Field(typeof(LevelBuilder), "controlledRNG"), x.operand)) + 1; // Makes negative seeds different from positive ones
			codeInstructions.Insert(i, Transpilers.EmitDelegate<Action>(() =>
			{
				if (Singleton<CoreGameManager>.Instance.Seed() < 0)
				{
					SkipRngTimes(EnvironmentExtraVariables.lb.controlledRNG, 5, 25);
				}

				void SkipRngTimes(System.Random rng, int min, int max)
				{
					int amount = rng.Next(min, max);
					for (int z = 0; z < amount; z++) { rng.Next(); }
				}
			}));

			i = codeInstructions.LastIndexAt(x => x.opcode == OpCodes.Stfld && ReferenceEquals(AccessTools.Field(typeof(LevelBuilder), "controlledRNG"), x.operand)) + 1;
			codeInstructions.Insert(i, Transpilers.EmitDelegate<Action>(() =>
			{
				if (Singleton<CoreGameManager>.Instance.Seed() < 0)
				{
					SkipRngTimes(EnvironmentExtraVariables.lb.controlledRNG, 25, 25 * (EnvironmentExtraVariables.lb.seedOffset + 1));
				}

				void SkipRngTimes(System.Random rng, int min, int max)
				{
					int amount = rng.Next(min, max);
					for (int z = 0; z < amount; z++) { rng.Next(); }
				}
			}));

			// Random Chance to be sticking to halls

			var stickToHalls = AccessTools.PropertyGetter(typeof(SpecialRoomCreator), "StickToHalls");

			i = codeInstructions.IndexAt(x => x.opcode == OpCodes.Callvirt && (MethodInfo)x.operand == stickToHalls); // Gets the line that calls the stickToHalls method



			codeInstructions.Replace(i + 1, new CodeInstruction(OpCodes.Nop)); // Literally disables that update spots thing to use the one from the delegate

			codeInstructions.Replace(i, Transpilers.EmitDelegate<Action>(() =>
			EnvironmentExtraVariables.lb.UpdatePotentialRoomSpawns(EnvironmentExtraVariables.currentFloor == Floors.F1 ? ContentUtilities.FindLastObjectOfType<SpecialRoomCreator>().StickToHalls : EnvironmentExtraVariables.lb.controlledRNG.NextDouble() >= 0.6d))); // If F1, just leave default which is true, otherwise, randomly choose between sticking or not to halls)
																																																																		// Really hard work here ^^

			return codeInstructions.AsEnumerable();
		}
	}

	[HarmonyPatch(typeof(RoomBuilder), "Setup")]
	internal class ApplyCustomDecorations
	{
		private static void Prefix(ref WeightedTransform[] ___decorations, RoomController room)
		{

			___decorations = ___decorations.AddRangeToArray(ContentManager.instance.GetDecorations(room.category));

		}
	}


	[HarmonyPatch(typeof(EnvironmentController), "GenerateLight")]
	internal class CheckIfRoomDark
	{
		private static bool Prefix(TileController tile)
		{
			bool darkRoom = false;
			if (ContentManager.instance.TryGetRoom(tile.room.category, out ContentManager.RoomData roomDat))
				darkRoom = roomDat.IsDarkRoom;

			return EnvironmentExtraVariables.allowHangingLights || !darkRoom;
		}
	}

	[HarmonyPatch(typeof(LevelBuilder), "AddMapTile")]
	internal class AdaptRoomColors
	{
		private static void Postfix(IntVector2 position, EnvironmentController ___ec, Map ___map)
		{
			TileController tileController = ___ec.tiles[position.x, position.z];
			if (tileController != null)
			{
				var mapTile = ___map.tiles[position.x, position.z];
				if (mapTile.SpriteRenderer.color == Color.white) // Unknown room, perfect to put a color on it
				{
					Color color = Color.white;
					if (ContentManager.instance.TryGetRoom(tileController.room.category, out var roomDat))
						color = roomDat.MapColor;

					mapTile.SpriteRenderer.color = color;
				}

			}
		}
	}

	[HarmonyPatch(typeof(CoreGameManager), "Start")]
	internal class NonPosAudioForCores
	{
		private static void Postfix(CoreGameManager __instance)
		{
			__instance.audMan.positional = false; // Literally only this
		}
	}

	[HarmonyPatch(typeof(MainGameManager), "LoadNextLevel")]
	internal class BeautifulCutscene
	{
		[HarmonyPrefix]
		private static bool LeCutsceneFinale(MainGameManager __instance)
		{
			if (EnvironmentExtraVariables.currentFloor != Floors.F3 || !AfterGen.finalElevator || Singleton<CoreGameManager>.Instance.currentMode != Mode.Main) // Fixes the Free mode softlocking
				return true;

			GameObject leBaldi = null; // Puts le Baldi in front of elevator for final scene

			for (int i = 0; i < __instance.Ec.Npcs.Count; i++) // Destroy any other Baldi available
			{
				var npc = __instance.Ec.Npcs[i];
				if (npc.Character != Character.Baldi & (npc.GetComponent<CustomNPCData>()?.isReplacing != Character.Baldi))
				{
					npc.Despawn();
					i--;
				}
				else
				{
					leBaldi = npc.gameObject;
					npc.GetComponent<Navigator>().enabled = false; // Disable this components to not throw annoying null exceptions (shut up)
					npc.GetComponent<Looker>().enabled = false;
					UnityEngine.Object.Destroy(npc);
				}
			}

			Singleton<CoreGameManager>.Instance.disablePause = true;
			EnvironmentExtraVariables.TurnSubtitles(false);

			var elevator = AfterGen.finalElevator;

			var colPosition = new Vector3(elevator.ColliderGroup.transform.position.x, 5f, elevator.ColliderGroup.transform.position.z);

			var mod = new MovementModifier(Vector3.zero, 0f);

			foreach (var player in __instance.Ec.Players)
			{
				if (player)
				{
					player.transform.position = colPosition - (elevator.dir.ToVector3() * 10f);
					player.GetComponent<ActivityModifier>().moveMods.Add(mod);
					player.GetComponent<ItemManager>().enabled = false;
				}
			}

			var camera = new GameObject("LeCutsceneCamera", typeof(Camera)).GetComponent<Camera>();
			camera.transform.position = colPosition;
			camera.transform.LookAt(colPosition - (elevator.dir.ToVector3() * 10f));
			camera.enabled = true;

			if (leBaldi != null)
			{
				leBaldi.transform.position = colPosition + (elevator.dir.ToVector3() * 10f);
			}

			__instance.StartCoroutine(CutsceneMoment(camera, elevator.transform.Find("Elevator").GetComponent<ElevatorDoor>(), __instance, leBaldi.transform ?? null));

			AfterGen.finalElevator = null;

			return false;
		}

		private static IEnumerator CutsceneMoment(Camera cam, ElevatorDoor elevator, MainGameManager man, Transform leBaldiPos = null)
		{
			float time = 3f;
			while (time > 0f)
			{
				time -= 1f * man.Ec.EnvironmentTimeScale * Time.deltaTime;
				yield return null;
			}

			elevator.Shut();

			time = 1.5f;

			while (time > 0f)
			{
				time -= man.Ec.EnvironmentTimeScale * Time.deltaTime;
				yield return null;
			}

			if (leBaldiPos)
			{
				cam.transform.position += Vector3.down * 2f;
				cam.transform.LookAt(leBaldiPos);
			}

			time = 4f;

			while (time > 0f)
			{
				time -= man.Ec.EnvironmentTimeScale * Time.deltaTime;
				yield return null;
			}

			Vector3 ogPos = cam.transform.position;
			Vector3 right = cam.transform.right;
			float shakeness = 0.1f;

			while (shakeness < 3f)
			{
				cam.transform.position = ogPos + (right * UnityEngine.Random.Range(-shakeness, shakeness)) + (Vector3.up * UnityEngine.Random.Range(-shakeness, shakeness));
				shakeness += 2f * man.Ec.EnvironmentTimeScale * Time.deltaTime;
				yield return null;
			}

			Shader.SetGlobalColor("_SkyboxColor", Color.black);
			Singleton<MusicManager>.Instance.StopFile();

			cam.transform.position = ogPos + (Vector3.down * 20f);
			cam.transform.LookAt(cam.transform.position + (Vector3.down * 5f));

			time = 4f;

			while (time > 0f)
			{
				time -= man.Ec.EnvironmentTimeScale * Time.deltaTime;
				yield return null;
			}

			man.Ec.gameObject.SetActive(false);
			UnityEngine.Object.Destroy(cam.gameObject);

			man.LoadNextLevel();


			yield break;
		}
	}

	[HarmonyPatch(typeof(BaseGameManager), "ApplyMap")]
	internal class AddIconsPatch
	{
		private static bool Prefix(Map map, EnvironmentController ___ec) // Add your custom icons here
																		 // Replacing the old ApplyMap to add a custom item inside the notebook thing
		{
			Transform GetMapGridPosition(Transform pos) => map.tiles[IntVector2.GetGridPosition(pos.transform.position).x, IntVector2.GetGridPosition(pos.transform.position).z].transform; // If you want to add into a map tile, just use this directly



			for (int i = 0; i < Singleton<CoreGameManager>.Instance.setPlayers; i++)
			{
				PlayerManager player = Singleton<CoreGameManager>.Instance.GetPlayer(i);
				map.targets.Add(player.transform);
			}
			foreach (Notebook notebook in ___ec.notebooks)
			{
				if (notebook.activity && !notebook.activity.GetType().Equals(typeof(NoActivity))) // If it is not a NoActivity Notebook
					notebook.icon = ContentManager.instance.AddMapIcon("mathNotebookIcon", GetMapGridPosition(notebook.transform));
				else
					notebook.icon = UnityEngine.Object.Instantiate(notebook.iconPre, GetMapGridPosition(notebook.transform));
			}
			foreach (Pickup pickup in ___ec.items)
			{
				pickup.icon = UnityEngine.Object.Instantiate(pickup.iconPre, GetMapGridPosition(pickup.transform));
			}

			// Custom Stuff Here

			foreach (var machine in UnityEngine.Object.FindObjectsOfType<FogMachine>())
			{
				machine.icon = ContentManager.instance.AddMapIcon("FogMachine", GetMapGridPosition(machine.transform));
			}

			foreach (var button in UnityEngine.Object.FindObjectsOfType<GameButton>())
			{
				ContentManager.instance.AddMapIcon("buttonIcon", GetMapGridPosition(button.transform));
			}

			foreach (var trash in UnityEngine.Object.FindObjectsOfType<TrashCan>())
			{
				ContentManager.instance.AddMapIcon("trashCan", GetMapGridPosition(trash.transform));
			}

			foreach (var trapdoor in UnityEngine.Object.FindObjectsOfType<Trapdoor>())
			{
				ContentManager.instance.AddMapIcon("trapDoor", GetMapGridPosition(trapdoor.transform));
			}


			return false;
		}
	}

	[HarmonyPatch(typeof(CoreGameManager), "EndGame")]
	internal class EndGameInvoking
	{
		private static void Prefix()
		{
			EnvironmentExtraVariables.OnEndGame.Invoke();
		}
	}

	[HarmonyPatch(typeof(CoreGameManager), "RestoreMap")] // Quick fix for the map restoring, so it doesn't crash the game
	internal class MapSizePatch
	{
		private static bool Prefix(Map map, ref bool ___restoreMap, bool[,] ___foundTilesToRestore)
		{
			if (ContentManager.instance.DebugMode)
			{
				Debug.Log("Do I have to restore map? : " + ___restoreMap);
				Debug.Log("Map Sizes: " + map.size.GetString() + " // Multiplied results in: " + map.size.x * map.size.z);
				Debug.Log("Size of array to restore: " + ___foundTilesToRestore.Length);
				Debug.Log("Size of map tiles array: " + map.tiles.Length);
			}
			if (___restoreMap)
			{
				for (int i = 0; i < map.size.x; i++)
				{
					for (int j = 0; j < map.size.z; j++)
					{
						if (___foundTilesToRestore[i, j])
						{
							map.tiles[i, j]?.Find(map); // the only change is here, to check whether the tile is null or not
						}
					}
				}
			}
			___restoreMap = false;

			return false;
		}
	}

	[HarmonyPatch(typeof(Window), "Initialize")]
	internal class CustomWindowAddition
	{
		private static void Postfix(Window __instance)
		{
			if (__instance.name.StartsWith("CustomWindow_")) // Skips custom windows or disabled
				return;

			var currentFloor = EnvironmentExtraVariables.currentFloor;

			var rng = EnvironmentExtraVariables.lb.controlledRNG;

			List<List<WindowObject>> windowCollection = new List<List<WindowObject>>();
			if (__instance.bTile)
			{
				var windowsToChoose = ContentManager.instance.GetWindows(currentFloor, true, __instance.bTile.room.category);
				if (windowsToChoose.Count > 0) windowCollection.Add(windowsToChoose);
			}
			if (__instance.aTile)
			{
				var secWindowsToChoose = ContentManager.instance.GetWindows(currentFloor, true, __instance.aTile.room.category);
				if (secWindowsToChoose.Count > 0) windowCollection.Add(secWindowsToChoose);
			}

			if (ContentManager.instance.DebugMode)
			{
				Debug.Log(__instance.aTile.room.category);
				Debug.Log(__instance.bTile.room.category);
				Debug.Log(windowCollection.Count + " Collections Found");
			}

			if (windowCollection.Count > 0 && rng.NextDouble() >= 0.85d)
			{
				var collection = windowCollection[rng.Next(windowCollection.Count)];
				ContentUtilities.ReplaceWindow(__instance, collection[rng.Next(collection.Count)],__instance.ec); // Gets a random collection
			}
		}
	}


	[HarmonyPatch(typeof(BaseGameManager))]
	public class AfterGen
	{
		[HarmonyPatch("EnterExit")]
		[HarmonyPrefix]
		private static void LastElevatorSet(ColliderGroup group)
		{
			finalElevator = group.transform.parent.GetComponent<Elevator>();
		}

		public static Elevator finalElevator = null;

		[HarmonyPatch("BeginSpoopMode")]
		[HarmonyPatch("LoadFieldTrip")]
		[HarmonyPostfix]
		private static void DisableQueuedMusic()
		{
			Singleton<MusicManager>.Instance.StopFile();
		}

		[HarmonyPatch("Initialize")]
		[HarmonyPrefix]
		private static void PostGen(int ___levelNo, BaseGameManager __instance)
		{

			if (ContentManager.instance.DebugMode) // Important stuff before the generation stuff lol
			{
				__instance.CompleteMapOnReady();
			}
			var ec = __instance.Ec;
			

			StandardDoor_ExtraFunctions.AssignDoorsToTheFunction(ec);
			WindowExtraFields.AssignWindowsToTheFunction();

			ContentManager.instance.DestroyTempDecorations(); // Destroys left garbage
			var currentFloor = EnvironmentExtraVariables.currentFloor;

			ContentManager.instance.TurnDecorations(false);

			if (currentFloor == Floors.None) return; // Fixes the crash on challenge, since it's an invalid floor
			var rng = EnvironmentExtraVariables.lb.controlledRNG;

		

			foreach (var elevator in EnvironmentExtraVariables.ElevatorCenterPositions)
			{
				var pos = ec.TileFromPos(elevator.Key + elevator.Value.ToIntVector2()).transform.position;
				PrefabInstance.SpawnPrefab<ExitSign>(new Vector3(pos.x, 9f, pos.z), default, ec);
			}

			foreach (var room in queuedElevatorsForFixing)
			{
				room.Key.FixElevatorTiles(room.Value);
			}
			queuedElevatorsForFixing.Clear();

		}

		public static void QueueElevatorFix(SpecialRoomCreator room, Texture2D ceiling)
		{
			if (!queuedElevatorsForFixing.ContainsKey(room))
				queuedElevatorsForFixing.Add(room, ceiling);
		}

		private readonly static Dictionary<SpecialRoomCreator, Texture2D> queuedElevatorsForFixing = new Dictionary<SpecialRoomCreator, Texture2D>();

		[HarmonyPatch("Initialize")]
		[HarmonyPostfix]
		private static void SpawnPlayerModel(BaseGameManager __instance)
		{
			foreach (var player in __instance.Ec.Players)
			{
				if (player) // If the player even exist, since it is an array
				{
					var playerModel = PrefabInstance.SpawnPrefab<PlayerModel>(player.transform, __instance.Ec, offset: Vector3.down * 1.2f);
					playerModel.SetPlayer(player);
				}
			}
		}

		[HarmonyPatch("AllNotebooks")]
		[HarmonyPrefix]
		private static void EndGame(BaseGameManager __instance)
		{
			if (EnvironmentExtraVariables.IsEndGame) return; // Not repeat same phrase twice if another notebook is somehow collected

			if (EnvironmentExtraVariables.currentFloor != Floors.END) // Skips if END floor
			{
				bool mainMode = Singleton<CoreGameManager>.Instance.currentMode == Mode.Main;
				SoundObject sound;
				if (EnvironmentExtraVariables.currentFloor == Floors.F3 && mainMode)
					sound = ContentAssets.GetAsset<SoundObject>("BaldiAngryEscape"); // Angry Speak!
				else
				{
					Singleton<MusicManager>.Instance.QueueFile(ContentAssets.GetAsset<LoopingSoundObject>("SchoolEscapeSong"), true); // Normal Escape Sequence
					sound = ContentAssets.GetAsset<SoundObject>("BaldiNormalEscape");
				}
				__instance.StartCoroutine(EnvironmentExtraVariables.SmoothFOVSlide(7.5f, 15, offset:25f));
				sound.subtitle = false; // No subtitles.
				if (mainMode)
					ItemSoundHolder.CreateSoundHolder(Singleton<CoreGameManager>.Instance.GetPlayer(0).transform, sound, false, maxDistance: 100f);
			}

			EnvironmentExtraVariables.EndGamePhase(); // Just turns on that boolean
		}

		[HarmonyPatch("ElevatorClosed")]
		[HarmonyPrefix]
		private static void RedSequence(BaseGameManager __instance, int ___elevatorsClosed, EnvironmentController ___ec, Elevator elevator)
		{
			if (EnvironmentExtraVariables.currentFloor != Floors.F3 || Singleton<CoreGameManager>.Instance.currentMode != Mode.Main) // If this ain't F3, no way there is gonna have a scary escape sequence
				return;

			IEnumerator LightChanger(EnvironmentController ec, List<TileController> lights, bool on, float delay)
			{
				float time = delay;
				while (lights.Count > 0)
				{
					while (time > 0f)
					{
						time -= Time.deltaTime * ec.EnvironmentTimeScale;
						yield return null;
					}
					time = delay;
					int num = UnityEngine.Random.Range(0, lights.Count);
					lights[num].lightColor = Color.red;
					ec.SetLight(on, lights[num]);
					lights.RemoveAt(num);
				}
				yield break;
			}

			IEnumerator BaldiInfiniteAnger(EnvironmentController ec)
			{
				while (true)
				{
					try
					{
						__instance.AngerBaldi(0.2f * ec.NpcTimeScale * Time.deltaTime);
					}
					catch
					{
						yield break;
					}
					yield return null;
				}
			}



			List<TileController> list = new List<TileController>();
			if (___elevatorsClosed == 1)
			{
				foreach (TileController tileController in ___ec.AllTilesNoGarbage(true, true))
				{
					if (tileController.lightStrength <= 1)
					{
						tileController.lightColor = Color.red;
						___ec.SetLight(true, tileController);
					}
					else
					{
						list.Add(tileController);
					}
				}
				Shader.SetGlobalColor("_SkyboxColor", Color.red);
				__instance.StartCoroutine(LightChanger(___ec, list, true, 0.2f));
				Singleton<MusicManager>.Instance.StopFile();
				Singleton<MusicManager>.Instance.QueueFile(ContentAssets.GetAsset<LoopingSoundObject>("AngrySchool_Phase1"), true);
			}
			else if (___elevatorsClosed == 2)
			{
				foreach (TileController tileController in ___ec.AllTilesNoGarbage(true, true)) // put all red directly
				{
					tileController.lightColor = Color.red;
					___ec.SetLight(true, tileController);
				}

				Singleton<MusicManager>.Instance.StopFile();
				Singleton<MusicManager>.Instance.QueueFile(ContentAssets.GetAsset<LoopingSoundObject>("AngrySchool_Phase2"), true);
			}
			else if (___elevatorsClosed == 3)
			{
				var sound = ContentAssets.GetAsset<SoundObject>("BaldiFinalWarning");
				sound.subtitle = false;
				ItemSoundHolder.CreateSoundHolder(Singleton<CoreGameManager>.Instance.GetPlayer(0).transform, sound, false, maxDistance: 100f);

				var gateTexs = new Texture2D[] { ContentAssets.GetAsset<Texture2D>("elevator_gateR"), ContentAssets.GetAsset<Texture2D>("elevator_gateU"), ContentAssets.GetAsset<Texture2D>("elevator_gateN") }; // R U N  Textures
				var elevatorGates = elevator.transform.Find("Gate").GetAllChilds();
				elevatorGates[0].GetComponent<MeshRenderer>().material.mainTexture = gateTexs[1]; // Sets the RUN word into the gates
				elevatorGates[1].GetComponent<MeshRenderer>().material.mainTexture = gateTexs[0];
				elevatorGates[2].GetComponent<MeshRenderer>().material.mainTexture = gateTexs[2];

				__instance.StartCoroutine(EnvironmentExtraVariables.SmoothFOVSlide(14f, 15, offset:75f));
				for (int i = 0; i < ___ec.CurrentEventTypes.Count; i++)
				{
					AccessTools.Field(typeof(RandomEvent), "remainingTime").SetValue(___ec.GetEvent(___ec.CurrentEventTypes[i]), 0f);
				}
				___ec.StopAllCoroutines();

				for (int i = 0; i < ___ec.Npcs.Count; i++)
				{
					var npc = ___ec.Npcs[i];
					if (npc.Character != Character.Baldi && (!npc.GetComponent<CustomNPCData>() || npc.GetComponent<CustomNPCData>().isReplacing != Character.Baldi))
					{
						npc.Despawn();
						i--;
					}
				}

				__instance.StartCoroutine(BaldiInfiniteAnger(___ec));

				Singleton<MusicManager>.Instance.QueueFile(ContentAssets.GetAsset<LoopingSoundObject>("AngrySchool_Phase3"), true);
				Singleton<MusicManager>.Instance.QueueFile(ContentAssets.GetAsset<LoopingSoundObject>("AngrySchool_Phase4"), true); // Queue separately, so the last audio will be looping
				if (!Singleton<PlayerFileManager>.Instance.reduceFlashing)
				{
					___ec.standardDarkLevel = new Color(0.2f, 0f, 0f);
					___ec.FlickerLights(true);
				}
				for (int i = 0; i < Singleton<MusicManager>.Instance.MidiPlayer.Channels.Length; i++)
				{
					Singleton<MusicManager>.Instance.MidiPlayer.MPTK_ChannelEnableSet(i, false);
				}

				if (!ContentManager.instance.TryGetDecorationTransform(RoomCategory.Null, true, "SchoolFire", out Transform obj)) // If it fails to grab the school fire
					return;


				var tiles = ___ec.AllTilesNoGarbage(false, false);

				while (tiles.Count > 0)
				{
					int index = UnityEngine.Random.Range(0, tiles.Count);
					if (UnityEngine.Random.Range(0, 3) == 0)
					{
						var fire = UnityEngine.Object.Instantiate(obj, tiles[index].transform);

						float offsetX = UnityEngine.Random.Range(-2.0f, 2.0f);
						float offsetZ = UnityEngine.Random.Range(-2.0f, 2.0f);

						float scale = UnityEngine.Random.Range(1.0f, 1.8f);

						fire.position = tiles[index].transform.position + new Vector3(offsetX, 5f * scale, offsetZ);
						fire.localScale = new Vector3(scale, scale, scale);

						fire.gameObject.SetActive(true);
					}
					tiles.RemoveAt(index);
				}

			}
		}


	}

	[HarmonyPatch(typeof(LockerBuilder), "Build")]
	internal class ChangeLockersHere
	{
		private static void Postfix(System.Random cRNG)
		{

			var chance = 100f;
			foreach (var l in UnityEngine.Object.FindObjectsOfType<PlaceholderComponent>()) // Make green lockers
			{
				if (l.GetComponent<HideableLocker>()) // No blue lockers here
				{
					continue;
				}

				var random = cRNG.NextDouble() * 100f;
				if (random <= chance)
				{
					var locker = l.GetComponent<MeshRenderer>();
					chance /= (float)random;
					locker.materials[1].SetTexture("_MainTex", ContentAssets.GetAsset<Texture2D>("greenLocker"));
					locker.material.SetColor("_TextureColor", Color.green);
					var green = locker.gameObject.AddComponent<GreenLocker>();

					if (cRNG.NextDouble() > 0.25d)
					{
						green.MakeMeDecoy();
						locker.materials[1].SetTexture("_MainTex", ContentAssets.GetAsset<Texture2D>("d_greenLocker"));
					}
				}
				UnityEngine.Object.Destroy(l);
			}

			chance = 100f;


			foreach (var locker in UnityEngine.Object.FindObjectsOfType<PlaceholderComponent>())
			{
				if (!locker.GetComponent<HideableLocker>()) continue;

				var random = cRNG.NextDouble() * 100f;
				if (random <= chance)
				{
					chance /= (float)random;
					locker.GetComponent<MeshRenderer>().materials[1].SetTexture("_MainTex", ContentAssets.GetAsset<Texture2D>("d_blueLocker"));
					locker.gameObject.AddComponent<DecoyBlueLocker>();
					UnityEngine.Object.Destroy(locker.GetComponent<HideableLocker>());
				}
				UnityEngine.Object.Destroy(locker);
			}

		}
	}

	[HarmonyPatch(typeof(MainGameManager), "BeginPlay")]
	internal class ChangeSchoolMusicTheme
	{
		private static void Postfix()
		{
			if (EnvironmentExtraVariables.lb.controlledRNG.Next(0, 2) == 1) // Chance to change audio or not
				return;

			var musics = ContentManager.instance.GetSchoolHouseThemes(EnvironmentExtraVariables.currentFloor);
			if (musics.Length == 0) return; // If array is empty

			Singleton<MusicManager>.Instance.StopMidi();
			Singleton<MusicManager>.Instance.PlayMidi(musics[EnvironmentExtraVariables.lb.controlledRNG.Next(musics.Length)], true); // Gets a random music instance to play
		}
	}

	[HarmonyPatch(typeof(EndlessGameManager), "BeginPlay")]
	internal class ChangeSchoolMusicTheme_END
	{
		private static void Postfix()
		{
			if (EnvironmentExtraVariables.lb.controlledRNG.Next(0, 2) == 1) // Chance to change audio or not
				return;

			var musics = ContentManager.instance.GetSchoolHouseThemes(EnvironmentExtraVariables.currentFloor);
			if (musics.Length == 0) return; // If array is empty

			Singleton<MusicManager>.Instance.StopMidi();
			Singleton<MusicManager>.Instance.PlayMidi(musics[EnvironmentExtraVariables.lb.controlledRNG.Next(musics.Length)], true); // Gets a random music instance to play
		}
	}

	[HarmonyPatch(typeof(ElevatorScreen))]
	internal class StopMusicThereAswell
	{
		[HarmonyPatch("StartGame")]
		[HarmonyPatch("Initialize")]
		[HarmonyPrefix]
		private static void Prefix()
		{
			Singleton<MusicManager>.Instance.StopFile(); // Stops music before opening elevator
		}
	}



	[HarmonyPatch(typeof(VentBuilder), "Build")]
	internal class SetupVentBuilder
	{
		[HarmonyPrefix]
		private static void MakeVents(ref Transform ___ventCornerPre, ref Transform ___ventStriaghtPre, ref Transform ___ventTPre, out GameObject[] __state) // Setting Vent Builder Transforms (Apparently, they don't exist on main game)
		{
			var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
			obj.name = "Vent";

			Material ventMat = UnityEngine.Object.Instantiate(ContentUtilities.FindResourceObjectContainingName<Material>("vent"));
			ventMat.mainTexture = ContentAssets.GetAsset<Texture2D>("ventAtlasText");

			obj.transform.localScale = new Vector3(9.9f, 2f, 9.9f);

			Vector2[] mesh = obj.GetComponent<MeshFilter>().mesh.uv;
			mesh = ContentUtilities.ConvertSideToTexture(0, 0, 256, 256, 512, 258, 0, mesh); // Took me a long time to figure out this lol (Pro Tip: CodeMonkey tutorials are useful)
			mesh = ContentUtilities.ConvertSideToTexture(0, 0, 256, 256, 512, 258, 4, mesh);
			mesh = ContentUtilities.ConvertSideToTexture(-256, 0, 256, 256, 512, 258, 8, mesh);
			mesh = ContentUtilities.ConvertSideToTexture(256, 0, 256, 251, 512, 258, 12, mesh);
			mesh = ContentUtilities.ConvertSideToTexture(0, 0, 256, 256, 512, 258, 16, mesh);
			mesh = ContentUtilities.ConvertSideToTexture(0, 0, 256, 256, 512, 258, 20, mesh);

			obj.GetComponent<MeshFilter>().mesh.uv = mesh;


			obj.GetComponent<MeshRenderer>().material = ventMat;

			obj.layer = 2;



			// Setup Audio Stuff
			ContentUtilities.CreatePositionalAudio(obj, 0f, 20f);
			// Done with Audio Stuff

			obj.AddComponent<Vent>();
			obj.SetActive(false);

			var obj2 = UnityEngine.Object.Instantiate(obj);
			obj2.GetComponent<MeshRenderer>().material.mainTexture = ContentAssets.GetAsset<Texture2D>("ventTex");

			obj2.name = "StraightVent";

			obj2.transform.localScale = new Vector3(4f, 2f, 10f);

			var justACube = GameObject.CreatePrimitive(PrimitiveType.Cube);

			mesh = justACube.GetComponent<MeshFilter>().mesh.uv;

			obj2.GetComponent<MeshFilter>().mesh.uv = mesh;

			UnityEngine.Object.Destroy(justACube);

			__state = ContentUtilities.Array(obj, obj2);



			___ventCornerPre = obj.transform;
			___ventStriaghtPre = obj2.transform;
			___ventTPre = obj.transform;

		}
		[HarmonyPostfix]
		private static void MakeVentsPositionsALittleBetter(GameObject[] __state) // as the method name suggests
		{
			foreach (var vent in ContentUtilities.FindObjectsContainingName<Vent>("clone", true))
			{
				vent.gameObject.SetActive(true);
				vent.transform.localPosition = Vector3.up * 9f;
				if (vent.name.Contains("StraightVent"))
				{
					vent.TurnVent(false, true);
				}
			}
			__state.Do(x => UnityEngine.Object.Destroy(x));

		}

		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> RemoveStupidConditions(IEnumerable<CodeInstruction> instructions) // Fixes the vent builder to not just generate a single goofy ah ah vent
		{

			void SetNopes(ref List<CodeInstruction> list, int startingOffset, int endingOffset, int curIdx)
			{
				for (int i = startingOffset; i <= endingOffset; i++)
				{
					list[curIdx + i] = new CodeInstruction(OpCodes.Nop);
				}
			}

			var whatToRemove = AccessTools.Field(typeof(TileController), "containsObject");

			var insts = instructions.ToList();

			int index = insts.FindIndex(x => x.Is(OpCodes.Ldfld, whatToRemove));

			if (index > -1) // Removes the first one
			{
				SetNopes(ref insts, -1, 1, index);
			}

			index = insts.FindLastIndex(x => x.Is(OpCodes.Ldfld, whatToRemove));

			if (index > -1) // Removes the second one
			{
				SetNopes(ref insts, -3, 1, index);
			}

			return insts.AsEnumerable();
		}
	}

	//-------- Fixes ---------

	[HarmonyPatch(typeof(MysteryRoom), "ClaimARoom")] // Fixing mysteryroom room category to Mystery
	internal class FixRoomCategory
	{
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			bool success = false;
			var replacement = AccessTools.Field(typeof(RoomCategory), "Mystery");
			var target = AccessTools.Field(typeof(RoomCategory), "Test");
			using (var enumerator = instructions.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					var instruction = enumerator.Current;
					if (!success && instruction.Is(OpCodes.Stfld, target))
					{
						instruction.operand = replacement;
						success = true; // No more changes needed to instructions
					}
					yield return instruction;
				}
			}
		}
	}

	[HarmonyPatch(typeof(FloodEvent))]
	internal class FixWhirlpoolSpawn
	{
		[HarmonyTranspiler]
		[HarmonyPatch("Move", MethodType.Enumerator)]
		private static IEnumerable<CodeInstruction> FixWhirlpoolSpawns(IEnumerable<CodeInstruction> instructions)
		{
			var list = new List<CodeInstruction>(instructions);

			var allTiles = AccessTools.Method(typeof(EnvironmentController), "AllTilesNoGarbage");

			int idx = list.FindIndex(x => x.Is(OpCodes.Callvirt, allTiles));
			if (idx >= 0)
				list[idx - 1].opcode = OpCodes.Ldc_I4_0; // Just switches one boolean from true to false


			return list.AsEnumerable();
		}

		[HarmonyPrefix]
		[HarmonyPatch("MoveWater")]
		private static void ShutDoorTime(ref List<Door> ___doors, FloodEvent __instance)
		{
			if (__instance.Active) return;

			foreach (var door in ___doors)
			{
				door.StartCoroutine(door.ShutTimer(UnityEngine.Random.Range(5f, 10f)));
			}
			___doors.Clear();
		}
	}
	[HarmonyPatch(typeof(Whirlpool), "Teleport", MethodType.Enumerator)]
	internal class FixWhirlpoolSpawn_InWhirlpools
	{
		private static IEnumerable<CodeInstruction> FixWhirlpoolSpawns(IEnumerable<CodeInstruction> instructions)
		{
			var list = new List<CodeInstruction>(instructions);

			var allTiles = AccessTools.Method(typeof(EnvironmentController), "AllTilesNoGarbage");

			int idx = list.FindIndex(x => x.Is(OpCodes.Callvirt, allTiles));
			if (idx >= 0)
				list[idx - 1].opcode = OpCodes.Ldc_I4_0; // Just switches one boolean from true to false
			

			return list.AsEnumerable();
		}
	}

	[HarmonyPatch(typeof(ITM_PortalPoster), "Use")]

	internal class DestroyUnusedObject
	{
		private static void Postfix(ITM_PortalPoster __instance)
		{
			UnityEngine.Object.Destroy(__instance.gameObject);
		}
	}

	[HarmonyPatch(typeof(StandardDoor), "OpenTimed")] // Fixes the bug where the door open sound plays 2 times

	internal class FixDoorOpenSound
	{
		private static void Postfix(StandardDoor __instance)
		{
			if (!__instance.locked)
			{
				for (int i = 0; i < __instance.doors.Length; i++)
				{
					__instance.colliders[i].enabled = false;
					MaterialModifier.ChangeOverlay(__instance.doors[i], __instance.overlayOpen[i]);
					MeshRenderer[] array = __instance.doors;
					for (int j = 0; j < array.Length; j++)
					{
						array[j].gameObject.layer = 2;
					}
				}
				if (!__instance.open && __instance.makesNoise)
				{
					__instance.audMan.PlaySingle(__instance.audDoorOpen);
				}
			}
		}
	}

	[HarmonyPatch(typeof(StandardDoor), "Shut")] // Fixes the bug where the door close sound plays 2 times

	internal class FixDoorCloseSound
	{
		private static void Postfix(StandardDoor __instance)
		{
			for (int i = 0; i < __instance.doors.Length; i++)
			{
				__instance.colliders[i].enabled = __instance;
				MaterialModifier.ChangeOverlay(__instance.doors[i], __instance.overlayShut[i]);
				MeshRenderer[] array = __instance.doors;
				for (int j = 0; j < array.Length; j++)
				{
					array[j].gameObject.layer = __instance.doors[0].gameObject.layer;
				}
			}
			if (__instance.open && __instance.makesNoise)
			{
				__instance.audMan.PlaySingle(__instance.audDoorShut);
			}
		}
	}

	[HarmonyPatch(typeof(SwingDoor), "OpenTimed")] // Fixes the bug where the swinging door open sound plays 2 times

	internal class FixSwingDoorOpenSound
	{
		private static void Postfix(SwingDoor __instance)
		{
			if (!__instance.locked)
			{
				for (int i = 0; i < __instance.doors.Length; i++)
				{
					MaterialModifier.ChangeOverlay(__instance.doors[i], __instance.overlayOpen[i]);
				}
				if (!__instance.open)
				{
					__instance.audMan.PlaySingle(__instance.audDoorOpen);
				}
			}
		}
	}


	[HarmonyPatch(typeof(PlayerFileManager), "Find")] // Fixes an error that causes a crash because of the new NPCs

	internal class FixCrash
	{
		private static bool Prefix(PlayerFileManager __instance, bool[] type, int value)
		{
			if (value >= type.Length || value < 0) // Stops the game from choosing an index out of bounds
			{
				__instance.Save();
				return false;
			}
			return true;
		}
	}
	// ------ Patches from EnvironmentExtraVariables ------

	[HarmonyPatch(typeof(SubtitleManager), "Update")]
	internal class ForceDisableSubtitles
	{
		private static bool Prefix(SubtitleManager __instance)
		{
			if (EnvironmentExtraVariables.AreSubtitlesForceDisabled)
			{
				__instance.canvas.enabled = false;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(LevelBuilder), "CreateElevator")] // Literally adds the pos, so it can be get later with Ec.TileFromPos()
	internal class AddTilesToList
	{
		private static void Prefix(IntVector2 pos, Direction dir)
		{
			EnvironmentExtraVariables.elevatorTilePositions.Add(pos, dir);
			foreach (var direction in dir.PerpendicularList())
			{
				EnvironmentExtraVariables.elevatorTilePositions.Add(pos + direction.ToIntVector2(), dir); // Adds the 2 tiles that are missed from the method
			}
		}
	}

	[HarmonyPatch(typeof(OfficeBuilderStandard), "Builder", MethodType.Enumerator)]
	internal class AllPostersAtOnce // Exactly, all posters
	{
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			int booleansFound = 0;
			bool success = false;
			using (var enumerator = instructions.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					var instrunction = enumerator.Current;

					if (!success && instrunction.opcode == OpCodes.Ldc_I4_0)
					{
						booleansFound++;
						if (booleansFound == 2) // The exact second one
						{
							yield return Transpilers.EmitDelegate<Func<int>>(() => int.MinValue);
							success = true;
							continue;
						}
					}

					yield return instrunction;
				}
			}
		}
	}


	[HarmonyPatch(typeof(MysteryRoom), "SpawnItem")] // Get the custom items and adds them to the event array
	internal class IncludeExtraMysteryItems
	{
		private static void Prefix(ref WeightedSelection<ItemObject>[] ___items)
		{
			___items = ___items.AddRangeToArray(ContentManager.instance.MysteryItems.ToArray());
		}
	}

	[HarmonyPatch(typeof(PartyEvent), "Begin")]
	internal class IncludeExtraPartyItems
	{
		private static void Prefix(ref WeightedItemObject[] ___potentialItems)
		{
			___potentialItems.AddRangeToArray(ContentManager.instance.PartyItems.ToArray());
		}
	}

	// ---- Main Menu Changes ----

	[HarmonyPatch(typeof(MainMenu), "Start")]
	internal class ChangeToBeautifulImage
	{
		private static void Prefix(MainMenu __instance)
		{
			__instance.gameObject.transform.Find("Image").GetComponent<Image>().sprite = ContentAssets.GetAsset<Sprite>("newBaldiMenu"); // Changes main menu texture to my beautiful one lol
			ItemSoundHolder.CreateSoundHolder(__instance.transform.position, ContentAssets.GetAsset<SoundObject>("bbtimesopening"), false, 100, 101);
		}
	}

	[HarmonyPatch(typeof(MainModeButtonController), "OnEnable")]
	internal class RemoveChallenge
	{
		private static void Prefix(MainModeButtonController __instance)
		{
			__instance.transform.Find("Challenge").gameObject.SetActive(false); // Disables challenge button
			var pos = __instance.transform.Find("FieldTrips").position;
			__instance.transform.Find("FieldTrips").position = new Vector3(0, pos.y, pos.z);

			ContentUtilities.FindResourceObjectWithName<StandardMenuButton>("Medium").gameObject.SetActive(false); // Disable medium endless
		}
	}


	// ---- Basic NPC Startup ----

	[HarmonyPatch(typeof(NPC))]

	internal class SetupCustomNPCs
	{
		[HarmonyPatch("Awake")]
		[HarmonyPostfix]
		private static void FixNPC(NPC __instance, ref Character ___character, ref Navigator ___navigator, ref EnvironmentController ___ec, ref bool ___ignoreBelts, ref bool ___aggroed, ref PosterObject ___poster, ref Looker ___looker, ref bool ___ignorePlayerOnSpawn)
		{
			if (__instance.gameObject.name.StartsWith("CustomNPC_")) // setups NPC data here (for marked as customNpcs)
			{
				// Setup
				___navigator = __instance.gameObject.GetComponent<Navigator>();
				___navigator.npc = __instance;
				___navigator.ec = EnvironmentExtraVariables.ec;
				___ec = EnvironmentExtraVariables.ec;
				___looker = __instance.gameObject.GetComponent<Looker>();

				// Custom Data

				var data = __instance.gameObject.GetComponent<CustomNPCData>();
				___character = data.MyCharacter;
				___ignoreBelts = data.IgnoreBelts;
				___aggroed = data.Aggroed;
				___poster = data.poster;
				___ignorePlayerOnSpawn = data.forceSpawn;
			}

		}

		[HarmonyPatch("Despawn")]
		[HarmonyPrefix]
		private static void FixSomeNPCDespawnActions(NPC __instance, EnvironmentController ___ec)
		{
			try
			{
				switch (__instance.Character)
				{
					case Character.LookAt:
						___ec.RemoveFog(__instance.GetComponent<LookAtGuy>().fog);
						AccessTools.Method(typeof(LookAtGuy), "FreezeNPCs").Invoke(__instance, ContentUtilities.Array<object>(false)); // Invokes the freezing npc method
						break;
					case Character.Pomp:
						((TMP_Text)AccessTools.Field(typeof(NoLateTeacher), "popupText").GetValue(__instance)).text = "";
						((NoLateIcon)AccessTools.Field(typeof(NoLateTeacher), "mapIcon").GetValue(__instance)).gameObject.SetActive(false); // Disables any object that relates to mrs pomp
						break;
					case Character.Beans:
						if (__instance.GetComponent<Beans>().gum)
							UnityEngine.Object.Destroy(__instance.GetComponent<Beans>().gum.gameObject);
						break;
					case Character.Cumulo:
						((BeltManager)AccessTools.Field(typeof(Cumulo), "windManager").GetValue(__instance)).gameObject.SetActive(false); // Ends wind and noise
						((AudioManager)AccessTools.Field(typeof(Cumulo), "audMan").GetValue(__instance)).FlushQueue(true);
						break;
					default:
						break;
				}
			}
			catch (Exception e)
			{
				if (ContentManager.instance.DebugMode)
				{
					Debug.LogException(e);
					Debug.LogWarning("Failed to extra-despawn character: " + __instance.Character);
				}
			}
		}
	}

	[HarmonyPatch(typeof(Navigator), "Start")]

	internal class SetupCustomNPCs_Navigator
	{
		private static void Prefix(NPC ___npc, ref bool ___avoidRooms, ref bool ___useHeatMap) // Sets up the avoid rooms thing
		{
			if (___npc.gameObject.name.StartsWith("CustomNPC_"))
			{
				___avoidRooms = !___npc.gameObject.GetComponent<CustomNPCData>().EnterRooms;
				___useHeatMap = ___npc.gameObject.GetComponent<CustomNPCData>().useHeatMap;
			}
		}
	}

	[HarmonyPatch(typeof(Looker), "Update")]
	internal class SetupCustomNPCs_Looker
	{
		private static void Prefix(Looker __instance, ref NPC ___npc, LayerMask ___layerMask)
		{
			if (__instance.gameObject.name.StartsWith("CustomNPC_"))
			{
				if (___npc)
					return;


				___npc = __instance.gameObject.GetComponent<NPC>(); // Sets npc to the looker

			}
		}
	}

	// Setupping Event Data
	[HarmonyPatch(typeof(RandomEvent), "Initialize")]
	internal class SetupRandomEventData
	{
		private static void Prefix(RandomEvent __instance, ref string ___eventDescKey, ref RandomEventType ___eventType, ref float ___minEventTime, ref float ___maxEventTime)
		{
			if (__instance.gameObject.name.StartsWith("CustomEv_")) // Finds out what is a custom event
			{
				var data = __instance.gameObject.GetComponent<CustomEventData>();
				___eventDescKey = data.eventDescKey;
				___eventType = data.myEvent;
				___minEventTime = data.minEventTime;
				___maxEventTime = data.maxEventTime;
				__instance.gameObject.SetActive(true); // Set active the clone of the gameobject to not cause IEnumerator issues
			}
		}
	}

	[HarmonyPatch(typeof(FogEvent))]
	internal class PutNewBeautifulAudio // Patches fog method and replaces the og clip with a modified one that has the beatiful wind noise
	{
		[HarmonyPatch("Begin")]
		private static void Prefix(ref SoundObject ___music)
		{
			___music.soundClip = ContentAssets.GetAsset<AudioClip>("fogNewSong");
		}
	}

	// Patch for builders

	[HarmonyPatch(typeof(RoomBuilder), "Build")]
	internal class AddExtraStepsToBuilder1
	{
		private static void Postfix(RoomController ___room, LevelBuilder ___lg, System.Random ___cRNG, RoomBuilder __instance)
		{
			ContentManager.instance.StartCoroutine(ContentManager.instance.ExecuteRoomBuilderFunctions(___lg, ___room, ___cRNG, __instance));
		}
	}

	[HarmonyPatch(typeof(PlaygroundSpecialRoom), "AfterUpdatingTiles")]
	internal class WindowsForPlayground
	{
		private static void Prefix(PlaygroundSpecialRoom __instance, System.Random ___cRNG, LevelBuilder ___lg)
		{
			var window = ContentManager.instance.AllWindows[0].Object;
			window.windowPre.gameObject.SetActive(true);
			foreach (TileController tileController in __instance.Room.GetTilesOfShape(new List<TileShape>() { TileShape.Single, TileShape.Corner }, true))
			{
				if (___cRNG.NextDouble() * 100d < 30d)
				{
					___lg.Ec.BuildWindow(tileController, tileController.wallDirections[___cRNG.Next(0, tileController.wallDirections.Length)], window);
				}
			}
			window.windowPre.gameObject.SetActive(false);
		}
	}

	[HarmonyPatch(typeof(CafeteriaCreator))]
	internal class CafeteriaPatches
	{
		[HarmonyPatch("AfterUpdatingTiles")]
		[HarmonyPrefix]
		private static void Windows(CafeteriaCreator __instance, System.Random ___cRNG, LevelBuilder ___lg)
		{
			var window = ContentManager.instance.AllWindows[0].Object;
			window.windowPre.gameObject.SetActive(true);
			foreach (TileController tileController in __instance.Room.GetTilesOfShape(new List<TileShape>() { TileShape.Single, TileShape.Corner }, true))
			{
				if (___cRNG.NextDouble() * 100d < 30d)
				{
					___lg.Ec.BuildWindow(tileController, tileController.wallDirections[___cRNG.Next(0, tileController.wallDirections.Length)], window);
				}
			}
			window.windowPre.gameObject.SetActive(false);
		}

		[HarmonyPatch("Initialize")]
		[HarmonyPrefix]
		private static void EmptyTexture(RoomController ___room, System.Random ___cRNG)
		{
			if (___cRNG.NextDouble() > 0.9d) // If the bigroom is not highCeiling thing, uhm, no, but a very low chance because high ceiling is beautiful as heck.
			{
				return;
			}

			___room.ceilingTex = ContentUtilities.EmptyTexture(256, 256);
			___room.lightPre = null;
		}

		[HarmonyPatch("AfterUpdatingTiles")]
		[HarmonyPostfix]
		private static void AddHigherCeilingsAndItems(CafeteriaCreator __instance, RoomController ___room, LevelBuilder ___lg, System.Random ___cRNG)
		{

			if (___room.lightPre) return; // if there is still decoration, this hasn't been affected

			// Add high ceiling for cafeteria
			Texture2D wall = ___room.wallTex;

			__instance.CreateOpenAreaForSpecialRoom(___room, ContentUtilities.Array(wall, wall, wall, wall, wall, wall, ContentAssets.GetAsset<Texture2D>("fadeWall")), ContentUtilities.SolidTexture(256, 256, Color.black));

			__instance.FixElevatorTiles(UnityEngine.Object.Instantiate(ContentManager.Prefabs.specialRoomPre.Room.ceilingTex));

			if (ContentManager.instance.TryGetDecorationTransform(___room.category, true, "cafeHangingLight", out var light))
			{
				IntVector2 cornerOffset = ___room.size.x > ___room.size.z ? new IntVector2(2, 0) : new IntVector2(0, 2);
				var hangingLight = UnityEngine.Object.Instantiate(light);
				hangingLight.SetParent(__instance.transform);
				var tile = ___room.ec.TileFromPos(___room.ec.TileFromPos(___room.ec.RealRoomMid(___room)).position - cornerOffset);
				hangingLight.position = tile.transform.position;
				___room.ec.GenerateLight(tile, ___lg.ld.standardLightColor, ___lg.ld.standardLightStrength);

				hangingLight = UnityEngine.Object.Instantiate(light);
				hangingLight.SetParent(__instance.transform);
				tile = ___room.ec.TileFromPos(___room.ec.TileFromPos(___room.ec.RealRoomMid(___room)).position + cornerOffset);
				___room.ec.GenerateLight(tile, ___lg.ld.standardLightColor, ___lg.ld.standardLightStrength);
				hangingLight.position = tile.transform.position; // Adds hanging lights twice
			}


			WeightedItemObject[] cafeItems = ContentManager.instance.CafeteriaItems.ToArray();


			
			var amount = ___cRNG.Next(1, 6);
			var room = __instance.GetComponent<RoomController>();
			for (int i = 0; i < amount; i++)
			{
				if (room.itemSpawnPoints.Count == 0) break; // If there are no spawn points left

				___lg.Ec.CreateItem(room, WeightedItemObject.ControlledRandomSelection(cafeItems, ___cRNG), ___cRNG);
			}
			


		}
	}

	[HarmonyPatch(typeof(LibraryCreator), "AfterUpdatingTiles")]
	internal class ExtraFunctionsForLibraries
	{
		[HarmonyPrefix]
		private static void ExtraItems(System.Random ___cRNG, ref int ___maxEndItems, RoomController ___room)
		{
			int amount = ___room.GetTilesOfShape(new List<TileShape> { TileShape.End }, false).Count;
			if (amount == 0)
				return;
			int halfAmount = amount / 2;

			___maxEndItems = halfAmount + ___cRNG.Next(halfAmount + 1); // Half of items are filled + the changes of another half
		}
		[HarmonyPostfix]
		private static void CenterRareItem(System.Random ___cRNG, RoomController ___room)
		{
			___room.ec.CreateItem(___room, WeightedItemObject.ControlledRandomSelection(ContentManager.instance.GlobalItems.ToArray(), ___cRNG), ___room.ec.RealRoomMid(___room) + (Vector3.up * 5f));
		}
	}

	// Gameplay Patches

	[HarmonyPatch(typeof(HappyBaldi))]
	internal class ByeAnimation
	{
		[HarmonyPatch("Activate")]
		[HarmonyPostfix]
		private static void AfterLeaving(HappyBaldi __instance, ref SpriteRenderer ___sprite)
		{
			if (Singleton<CoreGameManager>.Instance.currentMode != Mode.Free) return;

			__instance.StartCoroutine(WaitToBaldiDisappear(___sprite, __instance));

			IEnumerator WaitToBaldiDisappear(SpriteRenderer baldiRenderer, HappyBaldi baldi) // Waits specifically to baldi despawn so this second baldi can just kick in
			{
				while (baldiRenderer.enabled) { yield return null; }

				PrefabInstance.SpawnPrefab<BaldiGoesAway>(baldi.transform.position, default, baldi.Ec);

				yield break;
			}
			
		}
	}

	[HarmonyPatch(typeof(ITM_GrapplingHook), "Update")]

	internal class BreakWindowsFromGrap
	{
		private static void Prefix(LayerMaskObject ___layerMask, ITM_GrapplingHook __instance, ref bool ___locked, PlayerManager ___pm)
		{
			if (___locked) return;

			if (Physics.Raycast(__instance.transform.position, __instance.transform.forward, out RaycastHit raycastHit, 5f, ___layerMask.mask, QueryTriggerInteraction.Collide) && raycastHit.transform.CompareTag("Window"))
			{
				raycastHit.transform.GetComponent<Window>().Break(true);
				if (raycastHit.transform.GetComponent<WindowExtraFields>().IsBroken)
				{
					___pm.RuleBreak("breakproperty", 1f);
					___locked = false;
				}
			}
		}
	}

	[HarmonyPatch(typeof(Principal), "Scold")]
	internal class CustomScolds // Give principal custom scolds with custom audios
	{
		private static void Postfix(string brokenRule, AudioManager ___audMan)
		{
			if (brokenRule != null)
			{
				var sound = ContentManager.instance.GetPrincipalLine(brokenRule);
				if (sound != null) ___audMan.QueueAudio(sound);
			}
		}
	}

	[HarmonyPatch(typeof(SpecialRoomCreator), "Initialize")]
	internal class PlaygroundElevator // Allow elevators in playground
	{
		private static void Postfix(SpecialRoomCreator __instance)
		{
			if (__instance.obstacle == Obstacle.Playground)
				__instance.Room.acceptsExits = true;
		}
	}

	[HarmonyPatch(typeof(RandomEvent), "Initialize")]
	internal class RegisterMyself // Add the event to a global event list
	{
		private static void Prefix(RandomEvent __instance)
		{
			EnvironmentExtraVariables.events.Add(__instance);
		}
	}

	[HarmonyPatch(typeof(Beans), "PlayNPC")]
	internal class GuiltyBeans_NPC
	{
		[HarmonyPrefix]
		private static void Prefix(Beans __instance)
		{
			AccessTools.Method(typeof(NPC), "SetGuilt").Invoke(__instance, new object[] { 10f, "gumming" }); // Calls the SetGuilt method for beans
		}
	}

	[HarmonyPatch(typeof(Beans), "PlayPlayer")]
	internal class GuiltyBeans_Player
	{
		[HarmonyPrefix]
		private static void Prefix(Beans __instance)
		{
			AccessTools.Method(typeof(NPC), "SetGuilt").Invoke(__instance, new object[] { 10f, "gumming" }); // Calls the SetGuilt method for beans
		}
	}

	[HarmonyPatch(typeof(NPC), "ClearGuilt")]
	internal class ActualGuiltAction
	{
		private static void Postfix(ref float ___guiltTime, NPC __instance)
		{
			if (___guiltTime > 0f)
			{
				IEnumerator InGuiltJail(float time, NPC npc)
				{
					if (!npc.Navigator.enabled) yield break;

					float timer = time;
					var modifier = new MovementModifier(Vector3.zero, 0f);
					npc.GetComponent<ActivityModifier>().moveMods.Add(modifier);

					while (timer > 0f)
					{
						timer -= Time.deltaTime * npc.ec.NpcTimeScale;
						yield return null;
					}
					npc.GetComponent<ActivityModifier>().moveMods.Remove(modifier);
					yield break;
				}

				__instance.StartCoroutine(InGuiltJail(15f, __instance));
			}
			___guiltTime = 0f;
		}

	}

	[HarmonyPatch(typeof(ItemManager), "UseItem")]
	internal class FixItemInactive // Replaces the current UseItem method with one that instances the item and also sets it active (for custom items)
	{
		private static bool Prefix(ItemManager __instance)
		{
			Item obj = UnityEngine.Object.Instantiate(__instance.items[__instance.selectedItem].item);
			obj.gameObject.SetActive(true);
			if (obj.Use(__instance.pm))
			{
				__instance.RemoveItem(__instance.selectedItem);
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(MathMachine))]
	internal class AddMoreBalloons
	{
		[HarmonyTranspiler]
		[HarmonyPatch("ReInit")]
		private static IEnumerable<CodeInstruction> AddMoreBals(IEnumerable<CodeInstruction> instructions)
		{
			bool patchedAmount = false;
			using (var enumerator = instructions.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					var instruction = enumerator.Current;
					if (!patchedAmount && instruction.Is(OpCodes.Ldc_I4_S, 10))
					{
						instruction.operand = 19; // should be 19 here
						patchedAmount = true;
					}
					yield return instruction;
				}
			}
		}
		[HarmonyTranspiler]
		[HarmonyPatch("NewProblem")]
		private static IEnumerable<CodeInstruction> AddMoreMathQuestions(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			int i = codes.IndexAt(x => x.opcode == OpCodes.Ldloc_2);
			codes[i] = new CodeInstruction(OpCodes.Nop);
			codes[i + 1] = new CodeInstruction(OpCodes.Nop);
			codes[codes.IndexAt(x => x.opcode == OpCodes.Ldc_I4_0) + 3].opcode = OpCodes.Ldc_I4_1; // Higher up the chance of having addition instead of subtraction
			return codes.AsEnumerable();
		}

		[HarmonyPrefix]
		[HarmonyPatch("ReInit")]
		private static void AddMoreBalObjects(MathMachine __instance, ref MathMachineNumber[] ___numberPres, out object[] __state)
		{
			__instance.Corrupt(false);
			List<MathMachineNumber> bals = new List<MathMachineNumber>();
			var ogList = new List<MathMachineNumber>(___numberPres);
			for (int i = 0; i < 9; i++)
			{
				MathMachineNumber number = UnityEngine.Object.Instantiate(___numberPres[0]);
				AccessTools.Field(typeof(MathMachineNumber), "value").SetValue(number, i + 10);
				var sprite = (Transform)AccessTools.Field(typeof(MathMachineNumber), "sprite").GetValue(number);
				sprite.GetComponent<SpriteRenderer>().sprite = ContentAssets.GetAsset<Sprite>("bal" + i + 10);
				___numberPres = ___numberPres.AddToArray(number);
				bals.Add(number);
			}
			__state = new object[2];
			__state[0] = bals;
			__state[1] = ogList;
		}

		[HarmonyPostfix]
		[HarmonyPatch("ReInit")]
		private static void RemoveUnusedBalObjects(object[] __state, ref MathMachineNumber[] ___numberPres)
		{
			foreach (var num in (List<MathMachineNumber>)__state[0])
			{
				UnityEngine.Object.Destroy(num.gameObject);
			}

			___numberPres = ((List<MathMachineNumber>)__state[1]).ToArray();
		}

		[HarmonyPostfix]
		[HarmonyPatch("NumberDropped")]
		private static void ChangeAnswerSize(MathMachine __instance)
		{
			var text = __instance.transform.Find("Answer").GetComponent<TMP_Text>();
			text.autoSizeTextContainer = false;
			text.autoSizeTextContainer = true;
		}
	}

	[HarmonyPatch(typeof(MathMachineNumber), "Pop")] // Pop animation
	internal class BalloonPopAnimation
	{
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) // Removes the instruction that disables the sprite
		{
			var list = new List<CodeInstruction>(instructions);
			int index = list.FindIndex(x => x.opcode == OpCodes.Ldarg_0);
			for (int i = 0; i < 5; i++)
			{
				list.RemoveAt(index);
			}

			return list.AsEnumerable();
		}
		[HarmonyPrefix]
		private static void ReplaceSprite(Transform ___sprite)
		{
			___sprite.GetComponent<SpriteRenderer>().sprite = ContentAssets.GetAsset<Sprite>("balExploding"); // Add a prefix then
		}
	}

	[HarmonyPatch()]
	internal class PatchBeltBuilders
	{
		static MethodBase TargetMethod() // Target the right method to patcj
		{
			return AccessTools.FirstMethod(typeof(BeltBuilder), x => x.Name == "Build" && x.IsPrivate);
		}

		private static void Postfix(BeltManager beltManager)
		{
			var speedField = AccessTools.Field(typeof(BeltManager), "speed");
			float speed = (float)speedField.GetValue(beltManager);
			speed += EnvironmentExtraVariables.lb.controlledRNG.Next(-EnvironmentExtraVariables.MaxConveyorSpeedOffset, EnvironmentExtraVariables.MaxConveyorSpeedOffset); // Sets a random speed for the conveyor
			beltManager.SetSpeed(speed);
			EnvironmentExtraVariables.belts.Add(beltManager, speed); // Stores the belt manager for public use
		}
	}

	// Patches for the blackout event

	[HarmonyPatch(typeof(SodaMachine), "ItemFits")]
	internal class BlackoutEventPatch_Sodas
	{
		private static bool Prefix()
		{
			return !BlackOut.OutageGoing; // If outage going, then return false to not use the soda machine
		}
	}

	
	[HarmonyPatch()]
	internal class MathMachineWOOOW
	{
		[HarmonyTargetMethod]
		private static MethodBase PointOutTheRightOne()
		{
			return AccessTools.Method(typeof(MathMachine), "Completed", new Type[] { typeof(int), typeof(bool), typeof(Activity) }); // Points out the second one
		}

		[HarmonyPrefix]
		private static void RegisterMachine(MathMachine __instance, AudioManager ___audMan, bool correct)
		{
			EnvironmentExtraVariables.completedMachines.Add(__instance);
			if (correct)
				___audMan.PlaySingle(ContentAssets.GetAsset<SoundObject>("baldi_WOW"));
		}
	}

	[HarmonyPatch(typeof(MathMachine))]
	internal class BlackoutEventPatch_MathMachine
	{
		[HarmonyPrefix]
		[HarmonyPatch("Start")]
		private static void MoreQuestions(ref int ___totalProblems)
		{
			___totalProblems = WeightedSelection<int>.RandomSelection(EnvironmentExtraVariables.MaxNewProblems);
		}

		[HarmonyPrefix]
		[HarmonyPatch("Clicked")]
		private static bool DisableClick() // If outage is going, disable interaction
		{
			return !BlackOut.OutageGoing;
		}

		[HarmonyPrefix]
		[HarmonyPatch("Update")]
		private static bool DisableMachineDisplay(ref TMP_Text ___signText, ref TMP_Text ___val2Text, ref TMP_Text ___val1Text, ref TMP_Text ___answerText)
		{
			if (BlackOut.OutageGoing) // Disables display
			{
				___signText.text = "";
				___val1Text.text = "";
				___val2Text.text = "";
				___answerText.text = "";
				return false;
			}
			return true; // Normal
		}
	}

	[HarmonyPatch(typeof(GameButtonBase), "Clicked")]
	internal class BlackoutEventPatch_ButtonClick
	{
		private static bool Prefix()
		{
			return !BlackOut.OutageGoing; // If outage going, disable button click
		}
	}

	[HarmonyPatch(typeof(StandardDoor), "ItemFits")]
	internal class PatchDoorUnlockFitting
	{
		private static void Postfix(StandardDoor __instance, ref bool __result, Items item)
		{
			if (!__instance.GetComponent<StandardDoor_ExtraFunctions>() || !__instance.locked) return; // If component doesn't exist or the door isn't locked, there's no reason to change it then

			__result = __instance.GetComponent<StandardDoor_ExtraFunctions>().ItemFittingFunction(item, __instance);
		}
	}

	[HarmonyPatch(typeof(GameCamera), "LateUpdate")]
	internal class UpdateCameraFOV
	{
		private static void Postfix(GameCamera __instance)
		{
			var sum = EnvironmentExtraVariables.GetFOVSum();
			var fov = EnvironmentExtraVariables.PlayerDefaultFOV + sum;
			EnvironmentExtraVariables.CurrentFOV = sum;

			__instance.camCom.fieldOfView = fov;
			__instance.billboardCam.fieldOfView = fov;
			Singleton<GlobalCam>.Instance.Cam.fieldOfView = fov;
		}
	}

	[HarmonyPatch(typeof(Cumulo), "FindDestination")]
	internal class CC_PahAtEnd
	{
		private static void Prefix(Cumulo __instance, List<List<TileController>> ___halls)
		{
			if (___halls.Count > 0)
			{
				ItemSoundHolder.CreateSoundHolder(__instance.transform, ContentAssets.GetAsset<SoundObject>("cumulo_PAH"), true, minDistance: 40f, maxDistance: 80f); // On the end of the blowtimer, just play this noise
			}
		}
	}

	[HarmonyPatch(typeof(Window))]
	internal class WindowPatches
	{
		[HarmonyPatch("Break")]
		[HarmonyPrefix]
		private static bool UnbreakableFeature(Window __instance)
		{
			if (!__instance.GetComponent<WindowExtraFields>())
				return true; // Already skips if field isn't found

			if (__instance.GetComponent<WindowExtraFields>().IsUnbreakable)
			{
				ItemSoundHolder.CreateSoundHolder(__instance.transform, ContentAssets.GetAsset<SoundObject>("windowHit"), true, 40, 70);
				return false;
			}

			return true;
		}

		[HarmonyPatch("Initialize")]
		[HarmonyPrefix]
		private static void Setup(Window __instance, WindowObject wObject)
		{
			var field = wObject.windowPre.GetComponent<WindowExtraFields>();
			if (field)
			{
				__instance.GetComponent<WindowExtraFields>().CopyFields(field);
				__instance.openOnStart = __instance.GetComponent<WindowExtraFields>().OpenByDefault;
			}
		}

		[HarmonyPatch("Start")]
		[HarmonyPatch("OnDestroy")]
		[HarmonyFinalizer]
		private static Exception ShutExceptions()
		{
			return null;
		}
	}

	[HarmonyPatch(typeof(Gum), "OnTriggerEnter")]
	internal class GumOnWall
	{
		private static void Prefix(Gum __instance, Collider other) // Just refer gum as the instance before doing the transpiler stuff
		{
			gum = __instance;
			GumOnWall.other = other;
		}

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var hide = AccessTools.Method(typeof(Gum), "Hide");
			bool foundMethod = false;
			bool addedContent = false;

			using (var enumerator = instructions.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					var instruction = enumerator.Current;
					if (!foundMethod && instruction.Is(OpCodes.Call, hide))
						foundMethod = true;

					else if (!addedContent && foundMethod)
					{
						addedContent = true;
						yield return Transpilers.EmitDelegate<Action>(() =>
						{
							if (EnvironmentExtraVariables.currentFloor == Floors.None) return;

							var pos = other.transform.position - (gum.transform.forward * 0.2f);
							pos.y = 4.7f;
							var backPos = pos + (gum.transform.forward * 0.02f);

							var back = PrefabInstance.SpawnPrefab<GumInWall>(backPos, Quaternion.Inverse(gum.transform.rotation), gum.ec, false);
							back.SetAsBackObject(PrefabInstance.SpawnPrefab<GumInWall>(pos, Quaternion.Inverse(gum.transform.rotation), gum.ec).gameObject);
							back.Execute();

						});
					}
					yield return instruction;
				}
			}
		}

		private static Gum gum;

		private static Collider other;
	}

	[HarmonyPatch(typeof(SubtitleController), "PositionSub")]

	internal class SubtitleFix // Basically changes the sub distance based on the FOV change
	{
		private static void Prefix(SubtitleController __instance, out float __state)
		{
			__state = __instance.distance;
			if (EnvironmentExtraVariables.CurrentFOV < 0f)
				__instance.distance = Mathf.Max(1f, __instance.distance - (EnvironmentExtraVariables.CurrentFOV * 2f));
		}

		private static void Postfix(SubtitleController __instance, float __state)
		{
			__instance.distance = __state;
		}
	}

	[HarmonyPatch(typeof(FarmTripManager), "Initialize")]
	internal class AddSheepsBEEH // Add sheeps duh
	{
		private static void Prefix(ref List<FarmAnimalType> ___animalTypes, ref List<FarmAnimalType> ___potentialTypes)
		{
			if (!___animalTypes.Contains(FarmAnimalType.Sheep))
			{
				___animalTypes.Add(FarmAnimalType.Sheep);
			}

			if (!___potentialTypes.Contains(FarmAnimalType.Sheep))
			{
				___potentialTypes.Add(FarmAnimalType.Sheep);
			}
		}
	}
	[HarmonyPatch(typeof(ITM_Scissors), "Use")]
	internal class CutASBully
	{
		private static void Prefix(PlayerManager pm)
		{
			if (pm.jumpropes.Count > 0)
			{
				pm.RuleBreak("Bullying", 2f);
			}
		}

		private static void Postfix(bool __result, PlayerManager pm)
		{
			if (__result)
			{
				ItemSoundHolder.CreateSoundHolder(pm.transform, ContentAssets.GetAsset<SoundObject>("sc_cut"), false, 40, 60);
			}
		}
	}

	[HarmonyPatch(typeof(ITM_ZestyBar), "Use")]
	internal class EatZesty
	{
		private static void Prefix(PlayerManager pm)
		{
			ItemSoundHolder.CreateSoundHolder(pm.transform, ContentAssets.GetAsset<SoundObject>("zesty_eat"), false, 40, 60);
		}
	}

	[HarmonyPatch(typeof(PlayerMovement), "StaminaUpdate")]
	public class StaminaRisingPatch
	{
		private static void Prefix(out float[] __state, PlayerMovement __instance) // Gets the stamina thing
		{
			__state = new float[] { __instance.staminaRise, __instance.staminaDrop, __instance.staminaMax };
			if (staminaModifiers.Count == 0) return;
			LookForType(ref __instance.staminaRise, StaminaToken.ModifierType.Rise);
			LookForType(ref __instance.staminaDrop, StaminaToken.ModifierType.Drop);
			LookForType(ref __instance.staminaMax, StaminaToken.ModifierType.Max);

			void LookForType(ref float val, StaminaToken.ModifierType type)
			{
				foreach (var mod in staminaModifiers)
				{
					if (mod.MyType == type)
						val *= mod.Value;
				}
			}
		}

		private static void Postfix(float[] __state, PlayerMovement __instance) // Reset the stamina variables for later
		{
			__instance.staminaRise = __state[0];
			__instance.staminaDrop = __state[1];
			__instance.staminaMax = __state[2];
		}

		public readonly static List<StaminaToken> staminaModifiers = new List<StaminaToken>();

		public class StaminaToken : GenericToken<float>
		{
			public enum ModifierType
			{
				Rise,
				Drop,
				Max
			}
			public StaminaToken(ModifierType type, float value) : base(Mathf.Max(0f, value), 0)
			{
				MyType = type;
			}

			public override float Value { get => base.Value; set => base.Value = Mathf.Max(0f, value); }

			public ModifierType MyType { get; }
		}
	}

	[HarmonyPatch(typeof(PlayerMovement), "Start")]
	internal class AddCustomAttributes
	{
		private static void Prefix(PlayerMovement __instance)
		{
			if (!__instance.GetComponent<CustomPlayerAttributes>())
				__instance.gameObject.AddComponent<CustomPlayerAttributes>(); // Add this
		}
	}

	[HarmonyPatch(typeof(Looker), "Update")]
	public class LookerDistancingPatch
	{
		private static void Prefix(Looker __instance, ref float ___distance, out float __state)
		{
			__state = ___distance;
			if (lookerModifiers.Count == 0) return;

			var vals = new List<float>();
			foreach (var token in lookerModifiers)
			{
				if (!ReferenceEquals(__instance, token.Target)) continue; // If it is not about the same target, then it is not for it

				vals.Add(token.Value);
				
			}

			if (vals.Count == 0) return; // Prevents from using an empty list, duuh

			___distance = Mathf.Min(___distance, vals.Min());


		}

		private static void Postfix(float __state, ref float ___distance)
		{
			___distance = __state;
		}

		public readonly static List<LookerToken> lookerModifiers = new List<LookerToken>();

		public class LookerToken : GenericToken<float>
		{
			public LookerToken(float value, Looker target) : base(Mathf.Max(minVal, value), 0)
			{
				Target = target;
			}

			public override float Value { get => base.Value; set => base.Value = Mathf.Max(minVal, value); }

			public Looker Target { get; }

			const float minVal = 0f;
		}
	}

	[HarmonyPatch(typeof(FieldTripManager), "End")]
	internal class FieldTripWinNoise
	{
		private static void Prefix(int rank, ref AudioManager ___baldiMan)
		{
			if (rank >= 3)
			{
				___baldiMan.PlaySingle(ContentAssets.GetAsset<SoundObject>("winFieldTrip"));
			}
		}
	}

	[HarmonyPatch(typeof(Door), "Block")] // Makes it so it doesn't logs exceptions
	internal class NoExceptionInWindows
	{
		private static Exception Finalizer()
		{
			return null;
		}
	}

	[HarmonyPatch(typeof(FacultyBuilderStandard), "Build")]
	internal class FacultyBuilderHasMoreWindowsNow
	{
		private static void Prefix(ref float ___windowChance)
		{
			___windowChance = 44f; // Increases chance lol
		}
	}




}