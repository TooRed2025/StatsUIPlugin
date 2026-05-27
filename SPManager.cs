using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;

namespace StatsUIPlugin
{
    internal class SPManager
    {
        private static readonly FieldInfo _playerUpgrades = AccessTools.Field(typeof(StatsUI), "playerUpgrades");
        private static readonly FieldInfo _text = AccessTools.Field(typeof(StatsUI), "Text");
        private static readonly FieldInfo _numbersText = AccessTools.Field(typeof(StatsUI), "textNumbers");
        private static readonly FieldInfo _header = AccessTools.Field(typeof(StatsUI), "upgradesHeader");
        private static readonly HashSet<string> _failedTrans = new HashSet<string>();
        private static readonly string _bugFilePath = Path.Combine(BepInEx.Paths.ConfigPath, "Translation", "zh", "Bug.txt");
        private static readonly StringBuilder _sb = new StringBuilder(512);
        private static readonly ConcurrentDictionary<string, string> _transCache = new ConcurrentDictionary<string, string>();
        private static bool _hasModUpg = false;
        private static int _lastUpgHash;

        internal static void DetectModdedUpgrades()
        {
            try
            {
                if (!SPConfig.AutoCheck.Value)
                {
                    _hasModUpg = true;
                    return;
                }

                //没就不用翻译了
                Type upgradesType = AccessTools.TypeByName("REPOLib.Modules.Upgrades");
                if (upgradesType == null)
                {
                    _hasModUpg = false;
                    StatsUIPlugin.LogDebug($"未检测到 REPOLib，大概不需翻译");
                    return;
                }

                //这个模组手动写升级项我也是醉了
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    if (assembly.FullName.Contains("GoopUpgrades"))
                    {
                        _hasModUpg = true;
                        StatsUIPlugin.LogDebug($"检测到 GoopUpgrades 升级项");
                        return;
                    }
                }

                //就反射一次懒得写缓存
                FieldInfo playerUpgradesField = AccessTools.Field(upgradesType, "_playerUpgrades");

                if (playerUpgradesField == null)
                {
                    _hasModUpg = false;
                    return;
                }

                object playerUpgrades = playerUpgradesField.GetValue(null);
                if (playerUpgrades is System.Collections.IDictionary dict)
                {
                    _hasModUpg = dict.Count > 0;
                    StatsUIPlugin.LogDebug($"检测到 {dict.Count} 个模组升级项");
                }
            }
            catch (Exception ex)
            {
                _hasModUpg = false;
                StatsUIPlugin.Log.LogError($"REPOLib 检测失败：{ex.Message}");
            }
        }

        internal static string GetTranslatedName(string displayName)
        {
            if (!_hasModUpg)
            {
                return displayName;
            }

            if (!SPConfig.EnableTranslation.Value)
            {
                return displayName;
            }

            if (string.IsNullOrEmpty(displayName))
            {
                return displayName;
            }

            string key = displayName.ToUpper();

            if (_transCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            try
            {
                if (AutoTranslator.Default.TryTranslate(key, out var translated))
                {
                    _transCache[key] = translated;
                    StatsUIPlugin.LogDebug($"翻译：{key} → {translated}");
                    return translated;
                }
            }
            catch (Exception ex)
            {
                StatsUIPlugin.Log.LogError($"翻译失败：{ex.Message}");
            }

            _transCache[key] = displayName;
            LogTranslationFailed(key);
            return displayName;
        }

        internal static void ProcessStatsUI(StatsUI instance)
        {
            try
            {
                if (!SPConfig.FontMode.Value)
                {
                    return;
                }

                var headerText = _header?.GetValue(instance) as TextMeshProUGUI;
                if (headerText?.enabled == false)
                {
                    _lastUpgHash = 0;
                    return;
                }

                var Text = _text?.GetValue(instance) as TextMeshProUGUI;
                var numbersText = _numbersText?.GetValue(instance) as TextMeshProUGUI;

                var upgrades = _playerUpgrades?.GetValue(instance) as Dictionary<string, int>;
                int upgradeHash = (upgrades == null || upgrades.Count == 0) ? 0 : upgrades.Values.Sum();
                bool isChanged = upgradeHash != _lastUpgHash || SPConfig.IsConfigChanged();

                if (isChanged)
                {
                    UpdateFontSize(Text, numbersText, headerText, upgrades?.Count ?? 0);
                    StatsUIPlugin.LogDebug($"字体大小更改");

                    _sb.Clear();
                    foreach (var playerUpgrade in upgrades)
                    {
                        if (playerUpgrade.Value > 0)
                        {
                            _sb.Append("<b><color=#FFFF00>")
                              .Append(playerUpgrade.Value)
                              .Append("</color></b>\n");
                        }
                    }
                    StatsUIPlugin.LogDebug($"数字拼接");
                    _lastUpgHash = upgradeHash;
                }

                numbersText.text = _sb.ToString();
            }
            catch (Exception ex)
            {
                StatsUIPlugin.Log.LogError($"处理StatsUI出错：{ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void UpdateFontSize(TextMeshProUGUI Text, TextMeshProUGUI numbersText, TextMeshProUGUI headerText, int upgradeCount)
        {
            float reduce = upgradeCount > 5 ? (upgradeCount - 5) * SPConfig.ReducePerItem.Value : 0;
            float newFontSize = Mathf.Clamp(SPConfig.BaseSize.Value - reduce, SPConfig.MinSize.Value, SPConfig.BaseSize.Value - reduce);
            Text.fontSize = newFontSize;
            numbersText.fontSize = newFontSize + SPConfig.NumOffset.Value;
            headerText.fontSize = newFontSize + SPConfig.HeaderOffset.Value;
        }

        internal static void LogTranslationFailed(string key)
        {
            if (_failedTrans.Contains(key)) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_bugFilePath));
                File.AppendAllText(_bugFilePath, $"{key}={key}\n", System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                StatsUIPlugin.Log.LogError($"写入Bug.txt失败：{ex.Message}");
            }
            _failedTrans.Add(key);
            StatsUIPlugin.Log.LogWarning($"翻译失败 [{key}]：无对应中文，请清除其它翻译模组或联系汉化作者TooRed求助，QQ群：1050816144");
        }
    }
}
