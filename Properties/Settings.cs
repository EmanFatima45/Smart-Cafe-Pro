// ============================================================
// SmartCafe Pro - Properties/Settings.cs
// Auto-generated user settings store
// ============================================================
using System.Configuration;
using System.ComponentModel;

namespace SmartCafePro.Properties
{
    [SettingsProvider(typeof(LocalFileSettingsProvider))]
    internal sealed partial class Settings : ApplicationSettingsBase
    {
        private static Settings _defaultInstance = (Settings)Synchronized(new Settings());

        public static Settings Default => _defaultInstance;

        [UserScopedSetting]
        [DefaultSettingValue("")]
        [DisplayName("Remembered Username")]
        public string RememberedUser
        {
            get => (string)this["RememberedUser"];
            set => this["RememberedUser"] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("Normal")]
        public string WindowState
        {
            get => (string)this["WindowState"];
            set => this["WindowState"] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("Dashboard")]
        public string LastSelectedModule
        {
            get => (string)this["LastSelectedModule"];
            set => this["LastSelectedModule"] = value;
        }
    }
}
