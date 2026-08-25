// SkillsManager — built by Roota AI · find us on Rednote (小红书): 若塔AI
// Copyright (c) 2026 Roota AI. All rights reserved.

using SkillsManager.Core;
using Xunit;

namespace SkillsManager.Core.Tests
{
    /// <summary>Fake environment: dictionary-backed, no real file system.</summary>
    internal sealed class FakeEnv : ISkillsEnvironment
    {
        public string? MyDocuments { get; init; }
        public string? UserProfile { get; init; }
        public string LocalAppData { get; init; } = Path.Combine("C:", "Users", "t", "AppData", "Local");
        public Dictionary<string, string> Vars { get; init; } = new();
        public HashSet<string> ExistingDirs { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        string? ISkillsEnvironment.MyDocuments => MyDocuments;
        string? ISkillsEnvironment.UserProfile => UserProfile;
        string ISkillsEnvironment.LocalAppData => LocalAppData;
        public string? GetEnvironmentVariable(string name) => Vars.TryGetValue(name, out var v) ? v : null;
        public bool DirectoryExists(string path) => ExistingDirs.Contains(path);
    }

    public class LibraryCatalogTests
    {
        private static readonly string Docs    = Path.Combine("C:", "Users", "t", "OneDrive", "Documents");
        private static readonly string Profile = Path.Combine("C:", "Users", "t");

        private static FakeEnv Env() => new() { MyDocuments = Docs, UserProfile = Profile };

        // ── Cowork resolution (must preserve v1.0.0 behavior) ──

        [Fact]
        public void Cowork_prefers_existing_documents_path()
        {
            var env = Env();
            string expected = Path.Combine(Docs, "Cowork", "Skills");
            env.ExistingDirs.Add(expected);
            Assert.Equal(expected, LibraryCatalog.ResolveCoworkRoot(env));
        }

        [Fact]
        public void Cowork_falls_back_to_onedrive_commercial_env_var()
        {
            var env = Env();
            string commercial = Path.Combine("C:", "Users", "t", "OneDrive - Contoso");
            env.Vars["OneDriveCommercial"] = commercial;
            string expected = Path.Combine(commercial, "Documents", "Cowork", "Skills");
            env.ExistingDirs.Add(expected);
            Assert.Equal(expected, LibraryCatalog.ResolveCoworkRoot(env));
        }

        [Fact]
        public void Cowork_uses_primary_candidate_when_nothing_exists_yet()
        {
            Assert.Equal(Path.Combine(Docs, "Cowork", "Skills"),
                LibraryCatalog.ResolveCoworkRoot(Env()));
        }

        [Fact]
        public void Cowork_last_resort_is_local_appdata()
        {
            var env = new FakeEnv { MyDocuments = null, UserProfile = Profile };
            Assert.Equal(Path.Combine(env.LocalAppData, "SkillsManager", "Cowork", "Skills"),
                LibraryCatalog.ResolveCoworkRoot(env));
        }

        // ── Built-in set: Cowork + Claude Code + Codex ──

        [Fact]
        public void BuiltIns_contain_cowork_claude_and_codex_in_that_order()
        {
            var libs = LibraryCatalog.BuiltIns(Env());
            Assert.Equal(new[] { "cowork", "claude", "codex" }, libs.Select(l => l.Id).ToArray());
            Assert.All(libs, l => Assert.True(l.IsBuiltIn));
        }

        [Fact]
        public void Claude_root_is_dot_claude_skills_under_user_profile()
        {
            var lib = LibraryCatalog.BuiltIns(Env()).Single(l => l.Id == "claude");
            Assert.Equal(Path.Combine(Profile, ".claude", "skills"), lib.Root);
        }

        [Fact]
        public void Codex_root_is_dot_codex_skills_under_user_profile()
        {
            var lib = LibraryCatalog.BuiltIns(Env()).Single(l => l.Id == "codex");
            Assert.Equal(Path.Combine(Profile, ".codex", "skills"), lib.Root);
        }

        [Fact]
        public void Codex_honors_CODEX_HOME_override()
        {
            var env = Env();
            string home = Path.Combine("D:", "codex-home");
            env.Vars["CODEX_HOME"] = home;
            var lib = LibraryCatalog.BuiltIns(env).Single(l => l.Id == "codex");
            Assert.Equal(Path.Combine(home, "skills"), lib.Root);
        }

        [Fact]
        public void Claude_and_codex_fall_back_to_local_appdata_without_user_profile()
        {
            var env = new FakeEnv { MyDocuments = Docs, UserProfile = null };
            var libs = LibraryCatalog.BuiltIns(env);
            Assert.Equal(Path.Combine(env.LocalAppData, "SkillsManager", "claude", "skills"),
                libs.Single(l => l.Id == "claude").Root);
        }

        // ── Full catalog = built-ins + customs from settings ──

        [Fact]
        public void All_appends_custom_libraries_after_builtins()
        {
            string teamPath = Path.Combine("D:", "team", "skills");
            var settings = new AppSettings
            {
                CustomLibraries = { new CustomLibrary { Name = "Team skills", Path = teamPath } }
            };
            var libs = LibraryCatalog.All(Env(), settings);
            Assert.Equal(4, libs.Count);
            var custom = libs[3];
            Assert.False(custom.IsBuiltIn);
            Assert.Equal("Team skills", custom.Name);
            Assert.Equal(teamPath, custom.Root);
        }

        [Fact]
        public void Custom_library_ids_are_stable_across_reloads()
        {
            var settings = new AppSettings
            {
                CustomLibraries = { new CustomLibrary { Name = "X", Path = Path.Combine("D:", "x") } }
            };
            string id1 = LibraryCatalog.All(Env(), settings)[3].Id;
            string id2 = LibraryCatalog.All(Env(), settings)[3].Id;
            Assert.Equal(id1, id2);
        }
    }
}
