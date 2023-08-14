using MTM101BaldAPI;
using MTM101BaldAPI.AssetManager;
using System.Collections;
using System.IO;
using UnityEngine;

namespace BB_MOD.NPCs
{

	// ------ NPC SUMMARY ------
	// If player touches this, it gives you a random item
	// Pros: Gives an item
	// Cons: Doesn't work with faculty nametag on
	
    public class HappyHolidays : NPC
    {

		

        private void Start() 
        {
            navigator.maxSpeed = normalSpeed;
            navigator.SetSpeed(normalSpeed);

			// Audio Setup

			audMan = GetComponent<AudioManager>();
			
			aud_MerryChristmas = ObjectCreatorHandlers.CreateSoundObject(ContentAssets.GetAsset<AudioClip>("HPH_holiday"), "Vfx_HapH_MerryChristmas", SoundType.Voice, new Color(153,0,0)); // Creates audioClip

		}

		

        private void Update()
        {
			if (!controlOverride && !disabled && !navigator.HasDestination)
			{
				WanderRandom();
				
			}
			
        }

		public override void DestinationEmpty()
		{
			base.DestinationEmpty();
			if (!controlOverride && !returningFromDetour && !disabled)
			{
				WanderRandom();
			}
		}
		

		private void OnTriggerEnter(Collider other)
		{
			if (other.tag == "Player" && !disabled)
			{
				PlayerManager player = other.GetComponent<PlayerManager>();
				if (!player.tagged && !player.invincible)
				{
					navigator.maxSpeed = 0f;
					player.itm.AddItem(ContentManager.instance.RandomItem);
					audMan.PlaySingle(aud_MerryChristmas);
					disabled = true;
					StartCoroutine(Cooldown(100f));
				}
			}
		}

		private IEnumerator Cooldown(float val)
		{
			navigator.enabled = false;
			spriteBase.transform.position -= new Vector3(0f, 10f, 0f);
			DisableCollision(true);
			disabled = true;
			float cooldown = val;
			while (cooldown > 0f)
			{
				cooldown -= Time.deltaTime * ec.NpcTimeScale;
				yield return null;
			}

			while (looker.IsVisible && Vector3.Distance(transform.position, Singleton<CoreGameManager>.Instance.GetPlayer(0).transform.position) < 80f) // Waits player to look away and be far enough
			{
				yield return null;
			}

			navigator.enabled = true;
			navigator.maxSpeed = normalSpeed;
			DisableCollision(false);
			disabled = false;
			spriteBase.transform.position += new Vector3(0f, 10f, 0f);
			yield break;
		}


        private AudioManager audMan;

        private SoundObject aud_MerryChristmas;

		private bool disabled = false;

		[SerializeField]
        private const float normalSpeed = ContentUtilities.PlayerDefaultWalkSpeed + 1f;

		
    }

}

