using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

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
			var obj = CreateSoundHolder(position.position, positionalAudio, maxDistance, supportDuppler);
			obj.transform.position = default;
			obj.transform.SetParent(position);
			obj.name = "SoundHolder_" + position.name;
			return obj;
		}

		/// <summary>
		/// Creates an audio holder set in a <paramref name="position"/>
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public static GameObject CreateSoundHolder(Vector3 position, bool positionalAudio, float maxDistance, bool supportDuppler = false)
		{
			var obj = new GameObject("Generic_SoundHolder", typeof(ItemSoundHolder), typeof(AudioManager));
			obj.transform.position = position;

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
		/// Creates an audio holder with <paramref name="position"/> as parent and automatically plays selected <paramref name="sound"/> with set <paramref name="maxDistance"/>, it also has an option to <paramref name="destructiveParent"/> after playing, which means, destruct the object that is holding the Sound Holder
		/// </summary>
		/// <param name="position"></param>
		/// <param name="sound"></param>

		public static void CreateSoundHolder(Transform position, SoundObject sound, bool positionalAudio, float maxDistance = 30f, bool supportDuppler = false, Transform destructiveParent = null)
		{
			var obj = CreateSoundHolder(position, positionalAudio, maxDistance, supportDuppler);
			obj.GetComponent<ItemSoundHolder>().StartCoroutine(obj.GetComponent<ItemSoundHolder>().PlaySoundAndDespawn(sound, obj.GetComponent<AudioManager>(), maxDistance, destructiveParent));
		}

		/// <summary>
		/// Creates an audio holder in a <paramref name="position"/> and automatically plays selected <paramref name="sound"/> with set <paramref name="maxDistance"/>, it also has an option to <paramref name="destructiveParent"/> after playing, which means, destruct the object that is holding the Sound Holder
		/// </summary>
		/// <param name="position"></param>
		/// <param name="sound"></param>
		public static void CreatePositionalSoundHolder(Transform position, SoundObject sound, bool positionalAudio, float maxDistance = 30f, bool supportDuppler = false, Transform destructiveParent = null)
		{
			var obj = CreateSoundHolder(position.transform.position, positionalAudio, maxDistance, supportDuppler);
			obj.GetComponent<ItemSoundHolder>().StartCoroutine(obj.GetComponent<ItemSoundHolder>().PlaySoundAndDespawn(sound, obj.GetComponent<AudioManager>(), maxDistance, destructiveParent));
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
