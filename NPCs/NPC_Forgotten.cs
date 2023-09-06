using MTM101BaldAPI;
using System.Collections;
using UnityEngine;

namespace BB_MOD.NPCs
{
	// ------ NPC SUMMARY ------
	// If the player gets in sight of Forgotten, it will run for 5 seconds to a random destination. If it touches the player, it will alert Baldi with a loud noise and move the player to a random destination for 5 seconds with it. After it moves the player, it cannot move the player again until you lose or beat the Floor, but it will still play the sound and alert Baldi.
	// Pros: Can move the player out of a dangerous situation if needed.
	// Cons: It will alert Baldi anyways.
	
    public class Forgotten : NPC
    {
        private void Start() 
        {
            navigator.maxSpeed = normalSpeed;
            navigator.SetSpeed(normalSpeed);
			navigator.accel = 50f; // Acceleration

			// Audio Setup

			audMan = GetComponent<AudioManager>();

			aud_ForgottenWarning = ContentAssets.GetAsset<SoundObject>("forgotten_warn"); // Creates audioClip
		}

        private void Update()
        {
			if (!controlOverride && !navigator.HasDestination)
			{
				WanderRandom();
			}

			if (isMoving)
			{
				movingPlayer.transform.position = gameObject.transform.position;
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
				navigator.maxSpeed = 50f;
				StartCoroutine(Cooldown(5f));
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			if (other.tag == "Player")
			{
				PlayerManager player = other.GetComponent<PlayerManager>();
				if (!player.tagged && !player.invincible)
				{
					audMan.PlaySingle(aud_ForgottenWarning);
					ec.MakeNoise(transform.position, 70); // Alert Baldi
					movingPlayer = player;
					if (canMoveAgain)
					{
						isMoving = true;
					}
					canMoveAgain = false;
					StopAllCoroutines();
					StartCoroutine(MovingCooldown(5f));
				}
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
			navigator.maxSpeed = normalSpeed;
			yield break;
		}

		private IEnumerator MovingCooldown(float val)
		{
			float cooldown = val;
			while (cooldown > 0f)
			{
				cooldown -= Time.deltaTime * ec.NpcTimeScale;
				yield return null;
			}
			while (looker.IsVisible && !isMoving)
			{
				yield return null;
			}
			navigator.maxSpeed = normalSpeed;
			isMoving = false;
			yield break;
		}

		private AudioManager audMan;

		private SoundObject aud_ForgottenWarning;

		private PlayerManager movingPlayer = null;

		private bool isMoving = false;

		private bool canMoveAgain = true;

		[SerializeField]
        private const float normalSpeed = 1f;
    }
}

