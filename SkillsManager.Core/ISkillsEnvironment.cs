// SkillsManager — built by Roota AI · find us on Rednote (小红书): 若塔AI
// Copyright (c) 2026 Roota AI. All rights reserved.

namespace SkillsManager.Core
{
    /// <summary>
    /// The slice of the machine environment that library resolution reads.
    /// An interface (rather than direct Environment/Directory calls) so the
    /// resolution rules are testable on any OS without a real file system.
    /// </summary>
    public interface ISkillsEnvironment
    {
        /// <summary>Documents folder (follows OneDrive Known Folder Move); null when unavailable.</summary>
        string? MyDocuments { get; }

        /// <summary>User profile folder (%USERPROFILE%); null when unavailable.</summary>
        string? UserProfile { get; }

        /// <summary>%LOCALAPPDATA% — always available, the last-resort parent.</summary>
        string LocalAppData { get; }

        string? GetEnvironmentVariable(string name);
        bool DirectoryExists(string path);
    }

    /// <summary>The real machine environment.</summary>
    public sealed class SystemEnvironment : ISkillsEnvironment
    {
        public string? MyDocuments
        {
            get
            {
                string p = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                return string.IsNullOrEmpty(p) ? null : p;
            }
        }

        public string? UserProfile
        {
            get
            {
                string p = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return string.IsNullOrEmpty(p) ? null : p;
            }
        }

        public string LocalAppData
            => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        public string? GetEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name);
        public bool DirectoryExists(string path) => Directory.Exists(path);
    }
}
