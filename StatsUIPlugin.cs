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
            if (AutoTranslator.Default == null)
            {
                Log.LogError("XUnity.AutoTranslator未加载！");
                return;
            }

            _harmony = new Harmony("StatsUIPlugin");
            _harmony.PatchAll();

            SPConfig.Init(Config);
            SPManager.DetectModdedUpgrades();
            Log.LogInfo("加载成功！");
        }

        //日志开关
        internal static void LogDebug(FormattableString message)
        {
            if (SPConfig.DebugMode.Value)
            {
                Log.LogDebug(message.ToString());
                Log.LogInfo(message.ToString());
            }
        }

        //Hook状态栏刷新
        [HarmonyPatch(typeof(StatsUI), "Fetch")]
        static class StatsUIFetchPatch
        {
            static void Postfix(StatsUI __instance)
            {
                SPManager.ProcessStatsUI(__instance);
            }
        }

        //Hook原版升级项翻译方法
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
