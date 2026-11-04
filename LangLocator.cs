using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ScLang
{
    internal static class LangLocator
    {
        public static List<string> FindAll()
        {
            var found = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var roots = new List<string>();

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string personal = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            roots.Add(Path.Combine(appData, "EXBO"));
            roots.Add(Path.Combine(localData, "EXBO"));
            roots.Add(Path.Combine(programFiles, "EXBO"));
            roots.Add(Path.Combine(programFilesX86, "EXBO"));
            roots.Add(Path.Combine(personal, "EXBO"));
            roots.Add(desktop);

            foreach (string root in roots)
            {
                if (!Directory.Exists(root)) continue;
                int maxDepth = string.Equals(root, desktop, StringComparison.OrdinalIgnoreCase) ? 2 : 10;
                Scan(root, 0, maxDepth, found, seen);
            }

            found.Sort(delegate(string a, string b)
            {
                int scoreA = ScorePath(a);
                int scoreB = ScorePath(b);
                if (scoreA != scoreB) return scoreB.CompareTo(scoreA);
                return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            });
            return found;
        }

        public static string ChoosePreferred(List<string> found, string current, string lastFile)
        {
            if (found == null || found.Count == 0) return null;
            if (!string.IsNullOrEmpty(current) && found.Contains(current, StringComparer.OrdinalIgnoreCase)) return current;
            if (!string.IsNullOrEmpty(lastFile) && File.Exists(lastFile) && found.Contains(lastFile, StringComparer.OrdinalIgnoreCase)) return lastFile;
            string russian = found.Find(delegate(string p)
            {
                return string.Equals(Path.GetFileName(p), "ru.lang", StringComparison.OrdinalIgnoreCase);
            });
            return russian ?? found[0];
        }

        public static int ScorePath(string path)
        {
            int score = 0;
            string lower = path.ToLowerInvariant();
            if (lower.Contains("stalcraft")) score += 5;
            if (lower.Contains("stalker") && lower.Contains("lang")) score += 10;
            if (string.Equals(Path.GetFileName(path), "ru.lang", StringComparison.OrdinalIgnoreCase)) score += 20;
            return score;
        }

        private static void Scan(string dir, int depth, int maxDepth, List<string> found, HashSet<string> seen)
        {
            if (depth > maxDepth) return;
            try
            {
                foreach (string file in Directory.GetFiles(dir, "*.lang"))
                {
                    if (seen.Add(file)) found.Add(file);
                }
                foreach (string sub in Directory.GetDirectories(dir))
                {
                    try
                    {
                        FileAttributes attributes = File.GetAttributes(sub);
                        if ((attributes & FileAttributes.ReparsePoint) != 0) continue;
                    }
                    catch { continue; }
                    Scan(sub, depth + 1, maxDepth, found, seen);
                }
            }
            catch
            {
                // закрытые каталоги пропускаем
            }
        }
    }
}
