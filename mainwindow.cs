using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using ImageMagick;

namespace Clickboard
{
    public partial class mainwindow : Form
    {
        private TextBox clipboardTextBox;
        private Button addClipboardButton;
        private List<ClipboardEntry> clipboardEntries = new List<ClipboardEntry>();
        private readonly string configPath = Path.Combine(Application.StartupPath, "clickboard.cfg");
        private readonly string keyPath = Path.Combine(Application.StartupPath, "clickboard.key");
        private readonly string pinPath = Path.Combine(Application.StartupPath, "clickboard.pin");

        private Theme currentTheme;
        private List<Theme> availableThemes = new List<Theme>();
        private Button themeLoaderButton;

        private Panel inputPanel;
        private Panel titleBar;

        private int originalWidth;
        private FlowLayoutPanel buttonListPanel;

        private Button closeButton;
        private Button settingsButton; 

        public mainwindow()
        {
            var loc = AudioPlayerForm.LoadMainWindowLocation(this.Size);
            if (loc.HasValue)
            {
                this.StartPosition = FormStartPosition.Manual;
                this.Location = loc.Value;
            }
            InitializeComponent();

            this.BackColor = ColorTranslator.FromHtml("#240115");

            using (var ms = new MemoryStream(Properties.Resources.clickboard))
            {
                this.Icon = new Icon(ms);
            }

            DebugLogger.Log("E1000: Application started.");
            bool firstRun = false;
            if (!File.Exists(keyPath))
            {
                GetOrCreateKey();
                firstRun = true;
            }
            if (!File.Exists(configPath))
            {
                var key = GetOrCreateKey();
                var exampleButton = " ";
                var encrypted = EncryptString(exampleButton, key);
                File.WriteAllText(configPath, encrypted);
                firstRun = true;
            }

            InitializeThemes();

            buttonListPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = ColorTranslator.FromHtml("#240115"),
                BorderStyle = BorderStyle.None,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(10),
                Margin = new Padding(0),
            };
            buttonListPanel.SizeChanged += (s, e) =>
            {
                int buttonWidth = 120 + 3 * 2; 
                buttonListPanel.Width = 5 * buttonWidth + 20; 
            };
            buttonListPanel.MinimumSize = new Size(5 * (120 + 3 * 2) + 20, 0);
            this.Controls.Add(buttonListPanel);

            inputPanel = new Panel
            {
                Height = 48,
                Dock = DockStyle.Top,
                BackColor = ColorTranslator.FromHtml("#2f131e")
            };
            this.Controls.Add(inputPanel);


            clipboardTextBox = new TextBox
            {
                Width = 300,
                Text = "Enter text to save",
                ForeColor = ColorTranslator.FromHtml("#240115"),
                Location = new Point(10, 8),
                BackColor = ColorTranslator.FromHtml("#87f5fb"),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };
            toolTip.SetToolTip(clipboardTextBox, "Type text to save as a clipboard button");
            clipboardTextBox.Enter += (s, e) =>
            {
                if (clipboardTextBox.Text == "Enter text to save")
                {
                    clipboardTextBox.Text = "";
                    clipboardTextBox.ForeColor = ColorTranslator.FromHtml("#240115");
                }
            };
            clipboardTextBox.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(clipboardTextBox.Text))
                {
                    clipboardTextBox.Text = "Enter text to save";
                    clipboardTextBox.ForeColor = ColorTranslator.FromHtml("#240115");
                }
            };

            addClipboardButton = new Button
            {
                Text = "Add",
                Width = 70,
                Height = 24,
                Location = new Point(clipboardTextBox.Width + 25, 9),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#de3c4b"),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            addClipboardButton.FlatAppearance.BorderSize = 0;
            addClipboardButton.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#de3c4b");
            addClipboardButton.FlatAppearance.MouseDownBackColor = ColorTranslator.FromHtml("#de3c4b");
            addClipboardButton.Click += addClipboardButton_Click;
            toolTip.SetToolTip(addClipboardButton, "Add clipboard button");

            var diagnosticsButton = new Button
            {
                Text = "L",
                Width = 24,
                Height = 24,
                Location = new Point(addClipboardButton.Location.X + addClipboardButton.Width + 10, 9),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#cec3c1"),
                ForeColor = ColorTranslator.FromHtml("#240115"),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            diagnosticsButton.Click += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start("explorer.exe", DebugLogger.GetLogFilePath());
                    DebugLogger.Log("E4000: Diagnostics log opened for user.");
                }
                catch (Exception ex)
                {
                    DebugLogger.LogException(ex, "E4001: Opening diagnostics log");
                    MessageBox.Show("Unable to open diagnostics log file.", "Clickboard", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            toolTip.SetToolTip(diagnosticsButton, "Open diagnostics log file to send to support");
            inputPanel.Controls.Add(diagnosticsButton);

            var pinToggleButton = new Button
            {
                Text = "PIN",
                Width = 70,
                Height = 24,
                Location = new Point(diagnosticsButton.Location.X + diagnosticsButton.Width + 10, 9),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#cec3c1"),
                ForeColor = ColorTranslator.FromHtml("#240115"),
                Font = new Font("Segoe UI", 9, FontStyle.Regular)
            };
            pinToggleButton.Click += (s, e) =>
            {
                using (var pinMgmt = new PinManagementForm(pinPath, currentTheme))
                {
                    pinMgmt.ShowDialog();
                }
            };
            toolTip.SetToolTip(pinToggleButton, "Set, change, or remove PIN code");
            inputPanel.Controls.Add(pinToggleButton);

            settingsButton = new Button
            {
                Width = 24,
                Height = 24,
                BackColor = ColorTranslator.FromHtml("#cec3c1"),
                ForeColor = ColorTranslator.FromHtml("#240115"),
                FlatStyle = FlatStyle.Flat,
                Text = "⚙",
                Font = new Font("Segoe UI Symbol", 10, FontStyle.Bold),
                Location = new Point(pinToggleButton.Location.X + pinToggleButton.Width + 10, pinToggleButton.Location.Y),
                TabStop = false
            };
            settingsButton.FlatAppearance.BorderSize = 0;
            settingsButton.Click += (s, e) =>
            {
                var settingsForm = new Form
                {
                    Text = "Settings",
                    Width = 320,
                    Height = 400,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent,
                    BackColor = Color.Black
                };
                settingsForm.BackColor = Color.Black;
                settingsForm.ForeColor = Color.White;
                settingsForm.Font = new Font("Segoe UI", 9, FontStyle.Regular);

                var wipeButton = new Button
                {
                    Text = "Wipe Data",
                    Width = 100,
                    Height = 28,
                    BackColor = Color.Red,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
                };
                wipeButton.FlatAppearance.BorderSize = 0;
                wipeButton.Location = new Point((settingsForm.ClientSize.Width - 100) / 2, settingsForm.ClientSize.Height - 60);
                wipeButton.Click += (wipeSender, wipeArgs) =>
                {
                    var confirm = MessageBox.Show("This will erase all clipboard data, theme, and settings. Are you sure?", "Confirm Data Wipe", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (confirm == DialogResult.Yes)
                    {
                        try
                        {
                            if (File.Exists(configPath)) File.Delete(configPath);
                            if (File.Exists(keyPath)) File.Delete(keyPath);
                            if (File.Exists(pinPath)) File.Delete(pinPath);
                            var themeFile = Path.Combine(Application.StartupPath, "selected.theme");
                            if (File.Exists(themeFile)) File.Delete(themeFile);
                            clipboardEntries.Clear();
                            buttonListPanel.Controls.Clear();
                            GetOrCreateKey();
                            InitializeThemes();
                            LoadClipboardButtons(true);
                            LoadSelectedTheme();
                            MessageBox.Show("All data and settings have been wiped.", "Wipe Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            settingsForm.Close();
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogException(ex, "Data wipe failed");
                            MessageBox.Show("Failed to wipe all data. Please check file permissions.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                };
                settingsForm.Controls.Add(wipeButton);

                var loadOnStartupCheckbox = new CheckBox
                {
                    Text = "Load on Startup",
                    Checked = Properties.Settings.Default.LoadOnStartup,
                    Left = 20,
                    Top = 20,
                    Width = 200,
                    ForeColor = Color.White,
                    BackColor = Color.Black
                };
                settingsForm.Controls.Add(loadOnStartupCheckbox);

                var saveSettingsButton = new Button
                {
                    Text = "Save Settings",
                    Width = 120,
                    Height = 28,
                    BackColor = ColorTranslator.FromHtml("#cec3c1"),
                    ForeColor = ColorTranslator.FromHtml("#240115"),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Location = new Point(20, 60)
                };
                saveSettingsButton.FlatAppearance.BorderSize = 0;
                saveSettingsButton.Click += (s2, e2) =>
                {
                    Properties.Settings.Default.LoadOnStartup = loadOnStartupCheckbox.Checked;
                    Properties.Settings.Default.Save();
                    MessageBox.Show("Settings saved.", "Clickboard", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };
                settingsForm.Controls.Add(saveSettingsButton);

                var illegibleLabel = new Label
                {
                    Text = "made with love, sobu - v2.0",
                    AutoSize = false,
                    Width = settingsForm.ClientSize.Width,
                    Height = 16,
                    Left = 0,
                    Top = settingsForm.ClientSize.Height - 22,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.FromArgb(40, 40, 40),
                    Font = new Font("Segoe UI", 7, FontStyle.Italic)
                };
                settingsForm.Controls.Add(illegibleLabel);

                settingsForm.ShowDialog();
            };
            toolTip.SetToolTip(settingsButton, "App Settings");
            inputPanel.Controls.Add(settingsButton);

            themeLoaderButton = new Button
            {
                Width = 24,
                Height = 24,
                BackColor = Color.Black,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Text = "T",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Location = new Point(settingsButton.Location.X + settingsButton.Width + 10, settingsButton.Location.Y),
                TabStop = false
            };
            themeLoaderButton.FlatAppearance.BorderSize = 0;
            themeLoaderButton.Click += (s, e) => ShowThemeSelector();
            toolTip.SetToolTip(themeLoaderButton, "Change Theme");

            
            closeButton = new Button
            {
                Text = "X",
                Width = 32,
                Height = 24,
                Location = new Point(themeLoaderButton.Location.X + themeLoaderButton.Width + 20, themeLoaderButton.Location.Y),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Red,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TabStop = false
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += closeButton_Click;
            toolTip.SetToolTip(closeButton, "Close Clickboard");
            inputPanel.Controls.Add(closeButton);

            inputPanel.Controls.Add(clipboardTextBox);
            inputPanel.Controls.Add(addClipboardButton);
            inputPanel.Controls.Add(themeLoaderButton);

            LoadClipboardButtons(firstRun);

            clipboardTextBox.AllowDrop = true;
            clipboardTextBox.DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.Bitmap))
                    e.Effect = DragDropEffects.Copy;
                else
                    e.Effect = DragDropEffects.None;
            };

            
            clipboardTextBox.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.V)
                {
                    if (Clipboard.ContainsImage())
                    {
                        var img = Clipboard.GetImage();
                        using (var ms = new MemoryStream())
                        {
                            img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            var entry = new ClipboardEntry
                            {
                                ImageData = ms.ToArray(),
                                ImageFormat = ".png",
                                DisplayName = "[Pasted Image]"
                            };
                            clipboardEntries.Add(entry);
                            AddClipboardButton(entry);
                        }
                        SaveClipboardButtons();
                        e.SuppressKeyPress = true;
                    }
                }
            };
            //stuff to make .webp work
            clipboardTextBox.DragDrop += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    foreach (var file in files)
                    {
                        try
                        {
                            var ext = Path.GetExtension(file).ToLower();
                            if (ext == ".webp")
                            {
                                using (var magickImage = new MagickImage(file))
                                {
                                    using (var ms = new MemoryStream())
                                    {
                                        magickImage.Format = MagickFormat.Png; 
                                        magickImage.Write(ms);
                                        var bmp = new Bitmap(ms);
                                        var entry = new ClipboardEntry
                                        {
                                            ImageData = ms.ToArray(),
                                            ImageFormat = ".png",
                                            DisplayName = Path.GetFileName(file)
                                        };
                                        clipboardEntries.Add(entry);
                                        AddClipboardButton(entry);
                                    }
                                }
                            }
                            //image handling for other formats
                            else if (ext == ".png" || ext == ".bmp" || ext == ".jpg" || ext == ".jpeg" || ext == ".img")
                            {
                                var img = Image.FromFile(file);
                                using (var ms = new MemoryStream())
                                {
                                    img.Save(ms, img.RawFormat);
                                    var entry = new ClipboardEntry
                                    {
                                        ImageData = ms.ToArray(),
                                        ImageFormat = ext,
                                        DisplayName = Path.GetFileName(file)
                                    };
                                    clipboardEntries.Add(entry);
                                    AddClipboardButton(entry);
                                }
                            }
                            else if (ext == ".gif")
                            {
                                var gifBytes = File.ReadAllBytes(file);
                                var entry = new ClipboardEntry
                                {
                                    ImageData = gifBytes,
                                    ImageFormat = ".gif",
                                    DisplayName = Path.GetFileName(file)
                                };
                                clipboardEntries.Add(entry);
                                AddClipboardButton(entry);
                            }
                            // AUDIO FILE SUPPORT
                            else if (ext == ".mp3" || ext == ".wav" || ext == ".ogg")
                            {
                                var audioBytes = File.ReadAllBytes(file);
                                var entry = new ClipboardEntry
                                {
                                    AudioData = audioBytes,
                                    AudioFormat = ext,
                                    DisplayName = Path.GetFileName(file)
                                };
                                clipboardEntries.Add(entry);
                                AddClipboardButton(entry);
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogException(ex, "E9001: DragDrop file failed");
                        }
                    }
                    SaveClipboardButtons();
                }
                else if (e.Data.GetDataPresent(DataFormats.Bitmap))
                {
                    var img = (Image)e.Data.GetData(DataFormats.Bitmap);
                    using (var ms = new MemoryStream())
                    {
                        img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        var entry = new ClipboardEntry
                        {
                            ImageData = ms.ToArray(),
                            ImageFormat = ".png",
                            DisplayName = "[IMG]"
                        };
                        clipboardEntries.Add(entry);
                        AddClipboardButton(entry);
                    }
                    SaveClipboardButtons();
                }
            };

 
            buttonListPanel.AllowDrop = true;
            buttonListPanel.DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.Bitmap))
                    e.Effect = DragDropEffects.Copy;
                else
                    e.Effect = DragDropEffects.None;
            };
            buttonListPanel.DragDrop += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    foreach (var file in files)
                    {
                        try
                        {
                            var ext = Path.GetExtension(file).ToLower();
                            if (ext == ".webp")
                            {
                                using (var magickImage = new ImageMagick.MagickImage(file))
                                {
                                    using (var ms = new MemoryStream())
                                    {
                                        magickImage.Format = ImageMagick.MagickFormat.Png;
                                        magickImage.Write(ms);
                                        var entry = new ClipboardEntry
                                        {
                                            ImageData = ms.ToArray(),
                                            ImageFormat = ".png",
                                            DisplayName = Path.GetFileName(file)
                                        };
                                        clipboardEntries.Add(entry);
                                        AddClipboardButton(entry);
                                    }
                                }
                            }
                            else if (ext == ".png" || ext == ".bmp" || ext == ".jpg" || ext == ".jpeg" || ext == ".img")
                            {
                                var img = Image.FromFile(file);
                                using (var ms = new MemoryStream())
                                {
                                    img.Save(ms, img.RawFormat);
                                    var entry = new ClipboardEntry
                                    {
                                        ImageData = ms.ToArray(),
                                        ImageFormat = ext,
                                        DisplayName = Path.GetFileName(file)
                                    };
                                    clipboardEntries.Add(entry);
                                    AddClipboardButton(entry);
                                }
                            }
                            else if (ext == ".gif")
                            {
                                var gifBytes = File.ReadAllBytes(file);
                                var entry = new ClipboardEntry
                                {
                                    ImageData = gifBytes,
                                    ImageFormat = ".gif",
                                    DisplayName = Path.GetFileName(file)
                                };
                                clipboardEntries.Add(entry);
                                AddClipboardButton(entry);
                            }
                            else if (ext == ".mp3" || ext == ".wav" || ext == ".ogg")
                            {
                                var audioBytes = File.ReadAllBytes(file);
                                var entry = new ClipboardEntry
                                {
                                    AudioData = audioBytes,
                                    AudioFormat = ext,
                                    DisplayName = Path.GetFileName(file)
                                };
                                clipboardEntries.Add(entry);
                                AddClipboardButton(entry);
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogException(ex, "E9001: DragDrop file failed");
                        }
                    }
                    SaveClipboardButtons();
                }
                else if (e.Data.GetDataPresent(DataFormats.Bitmap))
                {
                    var img = (Image)e.Data.GetData(DataFormats.Bitmap);
                    using (var ms = new MemoryStream())
                    {
                        img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        var entry = new ClipboardEntry
                        {
                            ImageData = ms.ToArray(),
                            ImageFormat = ".png",
                            DisplayName = "[IMG]"
                        };
                        clipboardEntries.Add(entry);
                        AddClipboardButton(entry);
                    }
                    SaveClipboardButtons();
                }
            };

            originalWidth = this.Width;
            int rightEdge = closeButton.Location.X + closeButton.Width;

            this.Width = rightEdge + 30;

            LoadSelectedTheme();

            titleBar = new Panel
            {
                Height = 25,
                Dock = DockStyle.Top,
                BackColor = currentTheme?.HeaderBarColor ?? ColorTranslator.FromHtml("#2f131e"),
                ForeColor = currentTheme?.HeaderBarTextColor ?? ColorTranslator.FromHtml("#87f5fb")
            };
            titleBar.MouseDown += TitleBar_MouseDown;
            this.Controls.Add(titleBar);

            
            if (Properties.Settings.Default.LoadOnStartup)
            {
                try
                {
                    Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    rk.SetValue("Clickboard", Application.ExecutablePath);
                }
                catch { }
            }
            else
            {
                try
                {
                    Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    rk.DeleteValue("Clickboard", false);
                }
                catch { }
            }
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            DebugLogger.Log("E1001: Application closing.");
            SaveClipboardButtons();
            this.Close();
        }

        private void addClipboardButton_Click(object sender, EventArgs e)
        {
            // egg
            if (clipboardTextBox.Text.Trim() == "sobuisgay")
            {
                StartRainbowEasterEgg();
                return;
            }
            // egg

            string text = clipboardTextBox.Text.Trim();
            if (string.IsNullOrEmpty(text) || text == "Enter text to save")
            {
                DebugLogger.Log("E2000: Attempted to add empty clipboard button.", "WARN");
                toolTip.Show("Please enter text or paste/drag an image.", addClipboardButton, 2000);
                return;
            }

            var entry = new ClipboardEntry
            {
                TextValue = text,
                DisplayName = text
            };
            clipboardEntries.Add(entry);
            AddClipboardButton(entry);
            DebugLogger.Log("E2001: Clipboard button added");
            clipboardTextBox.Text = "Enter text to save";
            clipboardTextBox.ForeColor = Color.Gray;

            SaveClipboardButtons();
        }

        private string audioEmoji = "🎵";
private string GetAudioEmoji() => string.IsNullOrWhiteSpace(audioEmoji) ? "🎵" : audioEmoji;

        private void AddClipboardButton(ClipboardEntry entry)
        {
            try
            {
                Button clipboardButton = new Button
                {
                    Width = 120,
                    Height = 40,
                    Margin = new Padding(3),
                    Tag = entry,
                    BackColor = currentTheme?.ButtonColor ?? ColorTranslator.FromHtml("#913B5D"),
                    ForeColor = currentTheme?.ButtonTextColor ?? ColorTranslator.FromHtml("#2f131e"),
                    FlatStyle = FlatStyle.Flat
                };

                if (entry.IsImage)
                {
                    clipboardButton.Text = string.IsNullOrEmpty(entry.DisplayName) ? "[Image]" : entry.DisplayName;
                    if (entry.ImageFormat == ".gif")
                    {
                        // Use animated GIF for button image (shows animation in button)
                        using (var ms = new MemoryStream(entry.ImageData))
                        {
                            var gifImg = Image.FromStream(ms);
                            clipboardButton.Image = new Bitmap(gifImg, new Size(32, 28));
                        }
                        clipboardButton.ImageAlign = ContentAlignment.MiddleLeft;
                        clipboardButton.TextAlign = ContentAlignment.MiddleRight;
                        clipboardButton.TextImageRelation = TextImageRelation.ImageBeforeText;
                    }
                    else
                    {
                        var bmp = new Bitmap(entry.GetImage(), new Size(32, 28));
                        clipboardButton.Image = bmp;
                        clipboardButton.ImageAlign = ContentAlignment.MiddleLeft;
                        clipboardButton.TextAlign = ContentAlignment.MiddleRight;
                        clipboardButton.TextImageRelation = TextImageRelation.ImageBeforeText;
                    }
                }
                else if (entry.IsAudio)
                {
                    clipboardButton.Text = $"{GetAudioEmoji()} {entry.DisplayName}";
                    clipboardButton.Image = null;
                }
                else
                {
                    clipboardButton.Text = entry.DisplayName ?? entry.TextValue;
                }

                clipboardButton.Click += (s, args) =>
                {
                    if (entry.IsImage)
                    {
                        if (entry.ImageFormat == ".gif")
                        {
                            string tempGifPath = Path.Combine(Path.GetTempPath(), $"Clickboard_{Guid.NewGuid()}.gif");
                            File.WriteAllBytes(tempGifPath, entry.ImageData);
                            var dataObj = new DataObject();
                            dataObj.SetFileDropList(new System.Collections.Specialized.StringCollection { tempGifPath });
                            dataObj.SetData(DataFormats.Bitmap, Image.FromStream(new MemoryStream(entry.ImageData)));
                            dataObj.SetData("GIF", false, new MemoryStream(entry.ImageData));
                            Clipboard.SetDataObject(dataObj, true);
                            DebugLogger.Log("E2002: Clipboard GIF button clicked (as file drop and GIF)");
                            toolTip.Show("GIF copied to clipboard!", clipboardButton, 2000);
                        }
                        else
                        {
                            Clipboard.SetImage(entry.GetImage());
                            DebugLogger.Log("E2002: Clipboard image button clicked");
                            toolTip.Show("Image copied to clipboard!", clipboardButton, 2000);
                        }
                    }
                    else if (entry.IsAudio)
                    {
                        string safeFileName = string.IsNullOrWhiteSpace(entry.DisplayName) ? $"Clickboard_{Guid.NewGuid()}{entry.AudioFormat}" : entry.DisplayName;
                        foreach (char c in Path.GetInvalidFileNameChars())
                            safeFileName = safeFileName.Replace(c, '_');
                        string tempPath = Path.Combine(Path.GetTempPath(), safeFileName);
                        File.WriteAllBytes(tempPath, entry.AudioData);
                        var dataObj = new DataObject();
                        var files = new System.Collections.Specialized.StringCollection { tempPath };
                        dataObj.SetFileDropList(files);
                        Clipboard.SetDataObject(dataObj, true);
                        DebugLogger.Log("E2002: Clipboard audio button clicked (as file drop)");
                        toolTip.Show("Audio file copied to clipboard!", clipboardButton, 2000);
                    }
                    else
                    {
                        Clipboard.SetText(entry.TextValue);
                        DebugLogger.Log("E2002: Clipboard button clicked");
                        toolTip.Show("Copied to clipboard!", clipboardButton, 2000);
                    }
                };
                toolTip.SetToolTip(clipboardButton, entry.IsImage ? "Click to copy this image" : entry.IsAudio ? "Click to copy this audio file" : "Click to copy this text");

                var contextMenu = new ContextMenuStrip();
                var deleteItem = new ToolStripMenuItem("Delete");
                deleteItem.Click += (s, e) =>
                {
                    buttonListPanel.Controls.Remove(clipboardButton);
                    clipboardEntries.Remove(entry);
                    DebugLogger.Log("E2003: Clipboard button deleted");
                    SaveClipboardButtons();
                };
                contextMenu.Items.Add(deleteItem);

                if (!entry.IsImage && !entry.IsAudio)
                {
                    var editValueItem = new ToolStripMenuItem("Edit");
                    editValueItem.Click += (s, e) =>
                    {
                        string currentValue = entry.TextValue;
                        string newValue = ShowInputDialog("Edit Clipboard Value", "Edit the value to be pasted:", currentValue);
                        if (!string.IsNullOrWhiteSpace(newValue) && newValue != currentValue)
                        {
                            entry.TextValue = newValue;
                            SaveClipboardButtons();
                        }
                    };
                    contextMenu.Items.Add(editValueItem);
                }

                if (entry.IsAudio)
                {
                    var playItem = new ToolStripMenuItem("Play");
                    playItem.Click += (s, e) =>
                    {
                        try
                        {
                            var audioForm = new AudioPlayerForm(entry.AudioData, entry.AudioFormat, currentTheme);
                            audioForm.Show();
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogException(ex, "E9002: Play audio failed");
                            MessageBox.Show("Unable to play audio file.", "Clickboard", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    };
                    contextMenu.Items.Add(playItem);
                }

                var editDisplayNameItem = new ToolStripMenuItem("Edit Display Name");
                editDisplayNameItem.Click += (s, e) =>
                {
                    string currentDisplay = entry.DisplayName ?? (entry.IsImage ? "[Image]" : entry.TextValue);
                    string newDisplay = ShowInputDialog("Edit Display Name", "Edit the button's display name:", currentDisplay);
                    if (!string.IsNullOrWhiteSpace(newDisplay) && newDisplay != currentDisplay)
                    {
                        entry.DisplayName = newDisplay;
                        clipboardButton.Text = newDisplay;
                    }
                };
                contextMenu.Items.Add(editDisplayNameItem);

                clipboardButton.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        contextMenu.Show(clipboardButton, e.Location);
                    }
                };

                buttonListPanel.Controls.Add(clipboardButton);
                DebugLogger.Log($"Button added: IsImage={entry.IsImage}, IsAudio={entry.IsAudio}");
                DebugLogger.Log($"Added button to panel. Total controls: {buttonListPanel.Controls.Count}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "AddClipboardButton failed");
            }
        }

        private void SaveClipboardButtons()
        {
            try
            {
                DebugLogger.Log($"Attempting to serialize {clipboardEntries.Count} clipboard entries.");
                var key = GetOrCreateKey();
                using (var ms = new MemoryStream())
                {
                    var bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    bf.Serialize(ms, clipboardEntries);
                    DebugLogger.Log($"Serialization complete. Data size: {ms.Length} bytes.");
                    var encrypted = EncryptString(Convert.ToBase64String(ms.ToArray()), key);
                    File.WriteAllText(configPath, encrypted);
                    DebugLogger.Log("Clipboard entries successfully encrypted and written to config.");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "SaveClipboardButtons failed");
            }
        }

        private void LoadClipboardButtons(bool firstRun = false)
        {
            clipboardEntries.Clear();
            buttonListPanel.Controls.Clear();

            if (!File.Exists(configPath) || !File.Exists(keyPath))
                return;

            try
            {
                var key = File.ReadAllBytes(keyPath);
                var encrypted = File.ReadAllText(configPath);

                if (string.IsNullOrWhiteSpace(encrypted))
                {
                    DebugLogger.Log("Config file is empty, no clipboard buttons to load.");
                    return;
                }

                var plain = DecryptString(encrypted, key);
                if (string.IsNullOrWhiteSpace(plain))
                {
                    DebugLogger.Log("Decrypted config is empty, no clipboard buttons to load.");
                    return;
                }

                var bytes = Convert.FromBase64String(plain);
                DebugLogger.Log($"Attempting to deserialize clipboard entries. Data size: {bytes.Length} bytes.");
                if (bytes.Length == 0)
                {
                    DebugLogger.Log("Config bytes are empty, no clipboard buttons to load.");
                    return;
                }

                using (var ms = new MemoryStream(bytes))
                {
                    var bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    clipboardEntries = (List<ClipboardEntry>)bf.Deserialize(ms);
                }
                DebugLogger.Log($"Deserialization complete. Loaded {clipboardEntries.Count} clipboard entries.");
                foreach (var entry in clipboardEntries)
                {
                    AddClipboardButton(entry);
                }
                DebugLogger.Log($"buttonListPanel.Controls.Count after loading: {buttonListPanel.Controls.Count}");
                DebugLogger.Log("E3000: Clipboard buttons loaded from config.");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "E3001: Loading clipboard buttons");
                if (!firstRun)
                {
                    MessageBox.Show("Failed to load clipboard buttons. Key may be missing or file corrupted: If issues persist contact @s.o.b.u on discord.", "Clickboard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            DebugLogger.Log($"buttonListPanel.Controls.Count after loading: {buttonListPanel.Controls.Count}");
        }

        private byte[] GetOrCreateKey()
        {
            if (File.Exists(keyPath))
                return File.ReadAllBytes(keyPath);

            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] key = new byte[32];
                rng.GetBytes(key);
                File.WriteAllBytes(keyPath, key);
                return key;
            }
        }

        private string EncryptString(string plainText, byte[] key)
        {
            using (var aes = new AesManaged())
            {
                aes.Key = key;
                aes.GenerateIV();
                using (var encryptor = aes.CreateEncryptor())
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                    byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                    byte[] result = new byte[aes.IV.Length + cipherBytes.Length];
                    Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
                    Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
                    return Convert.ToBase64String(result);
                }
            }
        }

        private string DecryptString(string cipherText, byte[] key)
        {
            byte[] fullCipher = Convert.FromBase64String(cipherText);
            using (var aes = new AesManaged())
            {
                aes.Key = key;
                byte[] iv = new byte[aes.BlockSize / 8];
                Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
                aes.IV = iv;
                using (var decryptor = aes.CreateDecryptor())
                {
                    byte[] cipherBytes = new byte[fullCipher.Length - iv.Length];
                    Buffer.BlockCopy(fullCipher, iv.Length, cipherBytes, 0, cipherBytes.Length);
                    byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                    return Encoding.UTF8.GetString(plainBytes);
                }
            }
        }

        private string ShowInputDialog(string title, string prompt, string defaultValue)
        {
            Form promptForm = new Form()
            {
                Width = 350,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title,
                StartPosition = FormStartPosition.CenterParent
            };
            Label textLabel = new Label() { Left = 10, Top = 20, Text = prompt, Width = 320 };
            TextBox inputBox = new TextBox() { Left = 10, Top = 50, Width = 320, Text = defaultValue };
            Button confirmation = new Button() { Text = "OK", Left = 250, Width = 80, Top = 80, DialogResult = DialogResult.OK };
            promptForm.Controls.Add(textLabel);
            promptForm.Controls.Add(inputBox);
            promptForm.Controls.Add(confirmation);
            promptForm.AcceptButton = confirmation;

            return promptForm.ShowDialog() == DialogResult.OK ? inputBox.Text : defaultValue;
        }

        private Point lastPoint;
        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                lastPoint = new Point(e.X, e.Y);
                titleBar.MouseMove += TitleBar_MouseMove;
                titleBar.MouseUp += TitleBar_MouseUp;
            }
        }
        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Left += e.X - lastPoint.X;
                this.Top += e.Y - lastPoint.Y;
            }
        }
        private void TitleBar_MouseUp(object sender, MouseEventArgs e)
        {
            titleBar.MouseMove -= TitleBar_MouseMove;
            titleBar.MouseUp -= TitleBar_MouseUp;
        }

        private void InitializeThemes()
        {

            availableThemes.Clear();

            // Hardcoded themes
            availableThemes.Add(new Theme
            {
                Name = "Classic Dark",
                BackgroundColor = ColorTranslator.FromHtml("#240115"),
                ButtonColor = ColorTranslator.FromHtml("#913B5D"),
                ButtonTextColor = ColorTranslator.FromHtml("#2f131e"),
                InputFieldColor = ColorTranslator.FromHtml("#87f5fb"),
                InputFieldTextColor = ColorTranslator.FromHtml("#240115"),
                HeaderBarColor = ColorTranslator.FromHtml("#2f131e"),
                HeaderBarTextColor = ColorTranslator.FromHtml("#87f5fb"),
                TitleBarColor = ColorTranslator.FromHtml("#181824")
            });
            availableThemes.Add(new Theme
            {
                Name = "Light",
                BackgroundColor = ColorTranslator.FromHtml("#f5f5f5"),
                ButtonColor = ColorTranslator.FromHtml("#e0e0e0"),
                ButtonTextColor = ColorTranslator.FromHtml("#222"),
                InputFieldColor = ColorTranslator.FromHtml("#ffffff"),
                InputFieldTextColor = ColorTranslator.FromHtml("#222"),
                HeaderBarColor = ColorTranslator.FromHtml("#cccccc"),
                HeaderBarTextColor = ColorTranslator.FromHtml("#222"),
                TitleBarColor = ColorTranslator.FromHtml("#e0e0e0")
            });
            availableThemes.Add(new Theme
            {
                Name = "Blue",
                BackgroundColor = ColorTranslator.FromHtml("#1e2a38"),
                ButtonColor = ColorTranslator.FromHtml("#3a6ea5"),
                ButtonTextColor = ColorTranslator.FromHtml("#f5f5f5"),
                InputFieldColor = ColorTranslator.FromHtml("#eaf6fb"),
                InputFieldTextColor = ColorTranslator.FromHtml("#1e2a38"),
                HeaderBarColor = ColorTranslator.FromHtml("#274472"),
                HeaderBarTextColor = ColorTranslator.FromHtml("#eaf6fb"),
                TitleBarColor = ColorTranslator.FromHtml("#19304a")
            });
            availableThemes.Add(new Theme
            {
                Name = "Green",
                BackgroundColor = ColorTranslator.FromHtml("#1b2e1b"),
                ButtonColor = ColorTranslator.FromHtml("#4caf50"),
                ButtonTextColor = ColorTranslator.FromHtml("#f5f5f5"),
                InputFieldColor = ColorTranslator.FromHtml("#e8f5e9"),
                InputFieldTextColor = ColorTranslator.FromHtml("#1b2e1b"),
                HeaderBarColor = ColorTranslator.FromHtml("#388e3c"),
                HeaderBarTextColor = ColorTranslator.FromHtml("#e8f5e9"),
                TitleBarColor = ColorTranslator.FromHtml("#255d27")
            });
            availableThemes.Add(new Theme
            {
                Name = "Red",
                BackgroundColor = ColorTranslator.FromHtml("#2a1e1e"),
                ButtonColor = ColorTranslator.FromHtml("#de3c4b"),
                ButtonTextColor = ColorTranslator.FromHtml("#fff"),
                InputFieldColor = ColorTranslator.FromHtml("#fbe9e7"),
                InputFieldTextColor = ColorTranslator.FromHtml("#2a1e1e"),
                HeaderBarColor = ColorTranslator.FromHtml("#b71c1c"),
                HeaderBarTextColor = ColorTranslator.FromHtml("#fbe9e7"),
                TitleBarColor = ColorTranslator.FromHtml("#7b1f24")
            });
            availableThemes.Add(new Theme
            {
                Name = "Cyber Night",
                BackgroundColor = ColorTranslator.FromHtml("#181824"),
                ButtonColor = ColorTranslator.FromHtml("#00ffd0"),
                ButtonTextColor = ColorTranslator.FromHtml("#181824"),
                InputFieldColor = ColorTranslator.FromHtml("#23263a"),
                InputFieldTextColor = ColorTranslator.FromHtml("#00ffd0"),
                HeaderBarColor = ColorTranslator.FromHtml("#23263a"),
                HeaderBarTextColor = ColorTranslator.FromHtml("#00ffd0"),
                TitleBarColor = ColorTranslator.FromHtml("#0f0f1a")
            });
            availableThemes.Add(new Theme
            {
                Name = "Pink Dream",
                BackgroundColor = ColorTranslator.FromHtml("#ffe4f7"),
                ButtonColor = ColorTranslator.FromHtml("#ff69b4"),
                ButtonTextColor = ColorTranslator.FromHtml("#fff"),
                InputFieldColor = ColorTranslator.FromHtml("#fff0f6"),
                InputFieldTextColor = ColorTranslator.FromHtml("#ff69b4"),
                HeaderBarColor = ColorTranslator.FromHtml("#ffb6c1"),
                HeaderBarTextColor = ColorTranslator.FromHtml("#ff69b4"),
                TitleBarColor = ColorTranslator.FromHtml("#ffb6c1")
            });
            availableThemes.Add(new Theme
            {
                Name = "Christmas",
                BackgroundColor = ColorTranslator.FromHtml("#ffb36e"),
                ButtonColor = ColorTranslator.FromHtml("#c62828"),
                ButtonTextColor = ColorTranslator.FromHtml("#fff"),
                InputFieldColor = ColorTranslator.FromHtml("#fffde7"),
                InputFieldTextColor = ColorTranslator.FromHtml("#388e3c"),
                HeaderBarColor = ColorTranslator.FromHtml("#388e3c"),
                HeaderBarTextColor = ColorTranslator.FromHtml("#fffde7"),
                TitleBarColor = ColorTranslator.FromHtml("#c62828")
            });
            availableThemes.Add(new Theme
            {
                Name = "Solarized",
                BackgroundColor = ColorTranslator.FromHtml("#fdf6e3"),
                ButtonColor = ColorTranslator.FromHtml("#268bd2"),
                ButtonTextColor = ColorTranslator.FromHtml("#fdf6e3"),
                InputFieldColor = ColorTranslator.FromHtml("#eee8d5"),
                InputFieldTextColor = ColorTranslator.FromHtml("#268bd2"),
                HeaderBarColor = ColorTranslator.FromHtml("#073642"),
                HeaderBarTextColor = ColorTranslator.FromHtml("#b58900"),
                TitleBarColor = ColorTranslator.FromHtml("#b58900")
            });
            availableThemes.Add(new Theme
            {
                Name = "Midnight",
                BackgroundColor = ColorTranslator.FromHtml("#232946"),
                ButtonColor = ColorTranslator.FromHtml("#121629"),
                ButtonTextColor = ColorTranslator.FromHtml("#eebbc3"),
                InputFieldColor = ColorTranslator.FromHtml("#b8c1ec"),
                InputFieldTextColor = ColorTranslator.FromHtml("#232946"),
                HeaderBarColor = ColorTranslator.FromHtml("#121629"),
                HeaderBarTextColor = ColorTranslator.FromHtml("#eebbc3"),
                TitleBarColor = ColorTranslator.FromHtml("#232946")
            });
            availableThemes.Add(new Theme
            {
                Name = "Whiteaf",
                BackgroundColor = ColorTranslator.FromHtml("#ffffff"), 
                ButtonColor = ColorTranslator.FromHtml("#e0e0e0"), 
                ButtonTextColor = ColorTranslator.FromHtml("#222"), 
                InputFieldColor = ColorTranslator.FromHtml("#ffffff"),
                InputFieldTextColor = ColorTranslator.FromHtml("#de3c4b"), 
                HeaderBarColor = ColorTranslator.FromHtml("#de3c4b"),
                HeaderBarTextColor = ColorTranslator.FromHtml("#ffffff"), 
                TitleBarColor = ColorTranslator.FromHtml("#de3c4b") 
            });
            availableThemes.Add(new Theme
            {
                Name = "iykyk",
                BackgroundColor = ColorTranslator.FromHtml("#212B4E"),
                ButtonColor = ColorTranslator.FromHtml("#EBB436"),
                ButtonTextColor = ColorTranslator.FromHtml("#F2F6DE"),
                InputFieldColor = ColorTranslator.FromHtml("#EBB436"),
                InputFieldTextColor = ColorTranslator.FromHtml("#F2F6DE"),
                HeaderBarColor = ColorTranslator.FromHtml("#071D35"),
                HeaderBarTextColor = ColorTranslator.FromHtml("#071D35"),
                TitleBarColor = ColorTranslator.FromHtml("#071D35")
            });

            var themeFiles = Directory.GetFiles(Application.StartupPath, "*.theme.json");
            foreach (var file in themeFiles)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var theme = Newtonsoft.Json.JsonConvert.DeserializeObject<Theme>(json);
                    if (theme != null)
                    {
                        if (theme.TitleBarColor == default(Color))
                            theme.TitleBarColor = theme.HeaderBarColor;

                        if (
                            !string.IsNullOrWhiteSpace(theme.Name) &&
                            theme.BackgroundColor != default(Color) &&
                            theme.ButtonColor != default(Color) &&
                            theme.ButtonTextColor != default(Color) &&
                            theme.InputFieldColor != default(Color) &&
                            theme.InputFieldTextColor != default(Color) &&
                            theme.HeaderBarColor != default(Color) &&
                            theme.HeaderBarTextColor != default(Color))
                        {
                            availableThemes.Add(theme);
                        }
                    }
                }
                catch
                {

                }
            }
        }

        private void ShowThemeSelector()
        {
            var form = new Form
            {
                Text = "Select Theme",
                Width = 320,
                Height = 300,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.Black
            };

            var listBox = new ListBox
            {
                Dock = DockStyle.Top,
                Height = 200,
                BackColor = Color.Black,
                DrawMode = DrawMode.OwnerDrawFixed
            };

            foreach (var theme in availableThemes)
                listBox.Items.Add(theme.Name);


            listBox.DrawItem += (s, e) =>
            {
                e.DrawBackground();
                var textColor = Color.White;
                var font = new Font(listBox.Font, FontStyle.Bold);
                using (var brush = new SolidBrush(textColor))
                {
                    if (e.Index >= 0)
                        e.Graphics.DrawString(listBox.Items[e.Index].ToString(), font, brush, e.Bounds);
                }
                e.DrawFocusRectangle();
            };

            var okButton = new Button
            {
                Text = "Apply",
                Dock = DockStyle.Bottom,
                Height = 32,
                BackColor = currentTheme?.ButtonColor ?? Color.Black,
                ForeColor = currentTheme?.ButtonTextColor ?? Color.White,
                FlatStyle = FlatStyle.Flat
            };
            okButton.FlatAppearance.BorderSize = 0;
            okButton.Click += (s, e) =>
            {
                if (listBox.SelectedIndex >= 0)
                {
                    ApplyTheme(availableThemes[listBox.SelectedIndex]);
                    form.Close();
                }
            };

            form.Controls.Add(listBox);
            form.Controls.Add(okButton);
            form.ShowDialog();
        }

        private void ApplyTheme(Theme theme)
        {
            currentTheme = theme;
            this.BackColor = theme.BackgroundColor;
            buttonListPanel.BackColor = theme.BackgroundColor;
            buttonListPanel.ForeColor = theme.BackgroundColor;
            inputPanel.BackColor = theme.HeaderBarColor;
            clipboardTextBox.BackColor = theme.InputFieldColor;
            clipboardTextBox.ForeColor = theme.InputFieldTextColor;
            addClipboardButton.BackColor = theme.ButtonColor;
            addClipboardButton.ForeColor = theme.ButtonTextColor;
            foreach (Control c in buttonListPanel.Controls)
            {
                if (c is Button btn)
                {
                    btn.BackColor = theme.ButtonColor;
                    btn.ForeColor = theme.ButtonTextColor;
                }
            }
            if (titleBar != null)
            {
                titleBar.BackColor = theme.HeaderBarColor;
                titleBar.ForeColor = theme.HeaderBarTextColor;
            }

            SaveSelectedTheme(theme.Name);
        }
        private void SaveSelectedTheme(string themeName)
        {

            var theme = availableThemes.Find(t => t.Name.Equals(themeName, StringComparison.OrdinalIgnoreCase));
            if (theme != null)
            {
                // List of hardcoded theme names
                var hardcodedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Classic Dark", "Light", "Blue", "Green", "Red", "Cyber Night", "Pink Dream", "Christmas", "Solarized", "Midnight", "Whiteaf", "iykyk"
};
                if (!hardcodedNames.Contains(theme.Name))
                {
                    var themeFile = Path.Combine(Application.StartupPath, $"{theme.Name}.theme.json");
                    var themeJson = Newtonsoft.Json.JsonConvert.SerializeObject(theme);
                    File.WriteAllText(themeFile, themeJson);
                }
            }
            File.WriteAllText(Path.Combine(Application.StartupPath, "selected.theme"), themeName);
        }

        private void LoadSelectedTheme()
        {
            var themeFile = Path.Combine(Application.StartupPath, "selected.theme");
            if (File.Exists(themeFile))
            {
                var themeName = File.ReadAllText(themeFile).Trim();
                var theme = availableThemes.Find(t => t.Name.Equals(themeName, StringComparison.OrdinalIgnoreCase));
                if (theme != null)
                {
                    ApplyTheme(theme);
                    return;
                }
            }
            ApplyTheme(availableThemes[0]);
        }
//egg
        private Timer rainbowTimer;
        private double rainbowHue = 0;

        private void StartRainbowEasterEgg()
        {
            if (rainbowTimer != null)
                return;

            rainbowTimer = new Timer();
            rainbowTimer.Interval = 30;
            rainbowTimer.Tick += (s, e) =>
            {
                rainbowHue += 2;
                if (rainbowHue > 360) rainbowHue = 0;
                Color rainbow = ColorFromHSV(rainbowHue, 1, 1);

                // lmfao
                if (titleBar != null) titleBar.BackColor = rainbow;
                if (inputPanel != null) inputPanel.BackColor = rainbow;
                this.BackColor = rainbow;
                buttonListPanel.BackColor = rainbow;
            };
            rainbowTimer.Start();
        }

        private Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            switch (hi)
            {
                case 0: return Color.FromArgb(255, v, t, p);
                case 1: return Color.FromArgb(255, q, v, p);
                case 2: return Color.FromArgb(255, p, v, t);
                case 3: return Color.FromArgb(255, p, q, v);
                case 4: return Color.FromArgb(255, t, p, v);
                default: return Color.FromArgb(255, v, p, q);
            }
        }
        //egg
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            AudioPlayerForm.SaveMainWindowLocation(this.Location);
            DebugLogger.Log("E1001: Application closing.");
            SaveClipboardButtons();
            base.OnFormClosed(e);
        }
    }
}