using BepInEx;
using HarmonyLib;
using UnityEngine;
using MTM101BaldAPI.AssetManager;

namespace BB_MOD
{
    [BepInPlugin(ModInfo.id, ModInfo.name, ModInfo.version)]
    public class ExtraContent : BaseUnityPlugin
    {

        void Awake()
        {
            Harmony harmony = new Harmony(ModInfo.id);

			new GameObject("ModManager").AddComponent<ContentManager>();
			ContentManager.modPath = AssetManager.GetModPath(this);


			harmony.PatchAll();

			




		}

    }


}