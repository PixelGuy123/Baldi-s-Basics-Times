using BB_MOD.NPCs;
using BB_MOD.ExtraItems;
using BB_MOD.Events;
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
		None, // This value is reserved and should never be used in an object!
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
				if (EnumExtensions.GetExtendedName<Items>((int)item).ToLower() == name.ToLower())
					return item;
			}
			return Items.None;
		}

		public static Character GetCharacterByName(this List<Character> npcs, string name)
		{
			foreach (var item in npcs)
			{
				if (EnumExtensions.GetExtendedName<Character>((int)item).ToLower() == name.ToLower())
					return item;
			}
			return Character.Null;
		}

		public static RandomEventType GetEventByName(this List<RandomEventType> events, string name)
		{
			foreach (var item in events)
			{
				if (EnumExtensions.GetExtendedName<RandomEventType>((int)item).ToLower() == name.ToLower())
					return item;
			}
			return RandomEventType.Fog; // There's no null value, so returns Fog by default
		}

		public static void Replace<T>(this List<T> values, int index, T value)
		{
			if (index < 0 || index >= values.Count)
				return;

			values.RemoveAt(index);
			if (values.Count > 1)
				values.Insert(index - 1, value);
			else
				values.Add(value);
		}
	}

	public static class ContentUtilities
	{
		public static Vector2[] ConvertSideToTexture(int x, int y, int width, int height, int texWidth, int texHeight, int index, Vector2[] oldUv)
		{
			Vector2[] uv = oldUv;
			Vector2[] newUvs = GetUVRectangles(x, y, width, height, texWidth, texHeight);
			int setIdx = index;
			for (int i = 0; i < 4; i++)
			{
				uv[setIdx] = newUvs[i];
				setIdx++;
			}
			return uv;
		}
		private static Vector2 ConvertToUVCoords(int x, int y, int width, int height) => new Vector2((float)x / width, (float)y / height);

		private static Vector2[] GetUVRectangles(int x, int y, int width, int height, int textWidth, int textHeight) => new Vector2[] {
			ConvertToUVCoords(x, y + height, textWidth, textHeight),
			ConvertToUVCoords(x, y, textWidth, textHeight),
			ConvertToUVCoords(x + width, y, textWidth, textHeight),
			ConvertToUVCoords(x + width, y + height, textWidth, textHeight)
		};
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

			// Parameters explained in order:
			// name > Name of character (will also be the Character enum name)
			// weight > Spawn Weight/Chance of character
			// spriteFileName > All File Names of Textures that should be added to the npc (should be png), the first filename on the array will be the default texture
			// includeAnimator > Include the animator component (Do whatever you want with it lol)
			// pixelsPerUnit > Basically if this value is larger, the NPC's sprite is smaller
			// spriteYOffset > the offset of the sprite being rendered (if it passes the ground or goes too above, you can regulate by using this variable)
			// posterFileName > file name of the character's detention poster (should also be png), use the placeholder poster from the textures folder to make your own poster
			// keyForPosterName > The json key that is used for the name of the character (basically where the captions come from), go to Language/English/npcCaps and include your own there
			// keyForPOster > The json key that is used for the description of the character, same as above ^^
			// floor > array of floors that the npc can spawn
			// roomsAllowedToSpawn (Optional) > sets the rooms the npc will be able to spawn on (Use default values of Beans)
			// hasLooker (Optional, On by default) > If the npc includes a looker component (if it doesn't use the component, you can always disable to not waste resources)
			// enterRooms (Optional, On by default) > If the npc is able of getting inside rooms
			// Aggored (Optional, off by default) > Basically if the Npc's navigator can be overrided (used by Party Event for instance, to bring every npc to the office)
			// ignoreBelts (Optional, off by default) > If the npc ignores conveyor belts
			// capsuleRadius (Optional, default value of 2f) > Set a "size" to the collider of the npc, values higher than 4 causes doors to be opened without even entering the room
			// usingWanderRounds (Optional, off by default) > If the npc uses the method WanderRounds() instead of WanderRandom() on it's navigator, the reason is because WanderRounds uses a heat map, which is disabled by default
			// forceSpawn (optional, off by default) > If the npc ignores the Player's presence and spawn directly (like Gotta Sweep does)

			// What is CreateReplacementNPC<C>?
			// In case you're wondering, it has the exact same function as CreateNPC, the only main difference is: it does not spawn directly from the generator, instead, you can choose in an array of characters for it to replace)
			// This is useful for example: if you want to add a character that functions like Gotta Sweep, you can make it spawn but replacing Gotta Sweep since sweep has a closet anyways

			// CharactersToReplace > As the name suggests, an array of characters that the npc can replace


			// CreateNPC methods should be put here:

			allNpcs.Add(CreateNPC<OfficeChair>("Office Chair", 35, new string[] { "officechair.png", "officechair_disabled.png" }, false, 18f, -0.8f, "pri_ofc.png", "PST_OFC_Name", "PST_OFC_Desc", new Floors[] { Floors.F1, Floors.END }, new RoomCategory[] { RoomCategory.Faculty }, hasLooker: false, aggored: true, capsuleRadius: 4f, forceSpawn: true)); // PixelGuy
			allNpcs.Add(CreateNPC<HappyHolidays>("Happy Holidays", 15, new string[] { "happyholidays.png" }, false, 70f, -1.5f, "pri_hapho.png", "PST_HapH_Name", "PST_HapH_Desc", new Floors[] { Floors.F1 }, enterRooms: false, capsuleRadius: 3f)); // PixelGuy
			allNpcs.Add(CreateNPC<SuperIntendent>("Super Intendent", 65, new string[] { "Superintendent.png" }, false, 50f, -1f, "pri_SI.png", "PST_SI_Name", "PST_SI_Desc", new Floors[] { Floors.F2, Floors.END }, usingWanderRounds:true)); // PixelGuy
			allNpcs.Add(CreateNPC<Forgotten>("Forgotten", 40, new string[] { "forgotten.png" }, false, 25f, 0f, "pri_forgotten.png", "PST_Forgotten_Name", "PST_Forgotten_Name_Desc", new Floors[] {Floors.F2, Floors.F3, Floors.END }, enterRooms: true, capsuleRadius: 4f)); // JDvideosPR

			// CreateReplacementNPC methods should be put here:


			// End of Character Spawns

			addedNPCs = true; // To not repeat instancing again
		}


		private WeightedNPC CreateNPC<C>(string name, int weight, string[] spritesFileName, bool includeAnimator, float pixelsPerUnit, float spriteYOffset, string posterFileName, string keyForPosterName, string keyForPoster, Floors[] floor, bool hasLooker = true, bool enterRooms = true, bool aggored = false, bool ignoreBelts = false, float capsuleRadius = 0f, bool usingWanderRounds = false, bool forceSpawn = false) where C : NPC // The order of everything here must be IN THE ORDER I PUT, or else it'll log annoying null exceptions
		{
			try
			{
				// NOTE: Default Method will let custom npc spawn only in hallway

				var cBean = Instantiate(beans); // Instantiate a bean instance and customize it
				cBean.name = "CustomNPC_" + name;
				Destroy(cBean.GetComponent<Beans>()); // Removes beans component, useless



				var customData = cBean.AddComponent<CustomNPCData>();
				Character cEnum = EnumExtensions.ExtendEnum<Character>(name);
				customNPCEnums.Add(cEnum);
				customData.MyCharacter = cEnum;
				customData.EnterRooms = enterRooms;
				customData.Aggroed = aggored;
				customData.IgnoreBelts = ignoreBelts;
				customData.useHeatMap = usingWanderRounds;
				customData.forceSpawn = forceSpawn;
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

		private WeightedNPC CreateNPC<C>(string name, int weight, string[] spriteFileName, bool includeAnimator, float pixelsPerUnit, float spriteYOffset, string posterFileName, string keyForPosterName, string keyForPoster, Floors[] floor, RoomCategory[] roomsAllowedToSpawn, bool hasLooker = true, bool enterRooms = true, bool aggored = false, bool ignoreBelts = false, float capsuleRadius = 0f, bool usingWanderRounds = false, bool forceSpawn = false) where C : NPC
		{
			var npc = CreateNPC<C>(name, weight, spriteFileName, includeAnimator, pixelsPerUnit, spriteYOffset, posterFileName, keyForPosterName, keyForPoster, floor, hasLooker, enterRooms, aggored, ignoreBelts, capsuleRadius, usingWanderRounds, forceSpawn);
			npc.selection.spawnableRooms = roomsAllowedToSpawn.ToList();
			return npc;
		}

		private WeightedNPC CreateReplacementNPC<C>(string name, int weight, string[] spriteFileName, bool includeAnimator, float pixelsPerUnit, float spriteYOffset, string posterFileName, string keyForPosterName, string keyForPoster, Floors[] floor, Character[] charactersToReplace, bool hasLooker = true, bool enterRooms = true, bool aggored = false, bool ignoreBelts = false, float capsuleRadius = 0f, bool usingWanderRounds = false, bool forceSpawn = false) where C : NPC
		{
			var npc = CreateNPC<C>(name, weight, spriteFileName, includeAnimator, pixelsPerUnit, spriteYOffset, posterFileName, keyForPosterName, keyForPoster, floor, hasLooker, enterRooms, aggored, ignoreBelts, capsuleRadius, usingWanderRounds, forceSpawn);
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

		// Parameters of CreateItem() in order:

		// itemNameKey > key for the name of the item
		// itemDescKey > Item's Description (for store)
		// large/small sprite file > name of the file for large/small sprite, shopPrice > price for shop
		// itemCost > Another value for spawn chance, small values means higher chances of spawning (choose values between 1 and 70, values higher than 70 are almost impossible to get)
		// spawnWeight > chance to spawn (in many situations, such as fieldtrips, faculties, etc.)
		// SpawnFloors (Optional) set the floors the item will be able to spawn (default is all floors, including endless)
		// largerPixelsPerUnit > same as npcs, the pixels for the larger sprite (higher values = smaller sizes)
		// smallPixelsPerUnit > same as the previous one, but for the smaller sprite
		// shoppingFloors > floors that the shop will accept the item (if you don't want to, you can always use Array.Empty<Floors>() )
		// shoppingWeight > chance to appear on shop
		// activeObject (Optional; unsafe) > Enables the original gameObject that handles the item class, this is only useful in case you use IEnumerator methods (Just make sure to handle any NullExceptions anyways)
		// includeOnMysteryRoom (Optional, disabled by default) > if item should be included on mystery room (uses the same weight used for world spawn), doesn't matter the floor, if there's mystery room, it'll spawn
		// includeOnFieldTrip (Optional, disabled by default) > if item should be included on Field Trip (uses the same weight used for world spawn), ^^ same applies for field trips
		// includeOnParty (Optional, disabled by default) > If item is also included on Party Event (uses the same weight used for world spawn), ^^ same applies

		public void SetupItemWeights()
		{
			if (addedItems)
				return;

			addedItems = true;

			// Item Creation Here

			allNewItems.Add(CreateItem<ITM_Present>("PRS_Name", "PRS_Desc", "present.png", "present.png", "Present", 120, 40, 30, new Floors[] { Floors.F3 }, 55, new Floors[] { Floors.F3 }, 60, includeOnMysteryRoom:true)); // PixelGuy
		}


		private WeightedItemObject CreateItem<I>(string itemNameKey, string itemDescKey, string largeSpriteFile, string smallSpriteFile, string itemName, int shopPrice, int itemCost, int spawnWeight, int largerPixelsPerUnit, Floors[] shoppingFloors, int shoppingWeight, bool activeObject = false, bool includeOnMysteryRoom = false, bool includeOnFieldTrip = false, bool includeOnPartyEvent = false) where I : Item
		{
			try
			{
				Items cEnum = EnumExtensions.ExtendEnum<Items>(itemName);
				customItemEnums.Add(cEnum);
				var item = ObjectCreatorHandlers.CreateItemObject(itemNameKey, itemDescKey, AssetManager.SpriteFromTexture2D(AssetManager.TextureFromFile(Path.Combine(modPath, "Textures", "item", smallSpriteFile))), AssetManager.SpriteFromTexture2D(AssetManager.TextureFromFile(Path.Combine(modPath, "Textures", "item", largeSpriteFile)), new Vector2(0.5f, 0.5f), largerPixelsPerUnit), cEnum, shopPrice, itemCost);
				var itemInstance = new GameObject(itemName + "_ItemInstance").AddComponent<I>(); // Creates Item Component from a new game object
				DontDestroyOnLoad(itemInstance.gameObject); // Assure that it won't despawn so it doesn't break the item
				item.item = itemInstance;
				item.name = itemName;
				itemInstance.gameObject.SetActive(activeObject); // No null exceptions (Only if it contains Update() or similar )
				var weightedItem = new WeightedItemObject()
				{
					selection = item,
					weight = spawnWeight
				};
				allItems.Add(weightedItem);
				itemPair.Add(new Floors[] { Floors.F1, Floors.F2, Floors.F3, Floors.END });

				if (shoppingFloors.Length > 0 && (shoppingFloors[0] != Floors.None || shoppingFloors.Length > 1))
				{
					allShoppingItems.Add(new WeightedItemObject() { selection = item, weight = shoppingWeight }, shoppingFloors);
				}

				if (includeOnMysteryRoom)
					mysteryItems.Add(weightedItem);

				if (includeOnPartyEvent)
					partyItems.Add(weightedItem);

				if (includeOnFieldTrip)
					fieldTripItems.Add(new WeightedItem()
					{
						selection = item,
						weight = spawnWeight
					});

				return weightedItem;
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				Debug.LogWarning($"Looks like a parameter is wrong on the Item: {itemName}, please go back to your method and make sure every file path/name is correct!");
				Debug.LogWarning("Your Item will be created, but will never be used on the game");
			}
			itemPair.Add(Array.Empty<Floors>());
			return new WeightedItemObject()
			{
				weight = 50,
				selection = null
			};
		}


		private WeightedItemObject CreateItem<I>(string itemNameKey, string itemDescKey, string largeSpriteFile, string smallSpriteFile, string itemName, int shopPrice, int itemCost, int spawnWeight, Floors[] spawnFloors, int largerPixelsPerUnit, Floors[] shoppingFloors, int shoppingWeight, bool activeObject = false, bool includeOnMysteryRoom = false, bool includeOnFieldTrip = false, bool includeOnPartyEvent = false) where I : Item
		{
			var item = CreateItem<I>(itemNameKey, itemDescKey, largeSpriteFile, smallSpriteFile, itemName, shopPrice, itemCost, spawnWeight, largerPixelsPerUnit, shoppingFloors, shoppingWeight, activeObject, includeOnMysteryRoom, includeOnFieldTrip, includeOnPartyEvent);
			itemPair.Replace(itemPair.Count - 1, spawnFloors);
			return item;
		}

		// ---------------------------------------------- EVENT CREATION ----------------------------------------------


		// Parameters of CreateEvent<E>() in order:
		// eventName > name of event (also used to the event enum)
		// eventDescKey > Json key from Language/English/eventCaps.json with the description of the event
		// minEventTime > Minimum time the event will last (a rng chooses a value between the min and max time)
		// maxEventTime > Maximum time the event will last (a rng chooses a value between the min and max time)
		// availableFloors > Floors that the event will be enabled
		// weight > weight/chance for the event to be queued into the floor

		public void SetupEventWeights()
		{
			if (addedEvents)
				return;

			addedEvents = true;

			// Event Creation Here

			allEvents.Add(CreateEvent<PrincipalOut>("PrincipalOut", "Event_PriOut", 40f, 60f, new Floors[] { Floors.F2 }, 35)); // PixelGuy

		}



		private WeightedRandomEvent CreateEvent<E>(string eventName, string eventDescKey, float minEventTime, float maxEventTime, Floors[] availableFloors, int weight) where E : RandomEvent
		{
			var obj = new GameObject("CustomEv_" + eventName, typeof(E), typeof(CustomEventData));
			var data = obj.GetComponent<CustomEventData>();
			data.eventName = eventName;
			var cEnum = EnumExtensions.ExtendEnum<RandomEventType>(eventName);
			data.myEvent = cEnum;
			customEventEnums.Add(cEnum);
			data.eventDescKey = eventDescKey;
			data.minEventTime = minEventTime;
			data.maxEventTime = maxEventTime;

			eventPair.Add(availableFloors);

			DontDestroyOnLoad(obj);
			obj.SetActive(false);

			var weightedItem = new WeightedRandomEvent()
			{
				selection = obj.GetComponent<E>(),
				weight = weight
			};

			return weightedItem;
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

		private readonly Dictionary<Floors, bool> accessedShopItems = new Dictionary<Floors, bool>() // Prevents the weights from being added again (items)
		{
			{ Floors.F1, false },
			{ Floors.F2, false },
			{ Floors.F3, false },
			{ Floors.END, false }
		};

		private readonly Dictionary<Floors, bool> accessedEvents = new Dictionary<Floors, bool>() // Prevents the weights from being added again (items)
		{
			{ Floors.F1, false },
			{ Floors.F2, false },
			{ Floors.F3, false },
			{ Floors.END, false }
		};

		private readonly List<WeightedNPC> allNpcs = new List<WeightedNPC>();

		private readonly List<WeightedItemObject> allNewItems = new List<WeightedItemObject>();

		private readonly Dictionary<WeightedItemObject, Floors[]> allShoppingItems = new Dictionary<WeightedItemObject, Floors[]>();

		private readonly List<WeightedRandomEvent> allEvents = new List<WeightedRandomEvent>();

		private readonly List<Floors[]> npcPair = new List<Floors[]>();

		private readonly List<Floors[]> itemPair = new List<Floors[]>();

		private readonly List<Floors[]> eventPair = new List<Floors[]>();

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

		public List<WeightedRandomEvent> GetEvents(Floors floor)
		{
			if (accessedEvents[floor])
				return new List<WeightedRandomEvent>();

			accessedEvents[floor] = true;
			var npcs = new List<WeightedRandomEvent>();
			for (int i = 0; i < allEvents.Count; i++)
			{
				if (eventPair[i].Contains(floor)) // Check whether the item is from said floor
				{
					npcs.Add(allEvents[i]);
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
					npcs.Add(allNewItems[i]);
				}
			}
			return npcs;
		}

		public List<WeightedItemObject> GetShoppingItems(Floors floor)
		{
			if (accessedShopItems[floor])
				return new List<WeightedItemObject>();

			accessedShopItems[floor] = true;
			var npcs = new List<WeightedItemObject>();
			foreach (var item in allShoppingItems)
			{
				if (item.Value.Contains(floor)) // Check whether the item is from said floor
				{
					npcs.Add(item.Key);
				}
			}
			return npcs;
		}

		public List<WeightedNPC> AllNpcs 
			{
				get => allNpcs;
			}

		public List<WeightedRandomEvent> AllEvents
		{
			get => allEvents;
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

		public List<WeightedItemObject> MysteryItems
		{
			get => mysteryItems;
		}

		public List<WeightedItemObject> PartyItems
		{
			get => partyItems;
		}

		public List<WeightedItem> FieldTripItems
		{
			get
			{
				if (usedFieldTrip)
					return new List<WeightedItem>();
				usedFieldTrip = true;
				return fieldTripItems;

			}
		}

		private readonly List<WeightedItemObject> mysteryItems = new List<WeightedItemObject>();

		private readonly List<WeightedItemObject> partyItems = new List<WeightedItemObject>();

		private readonly List<WeightedItem> fieldTripItems = new List<WeightedItem>();

		private bool usedFieldTrip = false;

		private readonly List<WeightedItemObject> allItems = new List<WeightedItemObject>();

		private bool addedNPCs = false;

		private bool addedItems = false;

		private bool addedEvents = false;

		public static ContentManager instance;

		public static string modPath;

		public static EnvironmentController currentEc;

		public List<Items> customItemEnums = new List<Items>();

		public List<Character> customNPCEnums = new List<Character>();

		public List<RandomEventType> customEventEnums = new List<RandomEventType>();

	}
}
