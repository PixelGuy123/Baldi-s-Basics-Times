using MTM101BaldAPI;
using MTM101BaldAPI.AssetManager;
using HarmonyLib;
using System.Collections;
using System.IO;
using UnityEngine;

namespace BB_MOD.NPCs
{

	// ------ NPC SUMMARY ------
	// Puts you into detention, lmao
	
    public class MagicalStudent : NPC
    {

		private void SetupMagicObject()
		{
			var obj = new GameObject("Magic", typeof(StudentMagic), typeof(SphereCollider));
			var renderer = ContentUtilities.DefaultRenderer;
			renderer.SetActive(false);
			renderer.GetComponent<SpriteRenderer>().sprite = ContentAssets.GetAsset<Sprite>("MGS_MagicSprite");
			renderer.transform.SetParent(obj.transform);
			magic = obj.GetComponent<StudentMagic>();
			magic.SetRenderer(renderer);
			obj.GetComponent<SphereCollider>().isTrigger = true;
			obj.GetComponent<SphereCollider>().radius = 3f;
		}

        private void Start() 
        {
            navigator.maxSpeed = normalSpeed;
            navigator.SetSpeed(normalSpeed);

			// Audio Setup

			audMan = GetComponent<AudioManager>();

			audMan.audioDevice.minDistance = 20f;
			audMan.audioDevice.maxDistance = 240f;

			aud_Magic = ObjectCreatorHandlers.CreateSoundObject(ContentAssets.GetAsset<AudioClip>("MGS_magic"), "Vfx_MGS_Magic", SoundType.Voice, new Color(0f, 0f, 0.0065f)); // Creates audioClip

			detentionPre = ContentUtilities.FindResourceObject<DetentionUi>(); // Gets the detention ui for future uses

			SetupMagicObject();

		}

		

        private void Update()
        {
			if (!controlOverride && !navigator.HasDestination)
			{
				Wander();
			}
			
        }

		private void Wander()
		{
			if (readyForMagic)
			{
				StartCoroutine(MagicSequence());
				readyForMagic = false;
				return;
			}
			WanderRounds();
		}

		public override void DestinationEmpty()
		{
			base.DestinationEmpty();
			if (!controlOverride && !returningFromDetour)
			{
				Wander();
			}
		}

		public override void PlayerInSight(PlayerManager player)
		{
			if (ec.TileFromPos(player.transform.position).room.category == RoomCategory.Office || !wantToMagic || player.Tagged || magic.IsActive) return;

			base.PlayerInSight(player);

			targetPlayer = player;
			wantToMagic = false;
			readyForMagic = true;
		}

		public override void PlayerLost(PlayerManager player)
		{
			base.PlayerLost(player);
			if (readyForMagic && targetPlayer == player)
			{
				readyForMagic = false;
				wantToMagic = true;
			}
		}

		private IEnumerator WannaMagicCooldown(float t)
		{
			while (magic.IsActive) { yield return null; } // Waits the magic to disappear

			float timer = t;
			while (timer > 0f)
			{
				timer -= ec.NpcTimeScale * Time.deltaTime;
				yield return null;
			}
			wantToMagic = true;
			yield break;
		}

		private IEnumerator MagicSequence()
		{
			Sprite[] sprites = GetComponent<CustomNPCData>().sprites;
			SpriteRenderer renderer = GetComponent<CustomNPCData>().spriteObject;
			navigator.maxSpeed = 0f;
			
			float spriteFloat = 1f;
			while (spriteFloat < 2f)
			{
				spriteFloat += ec.NpcTimeScale * Time.deltaTime * 2f;
				renderer.sprite = sprites[Mathf.FloorToInt(spriteFloat)];
				yield return null;
			}

			audMan.PlaySingle(aud_Magic);
			transform.LookAt(targetPlayer.transform);
			magic.Throw(transform.rotation, transform, ec, detentionPre);
			renderer.sprite = sprites[2];

			

			float time = 1f;
			while (time > 0f) { time -= ec.NpcTimeScale * Time.deltaTime; yield return null; } // Waits one second before going back to normal sprite

			renderer.sprite = sprites[0]; // default sprite

			StartCoroutine(WannaMagicCooldown(Random.Range(minCooldown, maxCooldown)));
			navigator.SetSpeed(normalSpeed);
			targetPlayer = null;

			yield break;
		}

		private DetentionUi detentionPre;

		private AudioManager audMan;

		private SoundObject aud_Magic;

		private StudentMagic magic;

		private bool wantToMagic = true;

		private bool readyForMagic = false;

		private PlayerManager targetPlayer = null;

		[SerializeField]
		const float minCooldown = 15f, maxCooldown = 25f;

		[SerializeField]
        private const float normalSpeed = 19f;

		
    }

	public class StudentMagic : MonoBehaviour
	{
		public void Throw(Quaternion dir, Transform source, EnvironmentController ec, DetentionUi detentionPre)
		{
			transform.rotation = dir;
			transform.position = source.transform.position;
			this.ec = ec;
			active = true;
			this.detentionPre = detentionPre;
			originObject = source.gameObject;
			renderer.SetActive(true);
		}

		public void SetRenderer(GameObject renderer) => this.renderer = renderer;

		private void Disable()
		{
			active = false;
			transform.position += Vector3.down * 10f;
			renderer.SetActive(false);
		}

		private void Update()
		{
			if (!active) return;

			transform.position += transform.forward * Time.deltaTime * ec.EnvironmentTimeScale * 40f;

			if (!ec.ContainsCoordinates(IntVector2.GetGridPosition(transform.position))) Disable(); // Disables if out of bounds
		}

		private void OnTriggerEnter(Collider other) // Most scripts from Gotta Sweep, but few changes
		{
			if (!active) return;

			if (other.tag == "Player")
			{
				Disable();
				int num = Random.Range(0, ec.offices.Count);
				PlayerManager targetedPlayer = other.GetComponent<PlayerManager>();
				targetedPlayer.Teleport(ec.RealRoomMid(ec.offices[num]));
				foreach (Door door in ec.offices[num].doors) // Default time of 15 seconds, no increments
				{
					door.LockTimed(15f);
				}
				ec.offices[num].functionObject.AddComponent<DetentionManager>().Initialize(15f, ec);
				
				if (instancedDetentionPre != null)
					Destroy(instancedDetentionPre.gameObject);
				
				instancedDetentionPre = Instantiate(detentionPre);
				instancedDetentionPre.Initialize(Singleton<CoreGameManager>.Instance.GetCamera(targetedPlayer.playerNumber).canvasCam, 15f, ec);
				ec.MakeNoise(targetedPlayer.transform.position, 95);
			}
			else if (other.gameObject != originObject && other.tag == "NPC" && other.isTrigger)
			{
				Disable();
				NPC component = other.GetComponent<NPC>();
				int num = Random.Range(0, ec.offices.Count);
				component.transform.position = ec.RealRoomMid(ec.offices[num]) + Vector3.up * 5f;
			}
		}

		bool active = false;

		EnvironmentController ec;

		private DetentionUi detentionPre = null;

		DetentionUi instancedDetentionPre = null;

		GameObject renderer;

		GameObject originObject;

		public bool IsActive => active;
	}

}

