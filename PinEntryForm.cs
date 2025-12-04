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
                DialogResult = DialogResult.OK
            };
            var cancelBtn = new Button
            {
                Text = "Cancel",
                Left = 130,
                Top = 85,
                Width = 60,
                DialogResult = DialogResult.Cancel
            };

            okBtn.Click += (s, e) =>
            {
                if (VerifyPin(pinBox.Text))
                    this.DialogResult = DialogResult.OK;
                else
                    MessageBox.Show("Incorrect PIN.", "Clickboard", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            var hash = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(input)));
            return hash == stored;
        }
    }
}
