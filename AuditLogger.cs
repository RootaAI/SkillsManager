// SkillsManager — built by Roota AI · find us on Rednote (小红书): 若塔AI
// Copyright (c) 2026 Roota AI. All rights reserved.

namespace SkillsManager
{
    /// <summary>
    /// Minimal thread-safe audit log.
    /// Line shape: timestamp | user@machine | ACTION | skill name.
    /// Lives under %LOCALAPPDATA%\SkillsManager so it works from a read-only
    /// install location. A write failure never interrupts the user; a file
    /// grown past the cap is restarted rather than rotated (this tool logs a
    /// handful of lines per session, so snapshots are not worth the code).
    /// </summary>
    internal static class AuditLogger
    {
        private const long MaxLogSizeBytes = 5 * 1024 * 1024; // 5 MB

        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SkillsManager", "SkillsManagerLog.txt");

        private static readonly object _lock = new();

        internal static void Log(string action, string skillName)
        {
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | " +
                          $"{Environment.UserName}@{Environment.MachineName} | " +
                          $"{action.ToUpperInvariant()} | {skillName}";
            lock (_lock)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                    if (File.Exists(LogPath) && new FileInfo(LogPath).Length > MaxLogSizeBytes)
                        File.Delete(LogPath);
                    File.AppendAllText(LogPath, line + Environment.NewLine);
                }
                catch { /* the audit log must never break the tool */ }
            }
        }
    }
}
