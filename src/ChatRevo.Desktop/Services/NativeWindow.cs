using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace ChatRevo.Desktop.Services;

internal static class NativeWindow
{
    private const int GwlExStyle = -20;
    private const long Transparent = 0x20, NoActivate = 0x08000000;
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern nint GetWindowLongPtr(nint hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern nint SetWindowLongPtr(nint hwnd, int index, nint value);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(nint hwnd, nint after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] internal static extern bool RegisterHotKey(nint hwnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(nint hwnd, int id);
    [DllImport("user32.dll")] internal static extern nint GetForegroundWindow();
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X,Y; }
    [DllImport("user32.dll")] private static extern nint WindowFromPoint(NativePoint point);
    [DllImport("user32.dll")] private static extern nint GetAncestor(nint hwnd,uint flags);
    internal static bool HitTestPassesThrough(Window window)
    {
        var point=window.PointToScreen(new Point(window.ActualWidth/2,window.ActualHeight/2));
        var hit=WindowFromPoint(new NativePoint{X=(int)point.X,Y=(int)point.Y});
        return GetAncestor(hit,2)!=new WindowInteropHelper(window).Handle;
    }
    internal static void SetLocked(Window window, bool locked)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == 0) return;
        var style = (long)GetWindowLongPtr(hwnd, GwlExStyle);
        style = locked ? style | Transparent | NoActivate : style & ~(Transparent | NoActivate);
        SetWindowLongPtr(hwnd, GwlExStyle, (nint)style);
        SetWindowPos(hwnd, new nint(-1), 0, 0, 0, 0, 0x1 | 0x2 | 0x10 | 0x20);
    }
    internal static bool IsClickThrough(Window window) =>
        ((long)GetWindowLongPtr(new WindowInteropHelper(window).Handle, GwlExStyle) & (Transparent | NoActivate)) == (Transparent | NoActivate);
}
