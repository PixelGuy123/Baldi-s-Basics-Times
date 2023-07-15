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

			__instance.ld.potentialNPCs.AddRange(ContentManager.instance.GetNPCs(sceneObject.levelTitle.ToFloorIdentifier()));

		items:

			Debug.Log("Items stuff here"); // Don't actually put anything in here, it's just a placeholder lol


		}
	}

	[HarmonyPatch(typeof(NPC), "Awake")]

	internal class SetupCustomNPCs
	{
		private static void Postfix(NPC __instance, ref Character ___character, ref Navigator ___navigator, ref EnvironmentController ___ec, ref bool ___ignoreBelts, ref bool ___aggroed, ref PosterObject ___poster, ref Looker ___looker)
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
