using UnityEngine;

namespace BB_MOD.NPCs
{
	// ----------------- WARNING --------------------
	//	THIS IS NOT AN ACTUAL NPC, IT'S A DUMMY CLASS THAT YOU CAN USE TO MAKE YOUR OWN NPC
	//	Please read through all the comments here, it'll explain each method function in details
				
	public class Dummy : NPC
    {
        private void Start() // This is the Start() method, a normal Monobehavior method, which means, when the npc is enabled, it'll run this method once, you can put your setup here
        {
            navigator.maxSpeed = normalSpeed;
            navigator.SetSpeed(normalSpeed); // On the start here, also highly important, it sets the speed of your npc using the constant float value (you can always change that and include other constant speed values)

			// Audio Setup

			audMan = GetComponent<AudioManager>(); // Highly important to set the audMan to the component, so the field refers to it
			// aud_SomeAudioField = ContentAssets.GetAsset<SoundObject>("soundName"); // Gets a SoundObject for the npc

		}

		public override void Initialize() // Basically just the same as Start() method, but it is ran before being enabled into the level, here you could add custom spawn points (just like Crazy Clock does)
		{
			base.Initialize();
		}

		private void Update() // On each frame tick, this is called, put here your script to make the npc alive
        {
			if (!controlOverride && !navigator.HasDestination) // If the npc has no destination and the control isn't being overriden, it'll constantly call this method (this is important so the npc wanders around)
			{
				WanderRandom();
			}
		}

		public override void DestinationEmpty() // Feature from the npcs, when their destination is empty (which means, when they have reached their destination tile), this is called, here you can set it to wander random with the met conditions
		{
			base.DestinationEmpty();
			if (!controlOverride && !returningFromDetour) // returningFromDetour means if the npc isn't searching for a detour (in other words, the floating objects from the Gravity Chaos event), or if it is searching for the target AFTER getting the floating object
														  // controlOverride means if the npc isn't being controlled by an external object such as events for example
			{
				WanderRandom();
			}
		}

		public override void ClearDetourState() // Basically resets the detour states such as if it was searching for a floating object (from Chaos Event) or targetting something after touching the floating object
												// Has no clear use to be overriden
		{
			base.ClearDetourState();
		}

		public override void Despawn() // Runs when the npc is about to despawn, it is really useful to remove an effect that the npc leaves behind, such as gotta sweep with it's sweeping power
		{
			base.Despawn();
		}

		public override float DistanceCheck(float val) // This is only used by Baldi, and it is a way to tell how long should it travel through the tiles, you probably won't use this method, since it has no special functionality for most of the NPCs
		{
			return base.DistanceCheck(val);
		}

		public override void Hear(Vector3 position, int value) // Again, only used by Baldi, useful if the NPC has something to do with the noise value received, on which you'll probably not use
		{
		}

		public override void SetSpriteRotation(float degrees) // Only used by the gravity event when setting the npcs rotation, there's no clear use of this method
		{
			base.SetSpriteRotation(degrees);
		}

		public override void PlayerLost(PlayerManager player) // When the player is lost from the NPC's sight (Looker component required)
		{
			base.PlayerLost(player);
		}

		public override void PlayerSighted(PlayerManager player) // Runs once the player is sighted, not constantly (Looker component required)
		{
			base.PlayerSighted(player);
		}

		public override void PlayerInSight(PlayerManager player) // This is constantly called in case the player is seen by the npc (Looker component required)
		{
		}

		public override void Sighted() // If the npc is sighted by the player, this method runs once (Looker component required)
		{
			base.Sighted();
		}

		public override void Unsighted()
		{
			base.Unsighted();
		}

		public override void TargetPlayer(Vector3 playerPosition) // This method is called when targeting the player, it doesn't seems to have any use to be overriden
		{
			base.TargetPlayer(playerPosition);
		}

		public override void TargetPosition(Vector3 target) // Basically same as TargetPlayer() but this one directly targets a path for it and it has no extra checks to check for any attributes that the Player has
		{
			base.TargetPosition(target);
		}

		public override void WanderRandom() // Called by the NPC when it wants to wander, there's no clear use of it to be overriden
		{
			base.WanderRandom();
		}

		public override void WanderRounds() // Called by the NPC when it wants to wander around the school, exploring previously not accessed areas (requires a heat map), there's no clear use of it to be overriden
		{
			base.WanderRounds();
		}

		// It's also recommended to check some of the properties from the NPC class and Looker aswell, they are really useful

		private AudioManager audMan;

		[SerializeField]
        private const float normalSpeed = 1f;
    }
}

