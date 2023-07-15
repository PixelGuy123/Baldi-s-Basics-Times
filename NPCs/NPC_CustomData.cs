﻿using HarmonyLib;
using MTM101BaldAPI.AssetManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.U2D;

namespace BB_MOD.NPCs
{
	[HarmonyPatch(typeof(Baldi))]
	[HarmonyPatch("OnTriggerEnter")] // Remember myself to remove this lol
	class Baldistop
	{
		static bool Prefix()
		{
			return false;
		}
	}

	[HarmonyPatch(typeof(PlayerManager))]
	[HarmonyPatch("Update")]

	class LogPos
	{
		static void Postfix(PlayerManager __instance)
		{
			if (Input.GetKeyDown(KeyCode.H))
			{
				IntVector2 pos = IntVector2.GetGridPosition(__instance.transform.position);
				Debug.Log("My player position is: " + pos.x + "," + pos.z);
			}
		}
	}

	[HarmonyPatch(typeof(RoomBuilder), "Setup")]

	class LogRooms
	{
		static void Prefix(RoomController room, WeightedTransform[] ___decorations)
		{
			Debug.Log("Builder Room Category: " + room.category + " at index: " + room.position.x + "," + room.position.z);
			Debug.Log("Builder Decorations:");
			___decorations.Do(x => Debug.Log("Selection: " + x.selection.name + " | Weight: " + x.weight));
		}
	}

	[HarmonyPatch(typeof(HappyBaldi), "SpawnWait", MethodType.Enumerator)]
	class ChangeCount
	{
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			bool changedCount = false;
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

	// Any patches above here are used for debugging reasons and will only be deleted on final state

	public class CustomNPCData : MonoBehaviour // EVERY CUSTOM NPC MUST HAVE THIS IN ORDER TO GET IT'S DATA
	{
		// General NPC stuff
		public Character MyCharacter;

		// Navigator Stuff
		public bool EnterRooms;

		public bool IgnoreBelts;

		public bool Aggroed;

		public Sprite[] sprites;

		public SpriteRenderer spriteObject;

		public PosterObject poster;

		public bool useHeatMap;

		public Character[] replacementCharacters = Array.Empty<Character>();
	}
}