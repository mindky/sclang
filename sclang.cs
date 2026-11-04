using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ScLang
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal sealed class LangEntry
    {
        public string Key;
        public string Value;
        public string Raw;
        public bool IsEntry;
    }

    internal sealed class PresetRule
    {
        public string From;
        public string To;
        public string Except;
    }

    internal sealed class PresetItem
    {
        public string Section;
        public string Display;
        public CustomPreset Custom;

        public override string ToString()
        {
            return Display;
        }
    }

    internal sealed class LangFileItem
    {
        public string Path;

        public override string ToString()
        {
            return System.IO.Path.GetFileName(Path) + "  —  " + System.IO.Path.GetDirectoryName(Path);
        }
    }

    internal sealed class Change
    {
        public int Index;
        public string Key;
        public string OldValue;
        public string NewValue;
    }

    internal sealed class MainForm : Form
    {
        private readonly List<LangEntry> _entries = new List<LangEntry>();
        private readonly List<int> _view = new List<int>();
        private readonly Dictionary<string, List<PresetRule>> _presets = new Dictionary<string, List<PresetRule>>();
        private readonly Dictionary<string, Dictionary<string, string>> _exact = new Dictionary<string, Dictionary<string, string>>();
        private readonly List<CustomPreset> _customPresets = new List<CustomPreset>();

        private string _path;
        private Encoding _encoding = new UTF8Encoding(false);
        private bool _bom;
        private string _newline = "\n";
        private bool _trailingNewline;
        private bool _dirty;
        private bool _loadingUi;

        private Dictionary<int, string> _undo;
        private string _undoName;

        private ComboBox _fileCombo;
        private ComboBox _presetCombo;
        private TextBox _searchBox;
        private Label _searchHint;
        private DataGridView _grid;
        private ToolStripStatusLabel _statusLabel;
        private Button _undoButton;
        private Button _applyButton;
        private Panel _topPanel;
        private StatusStrip _statusStrip;
        private Timer _gifTimer;
        private Image _bgImage;
        private System.IO.MemoryStream _bgStream;
        private AppSettings _settings;
        private ThemePalette _palette;

        public MainForm()
        {
            _settings = AppSettings.Load();

            Text = "SC Lang — редактор локализации";
            Width = Math.Max(900, _settings.WindowWidth);
            Height = Math.Max(600, _settings.WindowHeight);
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            try { Font = new Font("Segoe UI", 9f); } catch { }
            try { Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            BuildUi();
            ApplyTheme();
            LoadPresets();
            LoadCustomPresets();
            RebuildPresetCombo();
            Shown += delegate
            {
                if (!_settings.WelcomeShown) ShowWelcome();
                else AutoFind(true);
            };
            FormClosing += MainForm_FormClosing;
        }

        private void BuildUi()
        {
            _topPanel = new Panel { Dock = DockStyle.Top, Height = 78, Width = ClientSize.Width, Padding = new Padding(8, 6, 8, 4) };

            var openButton = MakeButton("Открыть…", 8, 6, 96);
            openButton.Click += delegate { OpenDialog(); };
            var saveButton = MakeButton("Сохранить", 110, 6, 100);
            saveButton.Click += delegate { Save(false); };
            var saveAsButton = MakeButton("Сохранить как…", 216, 6, 130);
            saveAsButton.Click += delegate { Save(true); };
            var findButton = MakeButton("Автонайти .lang", 352, 6, 130);
            findButton.Click += delegate { AutoFind(false); };
            var settingsButton = MakeButton("Настройки", 488, 6, 80);
            settingsButton.Click += delegate { OpenSettings(); };

            _fileCombo = new ComboBox
            {
                Left = 574,
                Top = 6,
                Width = 634,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
            };
            _fileCombo.SelectedIndexChanged += FileCombo_SelectedIndexChanged;

            var presetLabel = new Label { Text = "Пресет:", Left = 8, Top = 44, Width = 56, TextAlign = ContentAlignment.MiddleLeft };
            _presetCombo = new ComboBox
            {
                Left = 64,
                Top = 41,
                Width = 240,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _applyButton = MakeButton("Применить", 310, 40, 92);
            _applyButton.Click += delegate { ApplyPreset(); };
            _undoButton = MakeButton("Отменить пресет", 408, 40, 120);
            _undoButton.Enabled = false;
            _undoButton.Click += delegate { UndoPreset(); };
            var customButton = MakeButton("Свои…", 534, 40, 96);
            customButton.Click += delegate { OpenPresetManager(); };

            var searchLabel = new Label { Text = "Поиск:", Left = 636, Top = 44, Width = 52, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            _searchBox = new TextBox { Left = 692, Top = 41, Width = 516, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            _searchBox.TextChanged += delegate { ApplyFilter(); UpdateSearchHint(); };
            _searchBox.KeyDown += SearchBox_KeyDown;
            _searchBox.Enter += delegate { if (_searchHint != null) _searchHint.Visible = false; };
            _searchBox.Leave += delegate { UpdateSearchHint(); };

            _searchHint = new Label
            {
                Text = "ключ или значение",
                Left = 696,
                Top = 43,
                Width = 180,
                Height = 18,
                ForeColor = SystemColors.GrayText,
                BackColor = SystemColors.Window,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.IBeam,
                Anchor = AnchorStyles.Top
            };
            _searchHint.Click += delegate { _searchBox.Focus(); };

            _topPanel.Controls.Add(openButton);
            _topPanel.Controls.Add(saveButton);
            _topPanel.Controls.Add(saveAsButton);
            _topPanel.Controls.Add(findButton);
            _topPanel.Controls.Add(settingsButton);
            _topPanel.Controls.Add(_fileCombo);
            _topPanel.Controls.Add(presetLabel);
            _topPanel.Controls.Add(_presetCombo);
            _topPanel.Controls.Add(_applyButton);
            _topPanel.Controls.Add(_undoButton);
            _topPanel.Controls.Add(customButton);
            _topPanel.Controls.Add(searchLabel);
            _topPanel.Controls.Add(_searchBox);
            _topPanel.Controls.Add(_searchHint);
            _searchHint.BringToFront();

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                VirtualMode = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                EditMode = DataGridViewEditMode.EditOnEnter,
                BackgroundColor = SystemColors.Window
            };
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Ключ",
                Width = 380,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Значение",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            _grid.CellValueNeeded += Grid_CellValueNeeded;
            _grid.CellValuePushed += Grid_CellValuePushed;
            _grid.CellBeginEdit += delegate(object sender, DataGridViewCellCancelEventArgs e)
            {
                if (e.ColumnIndex == 0) e.Cancel = true;
            };
            _grid.CellEndEdit += delegate
            {
                if (!_loadingUi) SetDirty(true);
            };
            _grid.DataError += delegate(object sender, DataGridViewDataErrorEventArgs e) { e.ThrowException = false; };

            var status = new StatusStrip();
            _statusStrip = status;
            _statusLabel = new ToolStripStatusLabel("Файл не открыт");
            _statusLabel.Spring = true;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            status.Items.Add(_statusLabel);

            Controls.Add(_grid);
            Controls.Add(_topPanel);
            Controls.Add(status);
        }

        private void ApplyTheme()
        {
            _palette = ThemePalette.FromSettings(_settings);
            ThemeRuntime.Current = _palette;
            ThemeRuntime.Apply(this, _palette);

            BackColor = _palette.Form;
            _topPanel.BackColor = _palette.Panel;
            _statusStrip.BackColor = _palette.Panel;
            _statusStrip.ForeColor = _palette.Text;
            _statusLabel.ForeColor = _palette.Text;

            _grid.BackgroundColor = _palette.Grid;
            _grid.GridColor = _palette.Border;
            _grid.EnableHeadersVisualStyles = false;
            _grid.ColumnHeadersDefaultCellStyle.BackColor = _palette.Header;
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = _palette.HeaderText;
            _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = _palette.Header;
            _grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = _palette.HeaderText;
            _grid.DefaultCellStyle.BackColor = _palette.Cell;
            _grid.DefaultCellStyle.ForeColor = _palette.Text;
            _grid.DefaultCellStyle.SelectionBackColor = _palette.Selection;
            _grid.DefaultCellStyle.SelectionForeColor = _palette.SelectionText;
            _grid.AlternatingRowsDefaultCellStyle.BackColor = _palette.AltCell;
            _grid.AlternatingRowsDefaultCellStyle.ForeColor = _palette.Text;
            _grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = _palette.Selection;
            _grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = _palette.SelectionText;

            _searchHint.BackColor = _searchBox.BackColor;
            _searchHint.ForeColor = ThemePalette.Mix(_palette.Text, _palette.Cell, 0.45);

            _applyButton.BackColor = _palette.Accent;
            _applyButton.ForeColor = ThemePalette.Contrast(_palette.Accent);
            _applyButton.FlatStyle = FlatStyle.Flat;
            _applyButton.FlatAppearance.BorderColor = ThemePalette.Mix(_palette.Accent, Color.Black, 0.35);

            SetupBackgroundImage();
            ApplyFilter();
        }

        private void SetupBackgroundImage()
        {
            StopBackgroundAnimation();
            if (!string.Equals(_settings.Theme, "custom", StringComparison.OrdinalIgnoreCase)) return;
            if (string.IsNullOrEmpty(_settings.CustomImage) || !File.Exists(_settings.CustomImage)) return;

            try
            {
                byte[] data = File.ReadAllBytes(_settings.CustomImage);
                _bgStream = new System.IO.MemoryStream(data);
                _bgImage = Image.FromStream(_bgStream);
            }
            catch
            {
                if (_bgStream != null)
                {
                    _bgStream.Dispose();
                    _bgStream = null;
                }
                _bgImage = null;
                return;
            }

            BackgroundImage = _bgImage;
            BackgroundImageLayout = ImageLayout.Stretch;
            _topPanel.BackgroundImage = _bgImage;
            _topPanel.BackgroundImageLayout = ImageLayout.Stretch;
            _grid.BackgroundImage = _bgImage;
            _grid.BackgroundImageLayout = ImageLayout.Stretch;

            if (_settings.AnimateGif && ImageAnimator.CanAnimate(_bgImage))
            {
                _gifTimer = new Timer { Interval = 80 };
                _gifTimer.Tick += delegate
                {
                    try { ImageAnimator.UpdateFrames(_bgImage); }
                    catch { }
                    _grid.Invalidate();
                    _topPanel.Invalidate();
                    Invalidate(true);
                };
                _gifTimer.Start();
            }
        }

        private void StopBackgroundAnimation()
        {
            if (_gifTimer != null)
            {
                _gifTimer.Stop();
                _gifTimer.Dispose();
                _gifTimer = null;
            }
            BackgroundImage = null;
            _topPanel.BackgroundImage = null;
            _grid.BackgroundImage = null;
            if (_bgImage != null)
            {
                _bgImage.Dispose();
                _bgImage = null;
            }
            if (_bgStream != null)
            {
                _bgStream.Dispose();
                _bgStream = null;
            }
        }

        private void OpenSettings()
        {
            using (var form = new SettingsForm(_settings))
            {
                if (form.ShowDialog(this) == DialogResult.OK) ApplyTheme();
            }
        }

        private void ShowWelcome()
        {
            using (var form = new WelcomeForm(_settings))
            {
                DialogResult result = form.ShowDialog(this);
                string selected = form.SelectedFile;
                ApplyTheme();
                if (result == DialogResult.OK && !string.IsNullOrEmpty(selected))
                    LoadFile(selected);
                else
                    AutoFind(true);
            }
            _settings.WelcomeShown = true;
            _settings.Save();
        }

        private void OpenPresetManager()
        {
            string selected = null;
            var item = _presetCombo.SelectedItem as PresetItem;
            if (item != null) selected = item.Display;

            using (var form = new PresetManagerForm())
            {
                form.ShowDialog(this);
            }
            LoadCustomPresets();
            RebuildPresetCombo();
            if (selected != null)
            {
                for (int i = 0; i < _presetCombo.Items.Count; i++)
                {
                    var candidate = _presetCombo.Items[i] as PresetItem;
                    if (candidate != null && candidate.Display == selected)
                    {
                        _presetCombo.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void LoadCustomPresets()
        {
            _customPresets.Clear();
            _customPresets.AddRange(CustomPreset.LoadAll());
        }

        private void RebuildPresetCombo()
        {
            _presetCombo.Items.Clear();
            _presetCombo.Items.Add(new PresetItem { Section = "classic", Display = "Классика — знакомые названия" });
            _presetCombo.Items.Add(new PresetItem { Section = "fun", Display = "Fun — смешные названия" });
            foreach (CustomPreset preset in _customPresets)
            {
                _presetCombo.Items.Add(new PresetItem { Section = "custom", Display = "Свой: " + preset.Name, Custom = preset });
            }
            if (_presetCombo.Items.Count > 0) _presetCombo.SelectedIndex = 0;
        }

        private static Button MakeButton(string text, int left, int top, int width)
        {
            return new Button
            {
                Text = text,
                Left = left,
                Top = top,
                Width = width,
                Height = 28,
                UseVisualStyleBackColor = true
            };
        }

        private void FileCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingUi) return;
            var item = _fileCombo.SelectedItem as LangFileItem;
            if (item == null) return;
            if (string.Equals(item.Path, _path, StringComparison.OrdinalIgnoreCase)) return;
            if (!PromptSave()) return;
            LoadFile(item.Path);
        }

        private void OpenDialog()
        {
            if (!PromptSave()) return;
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Открыть .lang";
                dialog.Filter = "Файлы локализации (*.lang)|*.lang|Все файлы (*.*)|*.*";
                if (_path != null) dialog.InitialDirectory = System.IO.Path.GetDirectoryName(_path);
                if (dialog.ShowDialog(this) == DialogResult.OK) LoadFile(dialog.FileName);
            }
        }

        private void LoadFile(string path)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                bool bom;
                Encoding enc = DetectEncoding(bytes, out bom);
                int offset = bom ? 3 : 0;
                string text = enc.GetString(bytes, offset, bytes.Length - offset);

                string normalized = text.Replace("\r\n", "\n");
                _trailingNewline = normalized.EndsWith("\n");
                if (_trailingNewline && normalized.Length > 0) normalized = normalized.Substring(0, normalized.Length - 1);
                string[] lines = normalized.Split('\n');

                _loadingUi = true;
                _entries.Clear();
                foreach (string line in lines)
                {
                    int eq = line.IndexOf('=');
                    var entry = new LangEntry { Raw = line };
                    if (eq > 0)
                    {
                        entry.IsEntry = true;
                        entry.Key = line.Substring(0, eq);
                        entry.Value = line.Substring(eq + 1);
                    }
                    _entries.Add(entry);
                }

                _path = path;
                _settings.LastFile = path;
                _encoding = enc;
                _bom = bom;
                _newline = text.Contains("\r\n") ? "\r\n" : "\n";
                _undo = null;
                _undoName = null;
                _undoButton.Enabled = false;
                _searchBox.Text = string.Empty;
                SetDirty(false);
                ApplyFilter();
                SelectInCombo(path);
                RefreshFileCombo(path);
                _loadingUi = false;
                UpdateStatus();
            }
            catch (Exception ex)
            {
                _loadingUi = false;
                MessageBox.Show(this, "Не удалось открыть файл:\r\n" + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static Encoding DetectEncoding(byte[] bytes, out bool bom)
        {
            bom = false;
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                bom = true;
                return new UTF8Encoding(false);
            }
            try
            {
                var strict = new UTF8Encoding(false, true);
                strict.GetString(bytes);
                return new UTF8Encoding(false);
            }
            catch
            {
                return Encoding.GetEncoding(1251);
            }
        }

        private void RefreshFileCombo(string preferredPath)
        {
            try
            {
                string dir = System.IO.Path.GetDirectoryName(preferredPath);
                if (dir == null || !Directory.Exists(dir)) return;
                string[] files = Directory.GetFiles(dir, "*.lang");
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                _loadingUi = true;
                _fileCombo.Items.Clear();
                foreach (string file in files) _fileCombo.Items.Add(new LangFileItem { Path = file });
                SelectInCombo(preferredPath);
                _loadingUi = false;
            }
            catch
            {
                _loadingUi = false;
            }
        }

        private void SelectInCombo(string path)
        {
            for (int i = 0; i < _fileCombo.Items.Count; i++)
            {
                var item = _fileCombo.Items[i] as LangFileItem;
                if (item != null && string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase))
                {
                    _fileCombo.SelectedIndex = i;
                    return;
                }
            }
        }

        private void AutoFind(bool silent)
        {
            List<string> found = LangLocator.FindAll();

            if (found.Count == 0)
            {
                if (!silent)
                    MessageBox.Show(this, "Файлы .lang не найдены.\r\nОткройте файл вручную.", "Автонайти", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _loadingUi = true;
            _fileCombo.Items.Clear();
            foreach (string file in found) _fileCombo.Items.Add(new LangFileItem { Path = file });
            _loadingUi = false;

            string preferred = LangLocator.ChoosePreferred(found, _path, _settings.LastFile);

            _loadingUi = true;
            SelectInCombo(preferred);
            _loadingUi = false;

            if (_path == null || !string.Equals(_path, preferred, StringComparison.OrdinalIgnoreCase))
                LoadFile(preferred);
            else
                UpdateStatus();

            if (!silent)
                _statusLabel.Text = "Найдено файлов .lang: " + found.Count + "   |   " + _path;
        }

        private void ApplyFilter()
        {
            string query = _searchBox.Text.Trim();
            _view.Clear();
            for (int i = 0; i < _entries.Count; i++)
            {
                LangEntry e = _entries[i];
                if (!e.IsEntry) continue;
                if (query.Length == 0 ||
                    e.Key.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    e.Value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _view.Add(i);
                }
            }
            _grid.RowCount = _view.Count;
            _grid.Invalidate();
            UpdateStatus();
        }

        private void UpdateSearchHint()
        {
            if (_searchHint != null) _searchHint.Visible = _searchBox.Text.Length == 0;
        }

        private void Grid_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _view.Count) return;
            LangEntry entry = _entries[_view[e.RowIndex]];
            if (e.ColumnIndex == 0) e.Value = entry.Key;
            else if (e.ColumnIndex == 1) e.Value = entry.Value;
        }

        private void Grid_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _view.Count || e.ColumnIndex != 1) return;
            LangEntry entry = _entries[_view[e.RowIndex]];
            string value = e.Value == null ? string.Empty : e.Value.ToString();
            if (entry.Value == value) return;
            entry.Value = value;
            SetDirty(true);
        }

        private void SetDirty(bool dirty)
        {
            _dirty = dirty;
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            string marker = _dirty ? " *" : string.Empty;
            string shown = _view.Count + " из " + _entries.Count(e => e.IsEntry);
            if (_path == null)
                _statusLabel.Text = "Файл не открыт";
            else
                _statusLabel.Text = _path + marker + "   |   показано: " + shown;
        }

        private bool PromptSave()
        {
            if (!_dirty) return true;
            DialogResult result = MessageBox.Show(this, "Сохранить изменения в текущем файле?", "SC Lang",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Cancel) return false;
            if (result == DialogResult.Yes) return Save(false);
            return true;
        }

        private bool Save(bool saveAs)
        {
            if (_entries.Count == 0)
            {
                MessageBox.Show(this, "Нет открытого файла.", "SC Lang", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            string target = _path;
            if (saveAs || target == null)
            {
                using (var dialog = new SaveFileDialog())
                {
                    dialog.Title = "Сохранить .lang";
                    dialog.Filter = "Файлы локализации (*.lang)|*.lang|Все файлы (*.*)|*.*";
                    dialog.FileName = target == null ? "ru.lang" : System.IO.Path.GetFileName(target);
                    if (target != null) dialog.InitialDirectory = System.IO.Path.GetDirectoryName(target);
                    if (dialog.ShowDialog(this) != DialogResult.OK) return false;
                    target = dialog.FileName;
                }
            }

            try
            {
                if (_settings.BackupOnSave && File.Exists(target))
                {
                    try { File.Copy(target, target + ".bak", true); }
                    catch { }
                }

                var sb = new StringBuilder();
                for (int i = 0; i < _entries.Count; i++)
                {
                    LangEntry entry = _entries[i];
                    sb.Append(entry.IsEntry ? entry.Key + "=" + entry.Value : entry.Raw);
                    if (i < _entries.Count - 1 || _trailingNewline) sb.Append(_newline);
                }

                Encoding writeEncoding;
                if (_encoding.CodePage == Encoding.UTF8.CodePage) writeEncoding = new UTF8Encoding(_bom);
                else writeEncoding = _encoding;

                File.WriteAllText(target, sb.ToString(), writeEncoding);
                _path = target;
                SetDirty(false);
                RefreshFileCombo(target);
                UpdateStatus();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Не удалось сохранить файл:\r\n" + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!PromptSave())
            {
                e.Cancel = true;
                return;
            }
            StopBackgroundAnimation();
            if (WindowState == FormWindowState.Normal)
            {
                _settings.WindowWidth = Width;
                _settings.WindowHeight = Height;
            }
            _settings.Save();
        }

        private void LoadPresets()
        {
            _presets.Clear();
            _exact.Clear();
            string file = System.IO.Path.Combine(Application.StartupPath, "presets.txt");
            if (!File.Exists(file))
            {
                try { File.WriteAllText(file, DefaultPresets, new UTF8Encoding(false)); }
                catch { }
            }

            string current = null;
            if (File.Exists(file))
            {
                foreach (string raw in File.ReadAllLines(file, Encoding.UTF8))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        current = line.Substring(1, line.Length - 2).Trim().ToLowerInvariant();
                        if (current == "exact" || current.EndsWith(".exact"))
                        {
                            if (!_exact.ContainsKey(current)) _exact[current] = new Dictionary<string, string>(StringComparer.Ordinal);
                        }
                        else
                        {
                            if (!_presets.ContainsKey(current)) _presets[current] = new List<PresetRule>();
                        }
                        continue;
                    }
                    if (current == null) continue;

                    if (current == "exact" || current.EndsWith(".exact"))
                    {
                        int eq = line.IndexOf('=');
                        if (eq > 0)
                        {
                            string key = line.Substring(0, eq).Trim();
                            string value = line.Substring(eq + 1).Trim();
                            if (key.Length > 0) _exact[current][key] = value;
                        }
                        continue;
                    }

                    int split = line.IndexOf("=>", StringComparison.Ordinal);
                    if (split <= 0) continue;
                    string from = line.Substring(0, split).Trim();
                    string rest = line.Substring(split + 2).Trim();
                    string to = rest;
                    string except = null;
                    int exceptSplit = rest.IndexOf("||", StringComparison.Ordinal);
                    if (exceptSplit >= 0)
                    {
                        to = rest.Substring(0, exceptSplit).Trim();
                        except = rest.Substring(exceptSplit + 2).Trim();
                    }
                    if (from.Length == 0) continue;
                    _presets[current].Add(new PresetRule { From = from, To = to, Except = except });
                }
            }

            _presetCombo.Items.Clear();
        }

        private void ApplyPreset()
        {
            if (_entries.Count == 0)
            {
                MessageBox.Show(this, "Сначала откройте .lang.", "SC Lang", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var item = _presetCombo.SelectedItem as PresetItem;
            if (item == null) return;

            List<PresetRule> rules;
            Dictionary<string, string> exact;
            if (item.Custom != null)
            {
                rules = new List<PresetRule>(item.Custom.Rules);
                rules.Sort(delegate(PresetRule a, PresetRule b) { return b.From.Length.CompareTo(a.From.Length); });
                exact = new Dictionary<string, string>(item.Custom.Exact, StringComparer.Ordinal);
            }
            else
            {
                rules = GetRules(item.Section);
                exact = GetExactValues(item.Section);
            }
            if (rules.Count == 0 && exact.Count == 0)
            {
                MessageBox.Show(this, "В пресете нет правил.", "SC Lang",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var changesByIndex = new Dictionary<int, Change>();
            for (int i = 0; i < _entries.Count; i++)
            {
                LangEntry entry = _entries[i];
                if (!entry.IsEntry) continue;
                string newValue = ApplyRules(entry.Value, rules);
                string exactValue;
                if (exact.TryGetValue(entry.Key, out exactValue)) newValue = exactValue;
                if (newValue != entry.Value)
                {
                    changesByIndex[i] = new Change { Index = i, Key = entry.Key, OldValue = entry.Value, NewValue = newValue };
                }
            }
            var changes = changesByIndex.Values.OrderBy(delegate(Change c) { return c.Index; }).ToList();

            if (changes.Count == 0)
            {
                MessageBox.Show(this, "Нечего менять: подходящих названий в файле нет.", "SC Lang",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_settings.ShowPreview && !ShowPreview(item.Display, changes)) return;

            _undo = new Dictionary<int, string>();
            _undoName = item.Display;
            foreach (Change change in changes)
            {
                _undo[change.Index] = _entries[change.Index].Value;
                _entries[change.Index].Value = change.NewValue;
            }
            _undoButton.Enabled = true;
            SetDirty(true);
            ApplyFilter();

            MessageBox.Show(this, "Изменено значений: " + changes.Count + ".", "SC Lang",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private List<PresetRule> GetRules(string section)
        {
            var result = new List<PresetRule>();
            if (section == "fun" && _presets.ContainsKey("classic"))
                result.AddRange(_presets["classic"]);
            if (_presets.ContainsKey(section))
                result.AddRange(_presets[section]);
            result.Sort(delegate(PresetRule a, PresetRule b) { return b.From.Length.CompareTo(a.From.Length); });
            return result;
        }

        private Dictionary<string, string> GetExactValues(string section)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            AddExactSection(result, "exact");
            AddExactSection(result, "classic.exact");
            if (section == "fun") AddExactSection(result, "fun.exact");
            if (section != "classic" && section != "fun") AddExactSection(result, section + ".exact");
            return result;
        }

        private void AddExactSection(Dictionary<string, string> target, string name)
        {
            Dictionary<string, string> values;
            if (!_exact.TryGetValue(name, out values)) return;
            foreach (KeyValuePair<string, string> pair in values) target[pair.Key] = pair.Value;
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                MoveSearch(e.Shift ? -1 : 1);
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                _searchBox.Text = string.Empty;
                _grid.Focus();
                e.SuppressKeyPress = true;
            }
        }

        private void MoveSearch(int direction)
        {
            if (_view.Count == 0) return;
            int row = _grid.CurrentCell == null ? -1 : _grid.CurrentCell.RowIndex;
            if (row < 0 || row >= _view.Count) row = direction > 0 ? -1 : 0;
            int next = row + direction;
            if (next < 0) next = _view.Count - 1;
            if (next >= _view.Count) next = 0;
            _grid.CurrentCell = _grid.Rows[next].Cells[1];
            _grid.Focus();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.F))
            {
                _searchBox.Focus();
                _searchBox.SelectAll();
                return true;
            }
            if (keyData == Keys.F3)
            {
                MoveSearch(1);
                return true;
            }
            if (keyData == (Keys.Shift | Keys.F3))
            {
                MoveSearch(-1);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private static string ApplyRules(string value, List<PresetRule> rules)
        {
            string result = value;
            foreach (PresetRule rule in rules)
            {
                if (!string.IsNullOrEmpty(rule.Except) && result.IndexOf(rule.Except, StringComparison.Ordinal) >= 0) continue;
                result = result.Replace(rule.From, rule.To);
            }
            return result;
        }

        private bool ShowPreview(string title, List<Change> changes)
        {
            using (var form = new Form())
            {
                form.Text = "Предпросмотр: " + title;
                form.Width = 1100;
                form.Height = 640;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MinimizeBox = false;
                form.MaximizeBox = false;

                var label = new Label
                {
                    Dock = DockStyle.Top,
                    Height = 42,
                    Padding = new Padding(10, 10, 10, 4),
                    Text = "Будет изменено значений: " + changes.Count + (changes.Count > 1000 ? ". Показаны первые 1000." : ".")
                };

                var list = new ListView
                {
                    Dock = DockStyle.Fill,
                    View = View.Details,
                    FullRowSelect = true,
                    GridLines = true
                };
                list.Columns.Add("Ключ", 330);
                list.Columns.Add("Было", 330);
                list.Columns.Add("Стало", 330);

                int limit = Math.Min(changes.Count, 1000);
                for (int i = 0; i < limit; i++)
                {
                    var row = new ListViewItem(changes[i].Key);
                    row.SubItems.Add(changes[i].OldValue);
                    row.SubItems.Add(changes[i].NewValue);
                    list.Items.Add(row);
                }

                var buttons = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    Height = 46,
                    FlowDirection = FlowDirection.RightToLeft,
                    Padding = new Padding(8)
                };
                var ok = new Button { Text = "Применить", Width = 120, Height = 30, DialogResult = DialogResult.OK };
                var cancel = new Button { Text = "Отмена", Width = 100, Height = 30, DialogResult = DialogResult.Cancel };
                buttons.Controls.Add(ok);
                buttons.Controls.Add(cancel);

                form.Controls.Add(list);
                form.Controls.Add(buttons);
                form.Controls.Add(label);
                form.AcceptButton = ok;
                form.CancelButton = cancel;
                return form.ShowDialog(this) == DialogResult.OK;
            }
        }

        private void UndoPreset()
        {
            if (_undo == null || _undo.Count == 0) return;
            foreach (KeyValuePair<int, string> pair in _undo)
            {
                if (pair.Key >= 0 && pair.Key < _entries.Count)
                    _entries[pair.Key].Value = pair.Value;
            }
            MessageBox.Show(this, "Отменён пресет: " + _undoName, "SC Lang", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _undo = null;
            _undoName = null;
            _undoButton.Enabled = false;
            SetDirty(true);
            ApplyFilter();
        }

        private const string DefaultPresets = "# SC Lang — правила замен названий\r\n" +
            "# Формат: было => стало\r\n" +
            "# Замены применяются к значениям всех ключей. Регистр важен.\r\n" +
            "# Раздел [fun] автоматически включает правила [classic], затем свои.\r\n" +
            "\r\n" +
            "[classic]\r\n" +
            "База «Рубежа» => База «Долга»\r\n" +
            "База «Зари» => База «Свободы»\r\n" +
            "Свалка техники => Свалка\r\n" +
            "Агрокомплекс «Колос» => Агропром\r\n" +
            "Обочина => Кордон\r\n" +
            "Рубеж => Долг\r\n" +
            "Заря => Свобода\r\n" +
            "Приток => Припять\r\n" +
            "\r\n" +
            "[fun]\r\n" +
            "Кузня-11 => Кухня-11\r\n" +
            "Институт => Инсульт\r\n" +
            "Храмовая топь => Храбрая топь\r\n" +
            "Барьер => БульБуль\r\n" +
            "Отражение => еинежартО\r\n";
    }
}
