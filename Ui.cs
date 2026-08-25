// SkillsManager — built by Roota AI · find us on Rednote (小红书): 若塔AI
// Copyright (c) 2026 Roota AI. All rights reserved.

namespace SkillsManager
{
    /// <summary>
    /// Shared colors and cached fonts. Fonts are created once as static
    /// readonly instances so repeated use never leaks GDI handles.
    /// </summary>
    internal static class Ui
    {
        internal const string FontFamily   = "Segoe UI";
        internal const string FontConsolas = "Consolas";

        internal static readonly Color ContentBackground = Color.FromArgb(245, 245, 245);

        internal static readonly Font FontStatus     = new(FontFamily, 10F);
        internal static readonly Font FontButton     = new(FontFamily, 10F, FontStyle.Bold);
        internal static readonly Font FontMeta       = new(FontFamily, 9F);
        internal static readonly Font FontGridHeader = new(FontFamily, 10F, FontStyle.Bold);
        internal static readonly Font FontEditor     = new(FontConsolas, 10F);
    }
}
