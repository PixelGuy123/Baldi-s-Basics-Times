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

	public static class FloorEnumExtensions
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

		public static Items GetItemByName(this List<Items> items, string name)
		{
			foreach (var item in items)
			{
				if (item.GetName().ToLower() == name.ToLower())
					return item;
			}
			return Items.None;
		}

		public static void Replace<T>(this List<T> values, int index, T value)
		{
			values.RemoveAt(index);
			values.Insert(index - 1, value);
		}
	}

	public class ContentManager : MonoBehaviour
	{

		private void Awake()
		{
			DontDestroyOnLoad(this);
			instance = this;

			// Add every single BB+ item into the list (default weight being 50 for every one) Note: this list is used later on npcs or mechanics that use item weighted selection

			allItems.AddRange(Resources.FindObjectsOfTypeAll<ItemObject>().ToList().ConvertAll(x => new WeightedItemObject()
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
			// (Optional) usingWanderRounds > if your npc uses WanderRound method, set this variable to true, there are some parameters that differs WanderRandom from this one (Disabled by Default)


			// What is CreateReplacementNPC<C>?
			// In case you're wondering, it has the exact same function as CreateNPC, the only main difference is: it does not spawn directly from the generator, instead, you can choose in an array of characters for it to replace)
			// This is useful for example: if you want to add a character that functions like Gotta Sweep, you can make it spawn but replacing Gotta Sweep since it has a closet


			allNpcs.Add(CreateNPC<OfficeChair>("Office Chair", 35, new string[] { "officechair.png", "officechair_disabled.png" }, false, 18f, -0.8f, "pri_ofc.png", "PST_OFC_Name", "PST_OFC_Desc", new Floors[] { Floors.F1 }, new RoomCategory[] { RoomCategory.Faculty }, hasLooker: false, aggored: true, capsuleRadius: 4f));
			allNpcs.Add(CreateNPC<HappyHolidays>("Happy Holidays", 15, new string[] { "happyholidays.png" }, false, 70f, -1.5f, "pri_hapho.png", "PST_HapH_Name", "PST_HapH_Desc", new Floors[] { Floors.F1 }, enterRooms: false, capsuleRadius: 3f));
			allNpcs.Add(CreateNPC<SuperIntendent>("Super Intendent", 65, new string[] { "Superintendent.png" }, false, 50f, -1f, "pri_SI.png", "PST_SI_Name", "PST_SI_Desc", new Floors[] { Floors.F1 }, usingWanderRounds:true));



			// End of Character Spawns

			addedNPCs = true; // To not repeat instancing again
		}


		private WeightedNPC CreateNPC<C>(string name, int weight, string[] spritesFileName, bool includeAnimator, float pixelsPerUnit, float spriteYOffset, string posterFileName, string keyForPosterName, string keyForPoster, Floors[] floor, bool hasLooker = true, bool enterRooms = true, bool aggored = false, bool ignoreBelts = false, float capsuleRadius = 0f, bool usingWanderRounds = false) where C : NPC // The order of everything here must be IN THE ORDER I PUT, or else it'll log annoying null exceptions
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
				customData.useHeatMap = usingWanderRounds;
				List<Sprite> sprites = new List<Sprite>();
				spritesFileName.Do(x => sprites.Add(AssetManager.SpriteFromTexture2D(AssetManager.TextureFromFile(Path.Combine(modPath, "Textures", "npc", x)), new Vector2(0.5f, 0.5f), pixelsPerUnit)));

				customData.sprites = sprites.ToArray(); // Array of sprites can be accessed through CustomNPCData Component

				DontDestroyOnLoad(cBean);

				var beanPoster = beans.GetComponent<Beans>().Poster; // Get beans poster to set data

				customData.poster = ObjectCreatorHandlers.CreatePosterObject(AssetManager.TextureFromFile(Path.Combine(modPath, "Textures", "npc", posterFileName)), beanPoster.material, ConvertPosterTextData(beanPoster.textData, keyForPosterName, keyForPoster));

				cBean.AddComponent<C>(); // Adds main component: Custom NPC

				cBean.GetComponent<Animator>().enabled = includeAnimator;
				cBean.GetComponent<Looker>().enabled = hasLooker;


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

		private WeightedNPC CreateNPC<C>(string name, int weight, string[] spriteFileName, bool includeAnimator, float pixelsPerUnit, float spriteYOffset, string posterFileName, string keyForPosterName, string keyForPoster, Floors[] floor, RoomCategory[] roomsAllowedToSpawn, bool hasLooker = true, bool enterRooms = true, bool aggored = false, bool ignoreBelts = false, float capsuleRadius = 0f, bool usingWanderRounds = false) where C : NPC
		{
			var npc = CreateNPC<C>(name, weight, spriteFileName, includeAnimator, pixelsPerUnit, spriteYOffset, posterFileName, keyForPosterName, keyForPoster, floor, hasLooker, enterRooms, aggored, ignoreBelts, capsuleRadius, usingWanderRounds);
			npc.selection.spawnableRooms = roomsAllowedToSpawn.ToList();
			return npc;
		}

		private WeightedNPC CreateReplacementNPC<C>(string name, int weight, string[] spriteFileName, bool includeAnimator, float pixelsPerUnit, float spriteYOffset, string posterFileName, string keyForPosterName, string keyForPoster, Floors[] floor, Character[] charactersToReplace, bool hasLooker = true, bool enterRooms = true, bool aggored = false, bool ignoreBelts = false, float capsuleRadius = 0f, bool usingWanderRounds = false) where C : NPC
		{
			var npc = CreateNPC<C>(name, weight, spriteFileName, includeAnimator, pixelsPerUnit, spriteYOffset, posterFileName, keyForPosterName, keyForPoster, floor, hasLooker, enterRooms, aggored, ignoreBelts, capsuleRadius, usingWanderRounds);
			npc.selection.gameObject.GetComponent<CustomNPCData>().replacementCharacters = charactersToReplace;
			return npc;
		}

		private PosterTextData[] ConvertPosterTextData(PosterTextData[] source, string keyForPosterName, string keyForPoster) // Gets the Poster Text Data[] and converts it to a new one, so it doesn't get by reference (I hate this)
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


		// ---------------------------------------------- ITEM CREATION ----------------------------------------------

		// Parameters of CreateItem():
		// itemNameKey > key for the name of the item, itemDescKey > Item's Description (for store), large/small sprite file > name of the file for large/small sprite, shopPrice > price for shop, spawnWeight > chance to spawn (in many situations, such as fieldtrips, faculties, etc.)

		public void SetupItemWeights()
		{
			if (addedItems)
				return;

			addedItems = true;

			allNewItems.Add(CreateItem<ITM_BSODA>("PRS_Name", "PRS_Desc", "present.png", "present.png", "Present", 50, 70));
		}


		private WeightedItemObject CreateItem<I>(string itemNameKey, string itemDescKey, string largeSpriteFile, string smallSpriteFile, string itemName, int shopPrice, int spawnWeight) where I : Item
		{
			Items cEnum = EnumExtensions.ExtendEnum<Items>(itemName);
			customEnums.Add(cEnum);
			var item = ObjectCreatorHandlers.CreateItemObject(itemNameKey, itemDescKey, AssetManager.SpriteFromTexture2D(AssetManager.TextureFromFile(Path.Combine(modPath, "Textures", "item", smallSpriteFile))), AssetManager.SpriteFromTexture2D(AssetManager.TextureFromFile(Path.Combine(modPath, "Textures", "item", largeSpriteFile))), cEnum, shopPrice, spawnWeight);
			var itemInstance = new GameObject(itemName + "_ItemInstance").AddComponent<I>(); // Creates Item Component from a new game object
			DontDestroyOnLoad(itemInstance.gameObject); // Assure that it won't despawn so it doesn't break the item
			item.item = itemInstance;
			itemInstance.gameObject.SetActive(false); // No null exceptions
			var weightedItem = new WeightedItemObject()
			{
				selection = item,
				weight = spawnWeight
			};
			allItems.Add(weightedItem);
			itemPair.Add(new Floors[] { Floors.F1, Floors.F2, Floors.F3, Floors.END });

			return weightedItem;
		}

		private WeightedItemObject CreateItem<I>(string itemNameKey, string itemDescKey, string largeSpriteFile, string smallSpriteFile, string itemName, int shopPrice, int spawnWeight, Floors[] spawnFloors) where I : Item
		{
			var item = CreateItem<I>(itemNameKey, itemDescKey, largeSpriteFile, smallSpriteFile, itemName, shopPrice, spawnWeight);
			itemPair.Replace(itemPair.Count - 1, spawnFloors);
			return item;
		}



		public GameObject beans;

		private readonly Dictionary<Floors, bool> accessedNPCs = new Dictionary<Floors, bool>() // Prevents the weights from being added again (npcs)
		{
			{ Floors.F1, false },
			{ Floors.F2, false },
			{ Floors.F3, false },
			{ Floors.END, false }
		};

		private readonly Dictionary<Floors, bool> accessedItems = new Dictionary<Floors, bool>() // Prevents the weights from being added again (items)
		{
			{ Floors.F1, false },
			{ Floors.F2, false },
			{ Floors.F3, false },
			{ Floors.END, false }
		};

		private readonly List<WeightedNPC> allNpcs = new List<WeightedNPC>();

		private readonly List<WeightedItemObject> allNewItems = new List<WeightedItemObject>();

		private readonly List<Floors[]> npcPair = new List<Floors[]>();

		private readonly List<Floors[]> itemPair = new List<Floors[]>();

		public List<WeightedNPC> GetNPCs(Floors floor, bool onlyReplacementNPCs = false)
		{
			if (accessedNPCs[floor] && !onlyReplacementNPCs)
				return new List<WeightedNPC>();

			accessedNPCs[floor] = true;
			var npcs = new List<WeightedNPC>();
			for (int i = 0; i < allNpcs.Count; i++)
			{
				if (npcPair[i].Contains(floor) && ((!onlyReplacementNPCs && allNpcs[i].selection.gameObject.GetComponent<CustomNPCData>().replacementCharacters.Length == 0) || (allNpcs[i].selection.gameObject.GetComponent<CustomNPCData>().replacementCharacters.Length > 0 && onlyReplacementNPCs))) // Check whether the npc is from said floor and if it is a replacement NPC or not
				{
					npcs.Add(allNpcs[i]);
				}
			}
			return npcs;
		}

		public List<WeightedItemObject> GetItems(Floors floor)
		{
			if (accessedItems[floor])
				return new List<WeightedItemObject>();

			accessedItems[floor] = true;
			var npcs = new List<WeightedItemObject>();
			for (int i = 0; i < allNewItems.Count; i++)
			{
				if (itemPair[i].Contains(floor)) // Check whether the item is from said floor
				{
					Debug.Log(allNewItems[i].selection.item);
					npcs.Add(allNewItems[i]);
				}
			}
			return npcs;
		}

		public List<WeightedNPC> AllNpcs 
			{
				get => allNpcs;
			}

		public List<WeightedItemObject> AllNewItems
		{
			get => allNewItems;
		}

		public ItemObject RandomItem
		{
			get => WeightedItemObject.RandomSelection(allItems.ToArray());
		}

		public List<WeightedItemObject> GlobalItems 
		{
			get => allItems;
		}

		private readonly List<WeightedItemObject> allItems = new List<WeightedItemObject>();

		private bool addedNPCs = false;

		private bool addedItems = false;

		public static ContentManager instance;

		public static string modPath;

		public static EnvironmentController currentEc;

		public List<Items> customEnums = new List<Items>();

	}
}
