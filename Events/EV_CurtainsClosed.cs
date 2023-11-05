using BB_MOD.ExtraComponents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BB_MOD.Events
{


	// ---- EVENT SUMMARY ----
	// Close curtains + adding them to windows

	public class CurtainsClosed : RandomEvent
	{
		public override void Begin()
		{
			base.Begin();
			foreach (var c in curtains)
			{
				if (c != null)
					c.SetIt(true);
			}
		}

		public override void End()
		{
			base.End();
			foreach (var c in curtains)
			{
				if (c != null)
					c.StartCoroutine(TimerToOpen(c));
			}
		}

		public override void AfterUpdateSetup()
		{
			base.AfterUpdateSetup();
			
			foreach (var window in FindObjectsOfType<Window>())
			{
				var curtain = PrefabInstance.SpawnPrefab<Curtains>(window.transform.position, window.transform.rotation, ec, false);
				curtain.SetWindow(window);
				curtain.Execute();
				curtains.Add(curtain);
			}
			
			
		}

		IEnumerator TimerToOpen(Curtains curt)
		{
			float timer = Random.Range(minTime, maxTime);
			while (timer > 0f)
			{
				timer -= ec.EnvironmentTimeScale * Time.deltaTime;
				yield return null;
			}
			curt?.SetIt(false);

			yield break;
		}

		readonly List<Curtains> curtains = new List<Curtains>();

		const float minTime = 0.5f, maxTime = 2f;
	}
}
