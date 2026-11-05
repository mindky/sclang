using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScLang
{
    internal sealed class ThemePalette
    {
        public Color Form;
        public Color Panel;
        public Color Cell;
        public Color Grid;
        public Color AltCell;
        public Color Text;
        public Color Header;
        public Color HeaderText;
        public Color Selection;
        public Color SelectionText;
        public Color Button;
        public Color ButtonText;
        public Color Border;
        public Color Accent;

        public static ThemePalette FromSettings(AppSettings settings)
        {
            if (settings == null) return Light();
            switch ((settings.Theme ?? "light").ToLowerInvariant())
            {
                case "dark": return Dark();
                case "oled": return Oled();
                case "exbo": return Exbo();
                case "exbolight": return ExboLight();
                case "exbodark": return ExboDark();
                case "stalzone": return Stalzone();
                case "custom": return Custom(settings);
                default: return Light();
            }
        }

        public static ThemePalette Light()
        {
            return new ThemePalette
            {
                Form = Color.FromArgb(242, 244, 247),
                Panel = Color.FromArgb(250, 251, 252),
                Cell = Color.White,
                Grid = Color.FromArgb(232, 235, 239),
                AltCell = Color.FromArgb(247, 249, 251),
                Text = Color.FromArgb(28, 32, 38),
                Header = Color.FromArgb(230, 234, 239),
                HeaderText = Color.FromArgb(30, 34, 40),
                Selection = Color.FromArgb(198, 224, 245),
                SelectionText = Color.FromArgb(16, 22, 30),
                Button = Color.FromArgb(250, 251, 252),
                ButtonText = Color.FromArgb(28, 32, 38),
                Border = Color.FromArgb(190, 197, 206),
                Accent = Color.FromArgb(198, 224, 245)
            };
        }

        public static ThemePalette Dark()
        {
            return new ThemePalette
            {
                Form = Color.FromArgb(30, 31, 34),
                Panel = Color.FromArgb(38, 40, 44),
                Cell = Color.FromArgb(35, 37, 41),
                Grid = Color.FromArgb(27, 28, 31),
                AltCell = Color.FromArgb(40, 42, 47),
                Text = Color.FromArgb(230, 230, 230),
                Header = Color.FromArgb(46, 49, 54),
                HeaderText = Color.FromArgb(242, 242, 242),
                Selection = Color.FromArgb(58, 90, 120),
                SelectionText = Color.White,
                Button = Color.FromArgb(48, 52, 58),
                ButtonText = Color.FromArgb(240, 240, 240),
                Border = Color.FromArgb(68, 74, 82),
                Accent = Color.FromArgb(58, 90, 120)
            };
        }

        public static ThemePalette Oled()
        {
            return new ThemePalette
            {
                Form = Color.Black,
                Panel = Color.Black,
                Cell = Color.Black,
                Grid = Color.Black,
                AltCell = Color.FromArgb(8, 8, 8),
                Text = Color.White,
                Header = Color.FromArgb(12, 12, 12),
                HeaderText = Color.White,
                Selection = Color.FromArgb(0, 70, 82),
                SelectionText = Color.White,
                Button = Color.FromArgb(10, 10, 10),
                ButtonText = Color.White,
                Border = Color.FromArgb(45, 45, 45),
                Accent = Color.FromArgb(0, 120, 140)
            };
        }

        public static ThemePalette Exbo()
        {
            return new ThemePalette
            {
                Form = Color.FromArgb(18, 22, 28),
                Panel = Color.FromArgb(26, 32, 40),
                Cell = Color.FromArgb(34, 42, 52),
                Grid = Color.FromArgb(15, 19, 24),
                AltCell = Color.FromArgb(30, 38, 48),
                Text = Color.FromArgb(230, 237, 244),
                Header = Color.FromArgb(42, 52, 64),
                HeaderText = Color.FromArgb(240, 245, 250),
                Selection = Color.FromArgb(36, 72, 107),
                SelectionText = Color.White,
                Button = Color.FromArgb(51, 59, 69),
                ButtonText = Color.FromArgb(240, 245, 250),
                Border = Color.FromArgb(70, 81, 94),
                Accent = Color.FromArgb(182, 58, 58)
            };
        }

        public static ThemePalette ExboLight()
        {
            return new ThemePalette
            {
                Form = Color.FromArgb(246, 250, 253),
                Panel = Color.FromArgb(255, 255, 255),
                Cell = Color.White,
                Grid = Color.FromArgb(233, 236, 239),
                AltCell = Color.FromArgb(241, 246, 251),
                Text = Color.FromArgb(33, 37, 41),
                Header = Color.FromArgb(228, 242, 252),
                HeaderText = Color.FromArgb(11, 42, 58),
                Selection = Color.FromArgb(64, 198, 255),
                SelectionText = Color.FromArgb(8, 51, 68),
                Button = Color.White,
                ButtonText = Color.FromArgb(33, 37, 41),
                Border = Color.FromArgb(222, 226, 230),
                Accent = Color.FromArgb(64, 198, 255)
            };
        }

        public static ThemePalette ExboDark()
        {
            return new ThemePalette
            {
                Form = Color.FromArgb(33, 37, 41),
                Panel = Color.FromArgb(43, 48, 53),
                Cell = Color.FromArgb(38, 43, 47),
                Grid = Color.FromArgb(24, 27, 29),
                AltCell = Color.FromArgb(44, 47, 51),
                Text = Color.FromArgb(236, 246, 255),
                Header = Color.FromArgb(52, 58, 64),
                HeaderText = Color.FromArgb(240, 248, 255),
                Selection = Color.FromArgb(64, 198, 255),
                SelectionText = Color.FromArgb(4, 34, 46),
                Button = Color.FromArgb(52, 58, 64),
                ButtonText = Color.FromArgb(240, 248, 255),
                Border = Color.FromArgb(73, 80, 87),
                Accent = Color.FromArgb(64, 198, 255)
            };
        }

        public static ThemePalette Stalzone()
        {
            return new ThemePalette
            {
                Form = Color.FromArgb(10, 10, 10),
                Panel = Color.FromArgb(18, 18, 18),
                Cell = Color.FromArgb(24, 24, 24),
                Grid = Color.FromArgb(0, 0, 0),
                AltCell = Color.FromArgb(26, 26, 26),
                Text = Color.FromArgb(236, 231, 225),
                Header = Color.FromArgb(31, 31, 31),
                HeaderText = Color.FromArgb(236, 231, 225),
                Selection = Color.FromArgb(233, 25, 50),
                SelectionText = Color.White,
                Button = Color.FromArgb(24, 24, 24),
                ButtonText = Color.FromArgb(236, 231, 225),
                Border = Color.FromArgb(46, 45, 45),
                Accent = Color.FromArgb(233, 25, 50)
            };
        }

        public static ThemePalette Custom(AppSettings settings)
        {
            Color back = AppSettings.ColorOf(settings.CustomBack, Color.FromArgb(20, 24, 28));
            Color panel = AppSettings.ColorOf(settings.CustomPanel, Color.FromArgb(28, 35, 43));
            Color cell = AppSettings.ColorOf(settings.CustomCell, Color.FromArgb(32, 40, 49));
            Color text = AppSettings.ColorOf(settings.CustomText, Color.FromArgb(232, 238, 244));
            Color accent = AppSettings.ColorOf(settings.CustomAccent, Color.FromArgb(59, 130, 160));

            return new ThemePalette
            {
                Form = back,
                Panel = panel,
                Cell = cell,
                Grid = Mix(cell, back, 0.45),
                AltCell = Mix(cell, panel, 0.25),
                Text = text,
                Header = Mix(panel, cell, 0.35),
                HeaderText = text,
                Selection = accent,
                SelectionText = Contrast(accent),
                Button = Mix(panel, cell, 0.35),
                ButtonText = text,
                Border = Mix(cell, text, 0.25),
                Accent = accent
            };
        }

        public static Color Mix(Color a, Color b, double amountB)
        {
            amountB = Math.Max(0, Math.Min(1, amountB));
            double amountA = 1 - amountB;
            return Color.FromArgb(
                (int)Math.Round(a.R * amountA + b.R * amountB),
                (int)Math.Round(a.G * amountA + b.G * amountB),
                (int)Math.Round(a.B * amountA + b.B * amountB));
        }

        public static Color Adjust(Color color, double factor)
        {
            if (factor < 0)
            {
                double f = 1 + factor;
                return Color.FromArgb((int)(color.R * f), (int)(color.G * f), (int)(color.B * f));
            }
            return Mix(color, Color.White, factor);
        }

        public static Color Contrast(Color background)
        {
            return Luminance(background) < 0.55 ? Color.White : Color.Black;
        }

        public static double Luminance(Color color)
        {
            return (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;
        }

        public static Color AverageColor(Image image)
        {
            if (image == null) return Color.FromArgb(32, 40, 49);
            try
            {
                using (var small = new Bitmap(16, 16))
                {
                    using (var g = Graphics.FromImage(small))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                        g.DrawImage(image, 0, 0, 16, 16);
                    }
                    long r = 0, g2 = 0, b = 0;
                    for (int y = 0; y < 16; y++)
                    {
                        for (int x = 0; x < 16; x++)
                        {
                            Color c = small.GetPixel(x, y);
                            r += c.R; g2 += c.G; b += c.B;
                        }
                    }
                    int count = 256;
                    return Color.FromArgb((int)(r / count), (int)(g2 / count), (int)(b / count));
                }
            }
            catch
            {
                return Color.FromArgb(32, 40, 49);
            }
        }
    }

    internal static class ThemeRuntime
    {
        public static ThemePalette Current = ThemePalette.Light();

        public static void Apply(Control root)
        {
            Apply(root, Current);
        }

        public static void Apply(Control root, ThemePalette palette)
        {
            if (root == null || palette == null) return;
            Current = palette;

            var form = root as Form;
            if (form != null) form.BackColor = palette.Form;
            else
            {
                var panel = root as Panel;
                if (panel != null) panel.BackColor = palette.Panel;
                else
                {
                    var page = root as TabPage;
                    if (page != null) page.BackColor = palette.Panel;
                }
            }

            var label = root as Label;
            if (label != null)
            {
                label.ForeColor = palette.Text;
                label.BackColor = Color.Transparent;
            }

            var check = root as CheckBox;
            if (check != null)
            {
                check.ForeColor = palette.Text;
                check.BackColor = Color.Transparent;
            }

            var button = root as Button;
            if (button != null)
            {
                button.FlatStyle = FlatStyle.Flat;
                button.BackColor = palette.Button;
                button.ForeColor = palette.ButtonText;
                button.FlatAppearance.BorderColor = palette.Border;
                button.FlatAppearance.MouseOverBackColor = palette.Selection;
                button.FlatAppearance.MouseDownBackColor = ThemePalette.Mix(palette.Selection, palette.Button, 0.25);
            }

            var textBox = root as TextBox;
            if (textBox != null)
            {
                textBox.BackColor = palette.Cell;
                textBox.ForeColor = palette.Text;
                textBox.BorderStyle = BorderStyle.FixedSingle;
            }

            var combo = root as ComboBox;
            if (combo != null)
            {
                combo.BackColor = palette.Cell;
                combo.ForeColor = palette.Text;
                combo.FlatStyle = FlatStyle.Flat;
            }

            var listBox = root as ListBox;
            if (listBox != null)
            {
                listBox.BackColor = palette.Cell;
                listBox.ForeColor = palette.Text;
            }

            var listView = root as ListView;
            if (listView != null)
            {
                listView.BackColor = palette.Cell;
                listView.ForeColor = palette.Text;
            }

            var tabs = root as TabControl;
            if (tabs != null) tabs.BackColor = palette.Panel;

            var status = root as StatusStrip;
            if (status != null)
            {
                status.BackColor = palette.Panel;
                status.ForeColor = palette.Text;
                foreach (ToolStripItem item in status.Items) item.ForeColor = palette.Text;
            }

            var grid = root as DataGridView;
            if (grid != null)
            {
                grid.BackgroundColor = palette.Grid;
                grid.GridColor = palette.Border;
                grid.EnableHeadersVisualStyles = false;
                grid.ColumnHeadersDefaultCellStyle.BackColor = palette.Header;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = palette.HeaderText;
                grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = palette.Header;
                grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = palette.HeaderText;
                grid.DefaultCellStyle.BackColor = palette.Cell;
                grid.DefaultCellStyle.ForeColor = palette.Text;
                grid.DefaultCellStyle.SelectionBackColor = palette.Selection;
                grid.DefaultCellStyle.SelectionForeColor = palette.SelectionText;
                grid.AlternatingRowsDefaultCellStyle.BackColor = palette.AltCell;
                grid.AlternatingRowsDefaultCellStyle.ForeColor = palette.Text;
                grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = palette.Selection;
                grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = palette.SelectionText;
            }

            foreach (Control child in root.Controls) Apply(child, palette);
        }
    }
}
