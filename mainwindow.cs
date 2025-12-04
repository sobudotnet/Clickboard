using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace Clickboard
{
    public partial class mainwindow : Form
    {
        private TextBox clipboardTextBox;
        private Button addClipboardButton;
        private List<string> clipboardEntries = new List<string>();
        private readonly string configPath = Path.Combine(Application.StartupPath, "clickboard.cfg");
        private readonly string keyPath = Path.Combine(Application.StartupPath, "clickboard.key");
        private readonly string pinPath = Path.Combine(Application.StartupPath, "clickboard.pin");
        
        public mainwindow()
        {
            InitializeComponent();

            this.BackColor = ColorTranslator.FromHtml("#240115");

            using (var ms = new MemoryStream(Properties.Resources.clickboard))
            {
                this.Icon = new Icon(ms);
            }

            DebugLogger.Log("E1000: Application started.");
            // fix/workaround of the first input not creating buttons on init, 
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

            if (File.Exists(pinPath))
            {
                using (var pinForm = new PinEntryForm(pinPath))
                {
                    if (pinForm.ShowDialog() != DialogResult.OK)
                    {
                        DebugLogger.Log("E5000: Incorrect PIN or cancelled. Exiting.");
                        Environment.Exit(0);
                    }
                }
            }

            var inputPanel = new Panel
            {
                Height = 40,
                Dock = DockStyle.Top,
                BackColor = ColorTranslator.FromHtml("#2f131e")
            };

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
                Text = "LOGS",
                Width = 120,
                Height = 24,
                Location = new Point(addClipboardButton.Location.X + addClipboardButton.Width + 10, 9),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#cec3c1"),
                ForeColor = ColorTranslator.FromHtml("#240115"),
                Font = new Font("Segoe UI", 9, FontStyle.Regular)
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
                using (var pinMgmt = new PinManagementForm(pinPath))
                {
                    pinMgmt.ShowDialog();
                }
            };
            toolTip.SetToolTip(pinToggleButton, "Set, change, or remove PIN code");
            inputPanel.Controls.Add(pinToggleButton);

            inputPanel.Controls.Add(clipboardTextBox);
            inputPanel.Controls.Add(addClipboardButton);

            this.Controls.Add(inputPanel);
            inputPanel.BringToFront();

            LoadClipboardButtons(firstRun);

            titleBar.MouseDown += TitleBar_MouseDown;
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            DebugLogger.Log("E1001: Application closing.");
            SaveClipboardButtons();
            this.Close();
        }

        private void addClipboardButton_Click(object sender, EventArgs e)
        {
            string text = clipboardTextBox.Text.Trim();
            if (string.IsNullOrEmpty(text) || text == "Enter text to save")
            {
                DebugLogger.Log("E2000: Attempted to add empty clipboard button.", "WARN");
                toolTip.Show("Please enter text.", addClipboardButton, 2000);
                return;
            }

            AddClipboardButton(text);
            clipboardEntries.Add(text);
            DebugLogger.Log($"E2001: Clipboard button added");
            clipboardTextBox.Text = "Enter text to save";
            clipboardTextBox.ForeColor = Color.Gray;

            SaveClipboardButtons();
        }

        private void AddClipboardButton(string text)
        {
            Button clipboardButton = new Button
            {
                Text = text,
                Width = 180,
                Height = 32,
                Margin = new Padding(3),
                Tag = text,
                BackColor = ColorTranslator.FromHtml("#913B5D"),
                ForeColor = ColorTranslator.FromHtml("#2f131e"),
                FlatStyle = FlatStyle.Flat
            };
            clipboardButton.Click += (s, args) =>
            {
                Clipboard.SetText((string)clipboardButton.Tag); // Always use the latest value
                DebugLogger.Log($"E2002: Clipboard button clicked");
                toolTip.Show("Copied to clipboard!", clipboardButton, 2000);
            };
            toolTip.SetToolTip(clipboardButton, "Click to copy this text");

            var contextMenu = new ContextMenuStrip();
            var deleteItem = new ToolStripMenuItem("Delete");
            deleteItem.Click += (s, e) =>
            {
                buttonListPanel.Controls.Remove(clipboardButton);
                clipboardEntries.Remove(text);
                DebugLogger.Log($"E2003: Clipboard button deleted");
                SaveClipboardButtons();
            };
            contextMenu.Items.Add(deleteItem);

            // Edit clipboard value (actual string to paste)
            var editValueItem = new ToolStripMenuItem("Edit");
            editValueItem.Click += (s, e) =>
            {
                string currentValue = (string)clipboardButton.Tag;
                string newValue = ShowInputDialog("Edit Clipboard Value", "Edit the value to be pasted:", currentValue);
                if (!string.IsNullOrWhiteSpace(newValue) && newValue != currentValue)
                {
                    int idx = clipboardEntries.IndexOf(currentValue);
                    DebugLogger.Log($"E2003: Clipboard button edited");
                    if (idx >= 0)
                    {
                        clipboardEntries[idx] = newValue;
                        clipboardButton.Tag = newValue;
                        SaveClipboardButtons();
                    }
                }
            };
            contextMenu.Items.Add(editValueItem);

            // Edit display name (button text)
            var editDisplayNameItem = new ToolStripMenuItem("Edit Display Name");
            editDisplayNameItem.Click += (s, e) =>
            {
                string currentDisplay = clipboardButton.Text;
                string newDisplay = ShowInputDialog("Edit Display Name", "Edit the button's display name:", currentDisplay);
                if (!string.IsNullOrWhiteSpace(newDisplay) && newDisplay != currentDisplay)
                {
                    clipboardButton.Text = newDisplay;
                    // clipboardButton.Tag remains unchanged, so clipboard value is not affected
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
        }

        private void SaveClipboardButtons()
        {
            try
            {
                var key = GetOrCreateKey();
                var plain = string.Join("\n", clipboardEntries);
                var encrypted = EncryptString(plain, key);
                File.WriteAllText(configPath, encrypted);
            }
            catch { }
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
                var plain = DecryptString(encrypted, key);
                foreach (var line in plain.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    clipboardEntries.Add(line);
                    AddClipboardButton(line);
                }
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
    }
}
