// SkillsManager — built by Roota AI · find us on Rednote (小红书): 若塔AI
// Copyright (c) 2026 Roota AI. All rights reserved.

namespace SkillsManager.Core
{
    /// <summary>
    /// One skills root the manager can operate on. Every library follows the
    /// same convention: &lt;Root&gt;\&lt;skill-name&gt;\SKILL.md.
    /// </summary>
    public sealed record SkillLibrary(string Id, string Name, string Root, bool IsBuiltIn);
}
