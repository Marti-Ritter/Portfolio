using System;
using System.Windows.Forms;

public class MyTreeView : TreeView
{
    protected override void WndProc(ref Message m)
    {
        // Suppress WM_LBUTTONDBLCLK
        if (m.Msg == 0x203) { m.Result = IntPtr.Zero; }
        else base.WndProc(ref m);
    }
}