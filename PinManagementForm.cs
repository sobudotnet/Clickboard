using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace Clickboard
{
    public class PinManagementForm : Form
    {
        private readonly string pinPath;
        private TextBox pinBox;
        private Button setBtn, removeBtn;

        public PinManagementForm(string pinPath, Theme theme)
        {
            this.pinPath = pinPath;
            this.Text = "Manage PIN";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Width = 320;
            this.Height = 200;
            this.BackColor = theme?.HeaderBarColor ?? SystemColors.Window;

            var label = new Label
            {
                Text = "Set or change 4-digit PIN:",
                ForeColor = theme?.HeaderBarTextColor ?? Color.Black,
                Dock = DockStyle.Top,
                Height = 32,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            pinBox = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 32,
                MaxLength = 4,
                PasswordChar = '●',
                TextAlign = HorizontalAlignment.Center,
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                BackColor = theme?.InputFieldColor ?? SystemColors.Window,
                ForeColor = theme?.InputFieldTextColor ?? SystemColors.WindowText,
                Margin = new Padding(16, 8, 16, 8)
            };
            pinBox.KeyPress += (s, e) =>
            {
                if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
                    e.Handled = true;
            };

            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 48
            };

            setBtn = new Button
            {
                Text = "Set PIN",
                Width = 120,
                Height = 36,
                Left = 24,
                Top = 6,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = theme?.ButtonColor ?? Color.Black,
                ForeColor = theme?.ButtonTextColor ?? Color.White,
                FlatStyle = FlatStyle.Flat
            };
            setBtn.FlatAppearance.BorderSize = 0;
            setBtn.Click += (s, e) =>
            {
                if (pinBox.Text.Length != 4)
                {
                    MessageBox.Show("PIN must be 4 digits.", "Clickboard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string salt;
                string hash = HashPin(pinBox.Text, out salt);
                File.WriteAllText(pinPath, $"{hash}:{salt}");
                MessageBox.Show("PIN set.", "Clickboard", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            };

            removeBtn = new Button
            {
                Text = "Remove PIN",
                Width = 120,
                Height = 36,
                Left = 160,
                Top = 6,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = theme?.ButtonColor ?? Color.Black,
                ForeColor = theme?.ButtonTextColor ?? Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = File.Exists(pinPath)
            };
            removeBtn.FlatAppearance.BorderSize = 0;
            removeBtn.Click += (s, e) =>
            {
                if (File.Exists(pinPath))
                {
                    File.Delete(pinPath);
                    MessageBox.Show("PIN removed.", "Clickboard", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
            };

            buttonPanel.Controls.Add(setBtn);
            buttonPanel.Controls.Add(removeBtn);

            this.Controls.Add(buttonPanel);
            this.Controls.Add(pinBox);
            this.Controls.Add(label);
        }

        // PBKDF2 hash with salt
        private static string HashPin(string pin, out string salt)
        {
            byte[] saltBytes = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(saltBytes);
            }
            salt = Convert.ToBase64String(saltBytes);

            using (var deriveBytes = new System.Security.Cryptography.Rfc2898DeriveBytes(pin, saltBytes, 10000))
            {
                byte[] hash = deriveBytes.GetBytes(32); // 256-bit hash
                return Convert.ToBase64String(hash);
            }
        }
    }
}
