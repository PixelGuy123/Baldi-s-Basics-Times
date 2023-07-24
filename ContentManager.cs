using BB_MOD.Events;
using BB_MOD.ExtraItems;
using BB_MOD.NPCs;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

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

	public enum SchoolTextType
	{
		None,
		Ceiling,
		Wall,
		Floor
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
			if (index < 0 || index >= values.Count || values.Count == 0)
				throw new ArgumentOutOfRangeException($"The index: {index} is out of the list range (Length: {values.Count})");

			values.RemoveAt(index);
			values.Insert(index, value);
		}

		public static IEnumerable<T> DoAndReturn<T>(this IEnumerable<T> values, Func<T, T> func)
		{
			using (var enumerator = values.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					yield return func(enumerator.Current);
				}
			}
		}
	}

	public static class EnvironmentExtraVariables
	{
		public static void ResetVariables()
		{
			forceDisableSubtitles = false;
			currentFloor = Floors.None;
			ec = null;
		}

		/// <summary>
		/// Turn the subtitles off or on
		/// </summary>
		/// <param name="turn"></param>
		public static void TurnSubtitles(bool turn)
		{
			forceDisableSubtitles = !turn;
		}

		public static bool IsPlayerOnLibrary
		{
			get => ec ? ec.TileFromPos(Singleton<CoreGameManager>.Instance.GetPlayer(0).transform.position).room.name.StartsWith("library", StringComparison.OrdinalIgnoreCase) : false;
		}

		public static EnvironmentController ec;

		public static Floors currentFloor;

		private static bool forceDisableSubtitles = false;

		public static bool AreSubtitlesForceDisabled
		{
			get => forceDisableSubtitles;
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

		public static void CreatePositionalAudio(GameObject obj, float minDistance, float maxDistance, bool supportDoppler = false)
		{
			var src = obj.AddComponent<AudioSource>();
			var aud = obj.AddComponent<AudioManager>();
			aud.audioDevice = src;
			src.maxDistance = maxDistance;
			src.minDistance = minDistance;
			src.rolloffMode = AudioRolloffMode.Custom;
			src.spatialBlend = 1f;
			src.dopplerLevel = supportDoppler ? 1f : 0f;
		}

		public static void CreatePositionalAudio(GameObject obj, float minDistance, float maxDistance, out AudioSource audio, out AudioManager source, bool supportDoppler = false)
		{
			var src = obj.AddComponent<AudioSource>();
			var aud = obj.AddComponent<AudioManager>();
			aud.audioDevice = src;
			src.maxDistance = maxDistance;
			src.minDistance = minDistance;
			src.rolloffMode = AudioRolloffMode.Custom;
			src.spatialBlend = 1f;
			src.dopplerLevel = supportDoppler ? 1f : 0f;
			audio = src;
			source = aud;
		}

		/// <summary>
		/// Creates a positional looping audio that plays infinitely (acts like how Playtime's music works)
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="minDistance"></param>
		/// <param name="maxDistance"></param>
		/// <param name="clip"></param>
		/// <param name="supportDoppler"></param>
		public static void CreateMusicManager(GameObject obj, float minDistance, float maxDistance, SoundObject clip, bool supportDoppler = false) // Creates a music like playtime does
		{
			CreatePositionalAudio(obj, minDistance, maxDistance, out _, out AudioManager musicAudMan, supportDoppler);
			clip.subDuration = float.MaxValue;
			musicAudMan.QueueAudio(clip);
			musicAudMan.SetLoop(true);
		}


		public static AudioClip GetAudioClip(string path) // This will temporarily replace the AssetManager.AudioClipFromFile() method, since the method is adding a 1 second delay on the end of the file, making it literally unable to be a loop
		{
			using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file:///" + path, AudioType.WAV))
			{
				www.SendWebRequest();
				while (www.result == UnityWebRequest.Result.InProgress) { }

				if (www.result != UnityWebRequest.Result.Success)
				{
					Debug.LogError("Error loading audio clip: " + www.error);
					return null;
				}

				return DownloadHandlerAudioClip.GetContent(www);
			}
		}




		public static PosterTextData[] ConvertPosterTextData(PosterTextData[] source, string keyForPosterName, string keyForPoster) // Gets the Poster Text Data[] and converts it to a new one, so it doesn't get by reference (I hate this)
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

		public static PosterTextData CreateTextData(string textKey, int fontSize, IntVector2 textSize, IntVector2 position, FontStyles style, TextAlignmentOptions alignment, Color textColor)
		{
			return new PosterTextData
			{
				textKey = textKey,
				font = ContentManager.instance.posterPre.textData[0].font,
				style = style,
				size = textSize,
				fontSize = fontSize,
				alignment = alignment,
				position = position,
				color = textColor
			};
		}


		public static IntVector2 Right
		{
			get => new IntVector2(1, 0);
		}
		public static IntVector2 Up
		{
			get => new IntVector2(0, 1);
		}
		public static IntVector2 Left
		{
			get => new IntVector2(-1, 0);
		}
		public static IntVector2 Down
		{
			get => new IntVector2(0, -1);
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

			// Parameters explained in order:
			// name > Name of character (will also be the Character enum name, but without spaces)
			// weight > Spawn Weight/Chance of character (100 is enough, above that will make it spawn in pratically every seed)
			// spriteFileName > All File Names of Textures that should be added to the npc (should be png), the first filename on the array will be the default texture
			// flatSprite > if the sprite of the NPC doesn't use billboard (As chalkles does when he is on a chalkboard, for example), if you want to switch between materials, the NPC_CustomData component already have a method for that
			// includeAnimator > Include the animator component (Do whatever you want with it lol)
			// pixelsPerUnit > Basically if this value is larger, the NPC's sprite is smaller
			// spriteYOffset > the offset of the sprite being rendered (if it passes the ground or goes too above, you can regulate by using this variable)
			// posterFileName > file name of the character's detention poster (should also be png), use the placeholder poster from the textures folder to make your own poster
			// keyForPosterName > The json key that is used for the name of the character (basically where the captions come from), go to Language/English/npcCaps and include your own there
			// keyForPOster > The json key that is used for the description of the character, same as above ^^
			// floor > array of floors that the npc can spawn
			// roomsAllowedToSpawn (Optional) > sets the rooms the npc will be able to spawn on (Only hallway by default)
			// hasLooker (Optional, On by default) > If the npc includes a looker component (if it doesn't use the component, you can always disable to not waste resources)
			// enterRooms (Optional, On by default) > If the npc is able of getting inside rooms
			// Aggored (Optional, off by default) > Basically if the Npc's navigator can be overrided (used by Party Event for instance, to bring every npc to the office)
			// ignoreBelts (Optional, off by default) > If the npc ignores conveyor belts
			// capsuleRadius (Optional, default value of 2f) > Set a "size" to the collider of the npc, values higher than 4 are not recommended
			// usingWanderRounds (Optional, off by default) > If the npc uses the method WanderRounds() instead of WanderRandom() on it's navigator, the reason is because WanderRounds uses a heat map, which is disabled by default
			// forceSpawn (optional, off by default) > If the npc ignores the Player's presence and spawn directly (like Gotta Sweep does)

			// What is CreateReplacementNPC<C>?
			// In case you're wondering, it has the exact same function as CreateNPC, the only main difference is: it does not spawn directly from the generator, instead, you can choose in an array of characters for it to replace)
			// This is useful for example: if you want to add a character that functions like Gotta Sweep, you can make it spawn but replacing Gotta Sweep since sweep has a closet anyways

			// CharactersToReplace > As the name suggests, an array of characters that the npc can replace


			// CreateNPC methods should be put here:

			allNpcs.Add(CreateNPC<OfficeChair>("Office Chair", 35, new string[] { "officechair.png", "officechair_disabled.png" }, false, false, 18f, -0.8f, "pri_ofc.png", "PST_OFC_Name", "PST_OFC_Desc", new Floors[] { Floors.F1, Floors.END }, new RoomCategory[] { RoomCategory.Faculty, RoomCategory.Office }, hasLooker: false, aggored: true, capsuleRadius: 4f, forceSpawn: true)); // PixelGuy
			allNpcs.Add(CreateNPC<HappyHolidays>("Happy Holidays", 15, new string[] { "happyholidays.png" }, false, false, 70f, -1.5f, "pri_hapho.png", "PST_HapH_Name", "PST_HapH_Desc", new Floors[] { Floors.F1 }, enterRooms: false, capsuleRadius: 3f)); // PixelGuy
			allNpcs.Add(CreateNPC<SuperIntendent>("Super Intendent", 65, new string[] { "Superintendent.png" }, false, false, 50f, -1f, "pri_SI.png", "PST_SI_Name", "PST_SI_Desc", new Floors[] { Floors.F2, Floors.END }, usingWanderRounds: true)); // PixelGuy
			allNpcs.Add(CreateNPC<CrazyClock>("Crazy Clock", 25, new string[] // All clock sprites in order
			{ "ClockGuy_Normal_Tick1.png", "ClockGuy_Normal_Tock1.png",
				"ClockGuy_Normal_Tick2.png", "ClockGuy_Normal_Tock2.png",
				"ClockGuy_Sight_Tick1.png", "ClockGuy_Sight_Tock1.png",
				"ClockGuy_Sight_Tick2.png", "ClockGuy_Sight_Tock2.png",
				"ClockGuy_Frown.png",
				"ClockGuy_Scream_Tick.png", "ClockGuy_Scream_Tock.png",
			"ClockGuy_Hide1.png", "ClockGuy_Hide2.png", "ClockGuy_Hide3.png", "ClockGuy_Hide4.png", "ClockGuy_Hide5.png", "ClockGuy_Hide6.png", "ClockGuy_Hide7.png", "ClockGuy_Hide8.png","ClockGuy_Hide9.png", "ClockGuy_Hide10.png", "ClockGuy_Hide11.png"} // Hiding Animation
			, true, false, 30f, 0f, "pri_crazyclock.png", "PST_CC_Name", "PST_CC_Desc", new Floors[] { Floors.F3 }, new RoomCategory[] { RoomCategory.FieldTrip, RoomCategory.Test }, forceSpawn: true, aggored: true, ignoreBelts: true)); // Poolgametm (Coded by PixelGuy)
			allNpcs.Add(CreateNPC<Forgotten>("Forgotten", 40, new string[] { "forgotten.png" }, false, false, 25f, 0f, "pri_forgotten.png", "PST_Forgotten_Name", "PST_Forgotten_Name_Desc", new Floors[] { Floors.F2, Floors.F3, Floors.END }, enterRooms: true, capsuleRadius: 4f)); // JDvideosPR
			allNpcs.Add(CreateNPC<LetsDrum>("Let's Drum", 45, new string[] { "Lets_Drum.png" }, false, false, 51f, -1f, "pri_letsdrum.png", "PST_DRUM_Name", "PST_DRUM_Desc", new Floors[] { Floors.F2, Floors.F3 }, enterRooms: false)); // PixelGuy


			// End of Character Spawns

			addedNPCs = true; // To not repeat instancing again
		}


		private WeightedNPC CreateNPC<C>(out bool success, string name, int weight, string[] spritesFileName, bool flatSprite, bool includeAnimator, float pixelsPerUnit, float spriteYOffset, string posterFileName, string keyForPosterName, string keyForPoster, Floors[] floor, bool hasLooker = true, bool enterRooms = true, bool aggored = false, bool ignoreBelts = false, float capsuleRadius = 0f, bool usingWanderRounds = false, bool forceSpawn = false) where C : NPC // The order of everything here must be IN THE ORDER I PUT, or else it'll log annoying null exceptions
		{
			var cBean = Instantiate(beans); // Instantiate a bean instance and customize it
			cBean.name = "CustomNPC_" + name;
			try
			{
				// NOTE: Default Method will let custom npc spawn only in hallway

				CheckForParameters(weight, floor);

				Destroy(cBean.GetComponent<Beans>()); // Removes beans component, useless


				var customData = cBean.AddComponent<CustomNPCData>();
				Character cEnum = EnumExtensions.ExtendEnum<Character>(name.Replace(" ", ""));
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

				customData.poster = ObjectCreatorHandlers.CreatePosterObject(AssetManager.TextureFromFile(Path.Combine(modPath, "Textures", "npc", posterFileName)), beanPoster.material, ContentUtilities.ConvertPosterTextData(beanPoster.textData, keyForPosterName, keyForPoster));

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

				customData.materials[0] = renderer.material;
				customData.materials[1] = Instantiate(Resources.FindObjectsOfTypeAll<Material>().First(x => x.name.ToLower() == "chalkles"));


				customData.SwitchMaterials(flatSprite); // Gets chalkles material which has no billboard


				npcPair.Add(floor);

				if (capsuleRadius > 0)
					cBean.GetComponent<CapsuleCollider>().radius = capsuleRadius;
				success = true;
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
				Debug.LogWarning("Your NPC will be destroyed, and a disabled Beans Instance will be added to the list");
			}
			Destroy(cBean);
			npcPair.Add(Array.Empty<Floors>());
			success = false;
			return new WeightedNPC()
			{
				weight = 1,
				selection = beans.GetComponent<Beans>()
			};

		}

		private WeightedNPC CreateNPC<C>(string name, int weight, string[] spriteFileName, bool flatSprite, bool includeAnimator, float pixelsPerUnit, float spriteYOffset, string posterFileName, string keyForPosterName, string keyForPoster, Floors[] floor, RoomCategory[] roomsAllowedToSpawn, bool hasLooker = true, bool enterRooms = true, bool aggored = false, bool ignoreBelts = false, float capsuleRadius = 0f, bool usingWanderRounds = false, bool forceSpawn = false) where C : NPC
		{
			var npc = CreateNPC<C>(out bool _, name, weight, spriteFileName, flatSprite, includeAnimator, pixelsPerUnit, spriteYOffset, posterFileName, keyForPosterName, keyForPoster, floor, hasLooker, enterRooms, aggored, ignoreBelts, capsuleRadius, usingWanderRounds, forceSpawn);
			npc.selection.spawnableRooms = roomsAllowedToSpawn.ToList();
			return npc;
		}

		private WeightedNPC CreateNPC<C>(string name, int weight, string[] spriteFileName, bool flatSprite, bool includeAnimator, float pixelsPerUnit, float spriteYOffset, string posterFileName, string keyForPosterName, string keyForPoster, Floors[] floor, bool hasLooker = true, bool enterRooms = true, bool aggored = false, bool ignoreBelts = false, float capsuleRadius = 0f, bool usingWanderRounds = false, bool forceSpawn = false) where C : NPC
		{
			return CreateNPC<C>(out bool _, name, weight, spriteFileName, flatSprite, includeAnimator, pixelsPerUnit, spriteYOffset, posterFileName, keyForPosterName, keyForPoster, floor, hasLooker, enterRooms, aggored, ignoreBelts, capsuleRadius, usingWanderRounds, forceSpawn);
		}

		private WeightedNPC CreateReplacementNPC<C>(string name, int weight, string[] spriteFileName, bool flatSprite, bool includeAnimator, float pixelsPerUnit, float spriteYOffset, string posterFileName, string keyForPosterName, string keyForPoster, Floors[] floor, Character[] charactersToReplace, bool hasLooker = true, bool enterRooms = true, bool aggored = false, bool ignoreBelts = false, float capsuleRadius = 0f, bool usingWanderRounds = false, bool forceSpawn = false) where C : NPC
		{
			var npc = CreateNPC<C>(out bool success, name, weight, spriteFileName, flatSprite, includeAnimator, pixelsPerUnit, spriteYOffset, posterFileName, keyForPosterName, keyForPoster, floor, hasLooker, enterRooms, aggored, ignoreBelts, capsuleRadius, usingWanderRounds, forceSpawn);
			if (success)
			{
				npc.selection.gameObject.GetComponent<CustomNPCData>().replacementCharacters = charactersToReplace;
			}
			return npc;
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
		// includeOnMysteryRoom (Optional, disabled by default) > if item should be included on mystery room (uses the same weight used for world spawn), doesn't matter the floor, if there's mystery room, it'll spawn
		// includeOnFieldTrip (Optional, disabled by default) > if item should be included on Field Trip (uses the same weight used for world spawn), ^^ same applies for field trips
		// includeOnParty (Optional, disabled by default) > If item is also included on Party Event (uses the same weight used for world spawn), ^^ same applies

		public void SetupItemWeights()
		{
			if (addedItems)
				return;

			addedItems = true;

			// Item Creation Here

			allNewItems.Add(CreateItem<ITM_Present>("PRS_Name", "PRS_Desc", "present.png", "present.png", "Present", 120, 40, 30, new Floors[] { Floors.F3 }, 55, new Floors[] { Floors.F3 }, 60, includeOnMysteryRoom: true)); // PixelGuy
		}


		private WeightedItemObject CreateItem<I>(string itemNameKey, string itemDescKey, string largeSpriteFile, string smallSpriteFile, string itemName, int shopPrice, int itemCost, int spawnWeight, int largerPixelsPerUnit, Floors[] shoppingFloors, int shoppingWeight, bool includeOnMysteryRoom = false, bool includeOnFieldTrip = false, bool includeOnPartyEvent = false) where I : Item
		{
			Items cEnum = EnumExtensions.ExtendEnum<Items>(itemName);
			customItemEnums.Add(cEnum);
			var item = ObjectCreatorHandlers.CreateItemObject(itemNameKey, itemDescKey, AssetManager.SpriteFromTexture2D(AssetManager.TextureFromFile(Path.Combine(modPath, "Textures", "item", smallSpriteFile))), AssetManager.SpriteFromTexture2D(AssetManager.TextureFromFile(Path.Combine(modPath, "Textures", "item", largeSpriteFile)), new Vector2(0.5f, 0.5f), largerPixelsPerUnit), cEnum, shopPrice, itemCost);
			var itemInstance = new GameObject(itemName + "_ItemInstance").AddComponent<I>(); // Creates Item Component from a new game object
			try
			{
				CheckForParameters(spawnWeight, itemCost);

				DontDestroyOnLoad(itemInstance.gameObject); // Assure that it won't despawn so it doesn't break the item
				item.item = itemInstance;
				item.name = itemName;
				itemInstance.gameObject.SetActive(false); // No null exceptions (Only if it contains Update() or similar )
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
				Debug.LogWarning("Your Item will be destroyed");
			}
			Destroy(itemInstance);
			itemPair.Add(Array.Empty<Floors>());
			return new WeightedItemObject()
			{
				weight = 50,
				selection = null
			};
		}


		private WeightedItemObject CreateItem<I>(string itemNameKey, string itemDescKey, string largeSpriteFile, string smallSpriteFile, string itemName, int shopPrice, int itemCost, int spawnWeight, Floors[] spawnFloors, int largerPixelsPerUnit, Floors[] shoppingFloors, int shoppingWeight, bool includeOnMysteryRoom = false, bool includeOnFieldTrip = false, bool includeOnPartyEvent = false) where I : Item
		{
			var item = CreateItem<I>(itemNameKey, itemDescKey, largeSpriteFile, smallSpriteFile, itemName, shopPrice, itemCost, spawnWeight, largerPixelsPerUnit, shoppingFloors, shoppingWeight, includeOnMysteryRoom, includeOnFieldTrip, includeOnPartyEvent);
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
			try
			{
				CheckForParameters(weight, availableFloors);

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

				return new WeightedRandomEvent()
				{
					selection = obj.GetComponent<E>(),
					weight = weight
				};
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				Debug.LogWarning($"The event: {eventName} has failed to be added, that will happen if there\'s an invalid weight");
				Debug.LogWarning("Your event won\'t be added to the level");
				eventPair.Add(Array.Empty<Floors>());

				return new WeightedRandomEvent();
			}
		}

		// ------------------------------------ CUSTOM POSTER CREATION ---------------------------------------


		// Parameters of CreateSingle/Multi Poster:
		// textureName > name of the texture used for the poster (must be png, and in Textures/poster folder)
		// weight (default is 100) > sets the chance to the poster to spawn (default is 100 to have the same weight as other posters, community walls uses 10 on weight)
		// -- If your poster contains text --
		// textKey > the key (to read the text) from the posterCaps.json file
		// fontSize > size of the font
		// borderLength > basically where will the the text begin to break lines based from the position the text has been set
		// position > text position
		// fontStyle > style of the font (italic, bold, etc.)
		// alignment > alignment of the text
		// textColor > color of the text
		// -- If it is a multi poster --
		// posters > array of posters (you must use the CreatePosterObject() method to fill the array), the first poster on the array will be the main one

		// You can add the poster either for allposters list (general posters that spawn in hallways) or chalkboards list (the chalkboards that spawn at classrooms)

		// -------- WARNING --------
		// Before adding your posters, make sure that the poster texture has the EXACT SIZE OF 256x256 in both dimensions, or else the game will fail to load your texture and will leave an ugly gray wall

		public void SetupPosterWeights()
		{
			if (addedPosters)
				return;

			addedPosters = true;

			// Single Posters Here

			allPosters.Add(CreateSinglePoster("what.png", 30));

			// Multi-Posters Here

			allPosters.Add(CreateMultiPoster(new PosterObject[] {
				CreatePosterObject("Comic_BaldiCookie_0.png", "Comic_Cookie0", 12, new IntVector2(200, 30), new IntVector2(-2, 150), FontStyles.Normal, TextAlignmentOptions.Center, Color.black),
				CreatePosterObject("Comic_BaldiCookie_1.png", "Comic_Cookie1", 14, new IntVector2(90, 200), new IntVector2(120, 55), FontStyles.Normal, TextAlignmentOptions.Center, Color.black),
				CreatePosterObject("Comic_BaldiCookie_2.png", "Comic_Cookie2", 16, new IntVector2(200, 30), new IntVector2(10, 150), FontStyles.Normal, TextAlignmentOptions.Center, Color.black),
				CreatePosterObject("Comic_BaldiCookie_3.png", "Comic_Cookie3", 17, new IntVector2(110, 200), new IntVector2(25, 40), FontStyles.Normal, TextAlignmentOptions.Center, Color.black)
			}, 100));

			// Chalkboards here (use allChalkboards.Add() )


		}

		private WeightedPosterObject CreateSinglePoster(string textureName, string textKey, int fontSize, IntVector2 borderLength, IntVector2 position, FontStyles fontStyle, TextAlignmentOptions alignment, Color textColor, int weight = 100) // Create a full single poster
		{
			try
			{
				var text = AssetManager.TextureFromFile(Path.Combine(modPath, "Textures", "poster", textureName));
				CheckForParameters(weight, text, 256, 256);

				var material = posterPre.material.DoAndReturn(x => new Material(x)
				{
					mainTexture = text
				}).ToArray();
				return new WeightedPosterObject()
				{
					selection = ObjectCreatorHandlers.CreatePosterObject(text, material, new PosterTextData[] { ContentUtilities.CreateTextData(textKey, fontSize, borderLength, position, fontStyle, alignment, textColor) }),
					weight = weight
				};
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				Debug.LogWarning("Failed to load poster, the poster won\'t be added, make sure the file name is correct!");
			}
			return new WeightedPosterObject();

		}

		private WeightedPosterObject CreateSinglePoster(string textureName, PosterTextData[] texts, int weight = 100)
		{
			try
			{
				var text = AssetManager.TextureFromFile(Path.Combine(modPath, "Textures", "poster", textureName));
				CheckForParameters(weight, text, 256, 256);

				var material = posterPre.material.DoAndReturn(x => new Material(x)
				{
					mainTexture = text
				}).ToArray();
				return new WeightedPosterObject()
				{
					selection = ObjectCreatorHandlers.CreatePosterObject(text, material, texts),
					weight = weight
				};
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				Debug.LogWarning("Failed to load poster, the poster won\'t be added, make sure the file name is correct!");
			}
			return new WeightedPosterObject()
			{
				selection = posterPre,
				weight = 0
			};
		}

		private PosterObject CreatePosterObject(string textureName, string textKey, int fontSize, IntVector2 borderLength, IntVector2 position, FontStyles fontStyle, TextAlignmentOptions alignment, Color textColor)
		{
			return CreateSinglePoster(textureName, textKey, fontSize, borderLength, position, fontStyle, alignment, textColor).selection;
		}

		private PosterObject CreatePosterObject(string textureName)
		{
			return CreateSinglePoster(textureName).selection;
		}

		private WeightedPosterObject CreateSinglePoster(string textureName, int weight = 100) // Create an only-image poster (Community Walls for example)
		{
			try
			{
				var text = AssetManager.TextureFromFile(Path.Combine(modPath, "Textures", "poster", textureName));

				CheckForParameters(weight, text, 256, 256);

				var material = posterPre.material.DoAndReturn(x => new Material(x)
				{
					mainTexture = text
				}).ToArray();
				return new WeightedPosterObject()
				{
					selection = ObjectCreatorHandlers.CreatePosterObject(text, material, Array.Empty<PosterTextData>()),
					weight = weight
				};
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				Debug.LogWarning("Failed to load poster, the poster won\'t be added, make sure the file name is correct!");
			}
			return new WeightedPosterObject()
			{
				selection = posterPre,
				weight = 0
			};
		}

		private WeightedPosterObject CreateMultiPoster(PosterObject[] posters, int weight = 100) // Creates posters like those comics
		{
			if (posters.Any(x => !x))
			{
				return new WeightedPosterObject()
				{
					selection = posterPre,
					weight = 0
				};
			}
			posters[0].multiPosterArray = posters;
			return new WeightedPosterObject()
			{
				selection = posters[0],
				weight = weight
			};
		}


		// ------------------ WORLD MAP CREATION -------------------

		// Parameters of CreateSchoolTexture()
		// textureName > name of the texture (png file) to be loaded into the game (must be on Textures/schooltext)
		// floors > floors that the texture will be available
		// SchoolTextType (optional: more than 1 type) > the type of texture it is going to be, can be a wall texture, floor or ceiling texture
		// existOnClassrooms > if the texture also applies for classrooms (off by default)
		// existOnFaculties > if the texture also applies for faculties (off by default), also affects the office textures (office uses the same texture that faculties does)
		// roomsOnly > if the texture is only applied for the rooms (off by default)
		// weight > the chance of the texture being chosen (100 by default, to be equal to the other weights)

		// ---- WARNING ----
		// Just like the poster warning, the wall/ground/ceiling textures must be of the exact: 128x128 on both dimensions!

		public void SetupSchoolTextWeights()
		{
			if (addedTexts)
				return;

			addedTexts = true;

			// Add your textures here

			CreateSchoolTexture("lightCarpet.png", new Floors[] { Floors.F1, Floors.F2, Floors.F3, Floors.END }, SchoolTextType.Floor, existOnClassrooms: true, roomsOnly: true);
		}

		private void CreateSchoolTexture(string textureName, Floors[] floors, SchoolTextType[] types, bool existOnClassrooms = false, bool existOnFaculties = false, bool roomsOnly = false, int weight = 100)
		{
			try
			{
				if (roomsOnly && !existOnClassrooms && !existOnFaculties)
					throw new ArgumentException($"The texture: {textureName} supports only rooms but isn\'t applied for any room type");

				if (types.Length == 0)
					throw new ArgumentException("No school texture type has been selected (empty array)");


				var text = AssetManager.TextureFromFile(Path.Combine(modPath, "Textures", "schooltext", textureName));

				CheckForParameters(weight, text, 128, 128, floors);

				textPair.Add(floors);
				textPairForRooms.Add(new bool[]
				{
					existOnClassrooms,
					existOnFaculties,
					roomsOnly
				});
				allTexts.Add(new WeightedTexture2D
				{
					selection = text,
					weight = weight
				}, types);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				Debug.LogWarning($"The texture: {textureName} was failed to be loaded, the file name inputted is probably wrong");
				Debug.LogWarning("Your texture won\'t be added to the level");
			}

		}

		private void CreateSchoolTexture(string textureName, Floors[] floors, SchoolTextType type, bool existOnClassrooms = false, bool existOnFaculties = false, bool roomsOnly = false, int weight = 100) => CreateSchoolTexture(textureName, floors, new SchoolTextType[] { type }, existOnClassrooms, existOnFaculties, roomsOnly, weight);


		// ---- Exception Throwers ----

		private void CheckForParameters(int weight)
		{
			if (weight <= 0)
				throw new ArgumentOutOfRangeException("The weight value is below or equal to 0");
		}

		private void CheckForParameters(params int[] weights)
		{
			if (weights.Any(x => x <= 0))
				throw new ArgumentOutOfRangeException("The weight value is bwlor or equal to 0");
		}

		private void CheckForParameters(int weight, Floors[] floors)
		{
			CheckForParameters(weight);

			if (floors.Length == 0)
				throw new ArgumentException("No floors have been added for spawn");

			if (floors.Contains(Floors.None))
				throw new ArgumentException("Floors.None enum has been included on the array, that isn\'t allowed");
		}

		private void CheckForParameters(int weight, Texture2D text, int width, int height)
		{
			CheckForParameters(weight);

			if (text.height != height || text.width != width)
				throw new ArgumentOutOfRangeException($"The poster\'s resolution is invalid (Texture Current Resolution: {text.height}x{text.width} > must be {height}x{width})");
		}

		private void CheckForParameters(int weight, Texture2D text, int width, int height, Floors[] floors)
		{
			CheckForParameters(weight, floors);

			if (text.height != height || text.width != width)
				throw new ArgumentOutOfRangeException($"The poster\'s resolution is invalid (Texture Current Resolution: {text.height}x{text.width} > must be {height}x{width})");
		}


		public GameObject beans; // Most important game object for npc creation

		public PosterObject posterPre; // First poster instance to be used

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

		private readonly Dictionary<Floors, bool> accessedExtraStuff = new Dictionary<Floors, bool>()
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

		private readonly List<WeightedPosterObject> allPosters = new List<WeightedPosterObject>();

		private readonly List<WeightedPosterObject> allChalkboards = new List<WeightedPosterObject>();

		private readonly Dictionary<WeightedTexture2D, SchoolTextType[]> allTexts = new Dictionary<WeightedTexture2D, SchoolTextType[]>();

		private readonly List<Floors[]> npcPair = new List<Floors[]>();

		private readonly List<Floors[]> itemPair = new List<Floors[]>();

		private readonly List<Floors[]> eventPair = new List<Floors[]>();

		private readonly List<Floors[]> textPair = new List<Floors[]>();

		private readonly List<bool[]> textPairForRooms = new List<bool[]>();

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

		public List<WeightedTexture2D> GetSchoolText(Floors floor, SchoolTextType type, int roomType) // 0 = classroom, 1 = faculty room, 2 = if it is rooms only (not useful on this method)
		{
			if (accessedExtraStuff[floor])
				return new List<WeightedTexture2D>();

			var texts = new List<WeightedTexture2D>();
			for (int i = 0; i < allTexts.Count; i++)
			{
				if (textPair[i].Contains(floor) && allTexts.ElementAt(i).Value.Contains(type) && textPairForRooms[i][roomType]) // Check whether the text is from said floor and the texture type
				{
					texts.Add(allTexts.ElementAt(i).Key);
				}
			}
			return texts;

		}

		public List<WeightedTexture2D> GetSchoolText(Floors floor, SchoolTextType type)
		{
			if (accessedExtraStuff[floor])
				return new List<WeightedTexture2D>();

			var texts = new List<WeightedTexture2D>();
			for (int i = 0; i < allTexts.Count; i++)
			{
				if (textPair[i].Contains(floor) && allTexts.ElementAt(i).Value.Contains(type) && !textPairForRooms[i][2]) // Check whether the text is from said floor and the texture type
				{
					texts.Add(allTexts.ElementAt(i).Key);
				}
			}
			return texts;

		}

		public bool HasAccessedFloor(Floors floor) => accessedExtraStuff[floor];

		public void LockAccessedFloor(Floors floor) => accessedExtraStuff[floor] = true;

		public List<WeightedNPC> AllNpcs
		{
			get => allNpcs;
		}

		public List<WeightedRandomEvent> AllEvents
		{
			get => allEvents;
		}

		public List<WeightedPosterObject> AllPosters(bool isChalkBoard)
		{
			return isChalkBoard ? allChalkboards : allPosters;
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

		public LevelObject[] GetLevelObjectCopy(LevelObject ld)
		{
			LevelObject[] array = Array.Empty<LevelObject>();
			var copyArray = copyOfLevelObjects.Where(x => x).OrderBy(x => x.name).ToArray();

			for (int i = 0; i < copyArray.Length; i++)
			{
				if (copyArray[i].name.Contains(ld.name))
				{  // Found, stop
					break;
				}

				array = array.AddToArray(copyArray[i]);
			}

			return array;
		}

		public void AddLevelObject(LevelObject ld)
		{
			foreach (var item in copyOfLevelObjects)
			{
				if (item && item.name.Contains(ld.name)) return;
			}

			for (int i = 0; i < copyOfLevelObjects.Length; i++)
			{
				if (!copyOfLevelObjects[i])
				{
					copyOfLevelObjects[i] = ld;
					break;
				}
			}

		}





		public static ContentManager instance;

		public static string modPath;

		public List<Items> customItemEnums = new List<Items>();

		public List<Character> customNPCEnums = new List<Character>();

		public List<RandomEventType> customEventEnums = new List<RandomEventType>();

		// -------- DONT TOUCH Variables ---------

		private bool addedNPCs = false;

		private bool addedItems = false;

		private bool addedEvents = false;

		private bool addedPosters = false;

		private bool addedTexts = false;

		private readonly List<WeightedItemObject> mysteryItems = new List<WeightedItemObject>();

		private readonly List<WeightedItemObject> partyItems = new List<WeightedItemObject>();

		private readonly List<WeightedItem> fieldTripItems = new List<WeightedItem>();

		private bool usedFieldTrip = false;

		private readonly List<WeightedItemObject> allItems = new List<WeightedItemObject>();

		private bool debugMode = false;

		public bool DebugMode
		{
			get => debugMode;
			set => debugMode = value;
		}

		public Texture2D sweepPoster; // This variable is used SPECIFICALLY for a patch that changes gotta sweep parameters, so when switching back, I use this variable

		public Sprite sweepSprite; // Same applies here

		private LevelObject[] copyOfLevelObjects = new LevelObject[3]; // A copy of each level object to replace the current one (so custom characters aren't added to the next level)

	}
}
