using BB_MOD.NPCs;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetManager;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

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

		// ------------------------------------------------------ NPC CREATION STUFF ------------------------------------------------------

		public void SetupWeightNPCValues()
		{
			if (addedNPCs)
				return;

			// THIS IS THE PART WHERE YOU PUT YOUR CUSTOM CHARACTER
			// Add the custom npc to the list using the CreateNPC<C> method as seen below (C stands for Character Class, which is the class the NPC will use)

			// Parameters explained in order: name of game object, weight (chance to spawn), name of the png file used (add your pngs inside Textures/npcs !!), includeAnimator (include the Animator component, do whatever you want with it)
			// PixelsPerUnit for the sprite, spriteYOffset (as the name suggests, changes the Y offset of the sprite, leave 0f to not change it), Floors (enum) set the floor your npc will spawn (can be an array; multiple floors), (optional) Rooms it can spawn, (Optional) enable looker on npc (enabled by default), (optional) can enter rooms (enabled by default)
			// (Optional) Aggroed > basically if the npc DOESN'T go to the party event (disabled by default), (Optional) IgnoreBelts > if it ignores conveyor belts (disabled by default)

			allNpcs.Add(CreateNPC<OfficeChair>("OfficeChair", 50, "officechair.png", false, 15f, 0.25f, Floors.F1, RoomCategory.Faculty, hasLooker: false, aggored: true));

			// End of Character Spawns

			addedNPCs = true; // To not repeat instancing again
		}


		private WeightedNPC CreateNPC<C>(string name, int weight, string spriteFileName, bool includeAnimator, float pixelsPerUnit, float spriteYOffset, Floors[] floor, bool hasLooker = true, bool enterRooms = true, bool aggored = false, bool ignoreBelts = false) where C : NPC // The order of everything here must be IN THE ORDER I PUT, or else it'll log annoying null exceptions
		{
			// NOTE: Default Method will let custom npc spawn only in hallway

			var cBean = Instantiate(beans); // Instantiate a bean instance and customize it
			cBean.name = "CustomNPC_" + name;
			Destroy(cBean.GetComponent<Beans>()); // Removes beans component, useless

			var customData = cBean.AddComponent<CustomNPCData>();

			customData.MyCharacter = EnumExtensions.ExtendEnum<Character>(name);
			customData.EnterRooms = enterRooms;
			customData.Aggroed = aggored;
			customData.IgnoreBelts = ignoreBelts;

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
			poster.SetValue(cBean.GetComponent<C>(), beansPoster.GetValue(beans.GetComponent<Beans>())); // Set values of poster (temporarily uses beans poster, I'll change this later!!)



			cBean.GetComponent<C>().spriteBase = cBean.transform.Find("SpriteBase").gameObject;

			var sprite = cBean.GetComponent<C>().spriteBase.transform.Find("Sprite");

			sprite.localPosition = new Vector3(0f, 0f + spriteYOffset, 0f); // Sets offset

			var renderer = sprite.GetComponent<SpriteRenderer>();
			renderer.sprite = AssetManager.SpriteFromTexture2D(AssetManager.TextureFromFile(Path.Combine(modPath, "Textures", "npc", spriteFileName)), new Vector2(0.5f, 0.5f), pixelsPerUnit); // Adds custom sprite (yes, all of that just to get the sprite renderer)

			cBean.GetComponent<C>().spawnableRooms = new List<RoomCategory>() { RoomCategory.Hall };

			npcPair.Add(floor);

			return new WeightedNPC()
			{
				weight = weight,
				selection = cBean.GetComponent<C>()
			};

		}

		private WeightedNPC CreateNPC<C>(string name, int weight, string spriteFileName, bool includeAnimator, float pixelsPerUnit, float spriteYOffset, Floors floor, List<RoomCategory> roomsAllowedToSpawn, bool hasLooker = true, bool enterRooms = true, bool aggored = true, bool ignoreBelts = false) where C : NPC
		{
			var npc = CreateNPC<C>(name, weight, spriteFileName, includeAnimator, pixelsPerUnit, spriteYOffset, new Floors[] { floor }, hasLooker, enterRooms, aggored, ignoreBelts);
			npc.selection.spawnableRooms = roomsAllowedToSpawn;
			return npc;
		}

		private WeightedNPC CreateNPC<C>(string name, int weight, string spriteFileName, bool includeAnimator, float pixelsPerUnit, float spriteYOffset, Floors[] floor, List<RoomCategory> roomsAllowedToSpawn, bool hasLooker = true, bool enterRooms = true, bool aggored = true, bool ignoreBelts = false) where C : NPC
		{
			var npc = CreateNPC<C>(name, weight, spriteFileName, includeAnimator, pixelsPerUnit, spriteYOffset, floor, hasLooker, enterRooms, aggored, ignoreBelts);
			npc.selection.spawnableRooms = roomsAllowedToSpawn;
			return npc;
		}

		private WeightedNPC CreateNPC<C>(string name, int weight, string spriteFileName, bool includeAnimator, float pixelsPerUnit, float spriteYOffset, Floors floor, RoomCategory roomsAllowedToSpawn, bool hasLooker = true, bool enterRooms = true, bool aggored = true, bool ignoreBelts = false) where C : NPC
		{
			var npc = CreateNPC<C>(name, weight, spriteFileName, includeAnimator, pixelsPerUnit, spriteYOffset, new Floors[] { floor }, hasLooker, enterRooms, aggored, ignoreBelts);
			npc.selection.spawnableRooms = new List<RoomCategory>() { roomsAllowedToSpawn };
			return npc;
		}

		private WeightedNPC CreateNPC<C>(string name, int weight, string spriteFileName, bool includeAnimator, float pixelsPerUnit, float spriteYOffset, Floors[] floor, RoomCategory roomsAllowedToSpawn, bool hasLooker = true, bool enterRooms = true, bool aggored = true, bool ignoreBelts = false) where C : NPC
		{
			var npc = CreateNPC<C>(name, weight, spriteFileName, includeAnimator, pixelsPerUnit, spriteYOffset, floor, hasLooker, enterRooms, aggored, ignoreBelts);
			npc.selection.spawnableRooms = new List<RoomCategory>() { roomsAllowedToSpawn };
			return npc;
		}






		public GameObject beans;

		private readonly bool[] accessedNPCs = new bool[] // Prevents the weights from being added again
		{
			false,
			false,
			false,
			false
		};

		private readonly List<WeightedNPC> allNpcs = new List<WeightedNPC>();

		private readonly List<Floors[]> npcPair = new List<Floors[]>();

		public List<WeightedNPC> F1_Npcs
		{
			get
			{
				if (accessedNPCs[0])
					return new List<WeightedNPC>();

				accessedNPCs[0] = true;
				var npcs = new List<WeightedNPC>();
				for (int i = 0; i < allNpcs.Count; i++)
				{
					if (npcPair[i].Contains(Floors.F1))
					{
						npcs.Add(allNpcs[i]);
					}
				}
				return npcs;
			}
		}

		public List<WeightedNPC> F2_Npcs
		{
			get
			{
				if (accessedNPCs[1])
					return new List<WeightedNPC>();
				accessedNPCs[1] = true;
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
				if (accessedNPCs[2])
					return new List<WeightedNPC>();

				accessedNPCs[2] = true;
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
				if (accessedNPCs[3])
					return new List<WeightedNPC>();

				accessedNPCs[3] = true;
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
