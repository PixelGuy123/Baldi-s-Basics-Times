using BB_MOD.ExtraItems;
using HarmonyLib;
using MTM101BaldAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

		private Func<Items, StandardDoor, bool> itemFitFunc = new Func<Items, StandardDoor, bool>((_, _2) => true);
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
			AccessTools.Field(typeof(Window), "broken").SetValue(window, false);
			window.Shut();
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
			obj.name = component.NameForIt();
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
		public static T SpawnPrefab<T>(TileController tile, EnvironmentController ec, bool autoExecute = true) where T : PrefabInstance => SpawnPrefab<T>(tile.transform.position + Vector3.up * 5f, default, ec, autoExecute);

		public static T SpawnPrefab<T>(TileController tile, Quaternion rotation, EnvironmentController ec, bool autoExecute = true) where T : PrefabInstance => SpawnPrefab<T>(tile.transform.position + Vector3.up * 5f, rotation, ec, autoExecute);

		public virtual void Setup()
		{
		}

		public abstract string NameForIt();

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
		
		protected void CreateSprite(Material mat = null, Sprite sprite = null)
		{
			if (AvailableRender)
			{
				throw new InvalidOperationException($"[{name}] You can't add more than two sprite objects into the same prefab");
			}

			var obj = new GameObject("Sprite");
			obj.transform.SetParent(transform);
			obj.layer = ContentUtilities.defaultBillboardLayer;
			rendererSprite = obj.AddComponent<SpriteRenderer>();
			if (mat != null)
				rendererSprite.material = mat;
			if (sprite != null)
				rendererSprite.sprite = sprite;
		}
		

		protected EnvironmentController ec;

		protected SpriteRenderer rendererSprite = null;

		protected bool AvailableRender => rendererSprite != null;

		private readonly static List<PrefabInstance> prefabs = new List<PrefabInstance>();
	}

	public class GumInWall : PrefabInstance
	{
		public override void Setup()
		{
			CreateSprite(ContentManager.Prefabs.newFlatMaterial, frontSprite);
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
				Destroy(gameObject);
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

			Destroy(gameObject);

			yield break;
		}

		public override string NameForIt() => "CustomObj_GumInWall";

		readonly SoundObject splashNoise = ObjectCreatorHandlers.CreateSoundObject(ContentAssets.GetAsset<AudioClip>("gumSplash"), "Vfx_GumSplash", SoundType.Effect, new Color(255f, 0f, 255f));

		readonly Sprite backSprite = ContentAssets.GetAsset<Sprite>("GumInWall_Back");

		readonly Sprite frontSprite = ContentAssets.GetAsset<Sprite>("GumInWall");

		GameObject frontParent = null;
	}
}
