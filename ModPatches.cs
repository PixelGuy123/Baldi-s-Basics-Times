using BB_MOD.Events;
using BB_MOD.Extra;
using BB_MOD.NPCs;
using HarmonyLib;
using MTM101BaldAPI.AssetManager;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
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

			if (currentFloor != Floors.END && !ContentManager.instance.HasAccessedFloor(currentFloor))
			{
				ContentManager.instance.AddLevelObject(Object.Instantiate(__instance.ld));
			}

			__instance.ld.previousLevels = ContentManager.instance.GetLevelObjectCopy(__instance.ld);

			if (!ContentManager.instance.beans)
			{
				try
				{
					ContentManager.instance.beans = WeightedNPC.Convert(__instance.ld.potentialNPCs).First(x => x.selection.Character == Character.Beans).selection.gameObject; // Finds the first Beans instance to be used
				}
				catch
				{
					Debug.LogWarning("Beans somehow doesn\'t exist on the npc list, the mod won\'t spawn new npcs");
					goto items;
				}
			}



			ContentManager.instance.SetupWeightNPCValues();


			__instance.ld.potentialNPCs.AddRange(ContentManager.instance.GetNPCs(currentFloor));

		items: // Item Stuff

			ContentManager.instance.SetupItemWeights();

			__instance.ld.items = __instance.ld.items.AddRangeToArray(ContentManager.instance.GetItems(currentFloor).ToArray());

			__instance.ld.shopItems = __instance.ld.shopItems.AddRangeToArray(ContentManager.instance.GetShoppingItems(currentFloor).ToArray());


			if (__instance.ld.fieldTrip)
				__instance.ld.fieldTripItems.AddRange(ContentManager.instance.FieldTripItems); // Add field trip items


			// Event Stuff

			ContentManager.instance.SetupEventWeights();

			__instance.ld.randomEvents.AddRange(ContentManager.instance.GetEvents(currentFloor)); // Add queued events for the floor



			// Literally anything else


			// Reviving vent builder

			var ventBuilder = new GameObject("ventBuilder", typeof(VentBuilder));

			Object.DontDestroyOnLoad(ventBuilder);

			ventBuilder.SetActive(false);

			List<ObjectBuilder> builders = new List<ObjectBuilder>(__instance.ld.forcedSpecialHallBuilders);
			if (!accessedFloor)
				builders.Add(ventBuilder.GetComponent<VentBuilder>());


			__instance.ld.forcedSpecialHallBuilders = builders.ToArray();

			// Replacing Office's Door

			var newMat = ScriptableObject.CreateInstance<StandardDoorMats>();
			newMat.open = new Material(__instance.ld.classDoorMat.open) { mainTexture = AssetManager.TextureFromFile(Path.Combine(ContentManager.modPath, "Textures", "officeDoor_Open.png")) };
			newMat.shut = new Material(__instance.ld.classDoorMat.shut) { mainTexture = AssetManager.TextureFromFile(Path.Combine(ContentManager.modPath, "Textures", "officeDoor_Closed.png")) };
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
						__instance.ld.maxLightDistance -= 10;
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
						__instance.ld.maxLightDistance -= 5;
						break;
					case Floors.F3:
						__instance.ld.maxClassRooms = 12;
						__instance.ld.minSize += new IntVector2(8, 10);
						__instance.ld.maxSize += new IntVector2(20, 20);
						__instance.ld.maxPlots += 3;
						__instance.ld.minHallsToRemove += 1;
						__instance.ld.minReplacementHalls += 1;
						__instance.ld.maxReplacementHalls += 3;
						__instance.ld.minFacultyRooms += 1;
						__instance.ld.maxFacultyRooms += 2;
						__instance.ld.additionalNPCs += 5;
						__instance.ld.maxLightDistance = 35;
						break;
				}
			}

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

			ContentManager.instance.LockAccessedFloor(currentFloor); // On the end of the patch, so the features aren't applied twice

			if (!ContentManager.instance.sweepPoster)
			{
				var sweep = Resources.FindObjectsOfTypeAll<GottaSweep>().First();
				ContentManager.instance.sweepPoster = Object.Instantiate(sweep.Poster.baseTexture);
				ContentManager.instance.sweepSprite = Object.Instantiate(sweep.spriteBase.transform.Find("Sprite").GetComponent<SpriteRenderer>().sprite);
			}


		}
	}

	[HarmonyPatch(typeof(BaseGameManager), "Initialize")]
	internal class AfterGen
	{
		private static void Prefix(int ___levelNo)
		{
			var rng = new System.Random(Singleton<CoreGameManager>.Instance.Seed() + ___levelNo);

			var currentFloor = EnvironmentExtraVariables.currentFloor;
		}
	}

	[HarmonyPatch(typeof(VentBuilder), "Build")]
	internal class SetupVentBuilder
	{
		[HarmonyPrefix]
		private static void MakeVents(ref Transform ___ventCornerPre, ref Transform ___ventStriaghtPre, ref Transform ___ventTPre) // Setting Vent Builder Transforms (Apparently, they don't exist on main game)
		{
			var vent = GameObject.Find("Vent");
			if (!vent)
			{
				var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
				obj.name = "Vent";
				Object.DontDestroyOnLoad(obj);

				Material ventMat = Resources.FindObjectsOfTypeAll<Material>().First(x => x.name.ToLower() == "vent");
				ventMat.mainTexture = AssetManager.TextureFromFile(Path.Combine(ContentManager.modPath, "Textures", "ventAtlas.png"));

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
				;
				// Done with Audio Stuff

				obj.AddComponent<Vent>();
				obj.SetActive(false);

				vent = obj;
			}




			___ventCornerPre = vent.transform;
			___ventStriaghtPre = vent.transform;
			___ventTPre = vent.transform;

		}
		[HarmonyPostfix]
		private static void MakeVentsPositionsALittleBetter(System.Random cRng) // as the method name suggests
		{
			var vents = Object.FindObjectsOfType<Vent>(true).Where(x => x.gameObject.name.ToLower().Contains("clone"));
			vents.Do(x => x.gameObject.SetActive(true));
			vents.Do(x => x.transform.localPosition = new Vector3(cRng.Next(2) == 0 ? (float)cRng.NextDouble() * 2f : (float)cRng.NextDouble() * -2f, 9f, cRng.Next(2) == 0 ? (float)cRng.NextDouble() * 2f : (float)cRng.NextDouble() * -2f)); // Random value between -5 and 5 as x and z offset
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
			Object.Destroy(__instance.gameObject);
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


					EnvironmentExtraVariables.ec.npcsToSpawn[index2].spriteBase.transform.Find("Sprite").GetComponent<SpriteRenderer>().sprite = AssetManager.SpriteFromTexture2D(AssetManager.TextureFromFile(Path.Combine(ContentManager.modPath, "Textures", "npc", "old_sweep.png")), new Vector2(0.5f, 0.5f), 20f);
					EnvironmentExtraVariables.ec.npcsToSpawn[index2].Poster.baseTexture = AssetManager.TextureFromFile(Path.Combine(ContentManager.modPath, "Textures", "npc", "pri_oldsweep.png"));
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
			__instance.gameObject.transform.Find("Image").GetComponent<Image>().sprite = AssetManager.SpriteFromTexture2D(AssetManager.TextureFromFile(Path.Combine(ContentManager.modPath, "Textures", "otherMainMenu.png"))); // Changes main menu texture to my beautiful one lol
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
		private static void Prefix(Looker __instance, ref NPC ___npc)
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
			___music.soundClip = AssetManager.AudioClipFromFile(Path.Combine(ContentManager.modPath, "Audio", "new_CreepyOldComputer.wav"));
		}
	}



}
