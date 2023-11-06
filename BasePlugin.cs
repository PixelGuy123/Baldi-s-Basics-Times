using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using MTM101BaldAPI.AssetManager;
using BepInEx.Bootstrap;

namespace BB_MOD.BepInEx
{
	[BepInDependency(EndlessFloorsID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(ModInfo.id, ModInfo.name, ModInfo.version)]
    public class BasePlugin : BaseUnityPlugin
    {
		ConfigEntry<bool> debug;
        void Awake()
        {

			Harmony harmony = new Harmony(ModInfo.id);
			

			var man = new GameObject("ModManager").AddComponent<ContentManager>();
			ContentManager.modPath = AssetManager.GetModPath(this);

			Logger.LogInfo($"{ModInfo.name} {ModInfo.version} has been initialized! Made by PixelGuy");

			debug = Config.Bind(
				"General",
				"Debug Mode",
				false,
				"Enables/Disables the debug mode on the game, when enabled, Baldi won\'t be able to kill you + his countdown will begin on 1"
				);

			man.DebugMode = debug.Value;
			EndlessAvailable = Chainloader.PluginInfos.ContainsKey(EndlessFloorsID);

			ContentManager.instance.SetupAssetData();

			harmony.PatchAll();


		}

		public static bool EndlessAvailable = false;

		public const string EndlessFloorsID = "mtm101.rulerp.baldiplus.endlessfloors";


	}


}