using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;

namespace ScLang
{
    internal sealed class AppSettings
    {
        public string Theme = "light";
        public string CustomBack = "#14181C";
        public string CustomPanel = "#1C232B";
        public string CustomCell = "#202831";
        public string CustomText = "#E8EEF4";
        public string CustomAccent = "#3B82A0";
        public string CustomImage = "";
        public bool AnimateGif = true;
        public bool BackupOnSave = true;
        public bool ShowPreview = true;
        public bool WelcomeShown = false;
        public string LastFile = "";
        public int WindowWidth = 1240;
        public int WindowHeight = 820;

        public static string FilePath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.ini"); }
        }

        public static AppSettings Load()
        {
            var result = new AppSettings();
            try
            {
                if (!File.Exists(FilePath)) return result;
                foreach (string raw in File.ReadAllLines(FilePath, Encoding.UTF8))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";")) continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();
                    switch (key.ToLowerInvariant())
                    {
                        case "theme": result.Theme = value; break;
                        case "customback": result.CustomBack = value; break;
                        case "custompanel": result.CustomPanel = value; break;
                        case "customcell": result.CustomCell = value; break;
                        case "customtext": result.CustomText = value; break;
                        case "customaccent": result.CustomAccent = value; break;
                        case "customimage": result.CustomImage = value; break;
                        case "animategif": result.AnimateGif = ParseBool(value, true); break;
                        case "backuponsave": result.BackupOnSave = ParseBool(value, true); break;
                        case "showpreview": result.ShowPreview = ParseBool(value, true); break;
                        case "welcomeshown": result.WelcomeShown = ParseBool(value, false); break;
                        case "lastfile": result.LastFile = value; break;
                        case "windowwidth": result.WindowWidth = ParseInt(value, 1240); break;
                        case "windowheight": result.WindowHeight = ParseInt(value, 820); break;
                    }
                }
            }
            catch
            {
            }
            return result;
        }

        public void Save()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("theme=" + Theme);
                sb.AppendLine("customback=" + CustomBack);
                sb.AppendLine("custompanel=" + CustomPanel);
                sb.AppendLine("customcell=" + CustomCell);
                sb.AppendLine("customtext=" + CustomText);
                sb.AppendLine("customaccent=" + CustomAccent);
                sb.AppendLine("customimage=" + CustomImage);
                sb.AppendLine("animategif=" + (AnimateGif ? "1" : "0"));
                sb.AppendLine("backuponsave=" + (BackupOnSave ? "1" : "0"));
                sb.AppendLine("showpreview=" + (ShowPreview ? "1" : "0"));
                sb.AppendLine("welcomeshown=" + (WelcomeShown ? "1" : "0"));
                sb.AppendLine("lastfile=" + LastFile);
                sb.AppendLine("windowwidth=" + WindowWidth.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("windowheight=" + WindowHeight.ToString(CultureInfo.InvariantCulture));
                File.WriteAllText(FilePath, sb.ToString(), new UTF8Encoding(false));
            }
            catch
            {
            }
        }

        public AppSettings Clone()
        {
            var copy = new AppSettings();
            copy.CopyFrom(this);
            return copy;
        }

        public void CopyFrom(AppSettings other)
        {
            Theme = other.Theme;
            CustomBack = other.CustomBack;
            CustomPanel = other.CustomPanel;
            CustomCell = other.CustomCell;
            CustomText = other.CustomText;
            CustomAccent = other.CustomAccent;
            CustomImage = other.CustomImage;
            AnimateGif = other.AnimateGif;
            BackupOnSave = other.BackupOnSave;
            ShowPreview = other.ShowPreview;
            WelcomeShown = other.WelcomeShown;
            LastFile = other.LastFile;
            WindowWidth = other.WindowWidth;
            WindowHeight = other.WindowHeight;
        }

        public static Color ColorOf(string value, Color fallback)
        {
            try
            {
                if (string.IsNullOrEmpty(value)) return fallback;
                return ColorTranslator.FromHtml(value);
            }
            catch
            {
                return fallback;
            }
        }

        public static string ToHex(Color color)
        {
            return "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }

        private static bool ParseBool(string value, bool fallback)
        {
            if (value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (value == "0" || string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)) return false;
            bool parsed;
            return bool.TryParse(value, out parsed) ? parsed : fallback;
        }

        private static int ParseInt(string value, int fallback)
        {
            int parsed;
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) ? parsed : fallback;
        }
    }
}
