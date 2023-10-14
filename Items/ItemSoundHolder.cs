using System.Collections;
using UnityEngine;

namespace BB_MOD.ExtraItems
{
	// Basically the custom items can't have audiomanagers, so this gameobject will do the job


	public class ItemSoundHolder : MonoBehaviour
	{
		private static GameObject Internal_CreateSoundHolder(Transform position, bool positionalAudio, float minDistance, float maxDistance, bool supportDuppler = false)
		{
			var obj = Internal_CreateSoundHolder(position.position, positionalAudio, minDistance, maxDistance, supportDuppler);
			obj.transform.SetParent(position);
			obj.name = "SoundHolder_" + position.name;
			return obj;
		}
		private static GameObject Internal_CreateSoundHolder(Vector3 position, bool positionalAudio, float minDistance, float maxDistance, bool supportDuppler = false)
		{
			var obj = new GameObject("Generic_SoundHolder", typeof(ItemSoundHolder), typeof(AudioManager));
			obj.transform.position = position;

			if (positionalAudio)
				ContentUtilities.CreatePositionalAudio(obj, minDistance, maxDistance, supportDuppler);
			else
				obj.AddComponent<AudioSource>();
			

			obj.AddComponent<ItemSoundHolder>();
			obj.GetComponent<AudioManager>().audioDevice = obj.GetComponent<AudioSource>();
			obj.GetComponent<AudioManager>().positional = positionalAudio;
			obj.GetComponent<ItemSoundHolder>().enabled = true;
			obj.SetActive(true);
			return obj;
		}

		/// <summary>
		/// Creates an audio holder with <paramref name="position"/> as parent and automatically plays selected <paramref name="sound"/> with set <paramref name="maxDistance"/>, it also has an option to <paramref name="destructiveParent"/> after playing, which means, destruct the object that is holding the Sound Holder
		/// </summary>
		/// <param name="position"></param>
		/// <param name="sound"></param>

		public static ItemSoundHolder CreateSoundHolder(Transform position, SoundObject sound, bool positionalAudio, float minDistance = 30f, float maxDistance = 30f, bool supportDuppler = false, bool destructiveParent = false)
		{
			var obj = Internal_CreateSoundHolder(position, positionalAudio, minDistance, maxDistance, supportDuppler);
			obj.GetComponent<ItemSoundHolder>().StartCoroutine(obj.GetComponent<ItemSoundHolder>().PlaySoundAndDespawn(sound, obj.GetComponent<AudioManager>(), maxDistance, destructiveParent ? position : null));
			return obj.GetComponent<ItemSoundHolder>();
		}

		/// <summary>
		/// Creates an audio holder in a <paramref name="position"/> and automatically plays selected <paramref name="sound"/> with set <paramref name="maxDistance"/>, it also has an option to <paramref name="destructiveParent"/> after playing, which means, destruct the object that is holding the Sound Holder
		/// </summary>
		/// <param name="position"></param>
		/// <param name="sound"></param>
		public static ItemSoundHolder CreateSoundHolder(Vector3 position, SoundObject sound, bool positionalAudio, float minDistance = 30f, float maxDistance = 30f, bool supportDuppler = false, Transform destructiveParent = null)
		{
			var obj = Internal_CreateSoundHolder(position, positionalAudio, minDistance, maxDistance, supportDuppler);
			obj.GetComponent<ItemSoundHolder>().StartCoroutine(obj.GetComponent<ItemSoundHolder>().PlaySoundAndDespawn(sound, obj.GetComponent<AudioManager>(), maxDistance, destructiveParent));
			return obj.GetComponent<ItemSoundHolder>();
		}

		private IEnumerator PlaySoundAndDespawn(SoundObject sound, AudioManager audMan, float maxDistance, Transform parent = null)
		{
			audMan.audioDevice.maxDistance = maxDistance;
			audMan.PlaySingle(sound);
			device = audMan;
			while (audMan.IsPlaying)
			{
				yield return null;
			}

			if (parent) 
				Destroy(parent.gameObject);

			Destroy(gameObject);

			yield break;
		}

		private AudioManager device;

		public bool IsPlaying { get => device ? device.IsPlaying : false; }
	}
}
