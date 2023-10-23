using BB_MOD.ExtraComponents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BB_MOD.NPCs
{
	// ----------------- WARNING --------------------
	//	Stomps people, hihiha - PixelGuy
				
	public class Leapy : NPC
    {
        private void Start() // This is the Start() method, a normal Monobehavior method, which means, when the npc is enabled, it'll run this method once, you can put your setup here
        {
            navigator.maxSpeed = normalSpeed;
            navigator.SetSpeed(normalSpeed); // On the start here, also highly important, it sets the speed of your npc using the constant float value (you can always change that and include other constant speed values)

			// Audio Setup

			audMan = GetComponent<AudioManager>(); // Highly important to set the audMan to the component, so the field refers to it
			aud_jump = ContentAssets.GetAsset<SoundObject>("leapy_leap");
			aud_stomp = ContentAssets.GetAsset<SoundObject>("leapy_stomp");

			renderer = GetComponent<CustomNPCData>().spriteObject;
			sprites = GetComponent<CustomNPCData>().sprites;

			// add leapy audios from the assets and play them

		}

		private void Update()
        {
			UpdateMoveCooldown();
			if (!controlOverride && !navigator.HasDestination)
			{
				WanderRandom();
			}
		}

		private void UpdateMoveCooldown()
		{
			navigator.maxSpeed = IsMoving ? normalSpeed : 0f;
			navigator.SetSpeed(navigator.maxSpeed);
			if (jumpWaitCooldown > 0f && jumpingCooldown <= 0f)
			{
				jumpWaitCooldown -= 1f * ec.NpcTimeScale * Time.deltaTime;
			}
			else if (jumpingCooldown <= 0f && !aboutToJump)
			{
				StartCoroutine(Jump());
			}

			jumpingCooldown -= 1f * ec.NpcTimeScale * Time.deltaTime;
			if (!IsMoving && wasMoving)
			{
				renderer.sprite = sprites[0]; // Normal Sprite
				wasMoving = false;
			}
		}

		private IEnumerator Jump() // Literally jump
		{
			aboutToJump = true;
			float cooldown = 0.5f;

			renderer.sprite = sprites[1]; // About to jump

			while (cooldown > 0f)
			{
				cooldown -= ec.NpcTimeScale * Time.deltaTime;
				yield return null;
			}
			jumpingCooldown = maxJumpingCooldown;
			jumpWaitCooldown = maxJumpCooldown;
			wasMoving = true;
			aboutToJump = false;
			audMan.PlaySingle(aud_jump);

			renderer.sprite = sprites[2]; // Jumping

			yield break;
		}

		private void OnTriggerStay(Collider other) // Time TO Stomp
		{
			if (!IsMoving) return;

			if (other.tag == "Player" || (other.tag == "NPC" && other.isTrigger))
			{
				var mod = other.GetComponent<ActivityModifier>();
				if (mod && !stompedDudes.Contains(mod))
				{
					if (other.tag == "Player")
					{
						if (other.GetComponent<CustomPlayerAttributes>().TryGetImmunity("stomped", out _))
						{
							return; // Cancels if it has a immunity to it
						}
					}
					var effect = PrefabInstance.SpawnPrefab<GroundedEffect>(other.transform.position, default, ec, false);
					effect.SetupTarget(other.transform);
					effect.Execute();
					groundEffects.Add(effect);

					audMan.PlaySingle(aud_stomp);
					stompedDudes.Add(mod);
					mod.moveMods.Add(this.mod);
					StartCoroutine(StompTimer(mod, effect));
				}
			}
		}

		private IEnumerator StompTimer(ActivityModifier target, GroundedEffect effect)
		{

			float timer = Random.Range(5f, 10f);
			while (timer > 0f)
			{
				timer -= ec.NpcTimeScale * Time.deltaTime;
				yield return null;
			}

			target.moveMods.Remove(mod);
			stompedDudes.Remove(target);
			groundEffects.Remove(effect);
			effect.Despawn();

			yield break;
		}

		public override void DestinationEmpty()
		{
			base.DestinationEmpty();
			if (!controlOverride && !returningFromDetour)
			{
				WanderRandom();
			}
		}

		public override void Despawn()
		{
			StopAllCoroutines();
			foreach (var activity in stompedDudes)
			{
				activity.moveMods.Remove(mod);
			}
			foreach (var effect in groundEffects)
			{
				effect.Despawn();
			}
			base.Despawn();
		}

		public override float DistanceCheck(float val) // Test I guess
		{
			return val * 2f;
		}

		// It's also recommended to check some of the properties from the NPC class and Looker aswell, they are really useful

		private AudioManager audMan;

		SoundObject aud_jump, aud_stomp;

		private SpriteRenderer renderer;

		private Sprite[] sprites;

		private float jumpingCooldown = 0f;

		private float jumpWaitCooldown = 5f;

		readonly List<ActivityModifier> stompedDudes = new List<ActivityModifier>();

		readonly List<GroundedEffect> groundEffects = new List<GroundedEffect>();

		readonly MovementModifier mod = new MovementModifier(Vector3.zero, 0f);

		bool IsMoving => jumpingCooldown > 0f;

		bool aboutToJump = false;

		bool wasMoving = false;

		[SerializeField]
        private const float normalSpeed = 20f, maxJumpCooldown = 1f, maxJumpingCooldown = 0.5f;
    }
}

