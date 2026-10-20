using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices;

namespace mydesktop
{
    public class AppInfo
    {
        public string Alias { get; set; } = "";
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string Type { get; set; } = "";
    }

    public class AppLibrary
    {
        public List<AppInfo> Apps { get; set; } = new();
    }

    public static class AppDiscovery
    {

        public static List<AppInfo> FindApplications()
        {
            List<AppInfo> apps = new List<AppInfo>();

            // Tracks file paths we've already discovered.
            HashSet<string> discoveredPaths =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Desktop
            string desktop = Environment.GetFolderPath(
                Environment.SpecialFolder.DesktopDirectory);

            ScanFolder(desktop, apps, discoveredPaths);

            // 2. All users Start Menu
            string allUsersStartMenu =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.CommonPrograms);

            ScanFolder(allUsersStartMenu, apps, discoveredPaths);

            // 3. Current user's Start Menu
            string userStartMenu =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.Programs);

            ScanFolder(userStartMenu, apps, discoveredPaths);

            return apps;
        }



        private static void ScanFolder(
            string folder,
            List<AppInfo> apps,
            HashSet<string> discoveredPaths)
        {
            if (!Directory.Exists(folder))
                return;

            foreach (string file in Directory.GetFiles(
                folder,
                "*.*",
                SearchOption.AllDirectories))
            {
                string extension =
                    Path.GetExtension(file).ToLowerInvariant();

                if (extension != ".lnk" &&
                    extension != ".url" &&
                    extension != ".exe")
                {
                    continue;
                }

                string fullPath = Path.GetFullPath(file);

                // Skip a file path we've already seen.
                if (discoveredPaths.Contains(fullPath))
                    continue;

                // Resolve Windows shortcuts to their actual targets.
                string identityPath = extension == ".lnk"
                    ? GetShortcutTarget(fullPath)
                    : fullPath;

                identityPath = Path.GetFullPath(identityPath);

                // A different shortcut already points to this target.
                if (discoveredPaths.Contains(identityPath))
                {
                    discoveredPaths.Add(fullPath);
                    continue;
                }

                // Remember both the shortcut and its target.
                discoveredPaths.Add(fullPath);
                discoveredPaths.Add(identityPath);

                apps.Add(new AppInfo
                {
                    Name = Path.GetFileNameWithoutExtension(fullPath),
                    Path = fullPath,
                    Type = extension
                });
            }
        }


        private static string GetShortcutTarget(string shortcutPath)
        {
            try
            {
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");

                if (shellType == null)
                    return shortcutPath;

                object? shell = Activator.CreateInstance(shellType);

                if (shell == null)
                    return shortcutPath;

                try
                {
                    dynamic shortcut = shellType.InvokeMember(
                        "CreateShortcut",
                        System.Reflection.BindingFlags.InvokeMethod,
                        null,
                        shell,
                        new object[] { shortcutPath })!;

                    string? target = shortcut.TargetPath;

                    return !string.IsNullOrWhiteSpace(target)
                        ? Path.GetFullPath(target)
                        : shortcutPath;
                }
                finally
                {
                    Marshal.ReleaseComObject(shell);
                }
            }
            catch
            {
                return shortcutPath;
            }
        }




        public static void SaveApplications()
        {
            var library = new AppLibrary
            {
                Apps = FindApplications()
            };

            string json = JsonSerializer.Serialize(
                library,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            string libraryFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"mydesktop");

            Directory.CreateDirectory(libraryFolder);

            string filePath = Path.Combine(
                libraryFolder,
                "AppLibrary.json");

            File.WriteAllText(filePath, json);
        }
    }
}