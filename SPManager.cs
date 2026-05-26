using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private static FieldInfo _playerUpgradesField;
        private static FieldInfo _textField;
        private static FieldInfo _numbersTextField;
        private static FieldInfo _headerField;
        private static readonly StringBuilder _sb = new StringBuilder(512);
        private static readonly ConcurrentDictionary<string, string> _translationCache = new ConcurrentDictionary<string, string>();
        private static bool _hasModdedUpgrades = false;
        private static int _lastUpgradeHash;

        public static void InitReflectionCache()
        {
            _playerUpgradesField = AccessTools.Field(typeof(StatsUI), "playerUpgrades");
            _textField = AccessTools.Field(typeof(StatsUI), "Text");
            _numbersTextField = AccessTools.Field(typeof(StatsUI), "textNumbers");
            _headerField = AccessTools.Field(typeof(StatsUI), "upgradesHeader");

            DetectModdedUpgrades();
        }

        private static void DetectModdedUpgrades()
        {
            try
            {
                if (!SPConfig.AutoCheck.Value)
                {
                    _hasModdedUpgrades = true;
                    return;
                }

                Type upgradesType = AccessTools.TypeByName("REPOLib.Modules.Upgrades");
                if (upgradesType == null)
                {
                    _hasModdedUpgrades = false;
                    SPUtils.LogDebug($"未检测到 REPOLib中的模组升级项");
                    return;
                }

                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    if (assembly.FullName.Contains("GoopUpgrades"))
                    {
                        _hasModdedUpgrades = true;
                        SPUtils.LogDebug($"检测到 GoopUpgrades 升级项");
                        return;
                    }
                }

                FieldInfo playerUpgradesField = upgradesType.GetField("_playerUpgrades", 
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                
                if (playerUpgradesField == null)
                {
                    _hasModdedUpgrades = false;
                    return;
                }

                object playerUpgrades = playerUpgradesField.GetValue(null);
                if (playerUpgrades is System.Collections.IDictionary dict)
                {
                    _hasModdedUpgrades = dict.Count > 0;
                    SPUtils.LogDebug($"检测到 {dict.Count} 个模组升级项");
                }
            }
            catch (Exception ex)
            {
                _hasModdedUpgrades = false;
                StatsUIPlugin.Log.LogError($"REPOLib 检测失败：{ex.Message}");
            }
        }

        public static string GetTranslatedName(string displayName)
        {
            if (!_hasModdedUpgrades)
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

            if (_translationCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            try
            {
                if (AutoTranslator.Default.TryTranslate(key, out var translated))
                {
                    _translationCache[key] = translated;
                    SPUtils.LogDebug($"翻译：{key} → {translated}");
                    return translated;
                }
            }
            catch (Exception ex)
            {
                StatsUIPlugin.Log.LogError($"翻译失败：{ex.Message}");
            }

            _translationCache[key] = displayName;
            SPUtils.LogTranslationFailed(key);
            return displayName;
        }

        public static void ProcessStatsUI(StatsUI instance)
        {
            try
            {
                if (!SPConfig.FontMode.Value)
                {
                    return;
                }

                var headerText = _headerField?.GetValue(instance) as TextMeshProUGUI;
                if (headerText?.enabled == false)
                {
                    _lastUpgradeHash = 0;
                    return;
                }

                var Text = _textField?.GetValue(instance) as TextMeshProUGUI;
                var numbersText = _numbersTextField?.GetValue(instance) as TextMeshProUGUI;

                var upgrades = _playerUpgradesField?.GetValue(instance) as Dictionary<string, int>;
                int upgradeHash = (upgrades == null || upgrades.Count == 0) ? 0 : upgrades.Values.Sum();
                bool isChanged = upgradeHash != _lastUpgradeHash || SPConfig.IsConfigChanged();

                if (isChanged)
                {
                    UpdateFontSize(Text, numbersText, headerText, upgrades?.Count ?? 0);
                    SPUtils.LogDebug($"字体大小更改");

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
                    SPUtils.LogDebug($"数字拼接");
                    _lastUpgradeHash = upgradeHash;
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
            float reduce = upgradeCount > 5 ? (upgradeCount - 5) * SPConfig.FontReducePerItem.Value : 0;
            float newFontSize = Mathf.Clamp(SPConfig.BaseFontSize.Value - reduce, SPConfig.MinFontSize.Value, SPConfig.BaseFontSize.Value - reduce);
            Text.fontSize = newFontSize;
            numbersText.fontSize = newFontSize + SPConfig.NumFontPer.Value;
            if (headerText?.enabled ?? false) headerText.fontSize = newFontSize + SPConfig.HeaderFontOffset.Value;
        }
    }
}
