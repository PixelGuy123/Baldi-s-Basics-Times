using BB_MOD.Events;
using BB_MOD.Extra;
using BB_MOD.NPCs;
using HarmonyLib;
using MonoMod.Utils;
using MTM101BaldAPI;
using Rewired.Libraries.SharpDX.RawInput;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BB_MOD
{

	[HarmonyPatch(typeof(LevelGenerator), "StartGenerate")]
	internal class SetupExtraContent // Setup items, npcs, events, etc.
	{
		private static void Prefix(LevelGenerator __instance)
		{
			EnvironmentExtraVariables.ResetVariables();

			var sceneObject = Singleton<CoreGameManager>.Instance.sceneObject;
			Floors currentFloor = sceneObject.levelTitle.ToFloorIdentifier();
			bool accessedFloor = ContentManager.instance.HasAccessedFloor(currentFloor);
			EnvironmentExtraVariables.currentFloor = currentFloor;
			EnvironmentExtraVariables.ec = __instance.Ec;
			EnvironmentExtraVariables.lb = __instance;

			// Add custom posters

			if (!ContentManager.instance.posterPre)
			{
				try
				{
					ContentManager.instance.posterPre = __instance.ld.posters.First().selection;
				}
				catch
				{
					Debug.LogWarning("Unable to grab a poster object for instancing, custom posters won\'t be added to the level");
					goto skipPoster;
				}
			}
			ContentManager.instance.SetupPosterWeights();
			if (!accessedFloor)
			{
				__instance.ld.posters = __instance.ld.posters.AddRangeToArray(ContentManager.instance.AllPosters(false).ToArray());

				__instance.ld.chalkBoards = __instance.ld.chalkBoards.AddRangeToArray(ContentManager.instance.AllPosters(true).ToArray());
			}

			if (!ContentManager.instance.decorationPre)
				ContentManager.instance.decorationPre = ((WeightedTransform[])AccessTools.Field(typeof(RoomBuilder), "decorations").GetValue(__instance.ld.facultyBuilders[0].selection))[0].selection; // Get decoration

			if (currentFloor != Floors.END)
			{
				if (!ContentManager.instance.HasAccessedFloor(currentFloor))
					ContentManager.instance.AddLevelObject(UnityEngine.Object.Instantiate(__instance.ld));
				__instance.ld.previousLevels = ContentManager.instance.GetLevelObjectCopy(__instance.ld); // Basically replaces the previousLevels variable that is used on the npcs spawn, so npcs from specific floor aren't includes on unexpected ones
			}

			



			if (!ContentManager.instance.beans)
			{
				try
				{
					ContentManager.instance.beans = Character.Beans.GetFirstInstance().gameObject; // Finds the first Beans instance to be used
					if (!ContentManager.instance.beans) throw new ArgumentNullException();
				}
				catch
				{
					Debug.LogWarning("Beans somehow doesn\'t exist on the npc list, the mod won\'t spawn new npcs");
					goto items;
				}
			}



			ContentManager.instance.SetupWeightNPCValues(); // Anything that starts with Setup is... to setup :)


			__instance.ld.potentialNPCs.AddRange(ContentManager.instance.GetNPCs(currentFloor));

		items: // Skip Npcs Part

			ContentManager.instance.SetupObjectBuilders();

			if (!accessedFloor)
			{
				// Hall Builders
				__instance.ld.standardHallBuilders = __instance.ld.standardHallBuilders.AddRangeToArray(ContentManager.instance.StandardHallBuilders.ToArray());
				__instance.ld.forcedSpecialHallBuilders = __instance.ld.forcedSpecialHallBuilders.AddRangeToArray(ContentManager.instance.GetForcedHallBuilders(currentFloor).ToArray());
				__instance.ld.specialHallBuilders = __instance.ld.specialHallBuilders.AddRangeToArray(ContentManager.instance.GetObjectBuilders(currentFloor).ToArray());

				// Room Builders

				__instance.ld.classBuilders = __instance.ld.classBuilders.AddRangeToArray(ContentManager.instance.GetNewRoomBuilders(RoomCategory.Class).ToArray());
				__instance.ld.facultyBuilders = __instance.ld.facultyBuilders.AddRangeToArray(ContentManager.instance.GetNewRoomBuilders(RoomCategory.Faculty).ToArray());
				__instance.ld.officeBuilders = __instance.ld.officeBuilders.AddRangeToArray(ContentManager.instance.GetNewRoomBuilders(RoomCategory.Office).ToArray());

			}

			ContentManager.instance.SetupItemWeights();

			__instance.ld.items = __instance.ld.items.AddRangeToArray(ContentManager.instance.GetItems(currentFloor).ToArray());

			__instance.ld.shopItems = __instance.ld.shopItems.AddRangeToArray(ContentManager.instance.GetShoppingItems(currentFloor).ToArray());


			if (__instance.ld.fieldTrip)
				__instance.ld.fieldTripItems.AddRange(ContentManager.instance.FieldTripItems); // Add field trip items


			// Event Stuff

			ContentManager.instance.SetupEventWeights();

			__instance.ld.randomEvents.AddRange(ContentManager.instance.GetEvents(currentFloor)); // Add queued events for the floor

			// Replacing Office's Door

			var newMat = ScriptableObject.CreateInstance<StandardDoorMats>();
			newMat.open = new Material(__instance.ld.classDoorMat.open) { mainTexture = ContentAssets.GetAssetOrCreate<Texture2D>("officeDoorOpen", Path.Combine(ContentManager.modPath, "Textures", "officeDoor_Open.png")) };
			newMat.shut = new Material(__instance.ld.classDoorMat.shut) { mainTexture = ContentAssets.GetAssetOrCreate<Texture2D>("officeDoorClosed", Path.Combine(ContentManager.modPath, "Textures", "officeDoor_Closed.png")) };
			newMat.name = "OfficeDoor_Mat";
			__instance.ld.OfficeDoorMat = newMat; // Changing the office material to a custom one

			// Changing generator parameters

			if (!accessedFloor)
			{
				switch (currentFloor)
				{
					case Floors.F1:
						__instance.ld.maxClassRooms = 5;
						__instance.ld.maxSize += new IntVector2(6, 6);
						__instance.ld.maxPlots += 1;
						__instance.ld.maxFacultyRooms += 1;
						__instance.ld.additionalNPCs += 1;
						break;
					case Floors.F2:
						__instance.ld.minClassRooms = 6;
						__instance.ld.maxClassRooms = 8;
						__instance.ld.minSize += new IntVector2(5, 6);
						__instance.ld.maxSize += new IntVector2(10, 10);
						__instance.ld.maxPlots += 2;
						__instance.ld.exitCount += 1;
						__instance.ld.minHallsToRemove += 1;
						__instance.ld.maxHallsToRemove += 2;
						__instance.ld.maxReplacementHalls += 1;
						__instance.ld.minFacultyRooms += 1;
						__instance.ld.maxFacultyRooms += 2;
						__instance.ld.additionalNPCs += 2;
						__instance.ld.maxLightDistance = 10;
						__instance.ld.standardLightChance = 25;
						__instance.ld.maxSpecialBuilders += 1;
						break;
					case Floors.END:
						__instance.ld.minClassRooms = 6;
						__instance.ld.maxClassRooms = 8;
						__instance.ld.minSize += new IntVector2(5, 6);
						__instance.ld.maxSize += new IntVector2(10, 10);
						__instance.ld.maxPlots += 2;
						__instance.ld.minHallsToRemove += 1;
						__instance.ld.maxHallsToRemove += 2;
						__instance.ld.maxReplacementHalls += 2;
						__instance.ld.maxFacultyRooms += 3;
						__instance.ld.additionalNPCs += 3;
						__instance.ld.maxLightDistance = 12;
						__instance.ld.standardLightChance = 25;
						__instance.ld.maxSpecialBuilders += 2;
						break;
					case Floors.F3:
						__instance.ld.maxClassRooms = 12;
						__instance.ld.minSize += new IntVector2(8, 10);
						__instance.ld.maxSize += new IntVector2(20, 20);
						__instance.ld.minPlotSize -= 1;
						__instance.ld.minPlots += 6;
						__instance.ld.maxPlots += 9;
						__instance.ld.minHallsToRemove += 1;
						__instance.ld.minReplacementHalls += 1;
						__instance.ld.maxReplacementHalls += 3;
						__instance.ld.minFacultyRooms += 3;
						__instance.ld.maxFacultyRooms += 2;
						__instance.ld.additionalNPCs += 5;
						__instance.ld.maxLightDistance = 15;
						__instance.ld.maxSpecialBuilders += 2;
						break;
				}
			}

			

		skipPoster:

			// Adding custom textures
			ContentManager.instance.SetupSchoolTextWeights();

			// Changes classroom stuff

			__instance.ld.classCeilingTexs = __instance.ld.classCeilingTexs.AddRangeToArray(ContentManager.instance.GetSchoolText(currentFloor, SchoolTextType.Ceiling, 0).ToArray());
			__instance.ld.classWallTexs = __instance.ld.classWallTexs.AddRangeToArray(ContentManager.instance.GetSchoolText(currentFloor, SchoolTextType.Wall, 0).ToArray());
			__instance.ld.classFloorTexs = __instance.ld.classFloorTexs.AddRangeToArray(ContentManager.instance.GetSchoolText(currentFloor, SchoolTextType.Floor, 0).ToArray());

			// Changes faculty stuff
			__instance.ld.facultyCeilingTexs = __instance.ld.facultyCeilingTexs.AddRangeToArray(ContentManager.instance.GetSchoolText(currentFloor, SchoolTextType.Ceiling, 1).ToArray());
			__instance.ld.facultyWallTexs = __instance.ld.facultyWallTexs.AddRangeToArray(ContentManager.instance.GetSchoolText(currentFloor, SchoolTextType.Wall, 1).ToArray());
			__instance.ld.facultyFloorTexs = __instance.ld.facultyFloorTexs.AddRangeToArray(ContentManager.instance.GetSchoolText(currentFloor, SchoolTextType.Floor, 1).ToArray());

			// Changes school stuff
			__instance.ld.hallCeilingTexs = __instance.ld.hallCeilingTexs.AddRangeToArray(ContentManager.instance.GetSchoolText(currentFloor, SchoolTextType.Ceiling).ToArray());
			__instance.ld.hallFloorTexs = __instance.ld.hallFloorTexs.AddRangeToArray(ContentManager.instance.GetSchoolText(currentFloor, SchoolTextType.Floor).ToArray());
			__instance.ld.hallWallTexs = __instance.ld.hallWallTexs.AddRangeToArray(ContentManager.instance.GetSchoolText(currentFloor, SchoolTextType.Wall).ToArray());

			if (!ContentManager.instance.sweepPoster)
			{
				var sweep = Resources.FindObjectsOfTypeAll<GottaSweep>().First();
				ContentManager.instance.sweepPoster = UnityEngine.Object.Instantiate(sweep.Poster.baseTexture);
				ContentManager.instance.sweepSprite = UnityEngine.Object.Instantiate(sweep.spriteBase.transform.Find("Sprite").GetComponent<SpriteRenderer>().sprite);
			}

			

			

			ContentManager.instance.SetupExtraContent();

			ContentManager.instance.TurnDecorations(true);



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
			EnvironmentExtraVariables.lb.UpdatePotentialRoomSpawns(EnvironmentExtraVariables.currentFloor == Floors.F1 || EnvironmentExtraVariables.lb.controlledRNG.NextDouble() >= 0.85d))); // If F1, just leave default which is true, otherwise, randomly choose between sticking or not to halls)
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

	[HarmonyPatch(typeof(BaseGameManager), "Initialize")]
	internal class AfterGen
	{
		private static void Prefix(int ___levelNo, BaseGameManager __instance)
		{

			var rng = new System.Random(Singleton<CoreGameManager>.Instance.Seed() + ___levelNo);

			var currentFloor = EnvironmentExtraVariables.currentFloor;

			ContentManager.instance.LockAccessedFloor(currentFloor); // On the end of the patch, so the features aren't applied twice

			ContentManager.instance.TurnDecorations(false);
		}
	}

	[HarmonyPatch(typeof(VentBuilder), "Build")]
	internal class SetupVentBuilder
	{
		[HarmonyPrefix]
		private static void MakeVents(ref Transform ___ventCornerPre, ref Transform ___ventStriaghtPre, ref Transform ___ventTPre, out GameObject __state) // Setting Vent Builder Transforms (Apparently, they don't exist on main game)
		{
				var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
				obj.name = "Vent";

				Material ventMat = UnityEngine.Object.Instantiate(ContentUtilities.FindResourceObjectContainingName<Material>("vent"));
				ventMat.mainTexture = ContentAssets.GetAsset<Texture2D>("ventAtlasText");

				obj.transform.localScale = new Vector3(4f, 2f, 4f);

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

			__state = obj;



			___ventCornerPre = obj.transform;
			___ventStriaghtPre = obj.transform;
			___ventTPre = obj.transform;

		}
		[HarmonyPostfix]
		private static void MakeVentsPositionsALittleBetter(System.Random cRng, GameObject __state) // as the method name suggests
		{
			var vents = ContentUtilities.FindObjectsContainingName<Vent>("clone", true);
			vents.Do(x => x.gameObject.SetActive(true));
			vents.Do(x => x.transform.localPosition = new Vector3(cRng.Next(2) == 0 ? (float)cRng.NextDouble() * 2f : (float)cRng.NextDouble() * -2f, 9f, cRng.Next(2) == 0 ? (float)cRng.NextDouble() * 2f : (float)cRng.NextDouble() * -2f)); // Random value between -5 and 5 as x and z offset
			UnityEngine.Object.Destroy(__state);

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

	// NPC Replacement Code

	[HarmonyPatch(typeof(OfficeBuilderStandard), "Build")]
	internal class InitializeReplacementNPCs
	{
		private static void Prefix() // Basically iterates by randomly choosing a replacement NPC, then gets the array of the NPC and searches for the npc it replaces (random npc from array)
		{


			System.Random rng = new System.Random(Singleton<CoreGameManager>.Instance.Seed());

			// Extra Stuff

			int index2 = EnvironmentExtraVariables.ec.npcsToSpawn.FindIndex(x => x.Character == Character.Sweep); // If gotta sweep exist, then it has a chance of changing to his old texture, else change back
			if (index2 >= 0)
			{
				if (rng.Next(0, 3) == 0)
				{


					EnvironmentExtraVariables.ec.npcsToSpawn[index2].spriteBase.transform.Find("Sprite").GetComponent<SpriteRenderer>().sprite = ContentAssets.GetAsset<Sprite>("oldSweepSprite");
					EnvironmentExtraVariables.ec.npcsToSpawn[index2].Poster.baseTexture = ContentAssets.GetAsset<Texture2D>("oldSweepPoster");
					AccessTools.Field(typeof(GottaSweep), "speed").SetValue(EnvironmentExtraVariables.ec.npcsToSpawn[index2], 70f);
					AccessTools.Field(typeof(GottaSweep), "moveModMultiplier").SetValue(EnvironmentExtraVariables.ec.npcsToSpawn[index2], 1f);
				}
				else
				{

					EnvironmentExtraVariables.ec.npcsToSpawn[index2].spriteBase.transform.Find("Sprite").GetComponent<SpriteRenderer>().sprite = ContentManager.instance.sweepSprite;
					EnvironmentExtraVariables.ec.npcsToSpawn[index2].Poster.baseTexture = ContentManager.instance.sweepPoster;
					AccessTools.Field(typeof(GottaSweep), "speed").SetValue(EnvironmentExtraVariables.ec.npcsToSpawn[index2], 40f);
					AccessTools.Field(typeof(GottaSweep), "moveModMultiplier").SetValue(EnvironmentExtraVariables.ec.npcsToSpawn[index2], 0.9f);
				}
			}


			// Npc Replacement here

			var replacementNPCs = ContentManager.instance.GetNPCs(Singleton<CoreGameManager>.Instance.sceneObject.levelTitle.ToFloorIdentifier(), true);
			if (replacementNPCs.Count == 0) // In case no replacement NPCs exist
				return;

			for (int i = 0; i < 1; i++)
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

				if (characters.Length > 1)
				{
					List<WeightedSelection<NPC>> npcsToChoose = new List<WeightedSelection<NPC>>(); // Creates another weight selection to choose the npc that will be replaced
					foreach (var targetChar in characters)
					{
						int index = EnvironmentExtraVariables.ec.npcsToSpawn.FindIndex(x => x.Character == targetChar);
						if (index >= 0)
						{
							npcsToChoose.Add(new WeightedSelection<NPC>()
							{
								selection = EnvironmentExtraVariables.ec.npcsToSpawn[index],
								weight = 50
							});
						}
					}
					if (npcsToChoose.Count > 0)
					{
						NPC target = WeightedNPC.ControlledRandomSelectionList(npcsToChoose, rng);

						rNpc.spawnableRooms = EnvironmentExtraVariables.ec.npcsToSpawn[EnvironmentExtraVariables.ec.npcsToSpawn.IndexOf(target)].spawnableRooms;
						rNpc.GetComponent<CustomNPCData>().isReplacing = target.Character;


						success = true;

						EnvironmentExtraVariables.ec.npcsToSpawn.Replace(EnvironmentExtraVariables.ec.npcsToSpawn.IndexOf(target), rNpc);
					}
				}
				else
				{
					int index = EnvironmentExtraVariables.ec.npcsToSpawn.FindIndex(x => x.Character == characters[0]);
					if (index >= 0)
					{

						rNpc.spawnableRooms = EnvironmentExtraVariables.ec.npcsToSpawn[index].spawnableRooms;
						rNpc.GetComponent<CustomNPCData>().isReplacing = EnvironmentExtraVariables.ec.npcsToSpawn[index].Character;
						success = true;

						EnvironmentExtraVariables.ec.npcsToSpawn.Replace(index, rNpc);
					}
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
		}
	}


	// ---- Basic NPC Startup ----

	[HarmonyPatch(typeof(NPC), "Awake")]

	internal class SetupCustomNPCs
	{
		private static void Postfix(NPC __instance, ref Character ___character, ref Navigator ___navigator, ref EnvironmentController ___ec, ref bool ___ignoreBelts, ref bool ___aggroed, ref PosterObject ___poster, ref Looker ___looker, ref bool ___ignorePlayerOnSpawn)
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
			var window = ContentUtilities.FindResourceObjectContainingName<WindowObject>("Wood");
			foreach (TileController tileController in __instance.Room.GetTilesOfShape(new List<TileShape>() { TileShape.Single, TileShape.Corner }, true))
			{
				if (___cRNG.NextDouble() * 100d < 30d)
				{
					___lg.Ec.BuildWindow(tileController, tileController.wallDirections[___cRNG.Next(0, tileController.wallDirections.Length)], window);
				}
			}
		}
	}

	[HarmonyPatch(typeof(CafeteriaCreator), "AfterUpdatingTiles")]
	internal class WindowsForCafeteria
	{
		private static void Prefix(CafeteriaCreator __instance, System.Random ___cRNG, LevelBuilder ___lg)
		{
			var window = ContentUtilities.FindResourceObjectContainingName<WindowObject>("Wood");
			foreach (TileController tileController in __instance.Room.GetTilesOfShape(new List<TileShape>() { TileShape.Single, TileShape.Corner }, true))
			{
				if (___cRNG.NextDouble() * 100d < 30d)
				{
					___lg.Ec.BuildWindow(tileController, tileController.wallDirections[___cRNG.Next(0, tileController.wallDirections.Length)], window);
				}
			}
		}
	}

	// Gameplay Patches

	[HarmonyPatch(typeof(ITM_GrapplingHook), "Update")]

	internal class BreakWindowsFromGrap
	{
		private static void Prefix(LayerMaskObject ___layerMask, ITM_GrapplingHook __instance, ref bool ___locked, PlayerManager ___pm)
		{
			if (___locked) return;

			if (Physics.Raycast(__instance.transform.position, __instance.transform.forward, out RaycastHit raycastHit, 5f, ___layerMask.mask, QueryTriggerInteraction.Collide) && raycastHit.transform.CompareTag("Window"))
			{
				raycastHit.transform.GetComponent<Window>().Break(true);
				___pm.RuleBreak("breakproperty", 1f);
				___locked = false;
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
		private static void AddMoreBalObjects(MathMachine __instance, ref MathMachineNumber[] ___numberPres, out List<MathMachineNumber> __state)
		{
			__instance.Corrupt(false);
			List<MathMachineNumber> bals = new List<MathMachineNumber>();
			for (int i = 0; i < 9; i++)
			{
				MathMachineNumber number = UnityEngine.Object.Instantiate(___numberPres[0]);
				AccessTools.Field(typeof(MathMachineNumber), "value").SetValue(number, i + 10);
				var sprite = (Transform)AccessTools.Field(typeof(MathMachineNumber), "sprite").GetValue(number);
				sprite.GetComponent<SpriteRenderer>().sprite = ContentAssets.GetAsset<Sprite>("bal" + i + 10);
				___numberPres = ___numberPres.AddToArray(number);
				bals.Add(number);
			}
			__state = bals;
		}

		[HarmonyPostfix]
		[HarmonyPatch("ReInit")]
		private static void RemoveUnusedBalObjects(List<MathMachineNumber> __state)
		{
			foreach (var num in __state)
			{
				UnityEngine.Object.Destroy(num.gameObject);
			}
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
			int index = list.IndexAt(x => x.opcode == OpCodes.Ldarg_0);
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

		private static void Prefix(BeltManager beltManager)
		{
			var speedField = AccessTools.Field(typeof(BeltManager), "speed");
			float speed = (float)speedField.GetValue(beltManager);
			int constantValue = Singleton<CoreGameManager>.Instance.sceneObject.levelNo * 2;
			speed += EnvironmentExtraVariables.lb.controlledRNG.Next(-1 * constantValue, 1 * constantValue); // Sets a random speed for the conveyor
			speedField.SetValue(beltManager, speed);
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

	[HarmonyPatch(typeof(MathMachine))]

	internal class BlackoutEventPatch_MathMachine
	{
		[HarmonyPrefix]
		[HarmonyPatch("Completed")]
		private static void RegisterMachine(MathMachine __instance)
		{
			EnvironmentExtraVariables.completedMachines.Add(__instance);
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



}
