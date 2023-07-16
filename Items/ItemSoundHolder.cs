using UnityEngine;
using System.Collections;

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
		public static GameObject CreateSoundHolder(Transform position)
		{
			var obj = new GameObject("SoundHolder", typeof(ItemSoundHolder), typeof(AudioManager), typeof(AudioSource));
			obj.transform.SetParent(position);
			obj.AddComponent<ItemSoundHolder>();
			obj.GetComponent<AudioManager>().audioDevice = obj.GetComponent<AudioSource>();
			return obj;
		}

		/// <summary>
		/// Creates an audio holder with <paramref name="position"/> as parent and automatically plays selected <paramref name="sound"/> with set <paramref name="maxDistance"/>
		/// </summary>
		/// <param name="position"></param>
		/// <param name="sound"></param>

		public static void CreateSoundHolder(Transform position, SoundObject sound, float maxDistance = 30f)
		{
			var obj = CreateSoundHolder(position);
			obj.SetActive(true);
			obj.GetComponent<ItemSoundHolder>().enabled = true;
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

			Destroy(this);

			yield break;
		}
	}
}
