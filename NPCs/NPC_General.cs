using HarmonyLib;
using UnityEngine;

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
	}
}
