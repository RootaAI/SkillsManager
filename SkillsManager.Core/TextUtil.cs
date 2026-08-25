// SkillsManager — built by Roota AI · find us on Rednote (小红书): 若塔AI
// Copyright (c) 2026 Roota AI. All rights reserved.

namespace SkillsManager.Core
{
    /// <summary>
    /// Line-ending normalization shared by the paste interceptor (clipboard →
    /// CRLF for the WinForms editor) and Save (editor → LF on disk, so
    /// SKILL.md diffs cleanly and reads the same for every agent runtime).
    /// </summary>
    public static class TextUtil
    {
        public static string NormalizeToLf(string text)
            => text.Replace("\r\n", "\n").Replace('\r', '\n');

        public static string NormalizeToCrLf(string text)
            => NormalizeToLf(text).Replace("\n", "\r\n");
    }
}
