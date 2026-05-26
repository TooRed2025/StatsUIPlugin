using System;
using System.Collections.Generic;
using System.IO;

namespace StatsUIPlugin
{
    internal class SPUtils
    {
        private static readonly HashSet<string> _failedTranslations = new HashSet<string>();
        private static readonly string _bugFilePath = Path.Combine(BepInEx.Paths.ConfigPath, "Translation", "zh", "Bug.txt");

        // 翻译失败写入文本
        public static void LogTranslationFailed(string key)
        {
            if (_failedTranslations.Contains(key)) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_bugFilePath));
                File.AppendAllText(_bugFilePath, $"{key}={key}\n", System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                StatsUIPlugin.Log.LogError($"写入Bug.txt失败：{ex.Message}");
            }
            _failedTranslations.Add(key);
            StatsUIPlugin.Log.LogWarning($"翻译失败 [{key}]：无对应中文，请清除其它翻译模组或联系汉化作者TooRed求助，QQ群：1050816144");
        }

        public static void LogDebug(FormattableString message)
        {
            if (SPConfig.DebugMode.Value)
            {
                StatsUIPlugin.Log.LogDebug(message.ToString());
                StatsUIPlugin.Log.LogInfo(message.ToString());
            }
        }
    }
}
