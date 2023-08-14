using MTM101BaldAPI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BB_MOD.NPCs
{

	// ------ NPC SUMMARY ------
	// Sweeps the school with his super sweeping powers
	
    public class ZeroPrize : NPC
    {

		

        private void Start() 
        {
            navigator.maxSpeed = normalSpeed;
			navigator.SetSpeed(0f);

			// Audio Setup

			audMan = GetComponent<AudioManager>();
			audMan.audioDevice.dopplerLevel = 1f;
			audMan.audioDevice.minDistance = 20f;
			audMan.audioDevice.maxDistance = 200f;

			renderer = GetComponent<CustomNPCData>().spriteObject;


			aud_MustSweep = ObjectCreatorHandlers.CreateSoundObject(ContentAssets.GetAsset<AudioClip>("0prize_mustsweep"), "Vfx_0TH_Sweep", SoundType.Voice, new Color(0.8679f, 0.7536f, 0.434f)); // Creates audioClip

			aud_TimeToSweep = ObjectCreatorHandlers.CreateSoundObject(ContentAssets.GetAsset<AudioClip>("0prize_timetosweep"), "Vfx_0TH_WannaSweep", SoundType.Voice, new Color(0.8679f, 0.7536f, 0.434f)); // Creates audioClip

			home = transform.position;
			homeTile = ec.TileFromPos(home);

			// Note: First sprite is sleep, second sprite is not sleep :)

			StartCoroutine(DelayTimer());

		}



		private void Update()
		{
			if (!navigator.HasDestination)
			{
				if (!controlOverride && !readyToBeInactive) 
				{
					if (active) WanderRandom();

					else if (!timerActive)
					{
						StartCoroutine(DelayTimer());
					}
				}
				else if (active)
				{
					renderer.sprite = GetComponent<CustomNPCData>().sprites[1];
				}
			}

			moveMod.movementAddend = navigator.Velocity / Time.deltaTime * 0.95f;
			moveMod.forceTrigger = active;
			moveMod.movementMultiplier = active && !controlOverride ? 0f : 0.9f;

		}

		public override void DestinationEmpty()
		{
			base.DestinationEmpty();
			if (!controlOverride && !returningFromDetour)
			{
				if (active && !readyToBeInactive) WanderRandom();
				else if (readyToBeInactive && ReferenceEquals(homeTile, ec.TileFromPos(transform.position)))
				{
					readyToBeInactive = false;
					active = false;
				}
				else if (!timerActive) StartCoroutine(DelayTimer());
			}
		}
		

		private void OnTriggerEnter(Collider other) // Most scripts from Gotta Sweep, but few changes
		{
			if (active && (other.tag == "Player" || (other.tag == "NPC" && other.isTrigger)))
			{
				audMan.PlaySingle(aud_MustSweep);
				ActivityModifier component = other.GetComponent<ActivityModifier>();
				if (!component.moveMods.Contains(moveMod))
				{
					component.moveMods.Add(moveMod);
					actMods.Add(component);
				}
			}
		}

		private void OnTriggerExit(Collider other)
		{
			if (other.tag == "Player" || (other.tag == "NPC" && other.isTrigger))
			{
				ActivityModifier component = other.GetComponent<ActivityModifier>();
				component.moveMods.Remove(moveMod);
				actMods.Remove(component);
			}
		}

		private IEnumerator DelayTimer()
		{
			Sprite[] sprites = GetComponent<CustomNPCData>().sprites;
			renderer.sprite = sprites[0]; // sleep sprite
			timerActive = true;
			delayTime = Random.Range(minDelay, maxDelay);
			while (delayTime > 0f)
			{
				if (!controlOverride)
				{
					delayTime -= Time.deltaTime * ec.NpcTimeScale;
				}
				yield return null;
			}
			float animationTime = 0f;
			float limit = Random.Range(4f, 9f);
			while (animationTime < limit)
			{
				animationTime += Time.deltaTime * ec.NpcTimeScale * 1.5f;
				renderer.sprite = sprites[Mathf.FloorToInt(animationTime) % 2];
				yield return null;
			}

			renderer.sprite = sprites[1];

			active = true;
			navigator.SetSpeed(normalSpeed);
			WanderRandom();
			audMan.PlaySingle(aud_TimeToSweep);
			StartCoroutine(ActiveTimer());
			timerActive = false;
			yield break;
		}

		private IEnumerator ActiveTimer()
		{
			activeTime = Random.Range(minActive, maxActive);
			while (activeTime > 0f)
			{
				if (!controlOverride)
				{
					activeTime -= Time.deltaTime * ec.NpcTimeScale;
				}
				yield return null;
			}
			readyToBeInactive = true;
			TargetPosition(home);
			yield break;
		}

		public override void Despawn()
		{
			foreach (ActivityModifier activityModifier in actMods)
			{
				activityModifier.moveMods.Remove(moveMod);
			}
			base.Despawn();
		}


		private AudioManager audMan;

        private SoundObject aud_MustSweep;

		private SoundObject aud_TimeToSweep;

		private readonly MovementModifier moveMod = new MovementModifier(Vector3.zero, 0f);

		private SpriteRenderer renderer;

		private Vector3 home;

		private TileController homeTile;

		const int minDelay = 65, maxDelay = 125, minActive = 50, maxActive = 120;

		float activeTime, delayTime;

		private readonly List<ActivityModifier> actMods = new List<ActivityModifier>();

		private bool active = false;

		bool readyToBeInactive = false;

		private bool timerActive = false;

		[SerializeField]
        private const float normalSpeed = 80f;

		
    }

}

