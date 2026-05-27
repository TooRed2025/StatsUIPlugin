using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using XUnity.AutoTranslator.Plugin.Core;

namespace StatsUIPlugin
{
    [BepInPlugin("StatsUIPlugin", "状态栏辅助插件", "1.1.0")]
    [BepInDependency("gravydevsupreme.xunity.autotranslator", BepInDependency.DependencyFlags.HardDependency)]
    public class StatsUIPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log { get; private set; }
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            _harmony = new Harmony("StatsUIPlugin");

            SPConfig.Init(Config);

            if (AutoTranslator.Default == null)
            {
                Log.LogError("XUnity.AutoTranslator未加载！");
                return;
            }

            SPManager.InitReflectionCache();

            _harmony.PatchAll();
            Log.LogInfo("加载成功！");
        }
        internal static void LogDebug(FormattableString message)
        {
            if (SPConfig.DebugMode.Value)
            {
                Log.LogDebug(message.ToString());
                Log.LogInfo(message.ToString());
            }
        }

        [HarmonyPatch(typeof(StatsUI), "Fetch")]
        static class StatsUIFetchPatch
        {
            static void Postfix(StatsUI __instance)
            {
                SPManager.ProcessStatsUI(__instance);
            }
        }

        [HarmonyPatch(typeof(StatsUI), "GetUpgradeDisplayName")]
        static class GetUpgradeDisplayNamePatch
        {
            static void Postfix(ref string __result)
            {
                __result = SPManager.GetTranslatedName(__result);
            }
        }
    }
}
