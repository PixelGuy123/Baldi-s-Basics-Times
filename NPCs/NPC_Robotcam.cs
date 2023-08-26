using System.Collections;
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

			
			robocamCamera = gameObject.AddComponent<Camera>();
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
			if (Singleton<CoreGameManager>.Instance.Paused)
			{
				robocamCamera.enabled = false;
				return;
			}
			if (!player.Tagged && looker.IsVisible)
			{
				robocamCamera.enabled = true;
			}
			else robocamCamera.enabled = false;
		}

		public override void PlayerLost(PlayerManager player)
		{
			robocamCamera.enabled = false;
		}

		public override void Despawn()
		{
			robocamCamera.enabled = false;
			base.Despawn();
		}

		private Camera robocamCamera;

		[SerializeField]
		private const float normalSpeed = 12f;
	}
}
