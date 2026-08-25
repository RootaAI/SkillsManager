// SkillsManager — built by Roota AI · find us on Rednote (小红书): 若塔AI
// Copyright (c) 2026 Roota AI. All rights reserved.

using SkillsManager.Core;
using Xunit;

namespace SkillsManager.Core.Tests
{
    public class AppSettingsTests
    {
        [Fact]
        public void Roundtrips_custom_libraries_and_last_library()
        {
            var s = new AppSettings { LastLibraryId = "claude" };
            s.CustomLibraries.Add(new CustomLibrary { Name = "Team", Path = @"D:\team\skills" });

            var back = AppSettings.FromJson(s.ToJson());

            Assert.Equal("claude", back.LastLibraryId);
            var lib = Assert.Single(back.CustomLibraries);
            Assert.Equal("Team", lib.Name);
            Assert.Equal(@"D:\team\skills", lib.Path);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not json at all {")]
        [InlineData("[1,2,3]")]
        public void Corrupt_or_missing_json_yields_defaults_not_exceptions(string? json)
        {
            var s = AppSettings.FromJson(json);
            Assert.NotNull(s);
            Assert.Empty(s.CustomLibraries);
            Assert.Null(s.LastLibraryId);
        }

        [Fact]
        public void Unknown_json_properties_are_ignored_for_forward_compat()
        {
            var s = AppSettings.FromJson("""{"lastLibraryId":"codex","futureSetting":true}""");
            Assert.Equal("codex", s.LastLibraryId);
        }

        [Fact]
        public void Json_is_camel_cased_for_human_editing()
        {
            var s = new AppSettings { LastLibraryId = "cowork" };
            Assert.Contains("\"lastLibraryId\"", s.ToJson());
        }
    }
}
