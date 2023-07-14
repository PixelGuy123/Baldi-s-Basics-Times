using HarmonyLib;
using UnityEngine;

namespace BB_MOD.NPCs
{
	[HarmonyPatch(typeof(Baldi))]
	[HarmonyPatch("OnTriggerEnter")] // Remember to remove this lol
	class Baldistop
	{
		static bool Prefix()
		{
			return false;
		}
	}

	public class CustomNPCData : MonoBehaviour // EVERY CUSTOM NPC MUST HAVE THIS IN ORDER TO GET IT'S DATA (might not be much, but it really helps)
	{
		// General NPC stuff
		public Character MyCharacter;

		// Navigator Stuff
		public bool EnterRooms;

		public bool IgnoreBelts;

		public bool Aggroed;
	}
}
