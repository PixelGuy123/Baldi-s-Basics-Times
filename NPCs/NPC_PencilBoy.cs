using BB_MOD;
using MTM101BaldAPI;
using System.Collections;
using UnityEngine;

namespace BB_MOD.NPCs
{
	// ------ NPC SUMMARY ------
	// Takes out your stamina lmfaofafalfamfafafa

	public class PencilBoy : NPC
	{
		private void Start()
		{
			navigator.maxSpeed = normalSpeed;
			navigator.SetSpeed(normalSpeed);

			// Audio Setup

			audMan = GetComponent<AudioManager>();
			audMan.audioDevice.maxDistance = 110f;
			audMan.audioDevice.minDistance = 25f;

			looker.distance = 60f;

			for (int i = 0; i < 3; i++)
			{
				aud_wandering[i] = ContentAssets.GetAsset<SoundObject>($"pb_wander{i + 1}");
			}
			aud_spot = ContentAssets.GetAsset<SoundObject>("pb_spot");
			aud_evilLaugh = ContentAssets.GetAsset<SoundObject>("pb_catch");
			aud_stab = ContentAssets.GetAsset<SoundObject>("pb_stab");
			renderer = GetComponent<CustomNPCData>().spriteObject;
			sprites = GetComponent<CustomNPCData>().sprites;
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
				if (Random.value <= 0.05f)
				{
					audMan.PlayRandomAudio(aud_wandering);
				}
			}
		}

		public override void PlayerInSight(PlayerManager player)
		{
			if (isAngry && !player.Tagged)
			{
				if (!controlOverride)
				{
					TargetPlayer(player.transform.position);
					navigator.maxSpeed = runSpeed;
					navigator.SetSpeed(runSpeed);
				}
				aggroed = true;
				return;
			}
			if (aggroed && player.Tagged)
			{
				Directions.ReverseList(navigator.currentDirs);
				WanderRandom();
				navigator.maxSpeed = normalSpeed;
				navigator.SetSpeed(normalSpeed);
				navigator.ClearDestination();
				aggroed = false;
			}
		}

		public override void PlayerSighted(PlayerManager player)
		{
			if (isAngry && !player.Tagged)
			{
				audMan.PlaySingle(aud_spot);
				renderer.sprite = sprites[1];
			}
		}

		public override void PlayerLost(PlayerManager player)
		{
			if (isAngry && !controlOverride)
			{
				audMan.PlayRandomAudio(aud_wandering);
				Directions.ReverseList(navigator.currentDirs);
				WanderRandom();
				navigator.maxSpeed = normalSpeed;
				navigator.SetSpeed(normalSpeed);
				navigator.ClearDestination();
				renderer.sprite = sprites[0];
				aggroed = false;
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			if (isAngry && other.tag == "Player")
			{
				PlayerManager component = other.GetComponent<PlayerManager>();
				if (!component.Tagged)
				{
					component.plm.stamina -= angryStaminaDrop;
					if (component.plm.stamina < 0f) component.plm.stamina = 0f;
					audMan.PlaySingle(aud_evilLaugh);
					audMan.PlaySingle(aud_stab);
					navigator.maxSpeed = normalSpeed;
					navigator.SetSpeed(normalSpeed);
					SetGuilt(2f, "stabbing");
					StartCoroutine(Cooldown());
				}
			}
		}

		private IEnumerator Cooldown()
		{
			renderer.sprite = sprites[2];
			isAngry = false;
			cooldown = Random.Range(minCool, maxCool);
			while (cooldown > 0f)
			{
				cooldown -= ec.NpcTimeScale * Time.deltaTime;
				yield return null;
			}
			isAngry = true;
			renderer.sprite = sprites[0];
			yield break;
		}

		private AudioManager audMan;

		private SoundObject[] aud_wandering = new SoundObject[3];

		private SpriteRenderer renderer;

		private Sprite[] sprites = new Sprite[0];

		private SoundObject aud_spot;

		private SoundObject aud_stab;

		private SoundObject aud_evilLaugh;

		private float cooldown;

		private bool isAngry = true;

		private float angryStaminaDrop = 40f;

		[SerializeField]
		private const float normalSpeed = ContentUtilities.PlayerDefaultWalkSpeed, runSpeed = ContentUtilities.PlayerDefaultRunSpeed + 2f, minCool = 20f, maxCool = 40f;
	}
}
