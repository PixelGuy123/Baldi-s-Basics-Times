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

	enum Floors
	{
		F1,
		F2,
		F3,
		END
	}

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
			// SpriteSize (Vector2), Floors (enum) set the floor your npc will spawn (can be an array; multiple floors), (optional) Rooms it can spawn, (Optional) enable looker on npc (enabled by default), (optional) can enter rooms (enabled by default)
			allNpcs.Add(CreateNPC<OfficeChair>("OfficeChair", 50, "officechair.png", EnumExtensions.ExtendEnum<Character>("OfficeChair"), false, new Vector2(5f, 5f), Floors.F1, RoomCategory.Faculty, hasLooker: false));

			// End of Characters

			addedNPCs = true; // To not repeat instancing again
		}

		private WeightedNPC CreateNPC<C>(string name, int weight, string spriteFileName, Character character, bool includeAnimator, Vector2 spriteSize, Floors[] floor, bool hasLooker = true, bool enterRooms = true) where C : NPC // The order of everything here must be IN THE ORDER I PUT, or else it'll log annoying null exceptions
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

			npcPair.Add(floor);

			return new WeightedNPC()
			{
				weight = weight,
				selection = cBean.GetComponent<C>()
			};

		}

		private WeightedNPC CreateNPC<C>(string name, int weight, string spriteFileName, Character character, bool includeAnimator, Vector2 spriteSize, Floors floor, List<RoomCategory> roomsAllowedToSpawn, bool hasLooker = true, bool enterRooms = true) where C : NPC
		{
			var npc = CreateNPC<C>(name, weight, spriteFileName, character, includeAnimator, spriteSize, new Floors[] { floor }, hasLooker, enterRooms);
			npc.selection.spawnableRooms = roomsAllowedToSpawn;
			return npc;
		}

		private WeightedNPC CreateNPC<C>(string name, int weight, string spriteFileName, Character character, bool includeAnimator, Vector2 spriteSize, Floors[] floor, List<RoomCategory> roomsAllowedToSpawn, bool hasLooker = true, bool enterRooms = true) where C : NPC
		{
			var npc = CreateNPC<C>(name, weight, spriteFileName, character, includeAnimator, spriteSize, floor, hasLooker, enterRooms);
			npc.selection.spawnableRooms = roomsAllowedToSpawn;
			return npc;
		}

		private WeightedNPC CreateNPC<C>(string name, int weight, string spriteFileName, Character character, bool includeAnimator, Vector2 spriteSize, Floors floor, RoomCategory roomsAllowedToSpawn, bool hasLooker = true, bool enterRooms = true) where C : NPC
		{
			var npc = CreateNPC<C>(name, weight, spriteFileName, character, includeAnimator, spriteSize, new Floors[] { floor }, hasLooker, enterRooms);
			npc.selection.spawnableRooms = new List<RoomCategory>() { roomsAllowedToSpawn };
			return npc;
		}

		private WeightedNPC CreateNPC<C>(string name, int weight, string spriteFileName, Character character, bool includeAnimator, Vector2 spriteSize, Floors[] floor, RoomCategory roomsAllowedToSpawn, bool hasLooker = true, bool enterRooms = true) where C : NPC
		{
			var npc = CreateNPC<C>(name, weight, spriteFileName, character, includeAnimator, spriteSize, floor, hasLooker, enterRooms);
			npc.selection.spawnableRooms = new List<RoomCategory>() { roomsAllowedToSpawn };
			return npc;
		}

		public GameObject beans;

		private readonly List<WeightedNPC> allNpcs = new List<WeightedNPC>();

		private readonly List<Floors[]> npcPair = new List<Floors[]>();

		public List<WeightedNPC> F1_Npcs 
		{
			get
			{
				var npcs = new List<WeightedNPC>();
				for (int i = 0; i < allNpcs.Count; i++)
				{
					if (npcPair[i].Contains(Floors.F1))
					{
						npcs.Add(allNpcs[i]);
						Debug.Log(allNpcs[i].selection.gameObject.name);
					}
				}
				return npcs;
			}
		}

		public List<WeightedNPC> F2_Npcs
		{
			get
			{
				var npcs = new List<WeightedNPC>();
				for (int i = 0; i < allNpcs.Count; i++)
				{
					if (npcPair[i].Contains(Floors.F2))
						npcs.Add(allNpcs[i]);
				}
				return npcs;
			}
		}

		public List<WeightedNPC> F3_Npcs
		{
			get
			{
				var npcs = new List<WeightedNPC>();
				for (int i = 0; i < allNpcs.Count; i++)
				{
					if (npcPair[i].Contains(Floors.F3))
						npcs.Add(allNpcs[i]);
				}
				return npcs;
			}
		}

		public List<WeightedNPC> END_Npcs
		{
			get
			{
				var npcs = new List<WeightedNPC>();
				for (int i = 0; i < allNpcs.Count; i++)
				{
					if (npcPair[i].Contains(Floors.END))
						npcs.Add(allNpcs[i]);
				}
				return npcs;
			}
		}

		public List<WeightedNPC> AllNpcs
		{
			get => allNpcs;
		}


		private bool addedNPCs = false;

		public static ContentManager instance;

		public static string modPath;

		public static EnvironmentController currentEc;

	}
}
