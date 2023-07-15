using BB_MOD.NPCs;
using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace BB_MOD
{

	[HarmonyPatch(typeof(LevelGenerator), "StartGenerate")]
	internal class SetupExtraContent // Setup items, npcs, events, etc.
	{
		private static void Prefix(LevelGenerator __instance)
		{
			if (!ContentManager.instance.beans)
			{
				try
				{
					ContentManager.instance.beans = WeightedNPC.Convert(__instance.ld.potentialNPCs).First(x => x.selection.Character == Character.Beans).selection.gameObject; // Finds the first Beans instance to be used
				}
				catch
				{
					Debug.LogError("Beans somehow doesn\'t exist on the npc list, the mod won\'t spawn new npcs");
					goto items;
				}
			}

			ContentManager.currentEc = __instance.Ec;

			ContentManager.instance.SetupWeightNPCValues();

			var sceneObject = Singleton<CoreGameManager>.Instance.sceneObject;

			switch (sceneObject.levelTitle.ToLower()) // Add potential npcs based on each floor
			{
				case "f1":
					__instance.ld.potentialNPCs.AddRange(ContentManager.instance.F1_Npcs);
				break;

				case "f2":
					__instance.ld.potentialNPCs.AddRange(ContentManager.instance.F2_Npcs);
				break;

				case "f3":
					__instance.ld.potentialNPCs.AddRange(ContentManager.instance.F3_Npcs);
				break;

				case "end":
					__instance.ld.potentialNPCs.AddRange(ContentManager.instance.END_Npcs);
				break;
				default:
					Debug.LogWarning("Wasn\'t able to identify floor, putting all characters lol"); // Impossible case lmao
					__instance.ld.potentialNPCs.AddRange(ContentManager.instance.AllNpcs);
				break;
			}


			items:

			Debug.Log("Items stuff here"); // Don't actually put anything in here, it's just a placeholder lol


		}
	}

	[HarmonyPatch(typeof(NPC), "Awake")]

	internal class SetupCustomNPCs
	{
		private static void Postfix(NPC __instance, ref Character ___character, ref Navigator ___navigator, ref EnvironmentController ___ec, ref bool ___ignoreBelts, ref bool ___aggroed, ref PosterObject ___poster)
		{
			if (__instance.gameObject.name.StartsWith("CustomNPC_")) // setups NPC data here (for marked as customNpcs)
			{
				// Setup
				___navigator = __instance.gameObject.GetComponent<Navigator>();
				___navigator.npc = __instance;
				___navigator.ec = ContentManager.currentEc;
				___ec = ContentManager.currentEc;

				// Custom Data

				var data = __instance.gameObject.GetComponent<CustomNPCData>();
				___character = data.MyCharacter;
				___ignoreBelts = data.IgnoreBelts;
				___aggroed = data.Aggroed;
				___poster = data.poster;
			}
		}
	}

	[HarmonyPatch(typeof(Navigator), "Start")]

	internal class SetupCustomNPCs_Navigator
	{
		private static void Prefix(NPC ___npc , ref bool ___avoidRooms) // Sets up the avoid rooms thing
		{
			if (___npc.gameObject.name.StartsWith("CustomNPC_"))
			{
				___avoidRooms = !___npc.gameObject.GetComponent<CustomNPCData>().EnterRooms;
				Debug.Log(!___npc.gameObject.GetComponent<CustomNPCData>().EnterRooms);
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


}
