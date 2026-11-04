using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ScLang
{
    internal sealed class CustomPreset
    {
        public string Name = "Мой пресет";
        public string FilePath;
        public readonly List<PresetRule> Rules = new List<PresetRule>();
        public readonly Dictionary<string, string> Exact = new Dictionary<string, string>(StringComparer.Ordinal);

        public static string DirectoryPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "custom_presets"); }
        }

        public static List<CustomPreset> LoadAll()
        {
            var result = new List<CustomPreset>();
            try
            {
                if (!Directory.Exists(DirectoryPath)) Directory.CreateDirectory(DirectoryPath);
                string[] files = Directory.GetFiles(DirectoryPath, "*.preset");
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                foreach (string file in files)
                {
                    CustomPreset preset = Load(file);
                    if (preset != null) result.Add(preset);
                }
            }
            catch
            {
            }
            return result;
        }

        public static CustomPreset Load(string path)
        {
            try
            {
                var preset = new CustomPreset();
                preset.FilePath = path;
                preset.Name = Path.GetFileNameWithoutExtension(path);
                string section = "rules";
                foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        section = line.Substring(1, line.Length - 2).Trim().ToLowerInvariant();
                        continue;
                    }
                    if (section == "meta" || line.StartsWith("name=", StringComparison.OrdinalIgnoreCase))
                    {
                        int eqName = line.IndexOf('=');
                        if (eqName > 0) preset.Name = line.Substring(eqName + 1).Trim();
                        continue;
                    }
                    if (section == "rules")
                    {
                        int arrow = line.IndexOf("=>", StringComparison.Ordinal);
                        if (arrow <= 0) continue;
                        string from = line.Substring(0, arrow).Trim();
                        string rest = line.Substring(arrow + 2).Trim();
                        string to = rest;
                        string except = null;
                        int ex = rest.IndexOf("||", StringComparison.Ordinal);
                        if (ex >= 0)
                        {
                            to = rest.Substring(0, ex).Trim();
                            except = rest.Substring(ex + 2).Trim();
                        }
                        if (from.Length > 0) preset.Rules.Add(new PresetRule { From = from, To = to, Except = except });
                    }
                    else if (section == "exact")
                    {
                        int eq = line.IndexOf('=');
                        if (eq > 0)
                        {
                            string key = line.Substring(0, eq).Trim();
                            string value = line.Substring(eq + 1).Trim();
                            if (key.Length > 0) preset.Exact[key] = value;
                        }
                    }
                }
                return preset;
            }
            catch
            {
                return null;
            }
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(FilePath))
            {
                Directory.CreateDirectory(DirectoryPath);
                string baseName = Sanitize(Name);
                string path = Path.Combine(DirectoryPath, baseName + ".preset");
                int index = 2;
                while (File.Exists(path))
                {
                    path = Path.Combine(DirectoryPath, baseName + " (" + index + ").preset");
                    index++;
                }
                FilePath = path;
            }

            var sb = new StringBuilder();
            sb.AppendLine("# SC Lang custom preset");
            sb.AppendLine("name=" + Name);
            sb.AppendLine("[rules]");
            foreach (PresetRule rule in Rules)
            {
                sb.Append(rule.From).Append(" => ").Append(rule.To);
                if (!string.IsNullOrEmpty(rule.Except)) sb.Append(" || ").Append(rule.Except);
                sb.AppendLine();
            }
            sb.AppendLine("[exact]");
            foreach (KeyValuePair<string, string> pair in Exact)
            {
                sb.Append(pair.Key).Append('=').AppendLine(pair.Value);
            }
            File.WriteAllText(FilePath, sb.ToString(), new UTF8Encoding(false));
        }

        public void Delete()
        {
            try
            {
                if (!string.IsNullOrEmpty(FilePath) && File.Exists(FilePath)) File.Delete(FilePath);
            }
            catch
            {
            }
        }

        public CustomPreset Clone(string name)
        {
            var copy = new CustomPreset { Name = name };
            foreach (PresetRule rule in Rules)
                copy.Rules.Add(new PresetRule { From = rule.From, To = rule.To, Except = rule.Except });
            foreach (KeyValuePair<string, string> pair in Exact) copy.Exact[pair.Key] = pair.Value;
            return copy;
        }

        private static string Sanitize(string name)
        {
            string value = string.IsNullOrEmpty(name) ? "preset" : name.Trim();
            foreach (char c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_');
            value = value.Replace(' ', '_');
            if (value.Length > 80) value = value.Substring(0, 80);
            return value.Length == 0 ? "preset" : value;
        }
    }

    internal sealed class PresetManagerForm : Form
    {
        private readonly ListBox _list = new ListBox();
        private List<CustomPreset> _presets;

        public PresetManagerForm()
        {
            Text = "Свои пресеты";
            Width = 700;
            Height = 480;
            MinimumSize = new Size(560, 360);
            StartPosition = FormStartPosition.CenterParent;
            try { Font = new Font("Segoe UI", 9f); } catch { }

            var hint = new Label
            {
                Text = "Свои пресеты хранятся в папке custom_presets рядом с программой.",
                Dock = DockStyle.Top,
                Height = 32,
                Padding = new Padding(10, 8, 10, 0)
            };

            _list.Dock = DockStyle.Fill;
            _list.IntegralHeight = false;
            _list.DoubleClick += delegate { EditSelected(); };

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 170,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(8),
                WrapContents = false
            };
            AddButton(buttons, "Новый", delegate { NewPreset(); });
            AddButton(buttons, "Изменить", delegate { EditSelected(); });
            AddButton(buttons, "Дублировать", delegate { DuplicateSelected(); });
            AddButton(buttons, "Удалить", delegate { DeleteSelected(); });
            AddButton(buttons, "Закрыть", delegate { Close(); });

            Controls.Add(_list);
            Controls.Add(buttons);
            Controls.Add(hint);

            ThemeRuntime.Apply(this);
            Reload();
        }

        private static void AddButton(Control parent, string text, EventHandler handler)
        {
            var button = new Button { Text = text, Width = 145, Height = 31, Margin = new Padding(0, 0, 0, 6) };
            button.Click += handler;
            parent.Controls.Add(button);
        }

        private void Reload()
        {
            _presets = CustomPreset.LoadAll();
            _list.Items.Clear();
            foreach (CustomPreset preset in _presets) _list.Items.Add(preset.Name);
        }

        private CustomPreset Selected
        {
            get
            {
                int index = _list.SelectedIndex;
                if (index < 0 || index >= _presets.Count) return null;
                return _presets[index];
            }
        }

        private void NewPreset()
        {
            using (var editor = new PresetEditorForm(null))
            {
                if (editor.ShowDialog(this) == DialogResult.OK) Reload();
            }
        }

        private void EditSelected()
        {
            CustomPreset selected = Selected;
            if (selected == null) return;
            using (var editor = new PresetEditorForm(selected))
            {
                if (editor.ShowDialog(this) == DialogResult.OK) Reload();
            }
        }

        private void DuplicateSelected()
        {
            CustomPreset selected = Selected;
            if (selected == null) return;
            using (var editor = new PresetEditorForm(selected.Clone(selected.Name + " (копия)")))
            {
                if (editor.ShowDialog(this) == DialogResult.OK) Reload();
            }
        }

        private void DeleteSelected()
        {
            CustomPreset selected = Selected;
            if (selected == null) return;
            if (MessageBox.Show(this, "Удалить пресет \"" + selected.Name + "\"?", "Свои пресеты",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            selected.Delete();
            Reload();
        }
    }

    internal sealed class PresetEditorForm : Form
    {
        private readonly CustomPreset _preset;
        private readonly TextBox _nameBox = new TextBox();
        private readonly TextBox _rulesBox = new TextBox();
        private readonly TextBox _exactBox = new TextBox();

        public PresetEditorForm(CustomPreset preset)
        {
            _preset = preset ?? new CustomPreset();
            Text = preset == null || string.IsNullOrEmpty(preset.FilePath) ? "Новый пресет" : "Изменить пресет";
            Width = 760;
            Height = 600;
            MinimumSize = new Size(600, 420);
            StartPosition = FormStartPosition.CenterParent;
            try { Font = new Font("Segoe UI", 9f); } catch { }

            var nameLabel = new Label { Text = "Название:", Left = 12, Top = 14, Width = 80, TextAlign = ContentAlignment.MiddleLeft };
            _nameBox.Left = 96; _nameBox.Top = 11; _nameBox.Width = 500;
            _nameBox.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            _nameBox.Text = _preset.Name;

            var tabs = new TabControl { Left = 12, Top = 46, Width = 720, Height = 450 };
            tabs.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;

            var rulesPage = new TabPage("Замены");
            var rulesHint = new Label
            {
                Text = "По одной строке: Было => Стало. Исключение: Было => Стало || текст-исключение",
                Dock = DockStyle.Top,
                Height = 28,
                Padding = new Padding(0, 7, 0, 0)
            };
            _rulesBox.Multiline = true;
            _rulesBox.ScrollBars = ScrollBars.Both;
            _rulesBox.WordWrap = false;
            _rulesBox.Dock = DockStyle.Fill;
            _rulesBox.Text = RulesToText(_preset.Rules);
            rulesPage.Controls.Add(_rulesBox);
            rulesPage.Controls.Add(rulesHint);

            var exactPage = new TabPage("Точные значения");
            var exactHint = new Label
            {
                Text = "По одной строке: ключ=значение. Применяется после замен.",
                Dock = DockStyle.Top,
                Height = 28,
                Padding = new Padding(0, 7, 0, 0)
            };
            _exactBox.Multiline = true;
            _exactBox.ScrollBars = ScrollBars.Both;
            _exactBox.WordWrap = false;
            _exactBox.Dock = DockStyle.Fill;
            _exactBox.Text = ExactToText(_preset.Exact);
            exactPage.Controls.Add(_exactBox);
            exactPage.Controls.Add(exactHint);

            tabs.TabPages.Add(rulesPage);
            tabs.TabPages.Add(exactPage);

            var save = new Button { Text = "Сохранить", Left = 600, Top = 512, Width = 130, Height = 32, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            save.Click += delegate { SavePreset(); };
            var cancel = new Button { Text = "Отмена", Left = 480, Top = 512, Width = 110, Height = 32, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(nameLabel);
            Controls.Add(_nameBox);
            Controls.Add(tabs);
            Controls.Add(save);
            Controls.Add(cancel);
            AcceptButton = save;
            CancelButton = cancel;

            ThemeRuntime.Apply(this);
        }

        private void SavePreset()
        {
            string name = _nameBox.Text.Trim();
            if (name.Length == 0)
            {
                MessageBox.Show(this, "Укажите название пресета.", "Пресет", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _nameBox.Focus();
                return;
            }

            string ruleError;
            List<PresetRule> rules = ParseRules(_rulesBox.Text, out ruleError);
            if (ruleError != null)
            {
                MessageBox.Show(this, ruleError, "Пресет", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string exactError;
            Dictionary<string, string> exact = ParseExact(_exactBox.Text, out exactError);
            if (exactError != null)
            {
                MessageBox.Show(this, exactError, "Пресет", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (rules.Count == 0 && exact.Count == 0)
            {
                MessageBox.Show(this, "Добавьте хотя бы одну замену или точное значение.", "Пресет",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _preset.Name = name;
            _preset.Rules.Clear();
            _preset.Rules.AddRange(rules);
            _preset.Exact.Clear();
            foreach (KeyValuePair<string, string> pair in exact) _preset.Exact[pair.Key] = pair.Value;
            _preset.Save();
            DialogResult = DialogResult.OK;
            Close();
        }

        private static List<PresetRule> ParseRules(string text, out string error)
        {
            error = null;
            var result = new List<PresetRule>();
            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                int arrow = line.IndexOf("=>", StringComparison.Ordinal);
                if (arrow <= 0)
                {
                    error = "Строка " + (i + 1) + ": нужен формат \"Было => Стало\".";
                    return result;
                }
                string from = line.Substring(0, arrow).Trim();
                string rest = line.Substring(arrow + 2).Trim();
                string to = rest;
                string except = null;
                int ex = rest.IndexOf("||", StringComparison.Ordinal);
                if (ex >= 0)
                {
                    to = rest.Substring(0, ex).Trim();
                    except = rest.Substring(ex + 2).Trim();
                }
                if (from.Length == 0)
                {
                    error = "Строка " + (i + 1) + ": пустая левая часть.";
                    return result;
                }
                result.Add(new PresetRule { From = from, To = to, Except = except });
            }
            return result;
        }

        private static Dictionary<string, string> ParseExact(string text, out string error)
        {
            error = null;
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    error = "Строка " + (i + 1) + ": нужен формат \"ключ=значение\".";
                    return result;
                }
                result[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
            }
            return result;
        }

        private static string RulesToText(List<PresetRule> rules)
        {
            var sb = new StringBuilder();
            foreach (PresetRule rule in rules)
            {
                sb.Append(rule.From).Append(" => ").Append(rule.To);
                if (!string.IsNullOrEmpty(rule.Except)) sb.Append(" || ").Append(rule.Except);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private static string ExactToText(Dictionary<string, string> exact)
        {
            var sb = new StringBuilder();
            foreach (KeyValuePair<string, string> pair in exact)
                sb.Append(pair.Key).Append('=').AppendLine(pair.Value);
            return sb.ToString();
        }
    }
}
