﻿using MTM101BaldAPI;
using System.IO;
using UnityEngine;

namespace BB_MOD.Extra
{
	public class Vent : MonoBehaviour
	{
		private void Awake()
		{
			audMan = GetComponent<AudioManager>();
			Destroy(GetComponent<BoxCollider>());
			aud_ventNoise = ObjectCreatorHandlers.CreateSoundObject(ContentUtilities.GetAudioClip(Path.Combine(ContentManager.modPath, "Audio", "ventNoise.wav")), "Vfx_VentNoise", SoundType.Effect, Color.white);

			audMan.QueueAudio(aud_ventNoise);
			audMan.SetLoop(true);
		}

		private AudioManager audMan;
		private SoundObject aud_ventNoise;
	}
}
