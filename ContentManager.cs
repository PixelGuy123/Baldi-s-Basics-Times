using MTM101BaldAPI;
using MTM101BaldAPI.AssetManager;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using BB_MOD.NPCs;
using HarmonyLib;

namespace BB_MOD
{
	public class ContentManager : MonoBehaviour
	{

		private void Awake()
		{
			DontDestroyOnLoad(this);
			instance = this;
		}

		public void SetupWeightNPCValues()
		{
			if (addedNPCs)
				return;

			// THIS IS THE PART WHERE YOU PUT YOUR CUSTOM CHARACTER

			// Parameters: name, weight (chance to spawn), name of the png file used (add your pngs inside Textures/npcs !!), character "enum" (put the name of the enum), includeAnimator (include the Animator component, do whatever you want with it)
			// SpriteSize (Vector2), (optional) Rooms it can spawn, (Optional) enable looker on npc (enabled by default), (optional) can enter rooms (enabled by default)
			allNpcs.Add(CreateNPC<OfficeChair>("OfficeChair", 50, "officechair.png", EnumExtensions.ExtendEnum<Character>("OfficeChair"), false, new Vector2(5f, 5f), RoomCategory.Faculty, hasLooker: false));

			// End of Characters

			addedNPCs = true; // To not repeat instancing again

			if (f1Npcs.Count == 0) // Weighted NPCs for F1
			{
				f1Npcs.Add(allNpcs[0]); // Select the npcs inside the allNpcs list you want to spawn in F1
			}
		}

		private WeightedNPC CreateNPC<C>(string name, int weight, string spriteFileName, Character character, bool includeAnimator, Vector2 spriteSize, bool hasLooker = true, bool enterRooms = true) where C : NPC // The order of everything here must be IN THE ORDER I PUT, or else it'll log annoying null exceptions
		{
			// NOTE: Default Method will let custom npc spawn only in hallway

			var cBean = Instantiate(beans); // Instantiate a bean instance and customize it
			cBean.name = "CustomNPC_" + name;
			Destroy(cBean.GetComponent<Beans>()); // Removes beans component, useless

			var customData = cBean.AddComponent<CustomNPCData>();

			customData.MyCharacter = character;
			customData.EnterRooms = enterRooms;

			DontDestroyOnLoad(cBean);

			cBean.AddComponent<C>(); // Adds main component: Custom NPC

			if (!includeAnimator)
				cBean.GetComponent<Animator>().enabled = false;
			if (!hasLooker)
				cBean.GetComponent<Looker>().enabled = false;


			cBean.SetActive(false);
			cBean.GetComponent<C>().enabled = true;
			var poster = AccessTools.Field(typeof(C), "poster");
			var beansPoster = AccessTools.Field(typeof(Beans), "poster");
			poster.SetValue(cBean.GetComponent<C>(), beansPoster.GetValue(beans.GetComponent<Beans>())); // Set values of poster (temporarily uses beans poster, change this later!!)



			cBean.GetComponent<C>().spriteBase = cBean.transform.Find("SpriteBase").gameObject;

			var renderer = cBean.GetComponent<C>().spriteBase.transform.Find("Sprite").GetComponent<SpriteRenderer>();
			renderer.sprite = AssetManager.SpriteFromTexture2D(AssetManager.TextureFromFile(Path.Combine(modPath, "Textures", "npc", spriteFileName))); // Adds custom sprite (yes, all of that just to get the sprite renderer)
			renderer.size = spriteSize;

			cBean.GetComponent<C>().spawnableRooms = new List<RoomCategory>() { RoomCategory.Hall };


			return new WeightedNPC()
			{
				weight = weight,
				selection = cBean.GetComponent<C>()
			};

		}

		private WeightedNPC CreateNPC<C>(string name, int weight, string spriteFileName, Character character, bool includeAnimator, Vector2 spriteSize, List<RoomCategory> roomsAllowedToSpawn, bool hasLooker = true, bool enterRooms = true) where C : NPC
		{
			var npc = CreateNPC<C>(name, weight, spriteFileName, character, includeAnimator, spriteSize, hasLooker, enterRooms);
			npc.selection.spawnableRooms = roomsAllowedToSpawn;
			return npc;
		}

		private WeightedNPC CreateNPC<C>(string name, int weight, string spriteFileName, Character character, bool includeAnimator, Vector2 spriteSize, RoomCategory roomsAllowedToSpawn, bool hasLooker = true, bool enterRooms = true) where C : NPC
		{
			var npc = CreateNPC<C>(name, weight, spriteFileName, character, includeAnimator, spriteSize, hasLooker, enterRooms);
			npc.selection.spawnableRooms = new List<RoomCategory>() { roomsAllowedToSpawn };
			return npc;
		}

		public GameObject beans;

		public List<WeightedNPC> f1Npcs = new List<WeightedNPC>();

		private readonly List<WeightedNPC> allNpcs = new List<WeightedNPC>();

		private bool addedNPCs = false;

		public static ContentManager instance;

		public static string modPath;

		public static EnvironmentController currentEc;

	}
}
