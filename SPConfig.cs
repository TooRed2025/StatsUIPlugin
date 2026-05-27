using BepInEx.Configuration;

namespace StatsUIPlugin
{
    internal class SPConfig
    {
        public static ConfigEntry<bool> EnableTranslation { get; private set; }
        public static ConfigEntry<float> BaseSize { get; private set; }
        public static ConfigEntry<float> MinSize { get; private set; }
        public static ConfigEntry<float> ReducePerItem { get; private set; }
        public static ConfigEntry<float> HeaderOffset { get; private set; }
        public static ConfigEntry<float> NumOffset {  get; private set; }
        public static ConfigEntry<bool> FontMode { get; private set; }
        public static ConfigEntry<bool> DebugMode { get; private set; }
        public static ConfigEntry<bool> AutoCheck { get; private set; }

        public static bool ConfigChanged { get; private set; } = true;

        internal static void Init(ConfigFile config)
        {
            var sec = "状态栏设置";
            EnableTranslation = config.Bind(sec, "状态栏翻译功能", true, new ConfigDescription("启用翻译功能", new AcceptableValueList<bool>(true, false)));
            FontMode = config.Bind(sec, "字体大小变更", true, new ConfigDescription("字体大小变更", new AcceptableValueList<bool>(true, false)));
            AutoCheck = config.Bind(sec, "检测mod升级项", true, new ConfigDescription("自动检测额外的mod升级", new AcceptableValueList<bool>(true, false)));
            BaseSize = config.Bind(sec, "基础字体大小", 35f, new ConfigDescription("基础字体大小", new AcceptableValueRange<float>(8f, 48f)));
            MinSize = config.Bind(sec, "最小字体大小", 16f, new ConfigDescription("最小字体大小", new AcceptableValueRange<float>(4f, 24f)));
            ReducePerItem = config.Bind(sec, "字体递减", 1.4f, new ConfigDescription("每多一项升级减少的字体大小", new AcceptableValueRange<float>(0f, 5f)));
            HeaderOffset = config.Bind(sec, "标题偏移", 7f, new ConfigDescription("标题字体偏移", new AcceptableValueRange<float>(0f, 15f)));
            NumOffset = config.Bind(sec, "数字偏移", 0.4f, new ConfigDescription("数字字体偏移", new AcceptableValueRange<float>(-2f, 2f)));
            DebugMode = config.Bind(sec, "调试模式", false, new ConfigDescription("调试模式", new AcceptableValueList<bool>(true, false)));

            BaseSize.SettingChanged += (_, __) => ConfigChanged = true;
            MinSize.SettingChanged += (_, __) => ConfigChanged = true;
            ReducePerItem.SettingChanged += (_, __) => ConfigChanged = true;
            HeaderOffset.SettingChanged += (_, __) => ConfigChanged = true;
            NumOffset.SettingChanged += (_, __) => ConfigChanged = true;
            FontMode.SettingChanged += (_, __) => ConfigChanged = true;
        }

        internal static bool IsConfigChanged()
        {
            bool changed = ConfigChanged;
            ConfigChanged = false;
            return changed;
        }
    }
}
