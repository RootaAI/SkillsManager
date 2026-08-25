// SkillsManager — built by Roota AI · find us on Rednote (小红书): 若塔AI
// Copyright (c) 2026 Roota AI. All rights reserved.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace SkillsManager.Core
{
    /// <summary>A user-added skills root (settings.json entry).</summary>
    public sealed class CustomLibrary
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
    }

    /// <summary>
    /// App settings persisted as human-editable JSON under
    /// %LOCALAPPDATA%\SkillsManager\settings.json. Parsing is deliberately
    /// forgiving: a missing or corrupt file yields defaults (the built-in
    /// libraries always work), and unknown properties survive round-trips
    /// from newer versions being ignored rather than crashing older ones.
    /// </summary>
    public sealed class AppSettings
    {
        public List<CustomLibrary> CustomLibraries { get; set; } = new();
        public string? LastLibraryId { get; set; }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

        public static AppSettings FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new AppSettings();
            try
            {
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
            catch (JsonException)
            {
                return new AppSettings();
            }
        }
    }
}
