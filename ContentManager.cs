using BB_MOD.NPCs;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetManager;
using System;
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

			// Parameters explained in order: name of game object, weight (chance to spawn), name of all png files used (string array; support for multiple sprites, all saved on CustomNPCData component, first sprite is the default one), includeAnimator (include the Animator component, do whatever you want with it)
			// PixelsPerUnit for the sprite (Resuming: Size of it, if a higher integer, smaller it'll be), spriteYOffset (as the name suggests, changes the Y offset of the sprite, leave 0f to not change it), PosterFileName (Put the name of the png used for the poster), keyForPosterName > the key for the poster's title (Languages/English/npcCaps.json)
			// KeyForPoster > Key for NPC's Description, Floors (enum) set the floor your npc will spawn (can be an array; multiple floors), (optional) Rooms it can spawn
			// (Optional) enable looker on npc (enabled by default), (optional) can enter rooms (enabled by default)
			// (Optional) Aggroed > basically if the npc DOESN'T go to the party event (disabled by default), (Optional) IgnoreBelts > if it ignores conveyor belts (disabled by default), (Optional) capsuleRadius > basically the size of the collider of the NPC (Default is what beans uses)

			allNpcs.Add(CreateNPC<OfficeChair>("Office Chair", 50, new string[] { "officechair.png", "officechair_disabled.png" }, false, 18f, -0.8f, "pri_ofc.png", "PST_OFC_Name", "PST_OFC_Desc", Floors.F1, RoomCategory.Faculty, hasLooker: false, aggored: true, capsuleRadius: 4f));

			// End of Character Spawns

			addedNPCs = true; // To not repeat instancing again
		}


		private WeightedNPC CreateNPC<C>(string name, int weight, string[] spritesFileName, bool includeAnimator, float pixelsPerUnit, float spriteYOffset, string posterFileName, string keyForPosterName, string keyForPoster, Floors[] floor, bool hasLooker = true, bool enterRooms = true, bool aggored = false, bool ignoreBelts = false, float capsuleRadius = 0f) where C : NPC // The order of everything here must be IN THE ORDER I PUT, or else it'll log annoying null exceptions
		{
			try
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
				List<Sprite> sprites = new List<Sprite>();
				spritesFileName.Do(x => sprites.Add(AssetManager.SpriteFromTexture2D(AssetManager.TextureFromFile(Path.Combine(modPath, "Textures", "npc", x)), new Vector2(0.5f, 0.5f), pixelsPerUnit)));

				customData.sprites = sprites.ToArray(); // Array of sprites can be accessed through CustomNPCData Component

				DontDestroyOnLoad(cBean);

				cBean.AddComponent<C>(); // Adds main component: Custom NPC

				if (!includeAnimator)
					cBean.GetComponent<Animator>().enabled = false;
				if (!hasLooker)
					cBean.GetComponent<Looker>().enabled = false;


				cBean.SetActive(false);
				cBean.GetComponent<C>().enabled = true;
				var poster = AccessTools.Field(typeof(C), "poster");
				var clonePoster = (PosterObject)AccessTools.Field(typeof(Beans), "poster").GetValue(beans.GetComponent<Beans>());
				var textData = new PosterTextData[clonePoster.textData.Length];
				Array.Copy(clonePoster.textData, textData, clonePoster.textData.Length); // Copies data from og text data to the new one
				textData[0].textKey = keyForPosterName;
				textData[1].textKey = keyForPoster; // item 0 = title; item 1 = description
				
				poster.SetValue(cBean.GetComponent<C>(), ObjectCreatorHandlers.CreatePosterObject(AssetManager.TextureFromFile(Path.Combine(modPath, "Textures", "npc", posterFileName)), clonePoster.material, textData)); // Set values of poster (temporarily uses beans poster, I'll change this later!!)



				cBean.GetComponent<C>().spriteBase = cBean.transform.Find("SpriteBase").gameObject;

				var sprite = cBean.GetComponent<C>().spriteBase.transform.Find("Sprite");

				sprite.localPosition = new Vector3(0f, 0f + spriteYOffset, 0f); // Sets offset

				var renderer = sprite.GetComponent<SpriteRenderer>();
				renderer.sprite = customData.sprites[0]; // Adds custom sprite

				customData.spriteObject = renderer; // Sets the renderer to be used

				cBean.GetComponent<C>().spawnableRooms = new List<RoomCategory>() { RoomCategory.Hall };

				npcPair.Add(floor);

				if (capsuleRadius > 0)
					cBean.GetComponent<CapsuleCollider>().radius = capsuleRadius;

				return new WeightedNPC()
				{
					weight = weight,
					selection = cBean.GetComponent<C>()
				};
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				Debug.LogWarning($"Looks like a parameter is wrong on the NPC: {name}, please go back to your method and make sure every file path/name is correct!");
				Debug.LogWarning("Your NPC will be created, but will never be used on the game");
			}
			npcPair.Add(Array.Empty<Floors>());
			return new WeightedNPC()
			{
				weight = 50,
				selection = beans.GetComponent<Beans>()
			};

		}

		private WeightedNPC CreateNPC<C>(string name, int weight, string[] spriteFileName, bool includeAnimator, float pixelsPerUnit, float spriteYOffset, string posterFileName, string keyForPosterName, string keyForPoster, Floors floor, List<RoomCategory> roomsAllowedToSpawn, bool hasLooker = true, bool enterRooms = true, bool aggored = true, bool ignoreBelts = false, float capsuleRadius = 0f) where C : NPC
		{
			var npc = CreateNPC<C>(name, weight, spriteFileName, includeAnimator, pixelsPerUnit, spriteYOffset, posterFileName, keyForPosterName, keyForPoster, new Floors[] { floor }, hasLooker, enterRooms, aggored, ignoreBelts, capsuleRadius);
			npc.selection.spawnableRooms = roomsAllowedToSpawn;
			return npc;
		}

		private WeightedNPC CreateNPC<C>(string name, int weight, string[] spriteFileName, bool includeAnimator, float pixelsPerUnit, float spriteYOffset, string posterFileName, string keyForPosterName, string keyForPoster, Floors[] floor, List<RoomCategory> roomsAllowedToSpawn, bool hasLooker = true, bool enterRooms = true, bool aggored = true, bool ignoreBelts = false, float capsuleRadius = 0f) where C : NPC
		{
			var npc = CreateNPC<C>(name, weight, spriteFileName, includeAnimator, pixelsPerUnit, spriteYOffset, posterFileName, keyForPosterName, keyForPoster, floor, hasLooker, enterRooms, aggored, ignoreBelts, capsuleRadius);
			npc.selection.spawnableRooms = roomsAllowedToSpawn;
			return npc;
		}

		private WeightedNPC CreateNPC<C>(string name, int weight, string[] spriteFileName, bool includeAnimator, float pixelsPerUnit, float spriteYOffset, string posterFileName, string keyForPosterName, string keyForPoster, Floors floor, RoomCategory roomsAllowedToSpawn, bool hasLooker = true, bool enterRooms = true, bool aggored = true, bool ignoreBelts = false, float capsuleRadius = 0f) where C : NPC
		{
			var npc = CreateNPC<C>(name, weight, spriteFileName, includeAnimator, pixelsPerUnit, spriteYOffset, posterFileName, keyForPosterName, keyForPoster, new Floors[] { floor }, hasLooker, enterRooms, aggored, ignoreBelts, capsuleRadius);
			npc.selection.spawnableRooms = new List<RoomCategory>() { roomsAllowedToSpawn };
			return npc;
		}

		private WeightedNPC CreateNPC<C>(string name, int weight, string[] spriteFileName, bool includeAnimator, float pixelsPerUnit, float spriteYOffset, string posterFileName, string keyForPosterName, string keyForPoster, Floors[] floor, RoomCategory roomsAllowedToSpawn, bool hasLooker = true, bool enterRooms = true, bool aggored = true, bool ignoreBelts = false, float capsuleRadius = 0f) where C : NPC
		{
			var npc = CreateNPC<C>(name, weight, spriteFileName, includeAnimator, pixelsPerUnit, spriteYOffset, posterFileName, keyForPosterName, keyForPoster, floor, hasLooker, enterRooms, aggored, ignoreBelts, capsuleRadius);
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
