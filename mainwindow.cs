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
        private readonly string configPath = Path.Combine(Application.StartupPath, "clipboard.cfg");
        private readonly string keyPath = Path.Combine(Application.StartupPath, "clipboard.key");

        public mainwindow()
        {
            InitializeComponent();

            
            this.BackColor = ColorTranslator.FromHtml("#240115"); 

            using (var ms = new MemoryStream(Properties.Resources.clickboard))
            {
                this.Icon = new Icon(ms);
            }

            Logger.Log("E1000: Application started.");

            
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
                    System.Diagnostics.Process.Start("explorer.exe", Logger.GetLogFilePath());
                    Logger.Log("E4000: Diagnostics log opened for user.");
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "E4001: Opening diagnostics log");
                    MessageBox.Show("Unable to open diagnostics log file.", "Clickboard", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            toolTip.SetToolTip(diagnosticsButton, "Open diagnostics log file to send to support");
            inputPanel.Controls.Add(diagnosticsButton);

            inputPanel.Controls.Add(clipboardTextBox);
            inputPanel.Controls.Add(addClipboardButton);

            this.Controls.Add(inputPanel);
            inputPanel.BringToFront();

            LoadClipboardButtons();

            titleBar.MouseDown += TitleBar_MouseDown;
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            Logger.Log("E1001: Application closing.");
            SaveClipboardButtons();
            this.Close();
        }

        private void addClipboardButton_Click(object sender, EventArgs e)
        {
            string text = clipboardTextBox.Text.Trim();
            if (string.IsNullOrEmpty(text) || text == "Enter text to save")
            {
                Logger.Log("E2000: Attempted to add empty clipboard button.", "WARN");
                toolTip.Show("Please enter text.", addClipboardButton, 2000);
                return;
            }

            AddClipboardButton(text);
            clipboardEntries.Add(text);
            Logger.Log($"E2001: Clipboard button added: {text}");
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
                Clipboard.SetText(text);
                Logger.Log($"E2002: Clipboard button clicked: {text}");
                toolTip.Show("Copied to clipboard!", clipboardButton, 2000);
            };
            toolTip.SetToolTip(clipboardButton, "Click to copy this text");

            var contextMenu = new ContextMenuStrip();
            var deleteItem = new ToolStripMenuItem("Delete");
            deleteItem.Click += (s, e) =>
            {
                buttonListPanel.Controls.Remove(clipboardButton);
                clipboardEntries.Remove(text);
                Logger.Log($"E2003: Clipboard button deleted: {text}");
                SaveClipboardButtons();
            };
            contextMenu.Items.Add(deleteItem);

            clipboardButton.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    contextMenu.Show(clipboardButton, e.Location);
                }
            };

            buttonListPanel.Controls.Add(clipboardButton);
        }

        // --- Persistence with encryption ---
        private void SaveClipboardButtons()
        {
            try
            {
                var key = GetOrCreateKey();
                var plain = string.Join("\n", clipboardEntries);
                var encrypted = EncryptString(plain, key);
                File.WriteAllText(configPath, encrypted);
            }
            catch { /* Handle errors as needed */ }
        }

        private void LoadClipboardButtons()
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
                Logger.Log("E3000: Clipboard buttons loaded from config.");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "E3001: Loading clipboard buttons");
                MessageBox.Show("Failed to load clipboard buttons. Key may be missing or file corrupted: If issues persist contact @s.o.b.u on discord.", "Clickboard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private byte[] GetOrCreateKey()
        {
            if (File.Exists(keyPath))
                return File.ReadAllBytes(keyPath);

            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] key = new byte[32]; // AES-256
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
                    // Store IV + cipher
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
