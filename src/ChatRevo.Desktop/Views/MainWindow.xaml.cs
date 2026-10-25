using System.ComponentModel;
using System.Windows.Interop;
using ChatRevo.Desktop.Services;

namespace ChatRevo.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly AppController _controller;
    private nint _handle;
    public MainWindow(AppController controller) { _controller = controller; InitializeComponent(); DataContext = controller.ViewModel; }
    private void MinimizeClick(object sender,RoutedEventArgs e) => WindowState=WindowState.Minimized;
    private void MaximizeClick(object sender,RoutedEventArgs e) => WindowState=WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized;
    private void CloseClick(object sender,RoutedEventArgs e) => Close();
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _handle = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(_handle).AddHook(Hook);
        var ok = NativeWindow.RegisterHotKey(_handle, 1, 0x4003, 0x4C);
        ok &= NativeWindow.RegisterHotKey(_handle, 2, 0x4003, 0x4F);
        ok &= NativeWindow.RegisterHotKey(_handle, 3, 0x4003, 0x53);
        if (!ok) _controller.Report("일부 단축키를 다른 프로그램에서 사용 중입니다. 설정 창이나 트레이 메뉴를 이용해주세요.");
    }
    private nint Hook(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg != 0x312) return 0;
        switch ((int)wParam) { case 1: _controller.ToggleLock(); break; case 2: _controller.ToggleVisibility(); break; case 3: _controller.ShowSettings(); break; }
        handled = true; return 0;
    }
    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_controller.Exiting) { e.Cancel = true; Hide(); _controller.Report("트레이 아이콘에서 설정을 다시 열 수 있습니다."); }
        base.OnClosing(e);
    }
    protected override void OnClosed(EventArgs e) { for (var i = 1; i <= 3; i++) NativeWindow.UnregisterHotKey(_handle, i); base.OnClosed(e); }
}
