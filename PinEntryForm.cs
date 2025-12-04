using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace Clickboard
{
    public class PinEntryForm : Form
    {
        private readonly string pinPath;
        private TextBox pinBox;
        public PinEntryForm(string pinPath)
        {
            this.pinPath = pinPath;
            this.Text = "Enter PIN";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Width = 260;
            this.Height = 150;
            this.BackColor = ColorTranslator.FromHtml("#2f131e");

            var label = new Label
            {
                Text = "Enter 4-digit PIN:",
                ForeColor = ColorTranslator.FromHtml("#87f5fb"),
                Left = 20,
                Top = 20,
                Width = 200
            };
            pinBox = new TextBox
            {
                Left = 20,
                Top = 50,
                Width = 200,
                MaxLength = 4,
                PasswordChar = '●'
            };
            pinBox.KeyPress += (s, e) =>
            {
                if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
                    e.Handled = true;
            };

            var okBtn = new Button
            {
                Text = "OK",
                Left = 50,
                Top = 85,
                Width = 60,
                ForeColor = Color.White,
                BackColor = ColorTranslator.FromHtml("#de3c4b"),
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.None // dont touch
            };
            okBtn.FlatAppearance.BorderSize = 0;

            var cancelBtn = new Button
            {
                Text = "Cancel",
                Left = 130,
                Top = 85,
                Width = 60,
                ForeColor = Color.White,
                BackColor = ColorTranslator.FromHtml("#de3c4b"),
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            cancelBtn.FlatAppearance.BorderSize = 0;

            okBtn.Click += (s, e) =>
            {
                if (VerifyPin(pinBox.Text))
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Incorrect PIN.", "Clickboard", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    pinBox.Clear();
                    pinBox.Focus();
                }
            };

            this.Controls.Add(label);
            this.Controls.Add(pinBox);
            this.Controls.Add(okBtn);
            this.Controls.Add(cancelBtn);
            this.AcceptButton = okBtn;
            this.CancelButton = cancelBtn;
        }

        private bool VerifyPin(string input)
        {
            if (!File.Exists(pinPath)) return true;
            var stored = File.ReadAllText(pinPath);
            var parts = stored.Split(':');
            if (parts.Length != 2) return false;
            var storedHash = parts[0];
            var storedSalt = parts[1];

            byte[] saltBytes = Convert.FromBase64String(storedSalt);
            using (var deriveBytes = new Rfc2898DeriveBytes(input, saltBytes, 10000))
            {
                byte[] hash = deriveBytes.GetBytes(32);
                string hashString = Convert.ToBase64String(hash);
                return hashString == storedHash;
            }
        }
    }
}
