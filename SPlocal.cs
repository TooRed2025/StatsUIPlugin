using System;
using System.IO;
using UnityEngine;

namespace StatsUIPlugin
{
    internal static class SPlocal
    {
        private const string LocalizationsFolderName = "Localizations";
        private const string StreamingAssetsPath = "REPO_Data/StreamingAssets";

        private static readonly string[] TargetFiles = new[] { "Game.tsv", "HUD.tsv", "Menu.tsv", "version.ini" };

        internal static void Init()
        {
            Application.quitting += OnQuit;
        }

        private static void OnQuit()
        {
            if (!SPConfig.CleanupOnQuit.Value) return;

            try
            {
                StatsUIPlugin.Log.LogInfo("开始清理本地化文件...");
                bool deleted = false;

                var locDir = Path.Combine(BepInEx.Paths.GameRootPath, StreamingAssetsPath, LocalizationsFolderName);
                foreach (var file in TargetFiles)
                {
                    if (SafeDelete(Path.Combine(locDir, file))) deleted = true;
                }

                StatsUIPlugin.Log.LogInfo(deleted ? "本地化文件清理完成" : "游戏目录无汉化文件，跳过清理");
            }
            catch (Exception ex)
            {
                StatsUIPlugin.Log.LogError($"清理本地化文件异常：{ex.Message}");
            }
        }

        private static bool SafeDelete(string path)
        {
            try
            {
                if (!File.Exists(path)) return false;
                File.Delete(path);
                return true;
            }
            catch (Exception ex)
            {
                StatsUIPlugin.Log.LogWarning($"删除文件失败 {path}: {ex.Message}");
                return false;
            }
        }
    }
}
