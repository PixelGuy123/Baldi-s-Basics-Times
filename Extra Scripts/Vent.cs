using MTM101BaldAPI;
using UnityEngine;

namespace BB_MOD.Extra
{
	public class Vent : MonoBehaviour
	{
		private void Awake()
		{
			audMan = GetComponent<AudioManager>();
			Destroy(GetComponent<BoxCollider>());
			aud_ventNoise = ObjectCreatorHandlers.CreateSoundObject(ContentAssets.GetAsset<AudioClip>("ventNoises"), "Vfx_VentNoise", SoundType.Effect, Color.white);

			audMan.QueueAudio(aud_ventNoise);
			audMan.SetLoop(true);
		}

		public void TurnVent(bool turn)
		{
			if (!turn)
				audMan.FlushQueue(true);
			else
				audMan.QueueAudio(aud_ventNoise);

			audMan.SetLoop(turn);
		}

		private AudioManager audMan;
		private SoundObject aud_ventNoise;
	}
}
