using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using MTM101BaldAPI.AssetManager;

namespace BB_MOD
{
    [BepInPlugin(ModInfo.id, ModInfo.name, ModInfo.version)]
    public class ExtraContent : BaseUnityPlugin
    {
		ConfigEntry<bool> debug;
        void Awake()
        {
            Harmony harmony = new Harmony(ModInfo.id);

			var man = new GameObject("ModManager").AddComponent<ContentManager>();
			ContentManager.modPath = AssetManager.GetModPath(this);

			Logger.LogInfo($"{ModInfo.name} {ModInfo.version} has been initialized! Made by PixelGuy");

			harmony.PatchAll();

			debug = Config.Bind(
				"General",
				"Debug Mode",
				false,
				"Enables/Disables the debug mode on the game, when enabled, Baldi won\'t be able to kill you + his countdown will begin on 1"
				);

			man.DebugMode = debug.Value;


		}

    }


}