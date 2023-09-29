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

			if (disabledForever) return;

			audMan.QueueAudio(aud_ventNoise);
			audMan.SetLoop(true);
		}

		public void TurnVent(bool turn, bool perma = false)
		{
			if (turn && disabledForever)
				return;

			if (!turn)
			{
				disabledForever = perma;
				audMan?.FlushQueue(true);
			}
			else
				audMan?.QueueAudio(aud_ventNoise);

			audMan?.SetLoop(turn);
		}

		bool disabledForever = false;

		private AudioManager audMan;
		private SoundObject aud_ventNoise;
	}
}
