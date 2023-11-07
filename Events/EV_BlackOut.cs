using UnityEngine;
using HarmonyLib;
using BB_MOD.ExtraComponents;

namespace BB_MOD.Events
{


	// ---- EVENT SUMMARY ----
	// Power outage, everything stops working literally

	public class BlackOut : RandomEvent
	{
		public override void Initialize(EnvironmentController controller, System.Random rng)
		{
			base.Initialize(controller, rng);
			audMan = gameObject.AddComponent<AudioManager>(); // Setup audio

			ContentUtilities.CreateNonPositionalAudio(gameObject);
			audMan.audioDevice = GetComponent<AudioSource>();

			aud_powerOn = ContentAssets.GetAsset<SoundObject>("blackout_on");
			aud_powerOut = ContentAssets.GetAsset<SoundObject>("blackout_off");
		}
		public override void Begin()
		{
			base.Begin();
			audMan.PlaySingle(aud_powerOut);
			TurnStructures(false);

		}

		public override void AfterUpdateSetup() // Disable this to not cause issues
		{
			base.AfterUpdateSetup();
			OutageGoing = false;
		}

		private void TurnStructures(bool turn)
		{
			ec.MaxRaycast = turn ? float.PositiveInfinity : maxRaycast;
			ec.SetAllLights(turn);
			OutageGoing = !turn;
			foreach (var keyBelt in EnvironmentExtraVariables.belts)
			{
				var belt = keyBelt.Key;
				belt.SetSpeed(turn ? keyBelt.Value : 0f);
				var man = belt.transform.GetChild(0).GetComponent<AudioManager>();
				man.Pause(!turn);
				
				if (turn)
				{
					AccessTools.Method(typeof(AudioManager), "CreateSubtitle", 
						ContentUtilities.Array(typeof(SoundObject), typeof(bool), typeof(Color))) // Finds the method
						.Invoke(belt.transform.GetChild(0).GetComponent<AudioManager>(), 
						ContentUtilities.Array<object>(
							((SoundObject[])AccessTools.Field(typeof(AudioManager), "soundOnStart").GetValue(belt.transform.GetChild(0).GetComponent<AudioManager>()))[0], // Gets the audio
							true, Color.white)); // Invokes the createsub method for the loop by getting the audio
				}
				else 
					Singleton<SubtitleManager>.Instance.DestroySub(man.sourceId);


			}

			foreach (var vent in FindObjectsOfType<Vent>())
			{
				vent.TurnVent(turn);
			}

			if (turn)
			{
				foreach (var mathMach in FindObjectsOfType<MathMachine>())
				{
					if (!EnvironmentExtraVariables.completedMachines.Contains(mathMach)) // Checks if the answer hasn't been completed
						AccessTools.Method(typeof(MathMachine), "NewProblem").Invoke(mathMach, null); // Calls the method that makes a new problem for the machine
					else
						mathMach.Corrupt(true); // Corrupts math machine to not show a blank display

				}
			}
			var actualSodaMat = ContentUtilities.FindResourceObject<SodaMachine>().GetComponent<MeshRenderer>().materials[1].GetTexture("_LightGuide");
			foreach (var soda in FindObjectsOfType<SodaMachine>())
			{
				soda.GetComponent<MeshRenderer>().materials[1].SetTexture("_LightGuide", turn ? actualSodaMat : null); // Switches the texture from the material to make it not glow
			}
		}

		public override void End()
		{
			base.End();
			audMan.PlaySingle(aud_powerOn);
			TurnStructures(true);
		}

		AudioManager audMan;

		SoundObject aud_powerOut;

		SoundObject aud_powerOn;

		public static bool OutageGoing = false;

		const float maxRaycast = 35f;
	}
}
