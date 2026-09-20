using System;
using System.IO;

namespace OpenCodeStudio.Services
{
    /// <summary>
    /// Tracks whether the first-run setup wizard has been completed. The marker
    /// is written next to the installed extension assembly when that folder is
    /// writable, so uninstalling/reinstalling the extension shows the wizard
    /// again. Falls back to %LOCALAPPDATA%\OpenCodeStudio otherwise.
    /// </summary>
    public static class Onboarding
    {
        private const string MarkerName = "onboarding.done";

        public static bool IsCompleted()
        {
            foreach (var path in CandidatePaths())
            {
                try { if (File.Exists(path)) return true; } catch { }
            }
            return false;
        }

        public static void MarkCompleted()
        {
            foreach (var path in CandidatePaths())
            {
                try
                {
                    var dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    File.WriteAllText(path, DateTime.Now.ToString("o"));
                    return;
                }
                catch { }
            }
        }

        private static string[] CandidatePaths()
        {
            var list = new System.Collections.Generic.List<string>();
            var extDir = OpenCodeEnvironment.ExtensionDir;
            if (!string.IsNullOrEmpty(extDir))
                list.Add(Path.Combine(extDir, MarkerName));
            list.Add(Path.Combine(OpenCodeEnvironment.StudioDir, MarkerName));
            return list.ToArray();
        }
    }
}
