using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace Clickboard
{
    public class PinManagementForm : Form
    {
        private readonly string pinPath;
        private TextBox pinBox;
        private Button setBtn, removeBtn;

        public PinManagementForm(string pinPath)
        {
            this.pinPath = pinPath;
            this.Text = "Manage PIN";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Width = 300;
            this.Height = 180;
            this.BackColor = ColorTranslator.FromHtml("#2f131e");

            var label = new Label
            {
                Text = "Set or change 4-digit PIN:",
                ForeColor = ColorTranslator.FromHtml("#87f5fb"),
                Left = 20,
                Top = 20,
                Width = 240
            };
            pinBox = new TextBox
            {
                Left = 20,
                Top = 50,
                Width = 240,
                MaxLength = 4,
                PasswordChar = '●'
            };
            pinBox.KeyPress += (s, e) =>
            {
                if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
                    e.Handled = true;
            };

            setBtn = new Button
            {
                Text = "Set PIN",
                Left = 20,
                Top = 90,
                Width = 100,
                ForeColor = Color.White
            };
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
                Left = 140,
                Top = 90,
                Width = 120,
                Enabled = File.Exists(pinPath),
                ForeColor = Color.Black,
                BackColor = ColorTranslator.FromHtml("#de3c4b"),
                FlatStyle = FlatStyle.Flat
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

            this.Controls.Add(label);
            this.Controls.Add(pinBox);
            this.Controls.Add(setBtn);
            this.Controls.Add(removeBtn);
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

            using (var deriveBytes = new Rfc2898DeriveBytes(pin, saltBytes, 10000))
            {
                byte[] hash = deriveBytes.GetBytes(32); // 256-bit hash
                return Convert.ToBase64String(hash);
            }
        }
    }
}
