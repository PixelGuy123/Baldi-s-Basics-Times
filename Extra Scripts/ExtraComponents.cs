using BB_MOD.ExtraItems;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Collections;
using HarmonyLib;

namespace BB_MOD.ExtraComponents
{
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
			var prefab = prefabs.Find(x => x.GetType().Equals(typeof(T)));
			var obj = Instantiate(prefab);
			obj.gameObject.SetActive(true);
			obj.transform.SetParent(ec.transform);
			obj.transform.position = pos;
			obj.transform.rotation = rotation;
			obj.GetComponent<T>().SetReferences(ec, prefab.AvailableRender);
			if (autoExecute)
				obj.GetComponent<T>().Execute();

			return obj.GetComponent<T>();
		}
		public static T SpawnPrefab<T>(Transform pos, EnvironmentController ec, bool autoExecute = true, Vector3 offset = default) where T : PrefabInstance
		{
			var prefab = SpawnPrefab<T>(Vector3.zero, default, ec, autoExecute);
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
				rendererSprite = transform.Find("Sprite").GetComponent<SpriteRenderer>();
		}

		public virtual void Execute()
		{
			gameObject.SetActive(true);
		}

		public virtual void Despawn()
		{
			Destroy(gameObject);
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
			CreateSprite(ContentManager.Prefabs.NewFlatMaterial, offSprite);
		}
		private void Update()
		{
			if (ec.CurrentEventTypes.Count > 0)
			{
				if (currentEvents.Count == 0)
					currentEvents.AddRange(ec.CurrentEventTypes);
			}
			else if (currentEvents.Count > 0)
				currentEvents.Clear();
		}

		bool fixFogEvent = false;

		readonly Sprite onSprite = ContentAssets.GetAsset<Sprite>("fogMachine_ON");

		readonly Sprite offSprite = ContentAssets.GetAsset<Sprite>("fogMachine_OFF");

		readonly Items acceptedItem = ContentManager.instance.customItemEnums.GetItemByName("ScrewDriver");

		readonly List<RandomEventType> currentEvents = new List<RandomEventType>();

		public MapIcon icon;
	}

	public class StunlyEffect : PrefabInstance
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
		public void SetPlayer(PlayerManager player) => targetPlayer = player;

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
