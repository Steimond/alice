using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Alice.Editor
{
    /// <summary>Creates localization settings, default locales and base tables without replacing existing ones.</summary>
    public static class AliceLocalizationSetup
    {
        private const string SettingsFolder = "Assets/_Alice/Settings/Localization";
        private const string SettingsPath = SettingsFolder + "/LocalizationSettings.asset";
        private const string LocalesFolder = SettingsFolder + "/Locales";
        private const string TablesFolder = "Assets/_Alice/Data/Localization";

        // Russian is the source language; English is the fallback for unsupported system languages.
        private const string ProjectLocaleCode = "ru";
        private const string FallbackLocaleCode = "en";
        private static readonly string[] LocaleCodes = { ProjectLocaleCode, FallbackLocaleCode };
        private static readonly string[] StringTables = { "UI" };

        public static void Configure()
        {
            AliceProjectSetup.EnsureFolder(LocalesFolder);
            AliceProjectSetup.EnsureFolder(TablesFolder);

            bool created = false;
            LocalizationSettings settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings == null)
            {
                settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsPath);
                if (settings == null)
                {
                    settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                    AssetDatabase.CreateAsset(settings, SettingsPath);
                    created = true;
                }

                LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            }

            foreach (string code in LocaleCodes)
            {
                EnsureLocale(code);
            }

            // The package defaults the project locale to English; replace it only on first creation.
            if (created)
            {
                LocalizationSettings.ProjectLocale = LocalizationEditorSettings.GetLocale(ProjectLocaleCode);
            }

            foreach (IStartupLocaleSelector selector in settings.GetStartupLocaleSelectors())
            {
                if (selector is SpecificLocaleSelector specific && string.IsNullOrEmpty(specific.LocaleId.Code))
                {
                    specific.LocaleId = new LocaleIdentifier(FallbackLocaleCode);
                }
            }

            foreach (string table in StringTables)
            {
                if (LocalizationEditorSettings.GetStringTableCollection(table) == null)
                {
                    LocalizationEditorSettings.CreateStringTableCollection(table, TablesFolder);
                }
            }

            EditorUtility.SetDirty(settings);
        }

        private static void EnsureLocale(string code)
        {
            if (LocalizationEditorSettings.GetLocale(code) != null)
            {
                return;
            }

            string path = LocalesFolder + "/" + code + ".asset";
            Locale locale = AssetDatabase.LoadAssetAtPath<Locale>(path);
            if (locale == null)
            {
                locale = Locale.CreateLocale(new LocaleIdentifier(code));
                AssetDatabase.CreateAsset(locale, path);
            }

            LocalizationEditorSettings.AddLocale(locale);
        }
    }
}
