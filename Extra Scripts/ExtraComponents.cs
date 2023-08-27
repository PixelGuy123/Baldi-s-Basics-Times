using System;
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
			renderer = transform.Find("Sprite").GetComponent<SpriteRenderer>();
			renderer.material.SetTexture("_LightMap", null);
			spriteAnimation = new Sprite[] { ContentAssets.GetAsset<Sprite>("SchoolFire_FirstFrame"), ContentAssets.GetAsset<Sprite>("SchoolFire_SecondFrame") };
			ec = EnvironmentExtraVariables.ec;
		}

		private void Update()
		{
			animationTiming += 1.8f * Time.deltaTime * ec.EnvironmentTimeScale;
			animationTiming %= 2f;
			renderer.sprite = spriteAnimation[Mathf.FloorToInt(animationTiming)];
		}

		private SpriteRenderer renderer;

		private Sprite[] spriteAnimation = new Sprite[0];

		private EnvironmentController ec;

		private float animationTiming = UnityEngine.Random.Range(0f, 2f); // Not make the fires synchronized
	}
}
