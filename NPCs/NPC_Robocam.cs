using MTM101BaldAPI;
using MTM101BaldAPI.AssetManager;
using System.Collections;
using System.IO;
using UnityEngine;

namespace BB_MOD.NPCs
{
	// ------ NPC SUMMARY ------
	// If you look at Robocam, you will see in Robocam eyes unless you have a nametag. (The camera will switch to it's camera)
	// Pros: Using it's camera, you can see things that are out of you're vision normally.
	// Cons: You cannot see your stamina and items while the camera is switched and it can give bad situations.

	public class Robocam : NPC
    {
        private void Start() 
        {
            navigator.maxSpeed = normalSpeed;
            navigator.SetSpeed(normalSpeed);

			// Audio Setup

			gameObject.AddComponent<Camera>();
			robocamCamera = GetComponent<Camera>();
			robocamCamera.enabled = false;
		}

        private void Update()
        {
			if (!controlOverride && !navigator.HasDestination)
			{
				WanderRandom();
			}
		}

		public override void DestinationEmpty()
		{
			base.DestinationEmpty();
			if (!controlOverride && !returningFromDetour)
			{
				WanderRandom();
			}
		}

		public override void PlayerInSight(PlayerManager player)
		{
			if (!player.Tagged)
			{
				robocamCamera.enabled = true;
				StartCoroutine(Cooldown(0.1f));
			}
		}

		private IEnumerator Cooldown(float val)
		{
			float cooldown = val;
			while (cooldown > 0f)
			{
				cooldown -= Time.deltaTime * ec.NpcTimeScale;
				yield return null;
			}
			while (looker.IsVisible)
			{
				yield return null;
			}
			robocamCamera.enabled = false;
			yield break;
		}

		private Camera robocamCamera;

		[SerializeField]
        private const float normalSpeed = 12f;
    }
}

