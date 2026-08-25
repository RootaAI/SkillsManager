// SkillsManager — built by Roota AI · find us on Rednote (小红书): 若塔AI
// Copyright (c) 2026 Roota AI. All rights reserved.

namespace SkillsManager.Core
{
    /// <summary>
    /// Validates a skill name before it becomes a folder name. The folder IS
    /// the skill's identity for every supported runtime (Copilot Cowork,
    /// Claude Code, Codex), so names that Windows would mangle (trailing
    /// dots/spaces), refuse (invalid chars, reserved device names), or that
    /// mean "this directory" (./..) are rejected up front.
    /// </summary>
    public static class SkillName
    {
        // Path.GetInvalidFileNameChars() is platform-dependent (Linux only
        // bans / and NUL) — skills sync to Windows machines via OneDrive, so
        // the WINDOWS rules are enforced everywhere.
        private static readonly char[] InvalidChars =
            { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

        private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        };

        /// <summary>Null when valid; otherwise a human-readable reason.</summary>
        public static string? Validate(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "The skill name cannot be empty.";
            if (name != name.Trim())
                return "The skill name cannot start or end with a space.";
            if (name.EndsWith('.'))
                return "The skill name cannot end with a dot (Windows strips it, changing the identity).";
            if (name is "." or "..")
                return "'.' and '..' are not valid skill names.";
            if (name.IndexOfAny(InvalidChars) >= 0 || name.Any(char.IsControl))
                return "The skill name becomes a folder name — it cannot contain \\ / : * ? \" < > | or control characters.";
            if (ReservedNames.Contains(name))
                return $"'{name}' is a reserved Windows device name and cannot be a folder.";
            return null;
        }
    }
}
