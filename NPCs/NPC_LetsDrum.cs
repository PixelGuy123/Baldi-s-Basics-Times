using MTM101BaldAPI;
using MTM101BaldAPI.AssetManager;
using System.Collections;
using System.IO;
using UnityEngine;

namespace BB_MOD.NPCs
{

	// ------ NPC SUMMARY ------
	// If he announces he is gonna drum and player is nearby, well.... heh


	// purple color for audioclips: 64, 0, 128

	public class LetsDrum : NPC
	{



		private void Start()
		{
			navigator.maxSpeed = normalSpeed;
			navigator.SetSpeed(normalSpeed);

			// Audio Setup

			audMan = GetComponent<AudioManager>();
			audMan.audioDevice.maxDistance = 135f;
			audMan.audioDevice.minDistance = 15f;

			ContentUtilities.CreateMusicManager(gameObject, 15f, 100f, ObjectCreatorHandlers.CreateSoundObject(ContentAssets.GetAsset<AudioClip>("letsdrum_music"), "Vfx_DRUM_Music", SoundType.Voice, new Color(64, 0, 128)));

			aud_LetsDrum = ObjectCreatorHandlers.CreateSoundObject(ContentAssets.GetAsset<AudioClip>("letsdrum_wannadrum"), "Vfx_DRUM_LetsDrum", SoundType.Voice, new Color(64, 0, 128));
			aud_Drumming = ObjectCreatorHandlers.CreateSoundObject(ContentAssets.GetAsset<AudioClip>("letsdrum_DRUM"), "Vfx_DRUM_Annoyence", SoundType.Voice, new Color(64, 0, 128));

			looker.distance = 35f;

		}



		private void Update()
		{
			if (!controlOverride && !navigator.HasDestination)
			{
				WanderRandom();
			}

			if (hasWantedToDrum && !isDrumming)
			{
				hasWantedToDrum = false;
				StartCoroutine(WannaDrumCooldown());
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

		private IEnumerator DrummingCooldown()
		{
			hudAudio = new GameObject("DrummingAnnoyence", typeof(AudioSource), typeof(AudioManager));
			var hudMan = hudAudio.GetComponent<AudioManager>();
			hudMan.audioDevice = hudAudio.GetComponent<AudioSource>();
			hudMan.audioDevice.maxDistance = 100f;


			hudMan.QueueAudio(aud_Drumming);
			hudMan.SetLoop(true);

			EnvironmentExtraVariables.TurnSubtitles(false);


			float time = 20f;
			while (time > 0f)
			{

				time -= Time.deltaTime * ec.NpcTimeScale;
				yield return null;
			}

			hudMan.FlushQueue(true);
			Destroy(hudAudio);

			hudAudio = null;

			isDrumming = false;

			EnvironmentExtraVariables.TurnSubtitles(true);


			yield break;
		}

		private IEnumerator WannaDrumCooldown()
		{
			float time = Random.Range(minDrumCooldown, maxDrumCooldown);
			while (time > 0f)
			{
				time -= Time.deltaTime * ec.NpcTimeScale;
				yield return null;
			}
			audMan.QueueAudio(aud_LetsDrum);

			while (audMan.IsPlaying) { yield return null; } // Waits until it ends

			if (looker.PlayerInSight)
			{
				StartCoroutine(DrummingCooldown());
				isDrumming = true;
			}

			hasWantedToDrum = true;

			yield break;
		}


		private AudioManager audMan;

		private GameObject hudAudio = null;

		private SoundObject aud_LetsDrum;

		private SoundObject aud_Drumming;

		private bool isDrumming = false, hasWantedToDrum = true;

		[SerializeField]
		private const float normalSpeed = ContentUtilities.PlayerDefaultWalkSpeed, minDrumCooldown = 30f, maxDrumCooldown = 60f;


	}

}

