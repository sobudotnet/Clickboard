using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class MinimalScrollPanel : FlowLayoutPanel
{
    private const int WM_NCPAINT = 0x85;
    private const int WM_PAINT = 0x000F;

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if (m.Msg == WM_NCPAINT || m.Msg == WM_PAINT)
        {
            HideScrollBarTrack();
        }
    }

    private void HideScrollBarTrack()
    {
        var g = this.CreateGraphics();
        var rect = new Rectangle(this.Width - SystemInformation.VerticalScrollBarWidth, 0, SystemInformation.VerticalScrollBarWidth, this.Height);
        using (var b = new SolidBrush(this.BackColor))
        {
            g.FillRectangle(b, rect);
        }
        g.Dispose();
    }
}