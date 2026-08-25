// SkillsManager — built by Roota AI · find us on Rednote (小红书): 若塔AI
// Copyright (c) 2026 Roota AI. All rights reserved.

using SkillsManager.Core;
using Xunit;

namespace SkillsManager.Core.Tests
{
    public class SkillNameTests
    {
        [Theory]
        [InlineData("my-skill")]
        [InlineData("senior coding")]
        [InlineData("技能管理")]           // non-ASCII folder names are fine
        [InlineData("a.b")]
        public void Valid_names_pass(string name)
            => Assert.Null(SkillName.Validate(name));

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Empty_or_whitespace_is_rejected(string? name)
            => Assert.NotNull(SkillName.Validate(name));

        [Theory]
        [InlineData("a/b")]
        [InlineData("a\\b")]
        [InlineData("a:b")]
        [InlineData("a*b")]
        [InlineData("a?b")]
        [InlineData("a\"b")]
        [InlineData("a<b")]
        [InlineData("a>b")]
        [InlineData("a|b")]
        public void Windows_invalid_path_chars_are_rejected(string name)
            => Assert.NotNull(SkillName.Validate(name));

        [Theory]
        [InlineData(".")]
        [InlineData("..")]
        [InlineData("skill.")]     // trailing dot silently stripped by Win32 -> identity mismatch
        [InlineData(" skill")]     // leading space -> two visually identical folders
        [InlineData("skill ")]
        public void Dot_and_edge_whitespace_traps_are_rejected(string name)
            => Assert.NotNull(SkillName.Validate(name));

        [Theory]
        [InlineData("CON")]
        [InlineData("con")]
        [InlineData("PRN")]
        [InlineData("NUL")]
        [InlineData("COM1")]
        [InlineData("LPT9")]
        public void Reserved_windows_device_names_are_rejected(string name)
            => Assert.NotNull(SkillName.Validate(name));

        [Fact]
        public void Error_message_is_human_readable()
        {
            string? err = SkillName.Validate("a/b");
            Assert.False(string.IsNullOrWhiteSpace(err));
        }
    }
}
