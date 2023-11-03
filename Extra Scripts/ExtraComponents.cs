using BB_MOD.ExtraItems;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Collections;
using HarmonyLib;
using System.Linq;
using UnityEngine.AI;
using TMPro;
using System.Runtime.CompilerServices;

namespace BB_MOD.ExtraComponents
{
	public class Vent : MonoBehaviour
	{
		private void Awake()
		{
			audMan = GetComponent<AudioManager>();
			Destroy(GetComponent<BoxCollider>());
			aud_ventNoise = ContentAssets.GetAsset<SoundObject>("ventNoises");

			if (disabledForever) return;

			audMan.maintainLoop = true;
			audMan.QueueAudio(aud_ventNoise);
			audMan.SetLoop(true);
		}

		public void TurnVent(bool turn, bool perma = false)
		{
			if (turn && disabledForever)
				return;

			if (!turn)
			{
				disabledForever = perma;
				audMan?.FlushQueue(true);
			}
			else
				audMan?.QueueAudio(aud_ventNoise);

			if (audMan != null)
			{
				audMan.maintainLoop = turn;
				audMan.SetLoop(turn);
			}
		}

		bool disabledForever = false;

		private AudioManager audMan;
		private SoundObject aud_ventNoise;
	}
	public class CustomPlayerAttributes : MonoBehaviour
	{
		public bool TryGetImmunity(string immunity, out ImmunityToken token)
		{
			var val = ImmuneTo.Find(x => x.Value == immunity);
			token = val;
			return val != null;
		}
		public bool TryGetAttribute(string attribute, out AttributeToken token)
		{
			var val = AffectedBy.Find(x => x.Value == attribute);
			token = val;
			return val != null;
		}


		public List<ImmunityToken> ImmuneTo = new List<ImmunityToken>();

		public List<AttributeToken> AffectedBy = new List<AttributeToken>();

		public class ImmunityToken : GenericToken<string>
		{
			public ImmunityToken(string val) : base(val, 0)
			{
			}
		}

		public class AttributeToken : GenericToken<string>
		{
			public AttributeToken(string val) : base(val, 0)
			{
			}
		}
	}
	public class StandardDoor_ExtraFunctions : MonoBehaviour
	{
		public static void AssignDoorsToTheFunction(EnvironmentController ec)
		{
			foreach (var room in ec.rooms)
			{
				foreach (var door in room.doors)
				{
					if (!door.gameObject.GetComponent<StandardDoor_ExtraFunctions>())
						door.gameObject.AddComponent<StandardDoor_ExtraFunctions>(); // Adds the component
				}
			}
		}
		/// <summary>
		/// Gives a simple function to check if the item corresponds to the required one
		/// </summary>
		/// <param name="item"></param>
		public void AssignFuncToUnlock(Items item) => itemFitFunc = new Func<Items, StandardDoor, bool>((fItem, _) => item == fItem);
		/// <summary>
		/// Sets the current function to a customized <paramref name="func"/>
		/// </summary>
		/// <param name="func"></param>
		public void AssignFuncToUnlock(Func<Items, StandardDoor, bool> func) => itemFitFunc = func;
		/// <summary>
		/// Resets the function to a basic true/false one in case there's no real implement to the function
		/// </summary>
		/// <param name="toggle"></param>
		public void AssignFuncToUnlock(bool toggle) => itemFitFunc = new Func<Items, StandardDoor, bool>((_, _2) => toggle);
		

		public Func<Items, StandardDoor, bool> ItemFittingFunction { get => itemFitFunc; }

		private Func<Items, StandardDoor, bool> itemFitFunc = new Func<Items, StandardDoor, bool>((item, _2) => ContentManager.instance.CanItemUnlockDoors(item));
	}

	public class DecoyBlueLocker : MonoBehaviour, IClickable<int>
	{
		public void Clicked(int player)
		{
			var pm = Singleton<CoreGameManager>.Instance.GetPlayer(player);
			if (stealed || !pm.itm.HasItem()) return;

			pm.ec.MakeNoise(transform.position, 105);
			pm.itm.RemoveRandomItem();
			pm.RuleBreak("Lockers", guiltTime);
			StartCoroutine(TrololoSequence());
			stealed = true;
		}

		IEnumerator TrololoSequence()
		{
			
			var noise = ItemSoundHolder.CreateSoundHolder(transform, aud_troll, true, 40, 70);
			ItemSoundHolder.CreateSoundHolder(transform, slam, true, 40, 60);
			GetComponent<MeshRenderer>().materials[1].SetTexture("_MainTex", decoy_open);

			while (noise.IsPlaying) { yield return null; }

			ItemSoundHolder.CreateSoundHolder(transform, slam, true, 40, 60);
			GetComponent<MeshRenderer>().materials[1].SetTexture("_MainTex", decoy);

			Destroy(this); // Removes the IClickable component entirely so it can't be "interactable" anymore

			yield break;
		}


		bool stealed = false;

		const float guiltTime = 1f;

		readonly SoundObject aud_troll = ContentAssets.GetAsset<SoundObject>("trololo"), slam = ContentAssets.GetAsset<SoundObject>("lockerNoise");

		readonly Texture2D decoy_open = ContentAssets.GetAsset<Texture2D>("d_blueLocker_open"), decoy = ContentAssets.GetAsset<Texture2D>("d_blueLocker");
	}

	public class GreenLocker : MonoBehaviour, IItemAcceptor
	{
		private void Start()
		{
			ContentUtilities.CreatePositionalAudio(gameObject, 30, 60, out _, out AudioManager aud);
			audMan = aud;
			renderer = GetComponent<MeshRenderer>();

			// Setupping Textures
		}
		public void MakeMeDecoy()
		{
			decoy = true;
		}

		public bool ItemFits(Items item)
		{
			return !beenUsed && item == targetItm;
		}

		public void InsertItem(PlayerManager player, EnvironmentController ec)
		{
			beenUsed = true;
			audMan.PlaySingle(slam);

			player.RuleBreak("Lockers", guiltTime);
			if (decoy)
				StartCoroutine(HAHASequence(player));
			else
			{
				player.ec.MakeNoise(transform.position, 78);
				renderer?.materials[1].SetTexture("_MainTex", texs[1]);
				player.itm.SetItem(WeightedItemObject.RandomSelection(ContentManager.instance.GlobalItems.Where(x => x.selection.itemType != targetItm).ToArray()), player.itm.selectedItem);
			}
		}

		private IEnumerator HAHASequence(PlayerManager player)
		{
			audMan.PlaySingle(HA_HA);
			player.ec.MakeNoise(transform.position, 84);
			renderer?.materials[1].SetTexture("_MainTex", texs[3]);
			StealItems(itemsToSteal, player);

			while (audMan.IsPlaying) { yield return null; }

			audMan.PlaySingle(slam);
			renderer?.materials[1].SetTexture("_MainTex", texs[2]);

			yield break;
		}

		private void StealItems(int amount, PlayerManager player)
		{
			for (int i = 0; i < amount; i++) 
			{
				player.itm.RemoveRandomItem();
			}
		}

		readonly Items targetItm = ContentManager.instance.customItemEnums.GetItemByName("Lockpick");

		bool decoy = false, beenUsed = false;

		public bool IsDecoy => decoy;

		readonly Texture2D[] texs = new Texture2D[4] { 
			ContentAssets.GetAsset<Texture2D>("greenLocker"), 
			ContentAssets.GetAsset<Texture2D>("greenLocker_open"), 
			ContentAssets.GetAsset<Texture2D>("d_greenLocker"), // Decoy Textures
			ContentAssets.GetAsset<Texture2D>("d_greenLocker_open1")
		};

		readonly SoundObject slam = ContentAssets.GetAsset<SoundObject>("lockerNoise"), HA_HA = ContentAssets.GetAsset<SoundObject>("HA_HA");

		AudioManager audMan;

		MeshRenderer renderer;

		const int itemsToSteal = 2;

		const float guiltTime = 1f;
	}

	public class FireObject : MonoBehaviour
	{
		private void Awake()
		{
			ec = EnvironmentExtraVariables.ec;
			renderer = transform.Find("Sprite").GetComponent<SpriteRenderer>();
			renderer.material.SetTexture("_LightMap", null);
			spriteAnimation = new Sprite[] { ContentAssets.GetAsset<Sprite>("SchoolFire_FirstFrame"), ContentAssets.GetAsset<Sprite>("SchoolFire_SecondFrame") };
		}

		private void Update()
		{
			animationTiming += 1.8f * Time.deltaTime * ec.EnvironmentTimeScale;
			animationTiming %= 2f;
			renderer.sprite = spriteAnimation[Mathf.FloorToInt(animationTiming)];
		}

		private SpriteRenderer renderer;

		private Sprite[] spriteAnimation = new Sprite[0];

		private float animationTiming = UnityEngine.Random.Range(0f, 2f); // Not make the fires synchronized

		private EnvironmentController ec;
	}

	public class WindowExtraFields : MonoBehaviour
	{
		private void Awake()
		{
			window = GetComponent<Window>();
		}
		public static void AssignWindowsToTheFunction()
		{
			foreach (var window in FindObjectsOfType<Window>())
			{
				if (!window.GetComponent<WindowExtraFields>())
					window.gameObject.AddComponent<WindowExtraFields>(); // Adds the component
				
			}
		}
		public void Unbreak() // Unbreaks the window somehow lol
		{
			if (IsBroken)
			{
				AccessTools.Field(typeof(Window), "broken").SetValue(window, false);
				window.Shut();
			}
		}

		public void CopyFields(WindowExtraFields source)
		{
			IsUnbreakable = source.IsUnbreakable;
			OpenByDefault = source.OpenByDefault;
		}

		public bool IsUnbreakable { get; set; } = false;
		public bool OpenByDefault { get; set; } = false;
		public bool IsBroken => (bool)AccessTools.Field(typeof(Window), "broken").GetValue(window);

		Window window;
	}

	public abstract class PrefabInstance : MonoBehaviour
	{
		public static void CreateInstance<T>() where T : PrefabInstance
		{ 
			var obj = new GameObject();
			var component = obj.AddComponent<T>();
			obj.SetActive(false);
			component.Setup();
			obj.name = namePrefix + component.NameForIt;
			DontDestroyOnLoad(obj);
			prefabs.Add(component);
		}
		public static T SpawnPrefab<T>(Vector3 pos, Quaternion rotation, EnvironmentController ec, bool autoExecute = true) where T : PrefabInstance
		{
			try
			{
				if (EnvironmentExtraVariables.currentFloor == Floors.None)
					throw new OperationCanceledException();

				var prefab = prefabs.Find(x => x.GetType().Equals(typeof(T)));
				var obj = Instantiate(prefab);
				obj.gameObject.SetActive(true);
				obj.transform.SetParent(ec.transform);
				obj.transform.position = pos;
				obj.transform.rotation = rotation;
				var comp = obj.GetComponent<T>();
				comp.SetReferences(ec, prefab.AvailableRender);
				if (autoExecute)
					comp.Execute();

				return comp;
			}
			catch
			{
				Debug.LogWarning("Failed to create the prefab: " + typeof(T) + ", returning null");
				return null;
			}
		}
		public static T SpawnPrefab<T>(Transform pos, EnvironmentController ec, bool autoExecute = true, Vector3 offset = default) where T : PrefabInstance
		{
			var prefab = SpawnPrefab<T>(Vector3.zero, default, ec, autoExecute);
			if (prefab == null)
				return null;

			prefab.transform.SetParent(pos);
			prefab.transform.localPosition = offset;
			return prefab;
		}
		public static T SpawnPrefab<T>(TileController tile, EnvironmentController ec, bool autoExecute = true) where T : PrefabInstance => SpawnPrefab<T>(tile.transform.position + Vector3.up * 5f, default, ec, autoExecute);

		public static T SpawnPrefab<T>(TileController tile, Quaternion rotation, EnvironmentController ec, bool autoExecute = true) where T : PrefabInstance => SpawnPrefab<T>(tile.transform.position + Vector3.up * 5f, rotation, ec, autoExecute);

		public virtual void Setup()
		{
		}

		public abstract string NameForIt { get; }

		protected virtual void SetReferences(EnvironmentController ec, bool hasRender)
		{
			this.ec = ec;
			if (hasRender)
			{
				rendererSprite = transform.Find("Sprite").GetComponent<SpriteRenderer>();
			}
		}
		public virtual void Execute()
		{
			gameObject.SetActive(true);
		}

		public virtual void Despawn(bool withAnimation = false)
		{
			if (!withAnimation)
				Destroy(gameObject);
			else
				StartCoroutine(DespawnWithAnimation());
		}

		protected virtual IEnumerator DespawnWithAnimation()
		{
			yield return null;
			Despawn(false);
			yield break;
		}
		
		protected void CreateSprite(Material mat = null, Sprite sprite = null)
		{
			if (AvailableRender)
			{
				throw new InvalidOperationException($"[{name}] You can't add more than two sprite objects into the same prefab");
			}

			var renderer = ContentUtilities.AddVisualToSprite(transform, sprite, mat);
			rendererSprite = renderer;
		}
		

		protected EnvironmentController ec;

		protected SpriteRenderer rendererSprite = null;

		protected bool AvailableRender => rendererSprite != null;

		private readonly static List<PrefabInstance> prefabs = new List<PrefabInstance>();

		protected const string namePrefix = "CustomObj_";
	}

	public abstract class CustomHud : PrefabInstance
	{
		public override void Execute()
		{
			base.Execute();
			EnvironmentExtraVariables.customHuds.Add(this);
		}

		public override void Despawn(bool withAnimation = false)
		{
			base.Despawn(withAnimation);
			EnvironmentExtraVariables.customHuds.Remove(this);
		}


	}

	public class TrashCan : PrefabInstance, IClickable<int>
	{
		private void Start()
		{
			audMan = GetComponent<AudioManager>();
		}
		public void Clicked(int player)
		{
			var pm = Singleton<CoreGameManager>.Instance.GetPlayer(player);
			if (pm.itm.items[pm.itm.selectedItem].itemType != Items.None)
			{
				audMan?.PlaySingle(aud_throw);
				pm.itm.RemoveItem(pm.itm.selectedItem);
			}
		} 
		public override void Setup()
		{
			CreateSprite(ContentUtilities.DefaultBillBoardMaterial, ContentAssets.GetAsset<Sprite>("trashCan"));
			rendererSprite.transform.localPosition = Vector3.down * 2.5f;
			ContentUtilities.CreatePositionalAudio(gameObject, 30, 60, out _, out AudioManager audio);
			audMan = audio;
			var collider = gameObject.AddComponent<BoxCollider>();
			collider.size = new Vector3(1.8f, 10f, 1.8f);
			var obstacle = gameObject.AddComponent<NavMeshObstacle>();
			obstacle.size = new Vector3(1.8f, 10f, 1.8f);
		}
		public override string NameForIt => "TrashCan";

		readonly SoundObject aud_throw = ContentAssets.GetAsset<SoundObject>("throwTrash");

		AudioManager audMan;
	}

	public class Curtains : PrefabInstance
	{
		public override string NameForIt => "Curtains";

		private void Start()
		{
			audMan = GetComponent<AudioManager>();
			collider = GetComponent<BoxCollider>();
		}

		public override void Setup()
		{
			collider = ContentUtilities.AddCollisionToSprite(gameObject, transform.right * 8f + Vector3.up * 15f, Vector3.zero);
			CreateSprite(ContentManager.Prefabs.flatMaterial, curtains[1]);
			ContentUtilities.CreatePositionalAudio(gameObject, 40f, 70f, out _, out var audioMan);
			audMan = audioMan;
		}

		public void SetWindow(Window window)
		{
			transform.SetParent(window.transform);
			Vector3 pos = window.transform.Find("Buffer").transform.localPosition;
			transform.localPosition = new Vector3(pos.x, 0f, pos.z);
			rendererSprite.transform.localPosition = Vector3.up * 5f;
			myWindow = window;
		}

		public void SetIt(bool closed)
		{
			rendererSprite.sprite = curtains[closed ? 0 : 1];
			collider.enabled = closed;
			var comp = myWindow.GetComponent<WindowExtraFields>();

			if (comp.IsBroken != closed)
			{
				myWindow.Block(closed);
			}
			if (closed)
				isUnbreakable = comp.IsUnbreakable && !comp.IsBroken;
			comp.IsUnbreakable = isUnbreakable || closed;
			audMan.PlaySingle(closed ? aud_close : aud_open);
		}
		bool isUnbreakable = false;

		AudioManager audMan;

		Window myWindow;

		BoxCollider collider;

		readonly Sprite[] curtains = new Sprite[2]
		{
			ContentAssets.GetAsset<Sprite>("curtain_closed"),
			ContentAssets.GetAsset<Sprite>("curtain_open")
		};

		readonly SoundObject aud_open = ContentAssets.GetAsset<SoundObject>("curtainOpen"), aud_close = ContentAssets.GetAsset<SoundObject>("curtainClose");
	}

	public class Trapdoor : PrefabInstance
	{
		private void Start()
		{
			var text = new GameObject("Timer");
			timer = text.AddComponent<TextMeshPro>();
			timer.text = defaultCooldown.ToString();
			timer.transform.SetParent(transform);
			timer.transform.localPosition = Vector3.up * 0.1f;
			timer.transform.localScale = Vector3.one * 0.6f;
			timer.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
			timer.alignment = TextAlignmentOptions.Center;
			timer.gameObject.layer = ContentUtilities.defaultBillboardLayer;
			audMan = GetComponent<AudioManager>();
		}
		public override void Setup()
		{
			base.Setup();
			var collider = ContentUtilities.AddCollisionToSprite(gameObject, new Vector3(5f, 7f, 5f), Vector3.zero);
			collider.isTrigger = true;

			transform.rotation = Quaternion.Euler(90f, 0f, 0f);
			transform.localScale = new Vector3(1.8f, 1f, 1.8f);
			CreateSprite(ContentManager.Prefabs.NewFlatMaterial, closedSprite);

			rendererSprite.transform.localScale = Vector3.one;
			rendererSprite.gameObject.layer = 8; // Post processing layer
			rendererSprite.transform.rotation = Quaternion.Euler(180f, 0f, 0f);

			ContentUtilities.CreatePositionalAudio(gameObject, 15f, 60f);
		}

		public override void Execute()
		{
			base.Execute();
			if (!active || spawnAndDespawnOnly)
				StartCoroutine(AppearAnimation());

			isRandom = !linkedTrapdoor;
			if (spawnAndDespawnOnly)
			{
				Destroy(GetComponent<BoxCollider>()); // No need for a collider
			}
		}

		public void SetAlreadyActive() => active = true;

		public void SpawnAndDespawnOnly() => spawnAndDespawnOnly = true;

		public void SetTrapdoorLink(Trapdoor trapdoor) => linkedTrapdoor = trapdoor;

		public void ForceOpenTrapDoor(bool open)
		{
			onTeleport = open;
			SetState(!open, true);
		}

		protected override IEnumerator DespawnWithAnimation()
		{
			float speed = 0f;
			var total = Vector3.zero;
			while (true)
			{
				if (!Singleton<CoreGameManager>.Instance.Paused)
				{
					speed += 0.1f * ec.EnvironmentTimeScale * Time.deltaTime;
					transform.localScale -= Vector3.one * speed;
					if (transform.localScale.x <= total.x)
					{
						transform.localScale = total; // Just to end the loop here
						break;
					}
				}
				yield return null;
			}
			Despawn();

			yield break;
		}

		public IEnumerator TimeDespawn(float timer)
		{
			float time = timer;
			while (time > 0f)
			{
				time -= ec.EnvironmentTimeScale * Time.deltaTime;
				yield return null;
			}
			Despawn(true);
			yield break;
		}

		IEnumerator AppearAnimation()
		{
			transform.localScale = Vector3.zero;
			float speed = 0f;
			var total = new Vector3(1.8f, 1f, 1.8f);
			if (spawnAndDespawnOnly)
			{
				rendererSprite.sprite = openSprite;
				onTeleport = true;
			}
			while (true)
			{
				if (!Singleton<CoreGameManager>.Instance.Paused)
				{
					speed += 0.1f * ec.EnvironmentTimeScale * Time.deltaTime;
					transform.localScale += new Vector3(total[0] - transform.localScale[0], total[1] - transform.localScale[1], total[2] - transform.localScale[2]) * speed;
					if (transform.localScale.x >= total.x)
					{
						transform.localScale = total; // Just to end the loop here
						break;
					}
				}
				yield return null;
			}
			if (!spawnAndDespawnOnly)
			active = true;

			yield break;
		}

		private void Update()
		{
			if (!active || spawnAndDespawnOnly)
			{
				timer.text = "";
				return; 
			}
			if (openCooldown <= 0f)
			{
				if (!isOpen)
				{
					SetState(false);
					timer.text = "";
				}
			}
			else
			{
				openCooldown -= ec.EnvironmentTimeScale * Time.deltaTime;
				timer.text = Mathf.CeilToInt(openCooldown).ToString();
			}
		}

		private void OnTriggerStay(Collider other)
		{
			if (!isOpen || onTeleport) return;

			if (other.tag == "Player")
			{
				if (!other.GetComponent<PlayerManager>().plm.addendImmune)
					StartCoroutine(Teleport(other.transform, true));
			}
			else if (other.tag == "NPC" && other.isTrigger)
			{
				StartCoroutine(Teleport(other.transform, false));
			}
		}

		IEnumerator Teleport(Transform subject, bool player)
		{
			onTeleport = true;
			Vector3 newPos; // This is just the whirlpool code, but modified lol
			float limit = -3f;
			bool isTempDoor = false;
			if (player)
			{
				limit = 0.5f;
				subject.GetComponent<PlayerManager>().Hide(true);
			}
			else
			{
				subject.GetComponent<NPC>().DisableCollision(true);
			}
			if (linkedTrapdoor == null)
			{
				List<TileController> list = ec.AllTilesNoGarbage(false, false);
				var backupList = new List<TileController>(list);
				while (true)
				{
					int num = UnityEngine.Random.Range(0, list.Count);
					if (!ec.TileObstructed(list[num]))
					{
						newPos = list[num].transform.position;
						break;
					}
					else
					{
						list.RemoveAt(num);
					}
					if (list.Count == 0)
					{
						newPos = backupList[UnityEngine.Random.Range(0, backupList.Count)].transform.position;
						break; // Impossible but for sure
					}
				}
				
				linkedTrapdoor = SpawnTempTrapDoor(newPos);
				isTempDoor = true;
			}
			else
			{
				newPos = linkedTrapdoor.transform.position;
			}
			linkedTrapdoor.OnTeleport = true;
			float height = 5f;
			while (height > limit)
			{
				height -= Time.deltaTime * ec.EnvironmentTimeScale * sinkSpeed;
				subject.position = transform.position + Vector3.up * height;
				yield return null;
			}
			SetState(true, true);
			linkedTrapdoor?.ForceOpenTrapDoor(true);
			height = limit;
			subject.position = transform.position + Vector3.up * height;
			while (height < 5f)
			{
				height += Time.deltaTime * ec.EnvironmentTimeScale * sinkSpeed;
				subject.position = newPos + Vector3.up * height;
				yield return null;
			}
			subject.position = newPos + Vector3.up * 5f;
			if (player)
			{
				subject.GetComponent<PlayerManager>().Hide(false);
			}
			else
			{
				subject.GetComponent<NPC>().DisableCollision(false);
			}
			linkedTrapdoor?.ForceOpenTrapDoor(false);

			if (isTempDoor)
			{
				linkedTrapdoor?.StartCoroutine(linkedTrapdoor?.TimeDespawn(2f));
			}
			
			onTeleport = false;
			yield break;
		}

		private Trapdoor SpawnTempTrapDoor(Vector3 pos)
		{
			var door = SpawnPrefab<Trapdoor>(pos, default, ec, false);
			door.SpawnAndDespawnOnly();
			door.Execute();
			return door;
		}

		private void SetState(bool closed, bool resetCooldown = false)
		{
			if (closed)
			{
				isOpen = false;
				rendererSprite.sprite = isRandom ? closedSprite2 : closedSprite;
				if (resetCooldown)
				{
					openCooldown = defaultCooldown;
				}
				if (firstTime)
					audMan?.PlaySingle(aud_shut);
				else
					firstTime = true;
			}
			else if (!isOpen)
			{
				isOpen = true;
				rendererSprite.sprite = openSprite;
				if (firstTime)
					audMan?.PlaySingle(aud_open);
				else
					firstTime = true;
			}
		}

		private bool active = false, isOpen = false, spawnAndDespawnOnly = false, onTeleport = false, isRandom = false, firstTime = false;

		public bool OnTeleport { get => onTeleport; set => onTeleport = value; }

		float openCooldown = 0f;

		const float defaultCooldown = 20f, sinkSpeed = 2f;

		Trapdoor linkedTrapdoor = null;

		TextMeshPro timer = null;
		public Trapdoor LinkedTrapDoor => linkedTrapdoor;
		public override string NameForIt => "TrapDoor";

		private AudioManager audMan = null;

		readonly Sprite closedSprite = ContentAssets.GetAsset<Sprite>("trapdoor"), openSprite = ContentAssets.GetAsset<Sprite>("trapdoor_open"), closedSprite2 = ContentAssets.GetAsset<Sprite>("trapdoor_rng");
		readonly SoundObject aud_open = ContentAssets.GetAsset<SoundObject>("trapdoor_open"), aud_shut = ContentAssets.GetAsset<SoundObject>("trapdoor_shut");
	}

	public class BananaTree : PrefabInstance
	{
		private void Start()
		{
			try
			{
				myRoom = ec.TileFromPos(transform.position).room;
				banana = ContentManager.instance.GetItemByEnum(ContentManager.instance.customItemEnums.GetItemByName("Banana"));
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
				Debug.LogWarning("Banana tree failed to get the necessary stuff to work, destroying object...");
				Destroy(gameObject);
			}
		}
		public override void Setup()
		{
			base.Setup();
			CreateSprite(ContentUtilities.DefaultBillBoardMaterial, ContentAssets.GetAsset<Sprite>("bananaTree"));
			rendererSprite.transform.localPosition = Vector3.up * 12f;
			var collider = gameObject.AddComponent<BoxCollider>();
			collider.size = new Vector3(3f, 10f, 3f);
			var obstacle = gameObject.AddComponent<NavMeshObstacle>();
			obstacle.size = new Vector3(3f, 10f, 3f);
		}

		private void OnCollisionEnter(Collision collision)
		{
			if (!myRoom || !banana) return;

			if (collision.gameObject.CompareTag("GrapplingHook"))
			{
				ec.CreateItem(myRoom, banana, new Vector3(transform.position.x + UnityEngine.Random.Range(-dropOffset, dropOffset), 5f, transform.position.z + UnityEngine.Random.Range(-dropOffset, dropOffset)));
			}
		}

		public override string NameForIt => "BananaTree";

		const float dropOffset = 2f;

		RoomController myRoom = null;

		ItemObject banana = null;
	}

	public class BaldiGoesAway : PrefabInstance
	{
		public override string NameForIt => "BaldiGoesAwayLol";

		public override void Setup()
		{
			CreateSprite(ContentUtilities.DefaultBillBoardMaterial, animation[0]);
		}

		public override void Execute()
		{
			base.Execute();
			bye = true;
		}

		private void Update()
		{
			if (!bye) return;

			animationTimer += 25f * ec.EnvironmentTimeScale * Time.deltaTime;
			animationTimer %= animation.Length;
			rendererSprite.sprite = animation[Mathf.FloorToInt(animationTimer)];

			byeSpeed += 0.5f * ec.EnvironmentTimeScale * Time.deltaTime;
			transform.position += Vector3.up * byeSpeed;

			if (transform.position.y > 60f)
				Despawn();
		}

		float animationTimer = 0f;

		float byeSpeed = 0f;

		bool bye = false;

		readonly Sprite[] animation = new Sprite[] { ContentAssets.GetAsset<Sprite>("balDance1"), ContentAssets.GetAsset<Sprite>("balDance2") };
	}

	public class StunningStars : PrefabInstance
	{
		public override string NameForIt => "StunningStarsEffect";

		public override void Setup()
		{
			CreateSprite(ContentUtilities.DefaultBillBoardMaterial, ContentAssets.GetAsset<Sprite>(TextureName));
		}

		public void SetupTarget(Transform target)
		{
			targetParent = target;
			transform.SetParent(target);
		}

		public override void Execute()
		{
			base.Execute();
			if (targetParent == null)
			{
				Despawn();
				return;
			}

			transform.localPosition = new Vector3(0f, Height, 0f);
		}

		Transform targetParent = null;

		protected virtual float Height => 4f;

		protected virtual string TextureName => "StunningStars";
	}

	public class GroundedEffect : StunningStars
	{
		public override string NameForIt => "GroundedEffect";
		protected override string TextureName => "groundedEffect";
		protected override float Height => -2.8f;
	}

	public class ExitSign : PrefabInstance
	{
		public override string NameForIt => "ExitSign";

		public override void Setup()
		{
			var mat = ContentUtilities.DefaultBillBoardMaterial;
			mat.SetTexture(ContentUtilities.BillBoardMaskTextureName, ContentAssets.GetAsset<Texture2D>("exitSign_lightMap"));

			CreateSprite(mat, ContentAssets.GetAsset<Sprite>("exitSign"));
		}
	}

	public class GroundedGlue : PrefabInstance
	{
		public override string NameForIt => "GlueInGround";

		public override void Setup()
		{
			CreateSprite(ContentManager.Prefabs.flatMaterial, ContentAssets.GetAsset<Sprite>("glueInGround"));
			rendererSprite.gameObject.layer = 0;
			var collider = ContentUtilities.AddCollisionToSprite(gameObject, new Vector3(4.5f, 15f, 4.5f), Vector3.zero);
			collider.isTrigger = true;
			
		}

		public void SetOwner(GameObject obj) => myOwner = obj;

		private void OnTriggerStay(Collider other)
		{
			if (!active) return;

			if (other.gameObject != myOwner)
			{
				var comp = other.GetComponent<ActivityModifier>();
				if (comp != null)
				{
					bool immune = other.GetComponent<PlayerManager>()?.plm.addendImmune ?? false;

					if (!modifiers.Contains(comp))
					{
						if (!immune)
						{
							comp.moveMods.Add(movemod);
							modifiers.Add(comp);
							ItemSoundHolder.CreateSoundHolder(transform.position, aud_splash, true, 17, 20, true);
						}
					}
					else if (immune)
					{
						comp.moveMods.Remove(movemod);
						modifiers.Remove(comp);
					}
				}
			}
		}

		private void OnTriggerExit(Collider other)
		{
			var comp = other.GetComponent<ActivityModifier>();
			if (comp != null)
			{
				comp.moveMods.Remove(movemod);
				modifiers.Remove(comp);
			}
		}

		public override void Execute()
		{
			base.Execute();
			ItemSoundHolder.CreateSoundHolder(transform.position, aud_splash, true, 30, 70, true);
			StartCoroutine(SpawnAnimation());
		}

		protected override IEnumerator DespawnWithAnimation()
		{
			ClearUpModifiers();
			Destroy(GetComponent<BoxCollider>()); // Destroys the collider of course
			active = false;
			transform.localScale = Vector3.one;
			float val = 1f;
			float speed = 0f;
			while (transform.localScale.magnitude > 0.1f)
			{
				speed += ec.EnvironmentTimeScale * Time.deltaTime * 2f;
				val += (0f - val) * speed;
				transform.localScale = new Vector3(val, val, val);
				yield return null;
			}
			transform.localScale = Vector3.zero;
			Despawn();
			yield break;
		}

		IEnumerator SpawnAnimation()
		{
			transform.localScale = Vector3.zero;
			float val = 0f;
			while (transform.localScale.magnitude < 1f)
			{
				val += (1f - val) / 4f;
				transform.localScale = new Vector3(val, val, val);
				yield return null;
			}
			transform.localScale = Vector3.one;
			active = true;

			yield break;
		}

		GameObject myOwner;

		readonly MovementModifier movemod = new MovementModifier(Vector3.zero, 0.1f);

		readonly List<ActivityModifier> modifiers = new List<ActivityModifier>();

		readonly SoundObject aud_splash = ContentAssets.GetAsset<SoundObject>("glueSplash");

		void ClearUpModifiers()
		{
			foreach (var mod in modifiers)
			{
				mod.moveMods.Remove(movemod);
			}
			modifiers.Clear();
		}

		bool active = false;
	}

	public class FogMachine : PrefabInstance, IItemAcceptor
	{
		public override string NameForIt => "FogMachine";

		public bool ItemFits(Items item) => currentEvents.Count > 0 & !fixFogEvent && item == acceptedItem;

		public void InsertItem(PlayerManager pm, EnvironmentController ec)
		{
			if (currentEvents.Count > 0 & !fixFogEvent)
			{
				fixFogEvent = true;
				foreach (var rEvent in currentEvents)
				{
					var currentEvent = ec.GetEvent(rEvent);
					AccessTools.Field(typeof(RandomEvent), "remainingTime").SetValue(currentEvent, 0f); // Sets the event time to 0, so it naturally ends without any force
				}
				rendererSprite.sprite = onSprite;
				icon.sprite.enabled = false;
				currentEvents.Clear();
			}
		}

		public override void Setup()
		{
			ContentUtilities.AddCollisionToSprite(gameObject, transform.right * 9f + Vector3.up * 5f, Vector3.zero);
			CreateSprite(ContentManager.Prefabs.NewFlatMaterial, noEvSprite);
			rendererSprite.transform.localScale = new Vector3(0.96f, 1f, 1f);
		}
		private void Update()
		{
			if (ec.CurrentEventTypes.Count > 0)
			{
				if (currentEvents.Count == 0)
				{
					currentEvents.AddRange(ec.CurrentEventTypes);
					rendererSprite.sprite = offSprite;
				}
			}
			else if (currentEvents.Count > 0)
			{
				currentEvents.Clear();
				rendererSprite.sprite = noEvSprite;
			}
		}

		bool fixFogEvent = false;

		readonly Sprite onSprite = ContentAssets.GetAsset<Sprite>("fogMachine_ON");

		readonly Sprite offSprite = ContentAssets.GetAsset<Sprite>("fogMachine_OFF");

		readonly Sprite noEvSprite = ContentAssets.GetAsset<Sprite>("fogMachine_NOEV");

		readonly Items acceptedItem = ContentManager.instance.customItemEnums.GetItemByName("ScrewDriver");

		readonly List<RandomEventType> currentEvents = new List<RandomEventType>();

		public MapIcon icon;
	}

	public class HardHatHud : CustomHud
	{
		public override string NameForIt => "HardHat_Hud";

		public override void Setup()
		{
			var img = gameObject.AddComponent<Image>();
			img.sprite = ContentAssets.GetAsset<Sprite>("HardHatHud");
		}

		public void SetupHud(Canvas canvas)
		{
			transform.SetParent(canvas.transform);
			transform.localPosition = Vector3.zero;
			transform.localScale = new Vector3(3.5f, 3.7f, 3.5f);
		}
	}

	public class StunlyEffect : CustomHud
	{
		public override string NameForIt => "Stunly\'s Stun Hud";

		public override void Setup()
		{
			var img = gameObject.AddComponent<Image>();
			img.color = Color.white;
		}

		public override void Execute()
		{
			base.Execute();

			StartCoroutine(FadeOut());
		}

		public void SetupHud(Canvas canvas)
		{
			transform.SetParent(canvas.transform);
			transform.localPosition = Vector3.zero;
		}


		private IEnumerator FadeOut()
		{
			var img = GetComponent<Image>();
			var alpha = img.color;
			alpha.a = 1f;
			img.color = alpha;

			yield return new WaitForSeconds(1f);

			while (alpha.a > 0f)
			{
				alpha.a -= 0.05f * ec.EnvironmentTimeScale * Time.deltaTime;
				img.color = alpha;
				yield return null;
			}

			Despawn();

			yield break;

		}
	}

	public class PlayerModel : PrefabInstance
	{
		public override string NameForIt => "PlayerModel";

		public override void Setup()
		{
			CreateSprite(ContentUtilities.DefaultBillBoardMaterial, ContentAssets.GetAsset<Sprite>("playerVisual"));
		}

		public override void Execute()
		{
			base.Execute();
			EnvironmentExtraVariables.OnEndGame.AddListener(() => isActive = false); // When the game ends by Baldi, the player model is disabled
		}
		public void SetPlayer(PlayerManager player)
		{
			targetPlayer = player;
		}

		private void Update()
		{
			if (!targetPlayer || !AvailableRender) return;

			rendererSprite.enabled = !targetPlayer.hidden & isActive;
		}

		private PlayerManager targetPlayer;

		bool isActive = true;

	}

	public class GumInWall : PrefabInstance
	{
		public override void Setup()
		{
			CreateSprite(ContentManager.Prefabs.NewFlatMaterial, frontSprite);
		}
		public void SetAsBackObject(GameObject frontalParent)
		{
			rendererSprite.sprite = backSprite;
			frontParent = frontalParent;
		}
		/// <summary>
		/// First argument of <paramref name="args"/> must be a boolean
		/// </summary>
		/// <param name="ec"></param>
		/// <param name="args"></param>
		public override void Execute()
		{
			base.Execute();

			ItemSoundHolder.CreateSoundHolder(transform.position, splashNoise, true, 30, 70, true);
			StartCoroutine(ActiveTime());
		}

		private IEnumerator ActiveTime()
		{
			if (frontParent)
			{
				while (frontParent)
				{
					transform.localScale = frontParent.transform.localScale;
					yield return null;
				}
				Despawn();
				yield break;
			}

			float increaseSpeed = 0.01f;
			float setSize = 0.1f;
			transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
			while (setSize < 1f)
			{
				setSize += increaseSpeed;
				increaseSpeed += 0.05f * ec.EnvironmentTimeScale * Time.deltaTime;
				if (setSize < 1f)
					transform.localScale = new Vector3(setSize, setSize, setSize);

				yield return null;
			}

			transform.localScale = new Vector3(1f, 1f, 1f);
			float time = UnityEngine.Random.Range(10f, 15f);
			while (time > 0f)
			{
				time -= ec.EnvironmentTimeScale * Time.deltaTime;
				yield return null;
			}

			float decreaseSpeed = 0.01f;

			while (transform.localScale.magnitude > 0.1f)
			{
				decreaseSpeed += 0.02f * ec.EnvironmentTimeScale * Time.deltaTime;
				transform.localScale -= new Vector3(decreaseSpeed, decreaseSpeed, decreaseSpeed);
				yield return null;
			}

			Despawn();

			yield break;
		}

		public override string NameForIt => "GumInWall";

		readonly SoundObject splashNoise = ContentAssets.GetAsset<SoundObject>("gumSplash");

		readonly Sprite backSprite = ContentAssets.GetAsset<Sprite>("GumInWall_Back");

		readonly Sprite frontSprite = ContentAssets.GetAsset<Sprite>("GumInWall");

		GameObject frontParent = null;
	}
}
