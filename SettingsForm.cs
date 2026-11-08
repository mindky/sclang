using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ScLang
{
    internal sealed class ThemeChoice
    {
        public string Key;
        public string Label;

        public override string ToString()
        {
            return Label;
        }
    }

    internal sealed class SettingsForm : Form
    {
        private readonly AppSettings _settings;
        private readonly AppSettings _draft;
        private readonly ComboBox _themeCombo = new ComboBox();
        private readonly TextBox _imageBox = new TextBox();
        private readonly CheckBox _animateBox = new CheckBox();
        private readonly CheckBox _backupBox = new CheckBox();
        private readonly CheckBox _previewBox = new CheckBox();
        private readonly CheckBox _welcomeBox = new CheckBox();
        private readonly Panel _previewPanel = new Panel();
        private readonly Panel _customPanel = new Panel();
        private readonly Button _backButton = new Button();
        private readonly Button _panelButton = new Button();
        private readonly Button _cellButton = new Button();
        private readonly Button _textButton = new Button();
        private readonly Button _accentButton = new Button();
        private Image _previewImage;

        public SettingsForm(AppSettings settings)
        {
            _settings = settings;
            _draft = settings.Clone();

            Text = "Настройки";
            Width = 640;
            Height = 620;
            MinimumSize = new Size(600, 560);
            StartPosition = FormStartPosition.CenterParent;
            try { Font = new Font("Segoe UI", 9f); } catch { }

            var themeLabel = new Label { Text = "Тема:", Left = 16, Top = 18, Width = 60, TextAlign = ContentAlignment.MiddleLeft };
            _themeCombo.Left = 82; _themeCombo.Top = 15; _themeCombo.Width = 220;
            _themeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _themeCombo.Items.Add(new ThemeChoice { Key = "light", Label = "Светлая" });
            _themeCombo.Items.Add(new ThemeChoice { Key = "dark", Label = "Тёмная" });
            _themeCombo.Items.Add(new ThemeChoice { Key = "oled", Label = "OLED (чёрная)" });
            _themeCombo.Items.Add(new ThemeChoice { Key = "exbolight", Label = "Exbo (светлая)" });
            _themeCombo.Items.Add(new ThemeChoice { Key = "exbodark", Label = "Exbo (тёмная)" });
            _themeCombo.Items.Add(new ThemeChoice { Key = "exbo", Label = "Exbo (Launcher)" });
            _themeCombo.Items.Add(new ThemeChoice { Key = "stalzone", Label = "STALZONE" });
            _themeCombo.Items.Add(new ThemeChoice { Key = "custom", Label = "Своя" });
            SelectTheme(_draft.Theme);
            _themeCombo.SelectedIndexChanged += delegate { UpdateCustomState(); };

            _customPanel.Left = 16; _customPanel.Top = 52; _customPanel.Width = 590; _customPanel.Height = 260;
            _customPanel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;

            var imageLabel = new Label { Text = "Картинка/GIF:", Left = 12, Top = 14, Width = 100, TextAlign = ContentAlignment.MiddleLeft };
            _imageBox.Left = 116; _imageBox.Top = 11; _imageBox.Width = 310; _imageBox.ReadOnly = true;
            _imageBox.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            _imageBox.Text = _draft.CustomImage;
            var browse = new Button { Text = "Выбрать…", Left = 432, Top = 9, Width = 92, Height = 28, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            browse.Click += delegate { BrowseImage(); };
            var clear = new Button { Text = "Убрать", Left = 530, Top = 9, Width = 72, Height = 28, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            clear.Click += delegate { _draft.CustomImage = string.Empty; _imageBox.Text = string.Empty; RefreshPreview(); };

            _customPanel.Controls.Add(imageLabel);
            _customPanel.Controls.Add(_imageBox);
            _customPanel.Controls.Add(browse);
            _customPanel.Controls.Add(clear);

            var colorsLabel = new Label { Text = "Цвета:", Left = 12, Top = 54, Width = 60, TextAlign = ContentAlignment.MiddleLeft };
            _customPanel.Controls.Add(colorsLabel);
            SetupColorButton(_backButton, "Фон", "back", 12, 78);
            SetupColorButton(_panelButton, "Панели", "panel", 126, 78);
            SetupColorButton(_cellButton, "Ячейки", "cell", 240, 78);
            SetupColorButton(_textButton, "Текст", "text", 354, 78);
            SetupColorButton(_accentButton, "Акцент", "accent", 468, 78);
            _customPanel.Controls.Add(_backButton);
            _customPanel.Controls.Add(_panelButton);
            _customPanel.Controls.Add(_cellButton);
            _customPanel.Controls.Add(_textButton);
            _customPanel.Controls.Add(_accentButton);

            var auto = new Button { Text = "Цвета из картинки", Left = 12, Top = 116, Width = 160, Height = 28 };
            auto.Click += delegate { ColorsFromImage(); };
            _customPanel.Controls.Add(auto);

            _previewPanel.Left = 12; _previewPanel.Top = 154; _previewPanel.Width = 566; _previewPanel.Height = 94;
            _previewPanel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            _previewPanel.BorderStyle = BorderStyle.FixedSingle;
            _previewPanel.Paint += PreviewPanel_Paint;
            _customPanel.Controls.Add(_previewPanel);

            _animateBox.Text = "Анимировать GIF";
            _animateBox.Left = 16; _animateBox.Top = 324; _animateBox.Width = 180;
            _animateBox.Checked = _draft.AnimateGif;

            _backupBox.Text = "Резервная копия .bak при сохранении";
            _backupBox.Left = 16; _backupBox.Top = 350; _backupBox.Width = 320;
            _backupBox.Checked = _draft.BackupOnSave;

            _previewBox.Text = "Показывать предпросмотр перед применением пресета";
            _previewBox.Left = 16; _previewBox.Top = 376; _previewBox.Width = 400;
            _previewBox.Checked = _draft.ShowPreview;

            _welcomeBox.Text = "Показать приветствие при следующем запуске";
            _welcomeBox.Left = 16; _welcomeBox.Top = 402; _welcomeBox.Width = 400;
            _welcomeBox.Checked = !_draft.WelcomeShown;

            var ok = new Button { Text = "Сохранить", Left = 474, Top = 522, Width = 130, Height = 32, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            ok.Click += delegate { SaveAndClose(); };
            var cancel = new Button { Text = "Отмена", Left = 354, Top = 522, Width = 110, Height = 32, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(themeLabel);
            Controls.Add(_themeCombo);
            Controls.Add(_customPanel);
            Controls.Add(_animateBox);
            Controls.Add(_backupBox);
            Controls.Add(_previewBox);
            Controls.Add(_welcomeBox);
            Controls.Add(ok);
            Controls.Add(cancel);
            AcceptButton = ok;
            CancelButton = cancel;

            ThemeRuntime.Apply(this);
            NativeUi.DarkTitleBar(this);
            UpdateCustomState();
            RefreshColorButtons();
            RefreshPreview();
        }

        private void SetupColorButton(Button button, string text, string tag, int left, int top)
        {
            button.Text = text;
            button.Tag = tag;
            button.Left = left;
            button.Top = top;
            button.Width = 108;
            button.Height = 30;
            button.Click += delegate(object sender, EventArgs e) { PickColor((Button)sender); };
        }

        private void SelectTheme(string key)
        {
            for (int i = 0; i < _themeCombo.Items.Count; i++)
            {
                var choice = _themeCombo.Items[i] as ThemeChoice;
                if (choice != null && string.Equals(choice.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    _themeCombo.SelectedIndex = i;
                    return;
                }
            }
            _themeCombo.SelectedIndex = 0;
        }

        private string SelectedThemeKey
        {
            get
            {
                var choice = _themeCombo.SelectedItem as ThemeChoice;
                return choice == null ? "light" : choice.Key;
            }
        }

        private void UpdateCustomState()
        {
            _draft.Theme = SelectedThemeKey;
            _customPanel.Enabled = true;
            RefreshPreview();
        }

        private Color GetColor(string which)
        {
            switch (which)
            {
                case "back": return AppSettings.ColorOf(_draft.CustomBack, Color.FromArgb(20, 24, 28));
                case "panel": return AppSettings.ColorOf(_draft.CustomPanel, Color.FromArgb(28, 35, 43));
                case "cell": return AppSettings.ColorOf(_draft.CustomCell, Color.FromArgb(32, 40, 49));
                case "text": return AppSettings.ColorOf(_draft.CustomText, Color.FromArgb(232, 238, 244));
                default: return AppSettings.ColorOf(_draft.CustomAccent, Color.FromArgb(59, 130, 160));
            }
        }

        private void SetColor(string which, Color color)
        {
            switch (which)
            {
                case "back": _draft.CustomBack = AppSettings.ToHex(color); break;
                case "panel": _draft.CustomPanel = AppSettings.ToHex(color); break;
                case "cell": _draft.CustomCell = AppSettings.ToHex(color); break;
                case "text": _draft.CustomText = AppSettings.ToHex(color); break;
                default: _draft.CustomAccent = AppSettings.ToHex(color); break;
            }
        }

        private void PickColor(Button button)
        {
            string which = button.Tag == null ? "back" : button.Tag.ToString();
            using (var dialog = new ColorDialog())
            {
                dialog.FullOpen = true;
                dialog.Color = GetColor(which);
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                SetColor(which, dialog.Color);
                RefreshColorButtons();
                RefreshPreview();
            }
        }

        private void RefreshColorButtons()
        {
            RefreshColorButton(_backButton, "back");
            RefreshColorButton(_panelButton, "panel");
            RefreshColorButton(_cellButton, "cell");
            RefreshColorButton(_textButton, "text");
            RefreshColorButton(_accentButton, "accent");
        }

        private void RefreshColorButton(Button button, string which)
        {
            Color color = GetColor(which);
            button.BackColor = color;
            button.ForeColor = ThemePalette.Contrast(color);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = ThemePalette.Mix(color, Color.Black, 0.35);
        }

        private void BrowseImage()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Фоновая картинка или GIF";
                dialog.Filter = "Изображения (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|Все файлы (*.*)|*.*";
                if (_draft.CustomImage.Length > 0)
                {
                    try { dialog.InitialDirectory = Path.GetDirectoryName(_draft.CustomImage); }
                    catch { }
                }
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                _draft.CustomImage = dialog.FileName;
                _imageBox.Text = dialog.FileName;
                RefreshPreview();
            }
        }

        private void ColorsFromImage()
        {
            string path = _draft.CustomImage;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                MessageBox.Show(this, "Сначала выберите картинку.", "Своя тема", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                using (var image = Image.FromFile(path))
                {
                    Color average = ThemePalette.AverageColor(image);
                    bool dark = ThemePalette.Luminance(average) < 0.5;
                    Color back = ThemePalette.Adjust(average, dark ? -0.65 : 0.55);
                    Color panel = ThemePalette.Adjust(average, dark ? -0.45 : 0.42);
                    Color cell = ThemePalette.Adjust(average, dark ? -0.25 : 0.30);
                    Color text = dark ? Color.White : Color.Black;
                    Color accent = Color.FromArgb(
                        Clamp((int)(average.R * 0.55) + (dark ? 40 : 20)),
                        Clamp((int)(average.G * 0.75) + (dark ? 70 : 40)),
                        Clamp((int)(average.B * 0.95) + (dark ? 110 : 60)));
                    _draft.CustomBack = AppSettings.ToHex(back);
                    _draft.CustomPanel = AppSettings.ToHex(panel);
                    _draft.CustomCell = AppSettings.ToHex(cell);
                    _draft.CustomText = AppSettings.ToHex(text);
                    _draft.CustomAccent = AppSettings.ToHex(accent);
                    RefreshColorButtons();
                    RefreshPreview();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Не удалось прочитать картинку:\r\n" + ex.Message, "Своя тема", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static int Clamp(int value)
        {
            return Math.Max(0, Math.Min(255, value));
        }

        private void RefreshPreview()
        {
            if (_previewImage != null)
            {
                _previewImage.Dispose();
                _previewImage = null;
            }
            string path = _draft.CustomImage;
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                try
                {
                    using (var source = Image.FromFile(path)) _previewImage = new Bitmap(source);
                }
                catch
                {
                    _previewImage = null;
                }
            }
            _previewPanel.Invalidate();
        }

        private void PreviewPanel_Paint(object sender, PaintEventArgs e)
        {
            ThemePalette palette = ThemePalette.FromSettings(_draft);
            e.Graphics.Clear(palette.Form);

            if (_previewImage != null)
            {
                e.Graphics.DrawImage(_previewImage, new Rectangle(0, 0, _previewPanel.Width, _previewPanel.Height));
                using (var overlay = new SolidBrush(Color.FromArgb(150, palette.Form))) e.Graphics.FillRectangle(overlay, 0, 0, _previewPanel.Width, _previewPanel.Height);
            }

            using (var panelBrush = new SolidBrush(palette.Panel)) e.Graphics.FillRectangle(panelBrush, 8, 8, _previewPanel.Width - 16, 22);
            using (var cellBrush = new SolidBrush(palette.Cell)) e.Graphics.FillRectangle(cellBrush, 8, 34, _previewPanel.Width - 16, 26);
            using (var accentBrush = new SolidBrush(palette.Accent)) e.Graphics.FillRectangle(accentBrush, 8, 64, 120, 18);
            using (var textBrush = new SolidBrush(palette.Text)) e.Graphics.DrawString("Пример текста", Font, textBrush, 16, 39);
            using (var accentText = new SolidBrush(ThemePalette.Contrast(palette.Accent))) e.Graphics.DrawString("Акцент", Font, accentText, 16, 65);
        }

        private void SaveAndClose()
        {
            _draft.Theme = SelectedThemeKey;
            _draft.AnimateGif = _animateBox.Checked;
            _draft.BackupOnSave = _backupBox.Checked;
            _draft.ShowPreview = _previewBox.Checked;
            _draft.WelcomeShown = !_welcomeBox.Checked;
            _settings.CopyFrom(_draft);
            _settings.Save();
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _previewImage != null)
            {
                _previewImage.Dispose();
                _previewImage = null;
            }
            base.Dispose(disposing);
        }
    }
}
