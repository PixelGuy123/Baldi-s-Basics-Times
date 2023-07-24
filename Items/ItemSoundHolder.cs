using System.Collections;
using UnityEngine;

namespace BB_MOD.ExtraItems
{
	// Basically the custom items can't have audiomanagers, so this gameobject will do the job


	public class ItemSoundHolder : MonoBehaviour
	{
		/// <summary>
		/// Creates an audio holder with <paramref name="position"/> as parent
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public static GameObject CreateSoundHolder(Transform position, bool positionalAudio, float maxDistance, bool supportDuppler = false)
		{
			var obj = new GameObject("SoundHolder", typeof(ItemSoundHolder), typeof(AudioManager));
			obj.transform.SetParent(position);

			if (positionalAudio)
				ContentUtilities.CreatePositionalAudio(obj, 1f, maxDistance, supportDuppler);
			else
				obj.AddComponent<AudioSource>();

			obj.AddComponent<ItemSoundHolder>();
			obj.GetComponent<AudioManager>().audioDevice = obj.GetComponent<AudioSource>();
			obj.GetComponent<ItemSoundHolder>().enabled = true;
			obj.SetActive(true);
			return obj;
		}

		/// <summary>
		/// Creates an audio holder with <paramref name="position"/> as parent and automatically plays selected <paramref name="sound"/> with set <paramref name="maxDistance"/>
		/// </summary>
		/// <param name="position"></param>
		/// <param name="sound"></param>

		public static void CreateSoundHolder(Transform position, SoundObject sound, bool positionalAudio, float maxDistance = 30f, bool supportDuppler = false)
		{
			var obj = CreateSoundHolder(position, positionalAudio, maxDistance, supportDuppler);
			obj.GetComponent<ItemSoundHolder>().StartCoroutine(obj.GetComponent<ItemSoundHolder>().PlaySoundAndDespawn(sound, obj.GetComponent<AudioManager>(), maxDistance));
		}
		private IEnumerator PlaySoundAndDespawn(SoundObject sound, AudioManager audMan, float maxDistance)
		{
			audMan.audioDevice.maxDistance = maxDistance;
			audMan.PlaySingle(sound);
			while (audMan.IsPlaying)
			{
				yield return null;
			}

			Destroy(gameObject);

			yield break;
		}
	}
}
