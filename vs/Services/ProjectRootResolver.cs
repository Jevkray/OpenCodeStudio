using System;
using System.IO;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace OpenCodeStudio.Services
{
    public class ProjectRootResolver : IProjectRootResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public ProjectRootResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public string ResolveProjectRoot()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string solutionDir = GetSolutionDirectory();

            // Try git root from solution directory (higher priority)
            string gitRoot = FindGitRoot(solutionDir);
            if (!string.IsNullOrEmpty(gitRoot))
            {
                return NormalizePath(gitRoot);
            }

            // Fallback to solution directory
            if (!string.IsNullOrEmpty(solutionDir))
            {
                return NormalizePath(solutionDir);
            }

            // Try git root from user profile directory
            gitRoot = FindGitRoot(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            if (!string.IsNullOrEmpty(gitRoot))
            {
                return NormalizePath(gitRoot);
            }

            // Use user documents directory as last resort
            return NormalizePath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        }

        private string GetSolutionDirectory()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var svc = _serviceProvider;

                var dte = (svc?.GetService(typeof(DTE)) as DTE2) ??
                    Package.GetGlobalService(typeof(DTE)) as DTE2;
                if (dte?.Solution != null && !string.IsNullOrEmpty(dte.Solution.FullName))
                {
                    return Path.GetDirectoryName(dte.Solution.FullName);
                }
            }
            catch
            {
                // DTE may not be available (e.g., in tests)
            }

            return null;
        }

        private static string FindGitRoot(string startDir)
        {
            if (string.IsNullOrEmpty(startDir))
                return null;

            string current = startDir;

            while (!string.IsNullOrEmpty(current))
            {
                string gitPath = Path.Combine(current, ".git");
                if (Directory.Exists(gitPath) || File.Exists(gitPath))
                {
                    return current;
                }

                string parent = Path.GetDirectoryName(current);
                if (parent == current)
                    break; // Reached root
                current = parent;
            }

            return null;
        }

        /// <summary>
        /// Normalizes a path to use forward slashes and ensures it's a full path.
        /// </summary>
        public static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            string fullPath = Path.GetFullPath(path);
            return fullPath.Replace('\\', '/');
        }
    }
}
