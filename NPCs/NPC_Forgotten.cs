using MTM101BaldAPI;
using MTM101BaldAPI.AssetManager;
using System.Collections;
using System.IO;
using UnityEngine;

namespace BB_MOD.NPCs
{
	// ------ NPC SUMMARY ------
	// If the player gets in the sight of Forgotten, it will play a Loud Noise and it will run 5 seconds to a random destination. If it touches the player, it will alert Baldi with a loud noise and move the player to a random destination with it. After it moves the player, it cannot move the player again until you lose or beat the Floor.
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

			aud_ForgottenWarning = ObjectCreatorHandlers.CreateSoundObject(AssetManager.AudioClipFromFile(Path.Combine(ContentManager.modPath, "Audio", "npc", "ForgottenWarning.wav")), "Vfx_Forgotten_Warning", SoundType.Voice, new Color(43,42,51)); // Creates audioClip
		}

        private void Update()
        {
			if (!controlOverride && !navigator.HasDestination)
			{
				WanderRandom();
			}

			if (isMoving == true)
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
					if (canMoveAgain == true)
					{
						isMoving = true;
					}
					canMoveAgain = false;
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
			isMoving = false;
			yield break;
		}

		public AudioManager audMan;

        public SoundObject aud_ForgottenWarning;

		private PlayerManager movingPlayer = null;
		public bool isMoving = false;
		public bool canMoveAgain = true;

		[SerializeField]
        private const float normalSpeed = 1f;
    }
}

