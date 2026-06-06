#if WINDOWS
using H.NotifyIcon;
using Microsoft.UI.Xaml;

namespace Briefcase.Maui.Services;

public class WindowsTrayService : IDisposable
{
    private TaskbarIcon? _trayIcon;
    private Microsoft.UI.Xaml.Window? _window;

    public void Initialize(Microsoft.UI.Xaml.Window window)
    {
        _window = window;

        _trayIcon = new TaskbarIcon();
        _trayIcon.ToolTipText = "Briefcase";

        var contextMenu = new Microsoft.UI.Xaml.Controls.MenuFlyout();

        var showItem = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Show" };
        showItem.Click += (_, _) => ShowWindow();
        contextMenu.Items.Add(showItem);

        var newMessageItem = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "New Message" };
        newMessageItem.Click += (_, _) => ShowWindow();
        contextMenu.Items.Add(newMessageItem);

        var quickTransferItem = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Quick Transfer" };
        quickTransferItem.Click += (_, _) => ShowWindow();
        contextMenu.Items.Add(quickTransferItem);

        contextMenu.Items.Add(new Microsoft.UI.Xaml.Controls.MenuFlyoutSeparator());

        var quitItem = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Quit" };
        quitItem.Click += (_, _) =>
        {
            Dispose();
            Microsoft.UI.Xaml.Application.Current.Exit();
        };
        contextMenu.Items.Add(quitItem);

        _trayIcon.ContextFlyout = contextMenu;
        _trayIcon.LeftClickCommand = new RelayCommand(ShowWindow);
    }

    private void ShowWindow()
    {
        if (_window is null) return;
        _window.Activate();
    }

    public void Dispose()
    {
        _trayIcon?.Dispose();
        _trayIcon = null;
    }

    private class RelayCommand : System.Windows.Input.ICommand
    {
        private readonly Action _execute;
        public RelayCommand(Action execute) => _execute = execute;
#pragma warning disable CS0067
        public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
    }
}
#endif
