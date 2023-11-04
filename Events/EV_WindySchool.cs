using System.Collections.Generic;
using UnityEngine;

namespace BB_MOD.Events
{


	// ---- EVENT SUMMARY ----
	// Strange force everywhere

	public class WindySchool : RandomEvent
	{

		public override void Initialize(EnvironmentController controller, System.Random rng)
		{
			ContentUtilities.CreateNonPositionalAudio(gameObject);
			base.Initialize(controller, rng);
		}
		private void Start()
		{
			audMan = GetComponent<AudioManager>();
		}
		public override void Begin()
		{
			base.Begin();
			foreach (var npc in FindObjectsOfType<ActivityModifier>())
			{
				var comp = npc.GetComponent<ActivityModifier>();
				if (comp != null && comp.enabled)
				{
					moveMods.Add(comp);
					comp.moveMods.Add(mod);
				}
			}

			audMan.QueueAudio(aud_wind);
			audMan.SetLoop(true);
		}

		public override void End()
		{
			base.End();
			moveMods.ForEach(x => x?.moveMods.Remove(mod));
			audMan.FlushQueue(true);
		}

		public override void Pause()
		{
			base.Pause();
			audMan.Pause(true);
		}
		public override void Unpause() // Pauses and unpauses functions just so the noise pauses aswell.... I don't think I need this :)
		{
			base.Unpause();
			audMan.Pause(false);
		}
		private void Update()
		{
			if (!active) return;

			if (cooldown > 0f)
			{
				cooldown -= ec.EnvironmentTimeScale * Time.deltaTime;
			}
			else
			{
				cooldown = Random.Range(minCool, maxCool);
				mod.movementAddend = RandomStrongVector3;
			}
		}

		static Vector3 RandomStrongVector3 => new Vector3(Random.value * (Random.Range(0, 2) * 2 - 1) * 20.0f, 0f, Random.value * (Random.Range(0, 2) * 2 - 1) * 20.0f);



		AudioManager audMan;

		float cooldown = 0f;

		readonly List<ActivityModifier> moveMods = new List<ActivityModifier>();

		readonly MovementModifier mod = new MovementModifier(RandomStrongVector3, 1f);

		readonly SoundObject aud_wind = ContentAssets.GetAsset<SoundObject>("windy_wind");

		const float minCool = 5f, maxCool = 7f, maxForce = 7f;
	}
}
