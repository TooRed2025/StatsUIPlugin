using System;
using System.IO;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace StatsUIPlugin
{
    internal static class SPlocal
    {
        //重新应用覆盖
        public static void ApplyOverrides()
        {
            if (!SPConfig.EnableLocalization.Value) return;
            try
            {
                int total = ApplyTsvOverrides();
                StatsUIPlugin.LogDebug($"本地化覆盖已重新应用，共 {total} 条");
            }
            catch (Exception e)
            {
                StatsUIPlugin.Log.LogError($"外部本地化覆盖失败: {e.Message}");
            }
        }

        //覆盖流程
        private static int ApplyTsvOverrides()
        {
            string folder = Path.Combine(Application.streamingAssetsPath, "Localizations");
            if (!Directory.Exists(folder)) return 0;

            var db = LocalizationSettings.StringDatabase;
            if (db == null) return 0;

            // 优先对当前选中语言生效，回退到项目默认语言
            var locale = LocalizationSettings.SelectedLocale ?? LocalizationSettings.ProjectLocale;
            if (locale == null) return 0;

            int total = 0;
            foreach (var file in Directory.GetFiles(folder, "*.tsv"))
            {
                string tableName = Path.GetFileNameWithoutExtension(file);
                var table = db.GetTable(tableName, locale);
                if (table == null) continue;

                string[] lines = File.ReadAllText(file)
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split('\t');
                    if (parts.Length != 2) continue;

                    string key = parts[0];
                    string value = parts[1].Replace("\\n", string.Empty);
                    var entry = table.GetEntry(key);
                    if (entry != null) entry.Value = value;
                    else table.AddEntry(key, value);
                    total++;
                }
            }
            return total;
        }
    }
}
