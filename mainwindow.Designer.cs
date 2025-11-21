namespace Clickboard
{
    partial class mainwindow
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel titleBar;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.FlowLayoutPanel buttonListPanel;
        private System.Windows.Forms.ToolTip toolTip;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.titleBar = new System.Windows.Forms.Panel();
            this.closeButton = new System.Windows.Forms.Button();
            this.buttonListPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
//title
            this.titleBar.BackColor = System.Drawing.Color.LightGray;
            this.titleBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.titleBar.Height = 25;
            this.titleBar.Controls.Add(this.closeButton);
//close button
            this.closeButton.Text = "X";
            this.closeButton.Dock = System.Windows.Forms.DockStyle.Right;
            this.closeButton.Width = 32;
            this.closeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.closeButton.BackColor = System.Drawing.Color.Red;
            this.closeButton.ForeColor = System.Drawing.Color.White;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            this.toolTip.SetToolTip(this.closeButton, "Close Clickboard");
//button list panel
            this.buttonListPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonListPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.buttonListPanel.AutoScroll = true;
//mainwindow
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.buttonListPanel);
            this.Controls.Add(this.titleBar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Text = "Clickboard";
        }

        #endregion
    }
}

