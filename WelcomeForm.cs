using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ScLang
{
    internal sealed class WelcomeForm : Form
    {
        private readonly AppSettings _settings;
        private readonly ComboBox _themeCombo = new ComboBox();
        private readonly ComboBox _fileCombo = new ComboBox();
        private readonly Label _status = new Label();
        private readonly Panel _previewPanel = new Panel();
        private readonly Label _brandTop = new Label();
        private readonly Label _brandBottom = new Label();
        private readonly Button _startButton = new Button();
        private Image _previewImage;

        public string SelectedFile
        {
            get
            {
                var item = _fileCombo.SelectedItem as LangFileItem;
                return item == null ? null : item.Path;
            }
        }

        public WelcomeForm(AppSettings settings)
        {
            _settings = settings;
            if (!File.Exists(AppSettings.FilePath) && string.Equals(_settings.Theme, "light", StringComparison.OrdinalIgnoreCase))
                _settings.Theme = "exbo";

            Text = "SC Lang " + MainForm.Version + " — первый запуск";
            Width = 900;
            Height = 590;
            MinimumSize = new Size(820, 540);
            StartPosition = FormStartPosition.CenterScreen;
            try { Font = new Font("Segoe UI", 9f); } catch { }
            try { Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            NativeUi.DarkTitleBar(this);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            var brand = new Panel { Dock = DockStyle.Left, Width = 280, Padding = new Padding(22) };
            brand.Tag = "brand";

            _brandTop.Text = "SC\nLANG";
            _brandTop.Left = 22;
            _brandTop.Top = 70;
            _brandTop.Width = 236;
            _brandTop.Height = 120;
            try { _brandTop.Font = new Font("Segoe UI", 34f, FontStyle.Bold); } catch { }
            _brandTop.TextAlign = ContentAlignment.MiddleLeft;

            _brandBottom.Text = "STALZONE\nредактор локализации\n\nEXBO стиль";
            _brandBottom.Left = 22;
            _brandBottom.Top = 230;
            _brandBottom.Width = 236;
            _brandBottom.Height = 150;
            try { _brandBottom.Font = new Font("Segoe UI", 10f); } catch { }

            brand.Controls.Add(_brandTop);
            brand.Controls.Add(_brandBottom);

            var content = new Panel { Dock = DockStyle.Fill, Width = ClientSize.Width - 280, Height = ClientSize.Height, Padding = new Padding(34, 26, 30, 22) };

            var heading = new Label
            {
                Text = "Добро пожаловать",
                Left = 24, Top = 0, Width = 560, Height = 46,
                Font = new Font("Segoe UI", 20f, FontStyle.Bold)
            };

            var text = new Label
            {
                Text = "Настройте внешний вид и найдите файл локализации .lang.\r\nПозже это можно изменить в «Настройки».",
                Left = 24, Top = 52, Width = 560, Height = 48
            };

            var themeLabel = new Label { Text = "Тема интерфейса", Left = 24, Top = 110, Width = 220, Height = 24 };
            _themeCombo.Left = 24;
            _themeCombo.Top = 136;
            _themeCombo.Width = 260;
            _themeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _themeCombo.Items.Add(new ThemeChoice { Key = "light", Label = "Светлая" });
            _themeCombo.Items.Add(new ThemeChoice { Key = "dark", Label = "Тёмная" });
            _themeCombo.Items.Add(new ThemeChoice { Key = "oled", Label = "OLED (чёрная)" });
            _themeCombo.Items.Add(new ThemeChoice { Key = "exbolight", Label = "Exbo (светлая)" });
            _themeCombo.Items.Add(new ThemeChoice { Key = "exbodark", Label = "Exbo (тёмная)" });
            _themeCombo.Items.Add(new ThemeChoice { Key = "exbo", Label = "Exbo (Launcher)" });
            _themeCombo.Items.Add(new ThemeChoice { Key = "stalzone", Label = "STALZONE" });
            _themeCombo.Items.Add(new ThemeChoice { Key = "custom", Label = "Своя" });
            SelectTheme(_settings.Theme);
            _themeCombo.SelectedIndexChanged += delegate
            {
                var choice = _themeCombo.SelectedItem as ThemeChoice;
                if (choice != null) _settings.Theme = choice.Key;
                ApplyWelcomeTheme();
            };

            _previewPanel.Left = 24;
            _previewPanel.Top = 186;
            _previewPanel.Width = 560;
            _previewPanel.Height = 120;
            _previewPanel.BorderStyle = BorderStyle.FixedSingle;
            _previewPanel.Paint += Preview_Paint;

            var fileLabel = new Label { Text = "Файл локализации .lang", Left = 24, Top = 324, Width = 220, Height = 24 };
            _fileCombo.Left = 24;
            _fileCombo.Top = 350;
            _fileCombo.Width = 380;
            _fileCombo.DropDownStyle = ComboBoxStyle.DropDownList;

            var find = new Button { Text = "Найти .lang", Left = 410, Top = 348, Width = 110, Height = 30 };
            find.Click += delegate { FindFiles(); };
            var browse = new Button { Text = "Обзор…", Left = 520, Top = 348, Width = 80, Height = 30 };
            browse.Click += delegate { BrowseFile(); };

            _status.Text = "Файлы .lang ещё не искали.";
            _status.Left = 24;
            _status.Top = 388;
            _status.Width = 560;
            _status.Height = 44;

            _startButton.Text = "Начать";
            _startButton.Left = 424;
            _startButton.Top = 470;
            _startButton.Width = 150;
            _startButton.Height = 38;
            _startButton.Click += delegate { Finish(); };

            content.Controls.Add(heading);
            content.Controls.Add(text);
            content.Controls.Add(themeLabel);
            content.Controls.Add(_themeCombo);
            content.Controls.Add(_previewPanel);
            content.Controls.Add(fileLabel);
            content.Controls.Add(_fileCombo);
            content.Controls.Add(find);
            content.Controls.Add(browse);
            content.Controls.Add(_status);
            content.Controls.Add(_startButton);

            Controls.Add(content);
            Controls.Add(brand);
            AcceptButton = _startButton;

            string last = _settings.LastFile;
            if (!string.IsNullOrEmpty(last) && File.Exists(last))
            {
                _fileCombo.Items.Add(new LangFileItem { Path = last });
                _fileCombo.SelectedIndex = 0;
                _status.Text = "Последний файл: " + last;
            }

            ApplyWelcomeTheme();
            LoadPreviewImage();
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
            _themeCombo.SelectedIndex = 5;
        }

        private void ApplyWelcomeTheme()
        {
            ThemePalette palette = ThemePalette.FromSettings(_settings);
            ThemeRuntime.Current = palette;
            ThemeRuntime.Apply(this, palette);

            foreach (Control control in Controls)
            {
                var brand = control as Panel;
                if (brand != null && object.Equals(brand.Tag, "brand"))
                    brand.BackColor = ThemePalette.Mix(palette.Panel, palette.Selection, 0.18);
            }

            _brandTop.ForeColor = palette.Text;
            _brandBottom.ForeColor = ThemePalette.Mix(palette.Text, palette.Panel, 0.35);
            _startButton.BackColor = palette.Accent;
            _startButton.ForeColor = ThemePalette.Contrast(palette.Accent);
            _startButton.FlatStyle = FlatStyle.Flat;
            _startButton.FlatAppearance.BorderColor = ThemePalette.Mix(palette.Accent, Color.Black, 0.35);
            _previewPanel.BackColor = palette.Form;
            LoadPreviewImage();
            _previewPanel.Invalidate();
        }

        private void Preview_Paint(object sender, PaintEventArgs e)
        {
            ThemePalette palette = ThemePalette.FromSettings(_settings);
            e.Graphics.Clear(palette.Form);

            if (_previewImage != null)
            {
                e.Graphics.DrawImage(_previewImage, new Rectangle(0, 0, _previewPanel.Width, _previewPanel.Height));
                using (var overlay = new SolidBrush(Color.FromArgb(165, palette.Form)))
                    e.Graphics.FillRectangle(overlay, 0, 0, _previewPanel.Width, _previewPanel.Height);
            }

            using (var panelBrush = new SolidBrush(palette.Panel))
                e.Graphics.FillRectangle(panelBrush, 10, 10, _previewPanel.Width - 20, 30);
            using (var cellBrush = new SolidBrush(palette.Cell))
                e.Graphics.FillRectangle(cellBrush, 10, 46, _previewPanel.Width - 20, 30);
            using (var accentBrush = new SolidBrush(palette.Accent))
                e.Graphics.FillRectangle(accentBrush, 10, 84, 130, 24);
            using (var textBrush = new SolidBrush(palette.Text))
            {
                e.Graphics.DrawString("STALZONE", Font, textBrush, 18, 16);
                e.Graphics.DrawString("Пример текста локализации", Font, textBrush, 18, 52);
            }
            using (var accentText = new SolidBrush(ThemePalette.Contrast(palette.Accent)))
                e.Graphics.DrawString("Играть", Font, accentText, 50, 87);
        }

        private void FindFiles()
        {
            _status.Text = "Поиск .lang…";
            _status.Refresh();
            var found = LangLocator.FindAll();
            _fileCombo.Items.Clear();
            foreach (string file in found) _fileCombo.Items.Add(new LangFileItem { Path = file });
            if (found.Count == 0)
            {
                _status.Text = "Файлы .lang не найдены. Нажмите «Обзор…» или укажите файл позже.";
                return;
            }
            string preferred = LangLocator.ChoosePreferred(found, null, _settings.LastFile);
            for (int i = 0; i < _fileCombo.Items.Count; i++)
            {
                var item = _fileCombo.Items[i] as LangFileItem;
                if (item != null && string.Equals(item.Path, preferred, StringComparison.OrdinalIgnoreCase))
                {
                    _fileCombo.SelectedIndex = i;
                    break;
                }
            }
            _status.Text = "Найдено файлов: " + found.Count + ". Выбран: " + Path.GetFileName(preferred);
        }

        private void BrowseFile()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Открыть .lang";
                dialog.Filter = "Файлы локализации (*.lang)|*.lang|Все файлы (*.*)|*.*";
                if (_settings.LastFile.Length > 0)
                {
                    try { dialog.InitialDirectory = Path.GetDirectoryName(_settings.LastFile); }
                    catch { }
                }
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                var item = new LangFileItem { Path = dialog.FileName };
                _fileCombo.Items.Add(item);
                _fileCombo.SelectedItem = item;
                _status.Text = "Выбран: " + dialog.FileName;
            }
        }

        private void Finish()
        {
            _settings.WelcomeShown = true;
            _settings.Save();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void LoadPreviewImage()
        {
            if (_previewImage != null)
            {
                _previewImage.Dispose();
                _previewImage = null;
            }
            if (!string.Equals(_settings.Theme, "custom", StringComparison.OrdinalIgnoreCase)) return;
            if (string.IsNullOrEmpty(_settings.CustomImage) || !File.Exists(_settings.CustomImage)) return;
            try
            {
                using (var source = Image.FromFile(_settings.CustomImage))
                    _previewImage = new Bitmap(source);
            }
            catch
            {
                _previewImage = null;
            }
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
