using BepInEx.Logging;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;

namespace FontPatch
{
    public static class Paths
    {
        public static string ConfigPath => BepInEx.Paths.ConfigPath;
        public static string GameRootPath => BepInEx.Paths.GameRootPath;
    }

    public class Font
    {
        public static ManualLogSource Mylog { get; } = Logger.CreateLogSource("FontPatch");

        public static IEnumerable<string> TargetDLLs { get; } = Array.Empty<string>();

        private const string FontFileName = "notosanssc";
        private const string MarkerFileName = "汉化文本By_TooRed";
        private const string TranslationZhPath = "Translation/zh";
        private const string LocalizationsFolderName = "Localizations";
        private const string StreamingAssetsPath = "REPO_Data/StreamingAssets";
        private const string VersionFileName = "version.ini";
        private static readonly string[] TargetFiles = new[] { "Game.tsv", "HUD.tsv", "Menu.tsv", "version.ini" };

        public static void Patch(AssemblyDefinition _)
        {
        }

        public static void Finish()
        {
            try
            {
                var markerFilePath = Path.Combine(Paths.ConfigPath, TranslationZhPath, MarkerFileName);
                var markerExists = File.Exists(markerFilePath);

                bool needsUpdate = !markerExists || CheckVersionUpdate();

                if (needsUpdate)
                {
                    Mylog.LogInfo("开始更新文件...");
                    CopyFontFile();
                    CopyLocalizationFiles();
                    Mylog.LogInfo("文件更新完成");
                }
                else
                {
                    Mylog.LogInfo("无需更新，跳过");
                }

                if (!markerExists)
                {
                    CreateMarkerFile(markerFilePath);
                }
            }
            catch (Exception ex)
            {
                Mylog.LogError($"字体补丁执行异常：{ex.Message}");
            }
        }

        private static bool CheckVersionUpdate()
        {
            var sourceVersionPath = Path.Combine(Paths.ConfigPath, TranslationZhPath, LocalizationsFolderName, VersionFileName);
            var targetVersionPath = Path.Combine(Paths.GameRootPath, StreamingAssetsPath, LocalizationsFolderName, VersionFileName);

            if (!File.Exists(sourceVersionPath))
            {
                return false;
            }

            var sourceVersion = ReadVersion(sourceVersionPath);
            var targetVersion = File.Exists(targetVersionPath) ? ReadVersion(targetVersionPath) : string.Empty;

            if (string.IsNullOrEmpty(targetVersion) || CompareVersions(sourceVersion, targetVersion) > 0)
            {
                Mylog.LogInfo($"检测到新版本：源版本{sourceVersion} > 目标版本{targetVersion}，强制更新");
                return true;
            }

            return false;
        }

        private static void CopyFontFile()
        {
            var sourcePath = Path.Combine(Paths.ConfigPath, TranslationZhPath, FontFileName);
            var targetPath = Path.Combine(Paths.GameRootPath, FontFileName);

            if (!File.Exists(sourcePath))
            {
                Mylog.LogError($"源字体文件不存在：{sourcePath}");
                return;
            }

            Mylog.LogInfo($"复制字体文件");
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
            File.Copy(sourcePath, targetPath, overwrite: true);
        }

        private static void CopyLocalizationFiles()
        {
            var sourcePath = Path.Combine(Paths.ConfigPath, TranslationZhPath, LocalizationsFolderName);
            var targetPath = Path.Combine(Paths.GameRootPath, StreamingAssetsPath, LocalizationsFolderName);

            if (!Directory.Exists(sourcePath))
            {
                Mylog.LogError($"源本地化文件夹不存在：{sourcePath}");
                return;
            }

            foreach (var file in TargetFiles)
            {
                var sourceFile = Path.Combine(sourcePath, file);
                var targetFile = Path.Combine(targetPath, file);

                if (!File.Exists(sourceFile))
                {
                    Mylog.LogError($"源文件不存在：{sourceFile}");
                    continue;
                }

                if (File.Exists(targetFile))
                {
                    File.Delete(targetFile);
                }
                File.Copy(sourceFile, targetFile, overwrite: true);
            }
            Mylog.LogInfo($"复制本地化文件完成");
        }

        private static string ReadVersion(string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    if (line.StartsWith("version=", StringComparison.OrdinalIgnoreCase))
                    {
                        return line.Substring(8).Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                Mylog.LogError($"读取版本文件失败：{ex.Message}");
            }
            return string.Empty;
        }

        private static int CompareVersions(string version1, string version2)
        {
            if (string.IsNullOrEmpty(version1) || string.IsNullOrEmpty(version2))
            {
                return 0;
            }

            var v1Parts = version1.Split('.');
            var v2Parts = version2.Split('.');

            int maxLength = Math.Max(v1Parts.Length, v2Parts.Length);
            for (int i = 0; i < maxLength; i++)
            {
                int v1 = i < v1Parts.Length && int.TryParse(v1Parts[i], out var n1) ? n1 : 0;
                int v2 = i < v2Parts.Length && int.TryParse(v2Parts[i], out var n2) ? n2 : 0;

                if (v1 > v2) return 1;
                if (v1 < v2) return -1;
            }

            return 0;
        }

        private static void CreateMarkerFile(string markerFilePath)
        {
            try
            {
                using (File.Create(markerFilePath)) { }
                Mylog.LogInfo("汉化补丁执行完成！");
            }
            catch (IOException ex)
            {
                Mylog.LogError($"创建标记文件失败：{ex.Message}");
            }
        }
    }
}
