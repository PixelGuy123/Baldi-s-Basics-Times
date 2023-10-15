using HarmonyLib;
using BB_MOD;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace Patches.Cheats
{
	[HarmonyPatch(typeof(Baldi))]
	[HarmonyPatch("OnTriggerEnter")]
	internal class Baldistop
	{
		static bool Prefix()
		{
			return !ContentManager.instance.DebugMode;
		}
	}

	[HarmonyPatch(typeof(PlayerManager))]
	[HarmonyPatch("Update")]

	internal class LogPos
	{
		static void Postfix(PlayerManager __instance)
		{
			if (ContentManager.instance.DebugMode && Input.GetKeyDown(KeyCode.H))
			{
				IntVector2 pos = IntVector2.GetGridPosition(__instance.transform.position);
				Debug.Log("My player position is: " + pos.x + "," + pos.z);
			}
		}
	}

	[HarmonyPatch(typeof(ItemManager), "Update")]
	internal class GiveCoolItems
	{
		static void Prefix(ItemManager __instance)
		{
			if (ContentManager.instance.DebugMode && Input.GetKeyDown(KeyCode.P))
			{
				var item = ContentManager.instance.customItemEnums.GetItemByName("Speedpotion");
				__instance.AddItem(ContentManager.instance.GlobalItems.Find(x => x.selection.itemType == item).selection);
			}
		}
	}

	[HarmonyPatch(typeof(RoomBuilder), "Setup")]

	internal class LogRooms
	{
		static void Prefix(RoomController room, WeightedTransform[] ___decorations)
		{
			if (ContentManager.instance.DebugMode)
			{

				Debug.Log("Builder Room Category: " + room.category + " at index: " + room.position.x + "," + room.position.z);
				Debug.Log("Builder Decorations:");
				___decorations.Do(x => Debug.Log("Selection: " + x.selection.name + " | Weight: " + x.weight));

			}
		}
	}

	[HarmonyPatch(typeof(HappyBaldi), "SpawnWait", MethodType.Enumerator)]
	internal class ChangeCount
	{
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			bool changedCount = !ContentManager.instance.DebugMode;
			using (var enumerator = instructions.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					var instruction = enumerator.Current;
					if (!changedCount && instruction.Is(OpCodes.Ldc_I4_S, 9))
					{
						changedCount = true;
						instruction.operand = 0;
					}
					yield return instruction;
				}
			}
		}
	}
}
