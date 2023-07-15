using BB_MOD.NPCs;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// -------------------- PRO TIP ----------------------
// Recommended using UnityExplorer to debug your item, event or npc. It's a very useful tool
// I also recommend using Github Desktop to update the files when needed, it's much faster than having to replace everything manually
// Every note on the code is pretty much only useful for collaborators (those that are allowed to modify the code)

namespace BB_MOD
{

	public enum Floors
	{
		None,
		F1,
		F2,
		F3,
		END
	}

	public static class FloorExtensions
	{
		public static Floors ToFloorIdentifier(this string name)
		{
			switch (name.ToLower()) // Converts Floor name to Floors Enum
			{
				case "f1":
					return Floors.F1;

				case "f2":
					return Floors.F2;

				case "f3":
					return Floors.F3;

				case "end":
					return Floors.END;
				default:
					return Floors.None;
			}
		}
	}

	public class ContentManager : MonoBehaviour
	{

		private void Awake()
		{
			DontDestroyOnLoad(this);
			instance = this;

			// Add every single BB+ item into the list (default weight being 50 for every one) Note: this list is used later on npcs or mechanics that use item weighted selection

			allItems.AddRange(Resources.FindObjectsOfTypeAll<ItemObject>().ToList().ConvertAll(x => new WeightedSelection<ItemObject>()
			{
				weight = 50,
				selection = x
			}));
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
			// (Optional) Aggroed > basically if the npc DOESN'T go to the party event or can have the navigator overrided by something else (disabled by default), (Optional) IgnoreBelts > if it ignores conveyor belts (disabled by default), (Optional) capsuleRadius > basically the size of the collider of the NPC (Default is what beans uses)


			allNpcs.Add(CreateNPC<OfficeChair>("Office Chair", 35, new string[] { "officechair.png", "officechair_disabled.png" }, false, 18f, -0.8f, "pri_ofc.png", "PST_OFC_Name", "PST_OFC_Desc", new Floors[] { Floors.F1 }, new RoomCategory[] { RoomCategory.Faculty }, hasLooker: false, aggored: true, capsuleRadius: 4f));
			allNpcs.Add(CreateNPC<HappyHolidays>("Happy Holidays", 15, new string[] { "happyholidays.png" }, false, 70f, -1.5f, "pri_hapho.png", "PST_HapH_Name", "PST_HapH_Desc", new Floors[] { Floors.F1 }, enterRooms: false, capsuleRadius: 3f));

			// allNpcs.Add(CreateReplacementNPC<PlaceholderClass>) << NOT DONE YET, I'll make a comment soon explaining it's function


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

				var beanPoster = beans.GetComponent<Beans>().Poster; // Get beans poster to set data

				customData.poster = ObjectCreatorHandlers.CreatePosterObject(AssetManager.TextureFromFile(Path.Combine(modPath, "Textures", "npc", posterFileName)), beanPoster.material, ConvertPosterTextData(beanPoster.textData, keyForPosterName, keyForPoster));

				cBean.AddComponent<C>(); // Adds main component: Custom NPC

				if (!includeAnimator)
					cBean.GetComponent<Animator>().enabled = false;
				if (!hasLooker)
					cBean.GetComponent<Looker>().enabled = false;


				cBean.SetActive(false);
				cBean.GetComponent<C>().enabled = true;



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

		private WeightedNPC CreateNPC<C>(string name, int weight, string[] spriteFileName, bool includeAnimator, float pixelsPerUnit, float spriteYOffset, string posterFileName, string keyForPosterName, string keyForPoster, Floors[] floor, RoomCategory[] roomsAllowedToSpawn, bool hasLooker = true, bool enterRooms = true, bool aggored = false, bool ignoreBelts = false, float capsuleRadius = 0f) where C : NPC
		{
			var npc = CreateNPC<C>(name, weight, spriteFileName, includeAnimator, pixelsPerUnit, spriteYOffset, posterFileName, keyForPosterName, keyForPoster, floor, hasLooker, enterRooms, aggored, ignoreBelts, capsuleRadius);
			npc.selection.spawnableRooms = roomsAllowedToSpawn.ToList();
			return npc;
		}

		private WeightedNPC CreateReplacementNPC<C>(string name, int weight, string[] spriteFileName, bool includeAnimator, float pixelsPerUnit, float spriteYOffset, string posterFileName, string keyForPosterName, string keyForPoster, Floors[] floor, Character[] charactersToReplace, bool hasLooker = true, bool enterRooms = true, bool aggored = false, bool ignoreBelts = false, float capsuleRadius = 0f) where C : NPC
		{
			var npc = CreateNPC<C>(name, weight, spriteFileName, includeAnimator, pixelsPerUnit, spriteYOffset, posterFileName, keyForPosterName, keyForPoster, floor, hasLooker, enterRooms, aggored, ignoreBelts, capsuleRadius);
			npc.selection.gameObject.GetComponent<CustomNPCData>().replacementCharacters = charactersToReplace;
			return npc;
		}

		private PosterTextData[] ConvertPosterTextData(PosterTextData[] source, string keyForPosterName, string keyForPoster, int fontSize = 0) // Gets the Poster Text Data[] and converts it to a new one, so it doesn't get by reference (I hate this)
		{
			var newPosterTextData = new PosterTextData[source.Length];
			for (int i = 0; i < source.Length; i++)
			{
				newPosterTextData[i] = new PosterTextData()
				{
					position = source[i].position,
					alignment = source[i].alignment,
					style = source[i].style,
					color = source[i].color,
					font = source[i].font,
					fontSize = source[i].fontSize,
					size = source[i].size
				};

			}
			newPosterTextData[0].textKey = keyForPosterName;
			newPosterTextData[1].textKey = keyForPoster;

			return newPosterTextData;
		}


		public GameObject beans;

		private readonly Dictionary<Floors, bool> accessedNPCs = new Dictionary<Floors, bool>() // Prevents the weights from being added again
		{
			{ Floors.F1, false },
			{ Floors.F2, false },
			{ Floors.F3, false },
			{ Floors.END, false }
		};

		private readonly List<WeightedNPC> allNpcs = new List<WeightedNPC>();

		private readonly List<Floors[]> npcPair = new List<Floors[]>();

		public List<WeightedNPC> GetNPCs(Floors floor, bool onlyReplacementNPCs = false)
		{
			if (accessedNPCs[floor])
				return new List<WeightedNPC>();

			accessedNPCs[floor] = true;
			var npcs = new List<WeightedNPC>();
			for (int i = 0; i < allNpcs.Count; i++)
			{
				if (npcPair[i].Contains(floor) && (!onlyReplacementNPCs || allNpcs[i].selection.gameObject.GetComponent<CustomNPCData>().replacementCharacters.Length > 0)) // Checkc whether the npc is from said floor and if it is a replacement NPC or not
				{
					npcs.Add(allNpcs[i]);
				}
			}
			return npcs;
		}

		public ItemObject RandomItem
		{
			get => WeightedSelection<ItemObject>.RandomSelection(allItems.ToArray());
		}

		public List<WeightedSelection<ItemObject>> GlobalItems 
		{
			get => allItems;
		}

		private readonly List<WeightedSelection<ItemObject>> allItems = new List<WeightedSelection<ItemObject>>();

		private bool addedNPCs = false;

		public static ContentManager instance;

		public static string modPath;

		public static EnvironmentController currentEc;

	}
}
