using BB_MOD.Events;
using BB_MOD.NPCs;
using BB_MOD.Extra;
using HarmonyLib;
using MTM101BaldAPI.AssetManager;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BB_MOD
{

	[HarmonyPatch(typeof(LevelGenerator), "StartGenerate")]
	internal class SetupExtraContent // Setup items, npcs, events, etc.
	{
		private static void Prefix(LevelGenerator __instance)
		{
			var sceneObject = Singleton<CoreGameManager>.Instance.sceneObject;
			Floors currentFloor = sceneObject.levelTitle.ToFloorIdentifier();
			ContentManager.currentEc = __instance.Ec;
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

			List<WeightedItemObject> itemList = new List<WeightedItemObject>(__instance.ld.items);

			itemList.AddRange(ContentManager.instance.GetItems(currentFloor)); // Workaround to get new items

			__instance.ld.items = itemList.ToArray();

			itemList = new List<WeightedItemObject>(__instance.ld.shopItems); // Items for Jhonny's Store

			itemList.AddRange(ContentManager.instance.GetShoppingItems(currentFloor));

			__instance.ld.shopItems = itemList.ToArray();


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

			List<ObjectBuilder> builders = new List<ObjectBuilder>(__instance.ld.forcedSpecialHallBuilders)
			{
				ventBuilder.GetComponent<VentBuilder>()
			};

			__instance.ld.forcedSpecialHallBuilders = builders.ToArray();

			


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
				mesh = ContentUtilities.ConvertSideToTexture(0, 0, 256, 256, 512, 256, 0, mesh); // Took me a long time to figure out this lol (Pro Tip: CodeMonkey tutorials are useful)
				mesh = ContentUtilities.ConvertSideToTexture(0, 0, 256, 256, 512, 256, 4, mesh);
				mesh = ContentUtilities.ConvertSideToTexture(-256, 0, 256, 256, 512, 256, 8, mesh);
				mesh = ContentUtilities.ConvertSideToTexture(256, 0, 256, 251, 512, 256, 12, mesh);
				mesh = ContentUtilities.ConvertSideToTexture(0, 0, 256, 256, 512, 256, 16, mesh);
				mesh = ContentUtilities.ConvertSideToTexture(0, 0, 256, 256, 512, 256, 20, mesh);

				obj.GetComponent<MeshFilter>().mesh.uv = mesh;


				obj.GetComponent<MeshRenderer>().material = ventMat;

				obj.layer = 2;



				// Setup Audio Stuff
				var src = obj.AddComponent<AudioSource>();
				var aud = obj.AddComponent<AudioManager>();
				aud.audioDevice = src;
				src.maxDistance = 15f;
				src.minDistance = 0f;
				src.rolloffMode = AudioRolloffMode.Custom;
				src.spatialBlend = 1f;
				src.dopplerLevel = 0f;
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

	[HarmonyPatch(typeof(OfficeBuilderStandard), "Build")]
	internal class InitializeReplacementNPCs
	{
		private static void Prefix() // Basically iterates by randomly choosing a replacement NPC, then gets the array of the NPC and searches for the npc it replaces (random npc from array)
		{


			System.Random rng = new System.Random(Singleton<CoreGameManager>.Instance.Seed());

			// Extra Stuff

			int index2 = ContentManager.currentEc.npcsToSpawn.FindIndex(x => x.Character == Character.Sweep); // If gotta sweep exist, then it has a chance of changing to his old texture
			if (index2 >= 0 && rng.Next(0, 3) == 0)
			{
				ContentManager.currentEc.npcsToSpawn[index2].spriteBase.transform.Find("Sprite").GetComponent<SpriteRenderer>().sprite = AssetManager.SpriteFromTexture2D(AssetManager.TextureFromFile(Path.Combine(ContentManager.modPath, "Textures", "npc", "old_sweep.png")), new Vector2(0.5f, 0.5f), 20f);
				ContentManager.currentEc.npcsToSpawn[index2].Poster.baseTexture = AssetManager.TextureFromFile(Path.Combine(ContentManager.modPath, "Textures", "npc", "pri_oldsweep.png"));
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
				else if (rng.Next(0, replacementNPCs[0].weight) > replacementNPCs[0].weight / 2)
					rNpc = replacementNPCs[0].selection;

				if (!rNpc) // In case it still keeps as a null
					break;

				var characters = rNpc.gameObject.GetComponent<CustomNPCData>().replacementCharacters;

				if (characters.Length > 1)
				{
					List<WeightedSelection<NPC>> npcsToChoose = new List<WeightedSelection<NPC>>(); // Creates another weight selection to choose the npc that will be replaced
					foreach (var targetChar in characters)
					{
						int index = ContentManager.currentEc.npcsToSpawn.FindIndex(x => x.Character == targetChar);
						if (index >= 0)
						{
							npcsToChoose.Add(new WeightedSelection<NPC>()
							{
								selection = ContentManager.currentEc.npcsToSpawn[index],
								weight = 50
							});
						}
					}
					if (npcsToChoose.Count > 0)
					{
						NPC target = WeightedNPC.ControlledRandomSelectionList(npcsToChoose, rng);

						rNpc.spawnableRooms = ContentManager.currentEc.npcsToSpawn[ContentManager.currentEc.npcsToSpawn.IndexOf(target)].spawnableRooms;
						rNpc.GetComponent<CustomNPCData>().isReplacing = target.Character;


						success = true;

						ContentManager.currentEc.npcsToSpawn[ContentManager.currentEc.npcsToSpawn.IndexOf(target)] = rNpc;
					}
				}
				else
				{
					int index = ContentManager.currentEc.npcsToSpawn.FindIndex(x => x.Character == characters[0]);
					if (index >= 0)
					{ 

						rNpc.spawnableRooms = ContentManager.currentEc.npcsToSpawn[index].spawnableRooms;
						rNpc.GetComponent<CustomNPCData>().isReplacing = ContentManager.currentEc.npcsToSpawn[index].Character;

						success = true;

						ContentManager.currentEc.npcsToSpawn[index] = rNpc;
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
			var newItems = new List<WeightedSelection<ItemObject>>(___items);
			newItems.AddRange(ContentManager.instance.MysteryItems);
			___items = newItems.ToArray();
		}
	}

	[HarmonyPatch(typeof(PartyEvent), "Begin")]
	internal class IncludeExtraPartyItems
	{
		private static void Prefix(ref WeightedItemObject[] ___potentialItems)
		{
			var newItems = new List<WeightedItemObject>(___potentialItems);
			newItems.AddRange(ContentManager.instance.PartyItems);
			___potentialItems = newItems.ToArray();
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
				___navigator.ec = ContentManager.currentEc;
				___ec = ContentManager.currentEc;
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



}
