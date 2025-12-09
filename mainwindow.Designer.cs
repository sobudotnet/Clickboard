namespace Clickboard
{
    partial class mainwindow
    {
        private System.ComponentModel.IContainer components = null;
        // Removed titleBar and closeButton fields, as closeButton will be handled in mainwindow.cs in inputPanel
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
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            // Removed titleBar and closeButton initialization
            // mainwindow
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Text = "Clickboard";
        }

        #endregion
    }
}

