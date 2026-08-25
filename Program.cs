// SkillsManager — built by Roota AI · find us on Rednote (小红书): 若塔AI
// Copyright (c) 2026 Roota AI. All rights reserved.

namespace SkillsManager
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new SkillsManagerForm());
        }
    }
}
