using BB_MOD.NPCs;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BB_MOD.Events
{


	// ---- EVENT SUMMARY ----
	// It'll put Principal on his office basically
	// If he is distracted and gets out of the office, he'll comeback afterwards

	public class PrincipalOut : RandomEvent
	{
		public override void Begin()
		{
			base.Begin();
			RoomController[] array = ec.rooms.Where(x => x.category == RoomCategory.Office).ToArray();
			if (array.Length == 0)
				return;

			if (array.Length == 1)
			{
				office = array[0];
			}
			else
			{
				office = array[Random.Range(0, array.Length + 1)];
			}

			foreach (var npc in ec.Npcs)
			{
				if (npc.Navigator.enabled && (npc.Character == Character.Principal || npc.GetComponent<CustomNPCData>()?.isReplacing == Character.Principal))
				{
					npc.controlOverride = true;
					principals.Add(npc, office.TileAtIndex(crng.Next(0, office.TileCount)));
				}
			}

			StartCoroutine(GetPrincipalsIn());

		}

		public override void End()
		{
			base.End();
			foreach (var pri in principals)
			{
				if (pri.Key)
					pri.Key.controlOverride = false;
			}
		}

		private IEnumerator GetPrincipalsIn()
		{
			while (active)
			{
				foreach (var pri in principals)
				{
					if (pri.Key.Aggroed)
					{
						pri.Key.controlOverride = false;
						hadDestination = false;
					}
					else if (pri.Key.Navigator.enabled && (!hadDestination || !pri.Key.Navigator.HasDestination) && !ReferenceEquals(ec.TileFromPos(pri.Key.transform.position), pri.Value)) // Maintain principal on his office
					{
						pri.Key.controlOverride = true;
						hadDestination = true;
						pri.Key.Navigator.FindPath(pri.Key.transform.position, pri.Value.transform.position);
					}
				}
				yield return null;
			}
			yield break;
		}

		private RoomController office;

		private bool hadDestination = false;

		private readonly Dictionary<NPC, TileController> principals = new Dictionary<NPC, TileController>();
	}
}
