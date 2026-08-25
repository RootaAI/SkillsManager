// SkillsManager — built by Roota AI · find us on Rednote (小红书): 若塔AI
// Copyright (c) 2026 Roota AI. All rights reserved.

namespace SkillsManager.Core
{
    /// <summary>
    /// Builds the set of skill libraries the manager offers.
    ///
    /// Built-ins (never removable, resolved per machine):
    ///  - cowork  — Microsoft Copilot Cowork skills on the OneDrive-synced
    ///              Documents tree (enterprise %OneDriveCommercial% honored).
    ///  - claude  — Claude Code user skills: &lt;profile&gt;\.claude\skills.
    ///  - codex   — OpenAI Codex skills: %CODEX_HOME%\skills, else
    ///              &lt;profile&gt;\.codex\skills.
    ///
    /// Custom libraries come from settings.json and follow the same
    /// &lt;root&gt;\&lt;skill&gt;\SKILL.md convention.
    /// </summary>
    public static class LibraryCatalog
    {
        /// <summary>
        /// The default Cowork skills location is &lt;Documents&gt;\Cowork\Skills on
        /// the OneDrive-synced Documents folder. MyDocuments follows OneDrive
        /// Known Folder Move automatically, so it is the primary candidate;
        /// the OneDrive env-var paths cover setups where Documents is NOT
        /// redirected but the OneDrive folder still hosts a Documents tree.
        /// First EXISTING candidate wins; when none exists yet the primary is
        /// used and created on first save/New Skill.
        /// </summary>
        public static string ResolveCoworkRoot(ISkillsEnvironment env)
        {
            var candidates = new List<string>();
            if (env.MyDocuments is { } docs)
                candidates.Add(Path.Combine(docs, "Cowork", "Skills"));
            foreach (var name in new[] { "OneDriveCommercial", "OneDrive" })
            {
                if (env.GetEnvironmentVariable(name) is { Length: > 0 } od)
                {
                    string p = Path.Combine(od, "Documents", "Cowork", "Skills");
                    if (!candidates.Contains(p, StringComparer.OrdinalIgnoreCase))
                        candidates.Add(p);
                }
            }
            foreach (var c in candidates)
                if (env.DirectoryExists(c)) return c;
            return candidates.Count > 0
                ? candidates[0]
                : Path.Combine(env.LocalAppData, "SkillsManager", "Cowork", "Skills");
        }

        public static IReadOnlyList<SkillLibrary> BuiltIns(ISkillsEnvironment env) => new[]
        {
            new SkillLibrary("cowork", "Copilot Cowork", ResolveCoworkRoot(env), IsBuiltIn: true),
            new SkillLibrary("claude", "Claude Code", DotFolderRoot(env, ".claude", "claude"), IsBuiltIn: true),
            new SkillLibrary("codex", "Codex", ResolveCodexRoot(env), IsBuiltIn: true),
        };

        /// <summary>Built-ins followed by the custom libraries from settings.</summary>
        public static IReadOnlyList<SkillLibrary> All(ISkillsEnvironment env, AppSettings settings)
        {
            var libs = new List<SkillLibrary>(BuiltIns(env));
            foreach (var c in settings.CustomLibraries)
            {
                if (string.IsNullOrWhiteSpace(c.Path)) continue;
                string name = string.IsNullOrWhiteSpace(c.Name) ? Path.GetFileName(c.Path) : c.Name;
                // Id derived from the path: stable across restarts without
                // persisting a separate key, unique as long as paths are.
                libs.Add(new SkillLibrary("custom:" + c.Path.ToLowerInvariant(), name, c.Path, IsBuiltIn: false));
            }
            return libs;
        }

        private static string ResolveCodexRoot(ISkillsEnvironment env)
            => env.GetEnvironmentVariable("CODEX_HOME") is { Length: > 0 } home
                ? Path.Combine(home, "skills")
                : DotFolderRoot(env, ".codex", "codex");

        private static string DotFolderRoot(ISkillsEnvironment env, string dotFolder, string fallbackKey)
            => env.UserProfile is { } profile
                ? Path.Combine(profile, dotFolder, "skills")
                : Path.Combine(env.LocalAppData, "SkillsManager", fallbackKey, "skills");
    }
}
