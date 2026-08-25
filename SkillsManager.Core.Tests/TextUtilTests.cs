// SkillsManager — built by Roota AI · find us on Rednote (小红书): 若塔AI
// Copyright (c) 2026 Roota AI. All rights reserved.

using SkillsManager.Core;
using Xunit;

namespace SkillsManager.Core.Tests
{
    public class TextUtilTests
    {
        [Theory]
        [InlineData("a\r\nb", "a\nb")]
        [InlineData("a\rb", "a\nb")]
        [InlineData("a\nb", "a\nb")]
        [InlineData("a\r\nb\rc\nd", "a\nb\nc\nd")]
        [InlineData("", "")]
        public void NormalizeToLf_collapses_all_ending_styles(string input, string expected)
            => Assert.Equal(expected, TextUtil.NormalizeToLf(input));

        [Theory]
        [InlineData("a\nb", "a\r\nb")]
        [InlineData("a\r\nb", "a\r\nb")]     // already CRLF must not become CR CR LF
        [InlineData("a\rb", "a\r\nb")]
        [InlineData("", "")]
        public void NormalizeToCrLf_yields_exactly_one_cr_per_lf(string input, string expected)
            => Assert.Equal(expected, TextUtil.NormalizeToCrLf(input));
    }
}
