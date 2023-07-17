using UnityEngine;

namespace BB_MOD.Events
{

	public class CustomEventData : MonoBehaviour // EVERY CUSTOM EVENT MUST HAVE THIS IN ORDER TO GET IT'S DATA
	{
		// General NPC stuff
		public RandomEventType myEvent;

		public string eventName;

		public string eventDescKey;

		public float maxEventTime;

		public float minEventTime;
	}
}
