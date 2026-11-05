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
        private readonly ComboBox _snippet = new ComboBox();
        private readonly ComboBox _snippetFilter = new ComboBox();
        private List<SnippetItem> _allSnippets;
        private TabControl _tabs;

        private sealed class SnippetItem
        {
            public string Text;
            public string InsertText;
            public bool IsExact;
            public bool IsGroup;
            public string[] Lines;

            public override string ToString()
            {
                return Text;
            }
        }

        private static List<SnippetItem> BuildFormGroups()
        {
            var groups = new List<SnippetItem>();
            AddGroup(groups, "classic", "Рубеж", "Долг", new string[]
            {
                "Рубеж => Долг", "Рубежа => Долга", "Рубежу => Долгу",
                "Рубежем => Долгом", "Рубеже => Долге", "Рубежи => Долги", "Рубежом => Долгом"
            });
            AddGroup(groups, "classic", "Заря", "Свобода", new string[]
            {
                "Заря => Свобода", "Зари => Свободы", "Заре => Свободе",
                "Зарю => Свободу", "Зарей => Свободой", "Зарёй => Свободой"
            });
            AddGroup(groups, "classic", "Обочина", "Кордон", new string[]
            {
                "Обочина => Кордон", "Обочины => Кордона", "Обочине => Кордоне",
                "Обочину => Кордон", "Обочиной => Кордоном", "Обочинам => Кордонам",
                "Обочинами => Кордонами", "Обочинах => Кордонах"
            });
            AddGroup(groups, "fun", "Приток", "Криветок", new string[]
            {
                "Приток => Криветок", "Притока => Криветка", "Притоку => Криветку",
                "Притоком => Криветком", "Притоке => Криветке"
            });
            AddGroup(groups, "fun", "Институт", "Инсульт", new string[]
            {
                "Институт => Инсульт", "Института => Инсульта", "Институту => Инсульту",
                "Институтом => Инсультом", "Институте => Инсульте", "Институты => Инсульты"
            });
            AddGroup(groups, "fun", "Кузня-11", "Кухня-11", new string[]
            {
                "Кузня-11 => Кухня-11", "Кузни-11 => Кухни-11", "Кузне-11 => Кухне-11",
                "Кузню-11 => Кухню-11", "Кузней-11 => Кухней-11"
            });
            AddGroup(groups, "fun", "Барьер", "БульБуль", new string[]
            {
                "Барьер => БульБуль", "Барьера => БульБуля", "Барьеру => БульБулю",
                "Барьером => БульБулем", "Барьере => БульБуле"
            });
            AddGroup(groups, "classic", "Бестия", "Химера", new string[]
            {
                "Бестия => Химера", "Бестии => Химеры", "Бестию => Химеру", "Бестией => Химерой"
            });
            AddGroup(groups, "classic", "Шавка", "Слепой пёс", new string[]
            {
                "Шавка => Слепой пёс", "Шавки => Слепого пса", "Шавку => Слепого пса",
                "Шавкой => Слепым псом", "Шавок => Слепых псов"
            });
            AddGroup(groups, "classic", "Хрюша", "Плоть", new string[]
            {
                "Хрюша => Плоть", "Хрюши => Плоти", "Хрюшу => Плоть",
                "Хрюшей => Плотью", "Хрюш => Плотей"
            });
            AddGroup(groups, "classic", "Упырь", "Кровосос", new string[]
            {
                "Упырь => Кровосос", "Упыря => Кровососа", "Упырю => Кровососу",
                "Упырём => Кровососом", "Упыре => Кровососе", "Упыри => Кровососы", "Упырей => Кровососов"
            });
            AddGroup(groups, "classic", "Дикая гончая", "Псевдопёс", new string[]
            {
                "Дикая гончая => Псевдопёс", "Дикой гончей => Псевдопса", "Дикую гончую => Псевдопса",
                "Дикие гончие => Псевдопсы", "Диких гончих => Псевдопсов"
            });
            AddGroup(groups, "classic", "Бурелом", "Псевдогигант", new string[]
            {
                "Бурелом => Псевдогигант", "Бурелома => Псевдогиганта", "Бурелому => Псевдогиганту",
                "Буреломом => Псевдогигантом", "Буреломе => Псевдогиганте", "Буреломы => Псевдогиганты"
            });
            AddGroup(groups, "classic", "Пси-гончая", "Пси-собака", new string[]
            {
                "Пси-гончая => Пси-собака", "Пси-гончей => Пси-собаки", "Пси-гончие => Пси-собаки"
            });
            AddGroup(groups, "classic", "Шнырь", "Снорк", new string[]
            {
                "Шнырь => Снорк", "Шныря => Снорка", "Шнырю => Снорку",
                "Шнырём => Снорком", "Шныре => Снорке", "Шныри => Снорки", "Шнырей => Снорков"
            });
            AddGroup(groups, "classic", "Крыса", "Тушкан", new string[]
            {
                "Крыса => Тушкан", "Крысы => Тушкана", "Крысу => Тушкана",
                "Крысой => Тушканом", "Крыс => Тушканов"
            });
            AddGroup(groups, "classic", "Свалка техники", "Свалка", new string[]
            {
                "Свалка техники => Свалка", "Свалки техники => Свалки", "Свалке техники => Свалке",
                "Свалку техники => Свалку", "Свалкой техники => Свалкой"
            });
            AddGroup(groups, "classic", "Храмовая топь", "Храбрая топь", new string[]
            {
                "Храмовая топь => Храбрая топь", "Храмовой топи => Храброй топи",
                "Храмовую топь => Храбрую топь", "Храмовой топью => Храброй топью"
            });
            AddGroup(groups, "classic", "Разряд", "Электра", new string[]
            {
                "«Разряд» => «Электра»", "«Разряда» => «Электры»", "«Разряду» => «Электре»",
                "«Разряде» => «Электре»", "«Разряды» => «Электры»", "«Разрядов» => «Электр»"
            });
            AddGroup(groups, "classic", "Зажигалка", "Жарка", new string[]
            {
                "«Зажигалка» => «Жарка»", "«Зажигалки» => «Жарки»", "«Зажигалку» => «Жарку»",
                "«Зажигалкой» => «Жаркой»", "«Зажигалке» => «Жарке»"
            });
            AddGroup(groups, "classic", "Волчок", "Карусель", new string[]
            {
                "«Волчок» => «Карусель»", "«Волчка» => «Карусели»", "«Волчку» => «Карусели»",
                "«Волчки» => «Карусели»", "«Волчков» => «Каруселей»"
            });
            AddGroup(groups, "classic", "Холодец", "Кисель", new string[]
            {
                "«Холодец» => «Кисель»", "«Холодца» => «Киселя»", "«Холодцу» => «Киселю»",
                "«Холодцы» => «Кисели»", "«Холодцов» => «Киселей»"
            });
            AddGroup(groups, "classic", "Чайник", "Пар", new string[]
            {
                "«Чайник» => «Пар»", "«Чайника» => «Пара»", "«Чайнику» => «Пару»", "«Чайнике» => «Паре»"
            });
            AddGroup(groups, "classic", "Нора", "Пузырь", new string[]
            {
                "«Нора» => «Пузырь»", "«Норы» => «Пузыря»", "«Нор» => «Пузырей»",
                "«Норе» => «Пузырю»", "«Норами» => «Пузырями»"
            });
            AddGroup(groups, "classic", "Омут", "Воронка", new string[]
            {
                "«Омут» => «Воронка»", "«Омута» => «Воронки»", "«Омуту» => «Воронке»",
                "«Омуте» => «Воронке»", "«Омуты» => «Воронки»"
            });
            AddNamedGroup(groups, "classic", "Старые названия артефактов", new string[]
            {
                "«Призрачный кристалл» => «Частотный кристалл»", "«Трещотка» => «Бенгальский огонь»", "«Комета» => «Вспышка»",
                "«Ледяной ежик» => «Снежинка»", "«Гиря» => «Пустышка»", "«Лампочка Ильича» => «Лунный свет»",
                "«Красный кристалл» => «Кристалл»", "«Огонек» => «Огненный шар»", "«Пиявка» => «Капля»",
                "«Ветка Калины» => «Мамины бусы»", "«Гребешок» => «Глаз»", "«Жар-птица» => «Пламя»",
                "«Ягодка» => «Кровь камня»", "«Сало» => «Ломоть мяса»", "«Сердце» => «Душа»",
                "«Гантель» => «Пружина»", "«Кислотный кристалл» => «Кислый кристалл»", "«Флегма» => «Слизь»",
                "«Скорлупа» => «Слизняк»", "«Чернильница» => «Слюда»", "«Змеиный глаз» => «Светляк»",
                "«Жвачка» => «Пузырь»", "«Ряска» => «Пленка»", "«Репях» => «Колючка»",
                "«Липкий репях» => «Кристальная колючка»", "«Ершик» => «Морской еж»", "«Ежик» => «Колобок»",
                "«Стальной Ежик» => «Стальной колобок»", "«Роза» => «Каменный цветок»", "«Цибуля» => «Медуза»",
                "«Проклятая роза» => «Темная медуза»", "«Белая роза» => «Ночная звезда»", "«Протоцибуля» => «Протомедуза»",
                "«Вехотка» => «Выверт»", "«Прима» => «Грави»", "«Креветка» => «Золотая рыбка»",
                "«Золотистая Прима» => «Золотистый грави»", "«Темный кристалл» => «Черный кристалл»"
            });
            return groups;
        }

        private static void AddNamedGroup(List<SnippetItem> groups, string category, string name, string[] lines)
        {
            var item = new SnippetItem
            {
                Text = "[" + Label(category) + "] " + name,
                Lines = lines,
                IsGroup = true
            };
            groups.Add(item);
        }

        private static void AddGroup(List<SnippetItem> groups, string category, string from, string to, string[] lines)
        {
            var item = new SnippetItem
            {
                Text = "[" + Label(category) + "] " + from + " => " + to + " (все формы)",
                Lines = lines,
                IsGroup = true
            };
            groups.Add(item);
        }

        private static string Label(string category)
        {
            return category == "fun" ? "Fun" : "Classic";
        }

        private static List<SnippetItem> LoadSnippets()
        {
            var list = new List<SnippetItem>();
            list.AddRange(BuildFormGroups());
            string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "presets.txt");
            if (!File.Exists(file))
            {
                list.Add(new SnippetItem { Text = "База «Рубежа» => База «Долга»" });
                list.Add(new SnippetItem { Text = "Обочина => Кордон" });
                list.Add(new SnippetItem { Text = "Институт => Инсульт" });
                return list;
            }

            string current = null;
            foreach (string raw in File.ReadAllLines(file, Encoding.UTF8))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    current = line.Substring(1, line.Length - 2).Trim().ToLowerInvariant();
                    continue;
                }
                if (current == null) continue;
                if (current.EndsWith(".exact"))
                {
                    int eq = line.IndexOf('=');
                    if (eq > 0)
                    {
                        string prefix = current.StartsWith("fun") ? "[Fun exact] " : "[Classic exact] ";
                        string body = line.Substring(0, eq).Trim() + " = " + line.Substring(eq + 1).Trim();
                        list.Add(new SnippetItem { Text = prefix + body, InsertText = body, IsExact = true });
                    }
                }
                else
                {
                    int arrow = line.IndexOf("=>", StringComparison.Ordinal);
                    if (arrow > 0)
                    {
                        string from = line.Substring(0, arrow).Trim();
                        string rest = line.Substring(arrow + 2).Trim();
                        int except = rest.IndexOf("||", StringComparison.Ordinal);
                        if (except >= 0) rest = rest.Substring(0, except).Trim();
                        string prefix = current.StartsWith("fun") ? "[Fun] " : "[Classic] ";
                        string body = from + " => " + rest;
                        list.Add(new SnippetItem { Text = prefix + body, InsertText = body });
                    }
                }
            }
            return list;
        }

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
            _tabs = tabs;
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

            var snippetBar = new Panel { Dock = DockStyle.Top, Height = 34, Padding = new Padding(0, 4, 0, 2) };
            var snippetLabel = new Label { Text = "Готовые:", Left = 0, Top = 7, Width = 62, TextAlign = ContentAlignment.MiddleLeft };

            _snippetFilter.Left = 64;
            _snippetFilter.Top = 5;
            _snippetFilter.Width = 110;
            _snippetFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            _snippetFilter.Items.Add("Все");
            _snippetFilter.Items.Add("Формы");
            _snippetFilter.Items.Add("Classic");
            _snippetFilter.Items.Add("Fun");
            _snippetFilter.Items.Add("Classic exact");
            _snippetFilter.Items.Add("Fun exact");
            _snippetFilter.SelectedIndex = 0;
            _snippetFilter.SelectedIndexChanged += delegate { ApplySnippetFilter(); };

            _snippet.Left = 180;
            _snippet.Top = 5;
            _snippet.Width = 240;
            _snippet.DropDownStyle = ComboBoxStyle.DropDownList;

            _allSnippets = LoadSnippets();
            ApplySnippetFilter();

            var insertButton = new Button { Text = "Вставить", Left = 430, Top = 4, Width = 90, Height = 26 };
            insertButton.Click += delegate { InsertSnippet(); };
            snippetBar.Controls.Add(snippetLabel);
            snippetBar.Controls.Add(_snippetFilter);
            snippetBar.Controls.Add(_snippet);
            snippetBar.Controls.Add(insertButton);

            rulesPage.Controls.Add(_rulesBox);
            rulesPage.Controls.Add(snippetBar);
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

        private void ApplySnippetFilter()
        {
            if (_allSnippets == null) return;
            string filter = _snippetFilter.SelectedItem as string;
            _snippet.Items.Clear();
            foreach (SnippetItem snippet in _allSnippets)
            {
                if (filter == null || filter == "Все" || MatchesFilter(snippet, filter))
                    _snippet.Items.Add(snippet);
            }
            if (_snippet.Items.Count > 0) _snippet.SelectedIndex = 0;
        }

        private static bool MatchesFilter(SnippetItem snippet, string filter)
        {
            if (filter == "Формы") return snippet.IsGroup;
            if (filter == "Classic exact") return snippet.Text.StartsWith("[Classic exact]", StringComparison.OrdinalIgnoreCase);
            if (filter == "Fun exact") return snippet.Text.StartsWith("[Fun exact]", StringComparison.OrdinalIgnoreCase);
            if (filter == "Classic") return snippet.Text.StartsWith("[Classic]", StringComparison.OrdinalIgnoreCase) && !snippet.IsExact;
            if (filter == "Fun") return snippet.Text.StartsWith("[Fun]", StringComparison.OrdinalIgnoreCase) && !snippet.IsExact;
            return true;
        }

        private void InsertSnippet()
        {
            SnippetItem snippet = _snippet.SelectedItem as SnippetItem;
            if (snippet == null) return;
            TextBox target = snippet.IsExact ? _exactBox : _rulesBox;
            if (_tabs != null) _tabs.SelectedIndex = snippet.IsExact ? 1 : 0;
            string text = target.Text;
            if (text.Length > 0 && !text.EndsWith("\r\n") && !text.EndsWith("\n")) text += "\r\n";

            if (snippet.Lines != null && snippet.Lines.Length > 0)
            {
                text += string.Join("\r\n", snippet.Lines);
            }
            else
            {
                text += snippet.InsertText != null ? snippet.InsertText : snippet.Text;
            }

            target.Text = text;
            target.SelectionStart = text.Length;
            target.ScrollToCaret();
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
