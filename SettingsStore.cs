// SkillsManager — built by Roota AI · find us on Rednote (小红书): 若塔AI
// Copyright (c) 2026 Roota AI. All rights reserved.

using SkillsManager.Core;

namespace SkillsManager
{
    /// <summary>
    /// Loads/saves AppSettings at %LOCALAPPDATA%\SkillsManager\settings.json.
    /// The JSON shape lives in Core (tested); this is only the file I/O.
    /// A read failure yields defaults; a write failure is reported by the
    /// caller's status line rather than crashing.
    /// </summary>
    internal static class SettingsStore
    {
        internal static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SkillsManager", "settings.json");

        internal static AppSettings Load()
        {
            try
            {
                return AppSettings.FromJson(File.Exists(SettingsPath) ? File.ReadAllText(SettingsPath) : null);
            }
            catch
            {
                return new AppSettings();
            }
        }

        internal static void Save(AppSettings settings)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, settings.ToJson());
        }
    }
}
