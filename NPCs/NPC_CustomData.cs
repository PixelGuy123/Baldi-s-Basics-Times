using System;
using UnityEngine;

namespace BB_MOD.NPCs
{

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

		public bool forceSpawn;

		public Character isReplacing;

		public Material[] materials = new Material[2]; // 0 - Billboard sprite, 1 - Flat sprite

		private int currentMat = 0;

		public void SwitchMaterials(bool flatMat)
		{
			spriteObject.GetComponent<SpriteRenderer>().material = flatMat ? materials[1] : materials[0];
			spriteObject.GetComponent<Billboard>().enabled = !flatMat;
			currentMat = flatMat ? 1 : 0;
		}

		public void SwitchMaterials()
		{
			currentMat = currentMat + 1 >= materials.Length ? 0 : currentMat + 1;
			spriteObject.GetComponent<Billboard>().enabled = currentMat == 0;
			spriteObject.GetComponent<SpriteRenderer>().material = materials[currentMat];
		}
	}
}
