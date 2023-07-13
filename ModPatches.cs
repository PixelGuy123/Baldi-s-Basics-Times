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
					ContentManager.instance.beans = WeightedNPC.Convert(__instance.ld.potentialNPCs).First(x => x.selection.Character == Character.Beans).selection.gameObject;
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

			if (sceneObject.levelTitle == "F1") // Add potential npcs based on each floor
				__instance.ld.potentialNPCs.AddRange(ContentManager.instance.f1Npcs);


			items:

			Debug.Log("Items here lol");


		}
	}

	[HarmonyPatch(typeof(NPC), "Awake")]

	internal class SetupCustomNPCs
	{
		private static void Postfix(NPC __instance, ref Character ___character, ref Navigator ___navigator, ref EnvironmentController ___ec)
		{
			if (__instance.gameObject.name.StartsWith("CustomNPC_")) // setups NPC data here (for marked as customNpcs)
			{
				var data = __instance.gameObject.GetComponent<CustomNPCData>();
				___character = data.MyCharacter;
				___navigator = __instance.gameObject.GetComponent<Navigator>();
				___navigator.npc = __instance;
				___navigator.ec = ContentManager.currentEc;
				AccessTools.Field(typeof(Navigator), "avoidRooms").SetValue(___navigator, !data.EnterRooms); // Lazy to make another patch, so this is better
				___ec = ContentManager.currentEc;
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
