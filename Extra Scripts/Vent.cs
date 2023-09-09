using UnityEngine;

namespace BB_MOD.ExtraComponents
{
	public class Vent : MonoBehaviour
	{
		private void Awake()
		{
			audMan = GetComponent<AudioManager>();
			Destroy(GetComponent<BoxCollider>());
			aud_ventNoise = ContentAssets.GetAsset<SoundObject>("ventNoises");

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
